using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Kadaya
{
class Foe { public int Type, F, St, Kind; public bool Boss, Dead, Did, Aggro; public float X, Y, Z, Vx, Vy, Vz, Yaw, Hp, Mh, Sp, S, Tm, Flash, Walk, Mv, A, B, Ph, Cnt, Air, Al = 1, PoisonT, PoisonDmg, PoisonCd, BurnT, BurnDmg, BurnCd, GenjutsuT; }
    class Proj { public string K; public bool Own, Spent; public float X, Y, Z, Vx, Vy, Vz, L, R, Dmg, T0; }
    class Clone { public float X, Z, Yaw, L, Cd, At = -1, T, Mv; public int F; }
class SummonedCompanion { public SummoningDefinition Definition; public SummonUpgradeStats Upgrades; public float X, Y, Z, Yaw, Hp, Mh, Remaining, AttackCd, SpecialCd, HealCd, Animation, Mv, LeapT; }
    class Part { public float X, Y, Z, Vx, Vy, Vz, L, M, Sz, G; public Color C; public bool Dk; }
    class Fx { public string K; public float X, Y, Z, R, L, M, A; public int F; public Color C; public bool E; }
    class Trl { public Vector3 A, B; public float L; public bool Brk; }
    class Pl { public float X, Y, Z, Vx, Vy, Vz, Yaw, Hp, Mh = 100, En, CbT, Atk, AtkT, Inv, Dash, Dcd, Chg, Kcd, CloneCd, Coy; public int F, J, Cb; public bool Hit, Dead, Run, Ground; }

    public partial class Game1 : Game
    {
        const int W = 1280, H = 720; const float G = 600;
        static readonly Color CY = new Color(95, 243, 255), PU = new Color(138, 92, 255), VI = new Color(192, 107, 255), RD = new Color(255, 45, 85), OR = new Color(255, 179, 71), WH = Color.White;

        GraphicsDeviceManager gdm; SpriteBatch sb; RenderTarget2D rt, small; Texture2D px, circ, glow, tri, cres, vig; SpriteFont font;
        static Random rnd = new Random();
        KeyboardState cur, prev;
        int cameraWheel;
        MouseState mouseCur, mousePrev;

        string state = "title", banS = "";
        float t, shake, hitstop, cam, camY, camZ, flash, ult, banT, winT, summonCooldown, levelElapsed, camYaw = -MathHelper.PiOver2, camPitch = .19f, camDistance = 880, renderScale = 1, camMouseIdle; bool ultHit = true, bossDead, cameraCaptured, abilityMenuOpen, summoningMenuOpen;
        int lv, kills; Level L; float checkX, checkZ = 110; bool[] lit;
        bool levelSelectOpen;
        int mainMenuIndex, selectedMenuLevel, selectedMenuMap, queuedMenuLevel;
        float launchDelay;
        string titlePage = "main";
        ProgressionService progression;
        SummoningService summoningService;
        ShopService shopService;
        SummonedCompanion summonedCompanion;
        int selectedShopIndex, selectedAbilityIndex, selectedUpgradeIndex, selectedUpgradeCategory, selectedShopCategory;
        int settingsSection, settingsRow;
        bool capturingSettingBinding;
        string capturingSettingAction = string.Empty;
        GameSettings settings;
        string settingsPath;
        bool shopMenuOpen, byakuganScout, futureVision;
        float scoutX, scoutZ, rotationT, freezeT, susanooT, susanooCd, eyeCooldown, pushCooldown, genjutsuCooldown, sageT, sageCd;
        float shurikenCd, kenjutsuCd, hiddenCd, kunaiCd, hiddenT;
        HashSet<int> disabledSpikeIndices = new HashSet<int>();
        string normalProfilePath, legacySavePath, testProfilePath;
        bool testMode;
        Pl P = new Pl();
        List<Foe> en = new List<Foe>(); List<Proj> proj = new List<Proj>(); List<Clone> clones = new List<Clone>();
        List<Part> pt = new List<Part>(); List<Fx> fx = new List<Fx>(); List<Trl> trail = new List<Trl>();
        float[,] stars = new float[140, 4]; float[,] amb = new float[120, 5];

        public Game1()
        {
            gdm = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = W, PreferredBackBufferHeight = H };
            Content.RootDirectory = "Content"; IsMouseVisible = true; Window.AllowUserResizing = true; Window.Title = "KADAYA";
        }

        static float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static float Cl(float v, float a, float b) => Math.Max(a, Math.Min(b, v));
        static int Sg(float v) => v >= 0 ? 1 : -1;
        static Color Ad(Color c, float k) => new Color(c.R / 255f * k, c.G / 255f * k, c.B / 255f * k, Cl(k, 0, 1));
        static Color Pm(float a) => new Color(a, a, a, a);

        protected override void LoadContent()
        {
            sb = new SpriteBatch(GraphicsDevice); font = Content.Load<SpriteFont>("Font");
            InitializeSettings();
            try { menuMusicSong = Content.Load<Song>("Audio/MainMenu_Music"); } catch { }
            try { scytheAttackSound = Content.Load<SoundEffect>("Audio/Reaper_Scythe_Attack"); } catch { }
            try { transformationSong = Content.Load<Song>("Audio/Reaper_Transformation"); } catch { }
            px = new Texture2D(GraphicsDevice, 1, 1); px.SetData(new[] { Color.White });
            circ = Make(64, (u, v) => { float r = MathF.Sqrt(u * u + v * v); return Pm(Cl((1 - r) * 32, 0, 1)); });
            glow = Make(128, (u, v) => { float r = MathF.Sqrt(u * u + v * v); return Pm(MathF.Pow(Cl(1 - r, 0, 1), 2.2f)); });
            tri = Make(64, (u, v) => Pm((v > -1 && MathF.Abs(u) <= (v + 1) / 2) ? 1 : 0));
            cres = Make(256, (u, v) => { float ao = Cl((1 - MathF.Sqrt(u * u + v * v)) * 128, 0, 1); float ai = Cl((.9f - MathF.Sqrt((u - .42f) * (u - .42f) + v * v)) * 128, 0, 1); return Pm(ao * (1 - ai)); });
            vig = Make(256, (u, v) => { float r = MathF.Sqrt(u * u + v * v); return new Color(0, 0, 0, Cl(MathF.Pow(Cl((r - .42f) / .9f, 0, 1), 1.5f) * .95f, 0, 1)); });
            for (int i = 0; i < 140; i++) { stars[i, 0] = Rf(0, W); stars[i, 1] = Rf(0, 420); stars[i, 2] = Rf(.5f, 2); stars[i, 3] = Rf(0, 6); }
            for (int i = 0; i < 120; i++) { amb[i, 0] = Rf(0, W); amb[i, 1] = Rf(0, H); amb[i, 2] = Rf(-20, 20); amb[i, 3] = Rf(15, 60); amb[i, 4] = Rf(1.5f, 4); }
            Init3D(); InitializeProgression(); LoadLevel(lv, 250); state = "title";
            UpdateMenuMusic();
        }

        static readonly int[] ResolutionWidths = { 1280, 1600, 1920 };
        static readonly int[] ResolutionHeights = { 720, 900, 1080 };
        static readonly float[] QualityScales = { 1f, 1.25f, 1.5f, 2f };

        void InitializeSettings()
        {
            string appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            settingsPath = GameSettingsStore.GetPath(appDataRoot);
            settings = GameSettingsStore.Load(settingsPath);
            ApplyVideoSettings();
            ApplyAudioVolumes();
        }

        void SaveSettings()
        {
            if (settings == null) return;
            try { GameSettingsStore.Save(settings, settingsPath); }
            catch { banS = "SETTINGS SAVE FAILED"; banT = 2f; }
        }

        void ApplyVideoSettings()
        {
            if (settings == null) return;
            settings.Normalize();
            renderScale = QualityScales[settings.GraphicsQuality];
            gdm.PreferredBackBufferWidth = ResolutionWidths[settings.ResolutionIndex];
            gdm.PreferredBackBufferHeight = ResolutionHeights[settings.ResolutionIndex];
            gdm.IsFullScreen = settings.Fullscreen;
            gdm.SynchronizeWithVerticalRetrace = settings.VSync;
            IsFixedTimeStep = settings.VSync;
            try { gdm.ApplyChanges(); }
            catch
            {
                // Some displays cannot enter a requested fullscreen mode or resolution.
                settings.ResolutionIndex = 0; settings.Fullscreen = false; settings.VSync = true; settings.GraphicsQuality = 0;
                renderScale = QualityScales[0];
                gdm.PreferredBackBufferWidth = W; gdm.PreferredBackBufferHeight = H;
                gdm.IsFullScreen = false; gdm.SynchronizeWithVerticalRetrace = true; IsFixedTimeStep = true;
                try { gdm.ApplyChanges(); } catch { }
                SaveSettings();
            }
            CreateRenderTargets();
            RebuildQualityMeshes();
        }

        void ApplyAudioVolumes()
        {
            if (settings == null) return;
            float master = settings.Muted ? 0 : settings.MasterVolume;
            if (musInst != null) musInst.Volume = Cl(master * settings.MusicVolume, 0, 1);
            float mediaVolume = transformationSongActive ? settings.EffectsVolume : settings.MusicVolume;
            MediaPlayer.Volume = Cl(master * mediaVolume, 0, 1);
        }

        void ApplySfxVolumes()
        {
            float master = settings == null || settings.Muted ? 0 : settings.MasterVolume;
            MediaPlayer.Volume = Cl(master * (settings == null ? .8f : settings.EffectsVolume), 0, 1);
        }

        Keys BoundKey(string action, Keys fallback)
        {
            Keys key;
            return settings != null && Enum.TryParse(settings.GetBinding(action), true, out key) ? key : fallback;
        }

        bool DnAction(string action) => cur.IsKeyDown(BoundKey(action, Keys.None));
        bool PrAction(string action) => cur.IsKeyDown(BoundKey(action, Keys.None)) && prev.IsKeyUp(BoundKey(action, Keys.None));
        void CreateRenderTargets()
        {
            if (rt != null) rt.Dispose(); if (small != null) small.Dispose();
            while (true)
            {
                RenderTarget2D next = null;
                try
                {
                    int rw = (int)(W * renderScale), rh = (int)(H * renderScale);
                    next = new RenderTarget2D(GraphicsDevice, rw, rh, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
                    RenderTarget2D nextSmall = new RenderTarget2D(GraphicsDevice, Math.Max(1, rw / 4), Math.Max(1, rh / 4));
                    rt = next; small = nextSmall;
                    return;
                }
                catch
                {
                    if (next != null) next.Dispose();
                    if (settings == null || settings.GraphicsQuality <= 0) throw;
                    settings.GraphicsQuality--;
                    renderScale = QualityScales[settings.GraphicsQuality];
                    banS = "GRAPHICS MEMORY LIMIT - LOWERED TO " + (int)(renderScale * 100) + "%";
                    banT = 3f;
                    SaveSettings();
                }
            }
        }
        Texture2D Make(int n, Func<float, float, Color> f)
        {
            var tx = new Texture2D(GraphicsDevice, n, n); var c = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) c[y * n + x] = f((x + .5f) / n * 2 - 1, (y + .5f) / n * 2 - 1);
            tx.SetData(c); return tx;
        }

        // ---------- progression profiles ----------
        bool IsTestMode
        {
            get
            {
                return testMode;
            }
        }

        string ActiveProfilePath
        {
            get
            {
                if (testMode) return testProfilePath;
                return normalProfilePath;
            }
        }

        void InitializeProgression()
        {
            string appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            normalProfilePath = ProgressionStore.GetNormalProfilePath(appDataRoot);
            legacySavePath = ProgressionStore.GetLegacySavePath(appDataRoot);
            testProfilePath = ProgressionStore.GetTestProfilePath(appDataRoot);
            // Test Mode is always opt-in from Settings and starts in the normal profile.
            testMode = false;
            LoadActiveProgression();
        }

        void LoadActiveProgression()
        {
            string legacyPath = IsTestMode ? null : legacySavePath;
            ProgressionProfile profile = ProgressionStore.Load(ActiveProfilePath, legacyPath);
            progression = new ProgressionService(profile, IsTestMode);
            summoningService = new SummoningService(profile, IsTestMode);
            shopService = new ShopService(profile, IsTestMode);
            lv = progression.Profile.UnlockedLevel;
        }

        void SaveProgression()
        {
            if (progression == null || string.IsNullOrWhiteSpace(ActiveProfilePath)) return;
            if (progression.Profile.IsReadOnlyRecovery)
            {
                banS = "SAVE UNREADABLE - ORIGINAL PRESERVED";
                banT = 4f;
                return;
            }
            try { ProgressionStore.Save(progression.Profile, ActiveProfilePath); }
            catch
            {
                banS = "SAVE FAILED - CHECK DISK SPACE";
                banT = 4f;
            }
        }

        void ResetNormalProgression()
        {
            // Reset only the active profile (save.json or isolated test-save.json).
            progression.ResetNewGame();
            SaveProgression();
        }

        void NotifyAbilityUnlocks(IReadOnlyList<string> unlockedIds)
        {
            if (unlockedIds == null || unlockedIds.Count == 0) return;
            AbilityDefinition ability = AbilityCatalog.Get(unlockedIds[unlockedIds.Count - 1]);
            if (ability == null) return;
            banS = "UNLOCKED  " + ability.Name + "  [" + ability.Binding + "]";
            banT = 2.8f;
        }

        int CurrencyBalance(string currency)
        {
            int balance;
            return progression != null && progression.Profile.CurrencyBalances != null &&
                progression.Profile.CurrencyBalances.TryGetValue(currency, out balance) ? Math.Max(0, balance) : 0;
        }

        void AwardCurrency(bool boss)
        {
            SummoningEconomy.AwardEnemyDefeat(progression.Profile, boss);
        }

        IReadOnlyList<ShopMenuEntry> CurrentShopEntries
        {
            get { return ShopMenuModel.Build((ShopCategory)Math.Max(0, Math.Min(4, selectedShopCategory))); }
        }

        SummoningDefinition SelectedSummon
        {
            get
            {
                IReadOnlyList<ShopMenuEntry> entries = CurrentShopEntries;
                return summoningMenuOpen && selectedShopCategory == (int)ShopCategory.Summons &&
                    selectedShopIndex >= 0 && selectedShopIndex < entries.Count ? entries[selectedShopIndex].Summon : null;
            }
        }

        bool TryFindSummonSpawn(out float spawnX, out float spawnY, out float spawnZ)
        {
            float[,] offsets = { { -150, 0 }, { 150, 0 }, { -110, 110 }, { 110, -110 }, { 0, 180 }, { 0, -180 }, { -250, 0 }, { 250, 0 } };
            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                float x = Cl(P.X + offsets[i, 0], 40, bossDead ? L.GateX + 60 : L.GateX - 90);
                float z = Cl(P.Z + offsets[i, 1], -250, 470);
                float floor = FloorBelow(x, P.Y + 2, z);
                if (floor > 9999 || floor < P.Y - 180 || floor > P.Y + 400 || InGap(x)) continue;
                bool spike = false;
                foreach (var s in L.Spikes) if (x > s.X - 35 && x < s.Y + 35 && z > -250 && z < 450) { spike = true; break; }
                if (spike) continue;
                spawnX = x; spawnY = floor; spawnZ = z;
                return true;
            }
            spawnX = spawnY = spawnZ = 0;
            return false;
        }

        void TrySummon(string id)
        {
            SummoningDefinition definition = SummoningCatalog.Get(id);
            if (definition == null) return;
            if (!summoningService.IsOwned(id)) { banS = "SUMMON LOCKED - UNLOCK IT WITH EARNED DIAMONDS [M]"; banT = 1.8f; return; }
            if (summonedCompanion != null) { banS = "A SUMMON IS ALREADY ACTIVE - PRESS X"; banT = 1.5f; return; }
            bool freeTest = summoningService.IsTestMode;
            if (!freeTest && summonCooldown > 0) { banS = "SUMMON COOLDOWN  " + Math.Ceiling(summonCooldown) + "s"; banT = 1.2f; return; }
            if (!freeTest && P.En < definition.EnergyCost) { banS = "NEED " + definition.EnergyCost + " SOUL ENERGY"; banT = 1.2f; return; }
            float sx, sy, sz;
            if (!TryFindSummonSpawn(out sx, out sy, out sz)) { banS = "NO SAFE SUMMONING SPACE NEARBY"; banT = 1.6f; return; }
            SummonUpgradeStats upgrades = summoningService.GetStats(id);
            summonedCompanion = new SummonedCompanion
            {
                Definition = definition,
                Upgrades = upgrades,
                X = sx, Y = sy, Z = sz,
                Yaw = P.Yaw,
                Hp = definition.MaximumHealth * upgrades.HealthMultiplier,
                Mh = definition.MaximumHealth * upgrades.HealthMultiplier,
                Remaining = definition.DurationSeconds * upgrades.DurationMultiplier,
                AttackCd = .6f,
                SpecialCd = Math.Max(3f, definition.CooldownSeconds * .18f),
                HealCd = .2f
            };
            if (!freeTest)
            {
                P.En -= definition.EnergyCost;
                summonCooldown = definition.CooldownSeconds * upgrades.CooldownMultiplier;
            }
            Color color = RgbColor(definition.AccentRgb);
            Ring(sx, G - sy - 4, 120 * definition.Size, color, .75f, sz);
            Burst(sx, G - sy - 50, 42, color, 320, 5, -140, .8f, sz);
            Smoke(sx - 30, G - sy - 20, -80, sz); Smoke(sx + 30, G - sy - 20, 80, sz);
            Sfx("clone", sx);
            banS = definition.Name.ToUpperInvariant() + " SUMMONED";
            banT = 2f;
        }

        void SummonEquipped()
        {
            string id = progression.Profile.EquippedSummonId;
            if (string.IsNullOrWhiteSpace(id)) { banS = "EQUIP A SUMMON IN THE VAULT [M]"; banT = 1.6f; return; }
            TrySummon(id);
        }

        void DismissSummon(bool message)
        {
            if (summonedCompanion == null)
            {
                if (message) { banS = "NO ACTIVE SUMMON"; banT = 1f; }
                return;
            }
            SummonedCompanion old = summonedCompanion;
            Color color = RgbColor(old.Definition.AccentRgb);
            Ring(old.X, G - old.Y - 4, 105 * old.Definition.Size, color, .55f, old.Z);
            Burst(old.X, G - old.Y - 45, 24, color, 260, 4, -100, .55f, old.Z);
            summonedCompanion = null;
            if (message) { banS = "SUMMON DISMISSED"; banT = 1.2f; }
        }

        void UpdateSummonedCompanion(float dt)
        {
            summonCooldown = Math.Max(0, summonCooldown - dt);
            if (summonedCompanion == null) return;
            SummonedCompanion summon = summonedCompanion;
            SummoningDefinition definition = summon.Definition;
            SummonUpgradeStats upgrades = summon.Upgrades;
            summon.Remaining -= dt;
            summon.AttackCd -= dt;
            summon.SpecialCd -= dt;
            summon.HealCd -= dt;
            summon.Animation += dt;
            if (summon.Remaining <= 0 || summon.Hp <= 0)
            {
                DismissSummon(false);
                banS = summon.Hp <= 0 ? "SUMMON DEFEATED" : "SUMMON DURATION ENDED";
                banT = 1.6f;
                return;
            }
            if (definition.Role == SummonRole.Healer && summon.HealCd <= 0 && DistXZ(summon.X, summon.Z, P.X, P.Z) < 520)
            {
                summon.HealCd = .5f;
                P.Hp = Math.Min(P.Mh, P.Hp + definition.HealingPerSecond * upgrades.HealingMultiplier * .5f);
                if (summon.Animation % 2 < .5f) Burst(P.X, G - 55, 2, RgbColor(definition.AccentRgb), 55, 3, -30, .35f, P.Z);
            }
            if (definition.Role == SummonRole.Scout || definition.Category == SummonCategory.Bird || definition.Id == "sky-king-bird")
                foreach (var foe in en) if (!foe.Dead && DistXZ(foe.X, foe.Z, summon.X, summon.Z) < 1200) foe.Aggro = true;

            Foe target = null;
            float targetDistance = float.MaxValue;
            if (definition.Role != SummonRole.Healer) foreach (var foe in en)
            {
                if (foe.Dead) continue;
                float distance = DistXZ(foe.X, foe.Z, summon.X, summon.Z);
                if (distance < targetDistance) { target = foe; targetDistance = distance; }
            }
            float tx, tz;
            if (target != null && targetDistance < 1500)
            { tx = target.X; tz = target.Z; }
            else
            { tx = P.X - MathF.Cos(P.Yaw) * 115; tz = P.Z - MathF.Sin(P.Yaw) * 115; target = null; targetDistance = DistXZ(tx, tz, summon.X, summon.Z); }

            float speed = definition.MoveSpeed * upgrades.SpeedMultiplier;
            if (targetDistance > (target == null ? 85 : definition.AttackRange * .72f))
            {
                float dx = tx - summon.X, dz = tz - summon.Z;
                float distance = Math.Max(1, MathF.Sqrt(dx * dx + dz * dz));
                float nx = Cl(summon.X + dx / distance * speed * dt, 40, bossDead ? L.GateX + 60 : L.GateX - 90);
                float nz = Cl(summon.Z + dz / distance * speed * dt, -250, 470);
                if (!InGap(nx) && FloorBelow(nx, summon.Y + 2, nz) < 9999) { summon.X = nx; summon.Z = nz; }
                float floor = FloorBelow(summon.X, summon.Y + 2, summon.Z);
                if (floor < 9999) summon.Y = floor;
                summon.Mv = 1;
            }
            else summon.Mv = 0;
            if (summon.LeapT > 0)
            {
                float ground = FloorBelow(summon.X, summon.Y + 2, summon.Z);
                if (ground < 9999) summon.Y = ground - MathF.Sin((summon.LeapT / .45f) * MathHelper.Pi) * 95;
                summon.LeapT = Math.Max(0, summon.LeapT - dt);
            }
            else { float ground = FloorBelow(summon.X, summon.Y + 2, summon.Z); if (ground < 9999) summon.Y = ground; }
            summon.Yaw = MathF.Atan2(tz - summon.Z, tx - summon.X);

            if (target != null && targetDistance <= definition.AttackRange && summon.AttackCd <= 0 && definition.Damage > 0)
            {
                summon.AttackCd = Math.Max(.28f, definition.AttackInterval);
                float damage = definition.Damage * upgrades.DamageMultiplier;
                Color accent = RgbColor(definition.AccentRgb);
                bool interrupt = definition.Role == SummonRole.Ambush || definition.Category == SummonCategory.Dog || definition.Category == SummonCategory.Panther;
                if (definition.Category == SummonCategory.Bird || definition.Category == SummonCategory.Dragon)
                {
                    damage *= 1.25f;
                    Ring(target.X, G - target.Y - 5, 64, accent, .35f, target.Z);
                    Burst(target.X, G - target.Y - 60, 12, accent, 230, 4, -120, .45f, target.Z);
                }
                if (definition.Category == SummonCategory.Dog && definition.Id == "ninja-dog-pack" || definition.Id == "wolf-pack") damage *= 1.65f;
                if (interrupt && !target.Boss && target.St == 1) { target.St = 3; target.Tm = .7f; target.Vx = 0; }
                HitE(target, damage, Sg(target.X - summon.X) * (definition.Role == SummonRole.Tank ? 620 : 350));
                if ((definition.Category == SummonCategory.Snake || definition.Id == "ancient-serpent") && !target.Dead)
                { target.PoisonT = .9f; target.PoisonDmg = 2f * upgrades.DamageMultiplier; target.PoisonCd = .3f; if (!target.Boss) { target.St = 3; target.Tm = .45f; } }
                if (definition.Category == SummonCategory.Toad || definition.Id == "giant-toad-warrior") summon.LeapT = .45f;
            }
            if (target != null && summon.SpecialCd <= 0 && (definition.Role == SummonRole.Legendary || definition.Id == "giant-battle-toad" || definition.Id == "giant-turtle"))
                SummonSpecial(summon, target);
        }

        void SummonSpecial(SummonedCompanion summon, Foe target)
        {
            SummoningDefinition definition = summon.Definition;
            Color accent = RgbColor(definition.AccentRgb);
            bool shadow = definition.Id == "shadow-emperor" || definition.Id == "shadow-beast";
            float radius = definition.Id == "shadow-emperor" ? 360 : definition.SpecialDamage > 150 ? 260 : 190;
            float damage = definition.SpecialDamage * summon.Upgrades.SpecialDamageMultiplier;
            summon.SpecialCd = definition.Id == "shadow-emperor" ? 8f : 11f;
            Ring(summon.X, G - summon.Y - 10, radius, shadow ? new Color(255, 32, 62) : accent, .75f, summon.Z);
            Burst(summon.X, G - summon.Y - 55, shadow ? 70 : 38, shadow ? new Color(105, 5, 26) : accent,
                shadow ? 540 : 390, shadow ? 7 : 5, -100, .75f, summon.Z);
            foreach (var foe in en)
            {
                if (foe.Dead || DistXZ(foe.X, foe.Z, summon.X, summon.Z) > radius) continue;
                HitE(foe, damage, Sg(foe.X - summon.X) * (shadow ? 720 : 520));
                if ((definition.Category == SummonCategory.Snake || definition.Id == "ancient-serpent") && !foe.Dead)
                { foe.PoisonT = 1.2f; foe.PoisonDmg = 3f * summon.Upgrades.DamageMultiplier; foe.PoisonCd = .3f; if (!foe.Boss) { foe.St = 3; foe.Tm = .65f; } }
            }
            shake = Math.Max(shake, shadow ? 32 : 22);
            summon.SpecialCd = definition.Id == "shadow-emperor" ? 8f : 11f;
        }

        static Color RgbColor(int rgb)
        {
            return new Color((byte)((rgb >> 16) & 255), (byte)((rgb >> 8) & 255), (byte)(rgb & 255));
        }

        public void SetTestMode(bool enabled)
        {
            if (testMode == enabled) return;
            string previousState = state;
            SaveProgression();
            testMode = enabled;
            LoadActiveProgression();
            LoadLevel(lv, 250);
            state = previousState == "title" || previousState == "end" ? previousState : "play";
            banS = testMode ? "TEST MODE ENABLED" : "NORMAL MODE";
            banT = 2f;
        }

        bool HasEye(string id)
        {
            return progression != null && shopService != null &&
                progression.Profile.EquippedEyeId == id && shopService.IsOwned("eye." + id);
        }

        bool SpendEnergy(int amount)
        {
            if (P.En < amount) { banS = "NEED " + amount + " SOUL ENERGY"; banT = 1.2f; return false; }
            P.En -= amount; return true;
        }

        Vector3 PlayerWorldPosition() { return new Vector3(P.X, G - P.Y, P.Z); }
        Vector3 FoeWorldPosition(Foe foe) { return new Vector3(foe.X, G - foe.Y, foe.Z); }

        Foe NearestEyeTarget(float range)
        {
            Foe nearest = null; float best = range;
            foreach (Foe foe in en)
            {
                if (foe.Dead) continue;
                float distance = Vector3.Distance(PlayerWorldPosition(), FoeWorldPosition(foe));
                if (distance < best) { best = distance; nearest = foe; }
            }
            return nearest;
        }

        void UseEyeQ()
        {
            string eye = progression.Profile.EquippedEyeId;
            if (!HasEye(eye)) { CastShuriken(); return; }
            if (eye == "rinnegan") { AlmightyPush(); return; }
            if (eye == "kakashi") { CastKakashiChakra(); return; }
            if (eye == "sharingan" || eye == "mangekyo" || eye == "ems") { CastFireball(); return; }
            CastShuriken();
        }

        void UseEyeE()
        {
            string eye = progression.Profile.EquippedEyeId;
            if (!HasEye(eye)) return;
            if (eye == "byakugan")
            {
                if (eyeCooldown > 0 || !SpendEnergy(28)) return;
                eyeCooldown = 13f; rotationT = 6f; Ring(P.X, P.Y - 70, 145, new Color(115, 235, 255), .6f, P.Z);
                Burst(P.X, P.Y - 70, 70, new Color(115, 235, 255), 340, 4, 0, .55f, P.Z); banS = "BYAKUGAN ROTATION"; banT = 1.4f;
            }
            else if (eye == "jougan")
            {
                if (eyeCooldown > 0 || !SpendEnergy(35)) return;
                eyeCooldown = 16f; freezeT = 1.5f; flash = .24f; Ring(P.X, P.Y - 70, 360, new Color(120, 245, 255), .9f, P.Z);
                Burst(P.X, P.Y - 70, 90, new Color(130, 245, 255), 420, 4, 0, .85f, P.Z); banS = "JOUGAN — TIME FROZEN"; banT = 1.5f;
            }
            else if (eye == "mangekyo") CastGenjutsu();
            else if (eye == "ems") ActivateSusanoo();
            else { banS = "NO E POWER FOR THIS EYE"; banT = 1.1f; }
        }

        void UseEyeF()
        {
            string eye = progression.Profile.EquippedEyeId;
            if (!HasEye(eye)) { CastKenjutsu(); return; }
            if (eye == "byakugan")
            {
                byakuganScout = !byakuganScout;
                if (byakuganScout) { scoutX = P.X; scoutZ = P.Z; camYaw = -MathHelper.PiOver2; }
                else { cam = Cl(P.X - W * .5f, 0, Math.Max(0, L.GateX + 400 - W)); camZ = P.Z; camY = 0; }
                banS = byakuganScout ? "BYAKUGAN SCOUT — WASD MOVE · F RETURN" : "SCOUT CAMERA RETURNED"; banT = 1.6f;
            }
            else if (eye == "jougan")
            {
                futureVision = !futureVision; banS = futureVision ? "JOUGAN FUTURE VISION" : "FUTURE VISION OFF"; banT = 1.4f;
            }
            else if (eye == "kakashi")
            {
                string[] types = { "fire", "water", "wind", "earth", "lightning" };
                int index = Array.IndexOf(types, progression.Profile.EquippedChakraId);
                progression.Profile.EquippedChakraId = types[(index + 1 + types.Length) % types.Length];
                SaveProgression(); banS = "CHAKRA: " + progression.Profile.EquippedChakraId.ToUpperInvariant(); banT = 1.2f;
            }
            else CastKenjutsu();
        }

        float UpgradeMultiplier(string id) { return 1f + progression.GetUpgradeLevel(id) * .14f; }

        Vector2 CameraAim()
        {
            float x = -MathF.Sin(camYaw), z = -MathF.Cos(camYaw);
            float length = Math.Max(.001f, MathF.Sqrt(x * x + z * z));
            return new Vector2(x / length, z / length);
        }

        void CastShuriken()
        {
            AbilityDefinition ability = AbilityCatalog.Get("shuriken");
            if (!progression.IsUsable(ability.Id)) { banS = "SHURIKEN LOCKED — TAB FOR REQUIREMENTS"; banT = 1.2f; return; }
            if (shurikenCd > 0) { banS = "SHURIKEN COOLDOWN"; banT = .8f; return; }
            if (!SpendEnergy(ability.ResourceCost)) return;
            shurikenCd = ability.CooldownSeconds * Math.Max(.55f, 1 - progression.GetUpgradeLevel(ability.Id) * .07f);
            Vector2 aim = CameraAim(); float yaw = MathF.Atan2(aim.Y, aim.X);
            for (int i = -1; i <= 1; i++)
            {
                float angle = yaw + i * .13f;
                proj.Add(new Proj { K = "shuriken", Own = true, X = P.X + aim.X * 40, Y = P.Y - 76, Z = P.Z + aim.Y * 40,
                    Vx = MathF.Cos(angle) * 1100, Vy = 0, Vz = MathF.Sin(angle) * 1100, L = 1.15f, R = 17,
                    Dmg = 14 * UpgradeMultiplier(ability.Id) });
            }
            Burst(P.X + aim.X * 40, P.Y - 76, 12, new Color(218, 232, 255), 240, 3, 0, .35f, P.Z + aim.Y * 40);
        }

        void CastKenjutsu()
        {
            AbilityDefinition ability = AbilityCatalog.Get("kenjutsu");
            if (!progression.IsUsable(ability.Id)) { banS = "KENJUTSU LOCKED — TAB FOR REQUIREMENTS"; banT = 1.2f; return; }
            if (kenjutsuCd > 0) { banS = "KENJUTSU COOLDOWN"; banT = .8f; return; }
            if (!SpendEnergy(ability.ResourceCost)) return;
            kenjutsuCd = ability.CooldownSeconds * Math.Max(.55f, 1 - progression.GetUpgradeLevel(ability.Id) * .07f);
            Vector2 aim = CameraAim(); P.Yaw = MathF.Atan2(aim.Y, aim.X); P.X = Cl(P.X + aim.X * 85, 40, bossDead ? L.GateX + 60 : L.GateX - 90);
            Ring(P.X, P.Y - 65, 205, new Color(190, 220, 255), .35f, P.Z); Arc(P.X, P.Y - 75, P.F, 235, WH, .35f, -.15f, P.Z);
            foreach (Foe foe in en)
            {
                Vector3 delta = FoeWorldPosition(foe) - PlayerWorldPosition();
                float forward = delta.X * aim.X + delta.Z * aim.Y;
                if (!foe.Dead && forward > 0 && forward < 310 && Math.Abs(delta.Z * aim.X - delta.X * aim.Y) < 170 && Math.Abs(delta.Y) < 180)
                    HitE(foe, 38 * UpgradeMultiplier(ability.Id), (int)MathF.Sign(aim.X) * 500);
            }
            banS = "KENJUTSU CUT"; banT = 1f;
        }

        void ActivateHiddenJutsu()
        {
            AbilityDefinition ability = AbilityCatalog.Get("hidden");
            if (!progression.IsUsable(ability.Id)) { banS = "HIDDEN JUTSU LOCKED — TAB FOR REQUIREMENTS"; banT = 1.2f; return; }
            if (hiddenCd > 0) { banS = "HIDDEN JUTSU COOLDOWN"; banT = .8f; return; }
            if (!SpendEnergy(20)) return;
            hiddenCd = 9f * Math.Max(.55f, 1 - progression.GetUpgradeLevel(ability.Id) * .07f);
            hiddenT = 2.3f + progression.GetUpgradeLevel(ability.Id) * .22f; P.Inv = Math.Max(P.Inv, hiddenT);
            Burst(P.X, P.Y - 70, 48, new Color(138, 92, 255), 310, 4, 0, .65f, P.Z); banS = "HIDDEN JUTSU — EVADING"; banT = 1f;
        }

        void UseTeleportationKunai()
        {
            AbilityDefinition ability = AbilityCatalog.Get("kunai");
            if (!progression.IsUsable(ability.Id)) { banS = "TELEPORTATION KUNAI LOCKED — TAB FOR REQUIREMENTS"; banT = 1.2f; return; }
            if (kunaiCd > 0) { banS = "KUNAI COOLDOWN"; banT = .8f; return; }
            if (!SpendEnergy(18)) return;
            Foe target = NearestEyeTarget(1050); Vector2 aim = CameraAim();
            float x = target == null ? P.X + aim.X * 470 : target.X - aim.X * 130;
            float z = target == null ? P.Z + aim.Y * 470 : target.Z - aim.Y * 130;
            x = Cl(x, 40, L.GateX - 90); z = Cl(z, -250, 470);
            float floor = FloorBelow(x, P.Y + 8, z);
            if (floor > G + 5) { P.En = Math.Min(100, P.En + 18); banS = "NO SAFE KUNAI LANDING"; banT = 1f; return; }
            P.X = x; P.Z = z; P.Y = floor; P.Vx = P.Vy = P.Vz = 0; P.Inv = Math.Max(P.Inv, .6f);
            kunaiCd = 7f * Math.Max(.55f, 1 - progression.GetUpgradeLevel(ability.Id) * .07f);
            Burst(x, floor - 70, 34, new Color(130, 220, 255), 400, 4, 0, .55f, z);
            if (target != null && DistXZ(target.X, target.Z, x, z) < 230) HitE(target, 30 * UpgradeMultiplier(ability.Id), P.F * 400);
            banS = "TELEPORTATION KUNAI"; banT = 1f;
        }

        void AlmightyPush()
        {
            if (pushCooldown > 0) { banS = "ALMIGHTY PUSH COOLDOWN"; banT = 1f; return; }
            if (!SpendEnergy(40)) return;
            const float radius = 1000f; pushCooldown = 10f; Vector3 origin = PlayerWorldPosition();
            Ring(P.X, P.Y - 70, radius, new Color(196, 128, 255), .9f, P.Z);
            Ring(P.X, P.Y - 70, radius * .66f, Color.White, .55f, P.Z);
            Burst(P.X, P.Y - 70, 150, new Color(180, 115, 255), 850, 5, 0, .9f, P.Z); shake = 28;
            foreach (Foe foe in en)
            {
                if (foe.Dead || Vector3.Distance(origin, FoeWorldPosition(foe)) > radius) continue;
                if (foe.Boss) { HitE(foe, 90, Sg(foe.X - P.X) * 900); foe.Vz += Sg(foe.Z - P.Z) * 650; }
                else HitE(foe, 100000, Sg(foe.X - P.X) * 1050);
            }
            for (int i = 0; i < L.Spikes.Count; i++)
            {
                Vector2 spike = L.Spikes[i];
                if (Vector3.Distance(origin, new Vector3((spike.X + spike.Y) * .5f, G, P.Z)) <= radius)
                    disabledSpikeIndices.Add(i);
            }
            foreach (Proj projectile in proj)
                if (!projectile.Own && Vector3.Distance(origin, new Vector3(projectile.X, G - projectile.Y, projectile.Z)) <= radius)
                    projectile.L = 0;
            banS = "RINNEGAN — ALMIGHTY PUSH"; banT = 1.8f;
        }

        void CastFireball()
        {
            if (eyeCooldown > 0) { banS = "EYE POWER COOLDOWN"; banT = .9f; return; }
            if (!SpendEnergy(30)) return;
            eyeCooldown = 5.5f;
            Vector2 aim = CameraAim(); Vector3 direction = new Vector3(aim.X, 0, aim.Y);
            proj.Add(new Proj { K = "fire", Own = true, X = P.X + direction.X * 48, Y = P.Y - 82, Z = P.Z + direction.Z * 48,
                Vx = direction.X * 870, Vy = 0, Vz = direction.Z * 870, L = 1.45f, R = 48, Dmg = 52 });
            Burst(P.X + direction.X * 54, P.Y - 82, 36, new Color(255, 105, 32), 360, 5, 0, .55f, P.Z + direction.Z * 54);
            banS = "FIREBALL JUTSU"; banT = 1.1f;
        }

        void CastGenjutsu()
        {
            if (genjutsuCooldown > 0) { banS = "GENJUTSU COOLDOWN"; banT = .9f; return; }
            if (!SpendEnergy(38)) return;
            Foe target = NearestEyeTarget(1250f);
            if (target == null) { P.En = Math.Min(100, P.En + 38); banS = "NO ENEMY IN GENJUTSU RANGE"; banT = 1.1f; return; }
            genjutsuCooldown = 14f; target.GenjutsuT = target.Boss ? 1.4f : 4.5f; target.St = 3; target.Tm = target.GenjutsuT;
            Ring(target.X, target.Y - 70, 95, new Color(195, 88, 255), .7f, target.Z);
            Burst(target.X, target.Y - 70, 25, new Color(195, 88, 255), 220, 4, 0, .6f, target.Z);
            banS = target.Boss ? "BOSS RESISTS GENJUTSU" : "GENJUTSU — TARGET DISABLED"; banT = 1.5f;
        }

        void ActivateSusanoo()
        {
            if (susanooCd > 0) { banS = "SUSANOO COOLDOWN"; banT = 1f; return; }
            if (!SpendEnergy(42)) return;
            susanooT = shopService.GetSusanooDuration(); susanooCd = susanooT + 12f;
            Ring(P.X, P.Y - 80, 180, new Color(183, 125, 255), .8f, P.Z);
            Burst(P.X, P.Y - 80, 80, new Color(168, 102, 255), 440, 5, -40, .8f, P.Z);
            PlayTransformation();
            banS = "SUSANOO AWAKENED"; banT = 1.8f;
        }

        void CastKakashiChakra()
        {
            if (eyeCooldown > 0) { banS = "CHAKRA JUTSU COOLDOWN"; banT = .9f; return; }
            string type = progression.Profile.EquippedChakraId ?? "fire";
            if (type == "fire") { CastFireball(); return; }
            if (!SpendEnergy(28)) return;
            eyeCooldown = 4.5f;
            if (type == "wind")
            {
                Vector2 aim = CameraAim(); Ring(P.X + aim.X * 180, P.Y - 60, 360, new Color(190, 240, 255), .55f, P.Z + aim.Y * 180);
                foreach (Foe foe in en)
                {
                    float dx = foe.X - P.X, dz = foe.Z - P.Z, forward = dx * aim.X + dz * aim.Y;
                    if (!foe.Dead && forward > 0 && forward < 410 && Math.Abs(dx * aim.Y - dz * aim.X) < 250) HitE(foe, 42, (int)MathF.Sign(aim.X) * 700);
                }
            }
            else if (type == "earth")
            {
                Vector2 aim = CameraAim();
                for (int i = -1; i <= 1; i++)
                {
                    float offset = 200 + i * 120, x = P.X + aim.X * offset, z = P.Z + aim.Y * offset;
                    Ring(x, G - 10, 70, new Color(176, 130, 92), .5f, z);
                    foreach (Foe foe in en) if (!foe.Dead && DistXZ(foe.X, foe.Z, x, z) < 145) HitE(foe, 36, (int)MathF.Sign(aim.X) * 300);
                }
            }
            else
            {
                Vector2 aim = CameraAim();
                string projectileType = type == "water" ? "water" : "lightning";
                float damage = type == "water" ? 38 : 68;
                float speed = type == "water" ? 760 : 1150;
                proj.Add(new Proj { K = projectileType, Own = true, X = P.X + aim.X * 50, Y = P.Y - 78, Z = P.Z + aim.Y * 50,
                    Vx = aim.X * speed, Vy = 0, Vz = aim.Y * speed, L = 1.25f, R = 38, Dmg = damage });
                Burst(P.X + aim.X * 54, P.Y - 78, 28, type == "water" ? new Color(90, 185, 255) : new Color(195, 230, 255), 340, 4, 0, .5f, P.Z + aim.Y * 54);
            }
            banS = type.ToUpperInvariant() + " RELEASE"; banT = 1.1f;
        }

        void HandleShopInput()
        {
            bool mousePressed = mouseCur.LeftButton == ButtonState.Pressed && mousePrev.LeftButton == ButtonState.Released;
            Point mouse = MenuVirtualMouse();
            if (PrAction("MenuLeft")) { selectedShopCategory = (selectedShopCategory + 4) % 5; selectedShopIndex = 0; }
            if (PrAction("MenuRight")) { selectedShopCategory = (selectedShopCategory + 1) % 5; selectedShopIndex = 0; }
            for (int i = 0; i < 5; i++)
            {
                float x = 72 + i * 226;
                if (mouse.X >= x && mouse.X < x + 210 && mouse.Y >= 102 && mouse.Y <= 140)
                {
                    if (selectedShopCategory != i) { selectedShopCategory = i; selectedShopIndex = 0; }
                    if (mousePressed) return;
                }
            }
            IReadOnlyList<ShopMenuEntry> entries = CurrentShopEntries;
            if (entries.Count == 0) return;
            selectedShopIndex = (selectedShopIndex + entries.Count) % entries.Count;
            if (PrAction("MenuUp")) selectedShopIndex = (selectedShopIndex + entries.Count - 1) % entries.Count;
            if (PrAction("MenuDown")) selectedShopIndex = (selectedShopIndex + 1) % entries.Count;
            int first = Math.Max(0, Math.Min(entries.Count - 9, selectedShopIndex - 4));
            for (int row = 0; row < 9; row++)
            {
                int index = first + row;
                if (index >= entries.Count) break;
                float y = 193 + row * 44;
                if (mouse.X >= 72 && mouse.X <= 500 && mouse.Y >= y && mouse.Y <= y + 42)
                { selectedShopIndex = index; if (mousePressed) break; }
            }
            ShopMenuEntry entry = entries[selectedShopIndex];
            string reason = string.Empty;
            bool clickBuy = mousePressed && mouse.X >= 672 && mouse.X <= 842 && mouse.Y >= 590 && mouse.Y <= 634;
            bool clickEquip = mousePressed && mouse.X >= 850 && mouse.X <= 1020 && mouse.Y >= 590 && mouse.Y <= 634;
            bool clickAction = mousePressed && mouse.X >= 1028 && mouse.X <= 1204 && mouse.Y >= 590 && mouse.Y <= 634;
            bool buy = PrAction("Buy") || clickBuy;
            bool equip = PrAction("Confirm") || clickEquip;
            if (entry.Kind == ShopMenuEntryKind.Summon)
            {
                if (buy) { if (summoningService.TryPurchase(entry.Id, out reason)) SaveProgression(); ShowShopMessage(reason); }
                if (equip)
                {
                    bool equipped = progression.Profile.EquippedSummonId == entry.Id;
                    bool ok = equipped ? summoningService.TryUnequip(entry.Id, out reason) : summoningService.TryEquip(entry.Id, out reason);
                    if (ok) SaveProgression(); ShowShopMessage(reason);
                }
                if (PrAction("SummonCall") || clickAction) TrySummon(entry.Id);
            }
            else if (entry.Kind == ShopMenuEntryKind.ShopItem)
            {
                if (buy) { if (shopService.TryPurchase(entry.Id, out reason)) SaveProgression(); ShowShopMessage(reason); }
                if (equip)
                {
                    bool equipped = shopService.IsEquipped(entry.Id);
                    bool ok = equipped ? shopService.TryUnequip(entry.Id, out reason) : shopService.TryEquip(entry.Id, out reason);
                    if (ok) SaveProgression(); ShowShopMessage(reason);
                }
            }
            else if (entry.Kind == ShopMenuEntryKind.StarterWeapon && equip)
            {
                progression.Profile.EquippedWeaponId = string.Empty; progression.Profile.EquippedItemId = string.Empty;
                SaveProgression(); ShowShopMessage("REAPER SCYTHE EQUIPPED");
            }
            else if (entry.Kind == ShopMenuEntryKind.Ability && (buy || equip))
            {
                AbilityMenuRow row = FindAbilityRow(entry.Id);
                ShowShopMessage(row == null || row.IsUnlocked ? "ABILITY IS READY ON ITS LISTED CONTROL" : row.RequirementText.ToUpperInvariant());
            }
            if (PrAction("Back") || PrAction("Shop") || PrAction("Abilities") || PrAction("Summons") || clickAction && entry.Kind != ShopMenuEntryKind.Summon)
            {
                if (state == "title") titlePage = "main";
                else shopMenuOpen = false;
            }
        }

        void HandleAbilityMenuInput()
        {
            if (PrAction("MenuUp")) selectedAbilityIndex = (selectedAbilityIndex + AbilityCatalog.All.Count - 1) % AbilityCatalog.All.Count;
            if (PrAction("MenuDown")) selectedAbilityIndex = (selectedAbilityIndex + 1) % AbilityCatalog.All.Count;
            if (PrAction("Upgrade"))
            {
                string reason;
                AbilityDefinition ability = AbilityCatalog.All[selectedAbilityIndex];
                progression.TryUpgrade(ability.Id, out reason);
                banS = reason.ToUpperInvariant(); banT = 1.3f; SaveProgression();
            }
        }

        AbilityMenuRow FindAbilityRow(string id)
        {
            foreach (AbilityMenuRow row in AbilityMenuModel.Build(progression)) if (row.Definition.Id == id) return row;
            return null;
        }

        void ShowShopMessage(string reason)
        {
            banS = (reason ?? "UNAVAILABLE").ToUpperInvariant(); banT = 1.5f;
        }

        static readonly string[] RebindableActions =
        {
            "MoveForward", "MoveBack", "MoveLeft", "MoveRight", "Jump", "Dodge", "EyePrimary", "PowerSecondary",
            "ModeOrKenjutsu", "HiddenJutsu", "Kunai", "SageMode", "Shop", "Abilities", "Summons", "SummonCall",
            "SummonDismiss", "MenuUp", "MenuDown", "MenuLeft", "MenuRight", "Confirm", "Buy", "Upgrade"
        };

        static readonly string[] RebindableLabels =
        {
            "Move Forward", "Move Back", "Move Left", "Move Right", "Jump", "Dodge", "Eye Primary", "Power / Clone",
            "Mode / Kenjutsu", "Hidden Jutsu", "Teleport Kunai", "Sage Mode", "Open Shop", "Ability Archive", "Summon Shop",
            "Call Summon", "Dismiss Summon", "Menu Up", "Menu Down", "Menu Left", "Menu Right", "Confirm", "Buy / Unlock", "Upgrade"
        };

        int UpgradeEntryCount
        {
            get { return selectedUpgradeCategory == 0 ? AbilityCatalog.All.Count : selectedUpgradeCategory == 1 ? SummoningCatalog.All.Count : 1; }
        }

        void HandleUpgradeInput()
        {
            bool mousePressed = mouseCur.LeftButton == ButtonState.Pressed && mousePrev.LeftButton == ButtonState.Released;
            Point mouse = MenuVirtualMouse();
            if (PrAction("MenuLeft")) { selectedUpgradeCategory = (selectedUpgradeCategory + 2) % 3; selectedUpgradeIndex = 0; }
            if (PrAction("MenuRight")) { selectedUpgradeCategory = (selectedUpgradeCategory + 1) % 3; selectedUpgradeIndex = 0; }
            for (int i = 0; i < 3; i++)
            {
                float x = 86 + i * 365;
                if (mouse.X >= x && mouse.X < x + 340 && mouse.Y >= 114 && mouse.Y <= 158)
                { if (selectedUpgradeCategory != i) { selectedUpgradeCategory = i; selectedUpgradeIndex = 0; } }
            }
            selectedUpgradeIndex = (selectedUpgradeIndex + UpgradeEntryCount) % UpgradeEntryCount;
            if (PrAction("MenuUp")) selectedUpgradeIndex = (selectedUpgradeIndex + UpgradeEntryCount - 1) % UpgradeEntryCount;
            if (PrAction("MenuDown")) selectedUpgradeIndex = (selectedUpgradeIndex + 1) % UpgradeEntryCount;
            int first = Math.Max(0, Math.Min(UpgradeEntryCount - 8, selectedUpgradeIndex - 4));
            bool activate = PrAction("Upgrade") || PrAction("Confirm") || mousePressed && mouse.X >= 872 && mouse.X <= 1209 && mouse.Y >= 580 && mouse.Y <= 644;
            for (int row = 0; row < 8; row++)
            {
                int index = first + row;
                if (index >= UpgradeEntryCount) break;
                float y = 199 + row * 45;
                if (mouse.X >= 72 && mouse.X <= 550 && mouse.Y >= y && mouse.Y <= y + 42)
                    selectedUpgradeIndex = index;
            }
            if (activate) UpgradeSelected(out string reason);
        }

        void UpgradeSelected(out string reason)
        {
            bool success;
            if (selectedUpgradeCategory == 0)
                success = progression.TryUpgrade(AbilityCatalog.All[selectedUpgradeIndex].Id, out reason);
            else if (selectedUpgradeCategory == 1)
                success = summoningService.TryUpgrade(SummoningCatalog.All[selectedUpgradeIndex].Id, out reason);
            else
                success = shopService.TryUpgradeSusanoo(out reason);
            if (success) SaveProgression();
            ShowShopMessage(reason);
        }

        int SettingsRowCount
        {
            get
            {
                switch (settingsSection)
                {
                    case 0: return 4;
                    case 1: return 7;
                    case 2: return RebindableActions.Length + 1;
                    case 3: return 4;
                    case 4: return 2;
                    default: return 1;
                }
            }
        }

        void HandleSettingsInput()
        {
            bool mousePressed = mouseCur.LeftButton == ButtonState.Pressed && mousePrev.LeftButton == ButtonState.Released;
            Point mouse = MenuVirtualMouse();
            if (capturingSettingBinding)
            {
                foreach (Keys key in cur.GetPressedKeys())
                {
                    if (prev.IsKeyDown(key)) continue;
                    if (key == Keys.Escape) { capturingSettingBinding = false; return; }
                    bool changed = settings.TrySetBinding(capturingSettingAction, key.ToString());
                    capturingSettingBinding = false;
                    banS = changed ? "KEY BINDING SAVED" : "KEY IS RESERVED OR ALREADY IN USE"; banT = 1.5f;
                    if (changed) SaveSettings();
                    return;
                }
                return;
            }
            if (Pr(Keys.PageUp)) { settingsSection = (settingsSection + 4) % 5; settingsRow = 0; }
            if (Pr(Keys.PageDown)) { settingsSection = (settingsSection + 1) % 5; settingsRow = 0; }
            for (int i = 0; i < 5; i++)
            {
                float x = 74 + i * 226;
                if (mouse.X >= x && mouse.X <= x + 208 && mouse.Y >= 112 && mouse.Y <= 158)
                { if (settingsSection != i) { settingsSection = i; settingsRow = 0; } }
            }
            settingsRow = Math.Max(0, Math.Min(SettingsRowCount - 1, settingsRow));
            if (PrAction("MenuUp")) settingsRow = (settingsRow + SettingsRowCount - 1) % SettingsRowCount;
            if (PrAction("MenuDown")) settingsRow = (settingsRow + 1) % SettingsRowCount;
            int visible = 8;
            int first = Math.Max(0, Math.Min(SettingsRowCount - visible, settingsRow - visible / 2));
            for (int row = 0; row < visible; row++)
            {
                int index = first + row;
                if (index >= SettingsRowCount) break;
                float y = 200 + row * 48;
                if (mouse.X >= 78 && mouse.X <= 1198 && mouse.Y >= y && mouse.Y <= y + 42)
                {
                    settingsRow = index;
                    if (mousePressed)
                    {
                        if (settingsSection == 2 && index < RebindableActions.Length) StartBindingCapture(index);
                        else if (settingsSection == 2) { settings.RestoreDefaults(); SaveSettings(); }
                        else if (settingsSection == 0 && index < 3 && mouse.X >= 680 && mouse.X <= 955)
                        {
                            float value = Cl((mouse.X - 680) / 275f, 0, 1);
                            if (index == 0) settings.MasterVolume = value;
                            else if (index == 1) settings.MusicVolume = value;
                            else settings.EffectsVolume = value;
                            ApplyAudioVolumes(); SaveSettings();
                        }
                        else if (settingsSection == 0 && index == 3 || settingsSection != 0 && mouse.X >= 625) AdjustSettings(1);
                        return;
                    }
                }
            }
            if (settingsSection == 2 && mousePressed && mouse.X >= 1004 && mouse.X <= 1196 && mouse.Y >= 626 && mouse.Y <= 670)
            { settings.RestoreDefaults(); SaveSettings(); banS = "DEFAULT KEY BINDS RESTORED"; banT = 1.6f; return; }
            if (settingsSection != 2 && mousePressed && mouse.X >= 1004 && mouse.X <= 1196 && mouse.Y >= 626 && mouse.Y <= 670)
            { titlePage = "main"; return; }
            if (settingsSection == 2 && settingsRow == RebindableActions.Length && PrAction("Confirm"))
            { settings.RestoreDefaults(); SaveSettings(); banS = "DEFAULT KEY BINDS RESTORED"; banT = 1.6f; return; }
            if (settingsSection == 2 && settingsRow < RebindableActions.Length && PrAction("Confirm")) { StartBindingCapture(settingsRow); return; }
            if (PrAction("MenuLeft")) AdjustSettings(-1);
            if (PrAction("MenuRight")) AdjustSettings(1);
            if (PrAction("Confirm")) AdjustSettings(1);
        }

        void StartBindingCapture(int index)
        {
            if (index < 0 || index >= RebindableActions.Length) return;
            capturingSettingAction = RebindableActions[index];
            capturingSettingBinding = true;
        }

        void AdjustSettings(int direction)
        {
            bool applyVideo = false;
            bool audioChanged = false;
            if (settingsSection == 0)
            {
                if (settingsRow == 0) settings.MasterVolume = Cl(settings.MasterVolume + direction * .05f, 0, 1);
                else if (settingsRow == 1) settings.MusicVolume = Cl(settings.MusicVolume + direction * .05f, 0, 1);
                else if (settingsRow == 2) settings.EffectsVolume = Cl(settings.EffectsVolume + direction * .05f, 0, 1);
                else settings.Muted = !settings.Muted;
                audioChanged = true;
            }
            else if (settingsSection == 1)
            {
                if (settingsRow == 0) settings.ShowHud = !settings.ShowHud;
                else if (settingsRow == 1) settings.ShowVitals = !settings.ShowVitals;
                else if (settingsRow == 2) settings.ShowWallet = !settings.ShowWallet;
                else if (settingsRow == 3) settings.ShowHints = !settings.ShowHints;
                else if (settingsRow == 4) settings.HudScale = Cl(settings.HudScale + direction * .05f, .65f, 1.4f);
                else if (settingsRow == 5) settings.HudOffsetX = Math.Max(-180, Math.Min(180, settings.HudOffsetX + direction * 10));
                else settings.HudOffsetY = Math.Max(-120, Math.Min(120, settings.HudOffsetY + direction * 10));
            }
            else if (settingsSection == 3)
            {
                if (settingsRow == 0) settings.ResolutionIndex = (settings.ResolutionIndex + direction + ResolutionWidths.Length) % ResolutionWidths.Length;
                else if (settingsRow == 1) settings.Fullscreen = !settings.Fullscreen;
                else if (settingsRow == 2) settings.VSync = !settings.VSync;
                else settings.GraphicsQuality = (settings.GraphicsQuality + direction + QualityScales.Length) % QualityScales.Length;
                applyVideo = true;
            }
            else if (settingsSection == 4)
            {
                if (settingsRow == 0) settings.CameraSensitivity = Cl(settings.CameraSensitivity + direction * .1f, .25f, 2.5f);
                else { SetTestMode(!testMode); return; }
            }
            if (audioChanged) ApplyAudioVolumes();
            if (applyVideo) ApplyVideoSettings();
            SaveSettings();
        }

        void LoadLevel(int n, float sx, float sz = 110)
        {
            lv = n; L = LevelGen.Make(n); L.Foes.RemoveAll(f => f.X < sx + 150 && !f.Boss); en = L.Foes; abilityMenuOpen = false; summoningMenuOpen = false;
            shopMenuOpen = false; levelSelectOpen = false; disabledSpikeIndices.Clear(); byakuganScout = false; futureVision = false; freezeT = rotationT = susanooT = 0;
            foreach (var e in en) e.Z = 110;
            proj.Clear(); clones.Clear(); pt.Clear(); fx.Clear(); trail.Clear();
            summonedCompanion = null; summonCooldown = 0;
            P = new Pl { X = sx, Y = G, Z = sz, F = 1, Hp = 100, En = 50 }; checkX = sx; checkZ = sz; lit = new bool[L.Checks.Count]; for (int i = 0; i < lit.Length; i++) lit[i] = L.Checks[i] <= sx;
            bossDead = false; ult = 0; ultHit = true; levelElapsed = 0; cam = Cl(sx - W * .5f, 0, L.GateX + 400 - W); camY = 0; camZ = sz; banS = "LEVEL " + (n + 1).ToString("00") + "  -  " + L.Mission.Name; banT = 3.2f;
            PlaySong(L.Th.Style);
        }

        void PreviewMenuLevel(int n)
        {
            selectedMenuLevel = Math.Max(0, Math.Min(MissionCatalog.All.Count - 1, n));
            L = LevelGen.Make(selectedMenuLevel); en = L.Foes;
            foreach (Foe foe in en) foe.Z = 110;
            P = new Pl { X = 250, Y = G, Z = 110, F = 1, Hp = 100, En = 50 };
            lit = new bool[L.Checks.Count];
            for (int i = 0; i < lit.Length; i++) lit[i] = L.Checks[i] <= P.X;
            disabledSpikeIndices.Clear(); bossDead = false;
            cam = Cl(P.X - W * .5f, 0, Math.Max(0, L.GateX + 400 - W)); camY = 0; camZ = P.Z;
            camYaw = -MathHelper.PiOver2; camPitch = .19f; camDistance = 880;
        }

        void OpenMissionSelect()
        {
            levelSelectOpen = true;
            titlePage = "maps";
            selectedMenuLevel = Math.Max(0, Math.Min(progression.Profile.UnlockedLevel, MissionCatalog.All.Count - 1));
            selectedMenuMap = selectedMenuLevel / MissionCatalog.MissionsPerMap;
            PreviewMenuLevel(selectedMenuLevel);
        }

        bool HasContinueSave
        {
            get { return progression != null && !progression.Profile.IsReadOnlyRecovery && !string.IsNullOrWhiteSpace(ActiveProfilePath) && File.Exists(ActiveProfilePath); }
        }

        int MainMenuOptionCount { get { return HasContinueSave ? 6 : 5; } }

        void QueueMenuLevel(int level)
        {
            if (level < 0 || level >= MissionCatalog.All.Count)
            {
                banS = "MISSION SELECTION IS INVALID"; banT = 1.8f;
                return;
            }
            queuedMenuLevel = level; launchDelay = .35f; levelSelectOpen = false; titlePage = "main"; state = "launch";
        }

        void ActivateMainMenuItem(int item)
        {
            int action = item;
            if (HasContinueSave)
            {
                if (action == 0) { QueueMenuLevel(Math.Max(0, Math.Min(progression.Profile.UnlockedLevel, MissionCatalog.All.Count - 1))); return; }
                action--;
            }
            switch (action)
            {
                case 0: OpenMissionSelect(); break;
                case 1: titlePage = "shop"; selectedShopCategory = 0; selectedShopIndex = 0; break;
                case 2: titlePage = "upgrade"; selectedUpgradeIndex = 0; break;
                case 3: titlePage = "settings"; settingsSection = 0; settingsRow = 0; capturingSettingBinding = false; break;
                case 4: Exit(); break;
                default: OpenMissionSelect(); break;
            }
        }

        void BeginNewCampaign()
        {
            ResetNormalProgression(); lv = 0; LoadLevel(0, 250); state = "title"; titlePage = "main"; levelSelectOpen = false;
            mainMenuIndex = HasContinueSave ? 1 : 0; banS = "NEW CAMPAIGN READY"; banT = 1.6f;
        }

        void HandleTitleMenuInput()
        {
            bool mousePressed = mouseCur.LeftButton == ButtonState.Pressed && mousePrev.LeftButton == ButtonState.Released;
            Point mouse = MenuVirtualMouse();
            float mouseX = mouse.X, mouseY = mouse.Y;
            if (titlePage == "maps")
            {
                if (PrAction("MenuLeft")) selectedMenuMap = (selectedMenuMap + MissionCatalog.Maps.Count - 1) % MissionCatalog.Maps.Count;
                if (PrAction("MenuRight")) selectedMenuMap = (selectedMenuMap + 1) % MissionCatalog.Maps.Count;
                for (int i = 0; i < MissionCatalog.Maps.Count; i++)
                {
                    float x = 70 + i * 388;
                    if (mouseX >= x && mouseX <= x + 370 && mouseY >= 190 && mouseY <= 545)
                        selectedMenuMap = i;
                }
                if (L == null || L.Mission == null || L.Mission.MapIndex != selectedMenuMap)
                    PreviewMenuLevel(selectedMenuMap * MissionCatalog.MissionsPerMap);
                if (PrAction("Confirm") || mousePressed && mouseX >= 885 && mouseY >= 572 && mouseX <= 1210 && mouseY <= 650)
                {
                    titlePage = "levels"; levelSelectOpen = true;
                    selectedMenuLevel = selectedMenuMap * MissionCatalog.MissionsPerMap;
                    PreviewMenuLevel(selectedMenuLevel);
                    return;
                }
                if (mousePressed && mouseX >= 64 && mouseY >= 572 && mouseX <= 280 && mouseY <= 650)
                { levelSelectOpen = false; titlePage = "main"; PreviewMenuLevel(lv); }
                if (mousePressed && mouseX >= 435 && mouseY >= 572 && mouseX <= 770 && mouseY <= 650) BeginNewCampaign();
                if (Pr(Keys.N)) BeginNewCampaign();
                return;
            }

            if (titlePage == "levels")
            {
                int local = selectedMenuLevel % MissionCatalog.MissionsPerMap;
                int row = local / 5, col = local % 5;
                if (PrAction("MenuLeft")) col = (col + 4) % 5;
                if (PrAction("MenuRight")) col = (col + 1) % 5;
                if (PrAction("MenuUp")) row = (row + 2) % 3;
                if (PrAction("MenuDown")) row = (row + 1) % 3;
                selectedMenuLevel = selectedMenuMap * MissionCatalog.MissionsPerMap + row * 5 + col;
                for (int i = 0; i < MissionCatalog.MissionsPerMap; i++)
                {
                    int cardColumn = i % 5, cardRow = i / 5;
                    float x = 55 + cardColumn * 174, y = 158 + cardRow * 128;
                    if (mouseX >= x && mouseX <= x + 164 && mouseY >= y && mouseY <= y + 112)
                        selectedMenuLevel = selectedMenuMap * MissionCatalog.MissionsPerMap + i;
                }
                if (selectedMenuLevel < 0 || selectedMenuLevel >= MissionCatalog.All.Count || selectedMenuLevel / MissionCatalog.MissionsPerMap != selectedMenuMap)
                    selectedMenuLevel = selectedMenuMap * MissionCatalog.MissionsPerMap;
                if (L == null || L.Mission == null || L.Mission.Index != selectedMenuLevel) PreviewMenuLevel(selectedMenuLevel);
                if (PrAction("Confirm") || mousePressed && mouseX >= 885 && mouseY >= 572 && mouseX <= 1210 && mouseY <= 650)
                    QueueMenuLevel(selectedMenuLevel);
                if (mousePressed && mouseX >= 64 && mouseY >= 572 && mouseX <= 280 && mouseY <= 650)
                {
                    titlePage = "maps";
                    selectedMenuMap = selectedMenuLevel / MissionCatalog.MissionsPerMap;
                    return;
                }
                if (mousePressed && mouseX >= 435 && mouseY >= 572 && mouseX <= 770 && mouseY <= 650 || Pr(Keys.N)) BeginNewCampaign();
                return;
            }

            if (titlePage == "shop") { HandleShopInput(); return; }
            if (titlePage == "upgrade") { HandleUpgradeInput(); return; }
            if (titlePage == "settings") { HandleSettingsInput(); return; }

            mainMenuIndex = Math.Max(0, Math.Min(mainMenuIndex, MainMenuOptionCount - 1));
            if (PrAction("MenuUp")) mainMenuIndex = (mainMenuIndex + MainMenuOptionCount - 1) % MainMenuOptionCount;
            if (PrAction("MenuDown")) mainMenuIndex = (mainMenuIndex + 1) % MainMenuOptionCount;
            for (int i = 0; i < MainMenuOptionCount; i++)
            {
                float y = 315 + i * 53;
                if (mouseX >= 76 && mouseX <= 572 && mouseY >= y && mouseY <= y + 46)
                {
                    mainMenuIndex = i;
                    if (mousePressed) { ActivateMainMenuItem(i); return; }
                }
            }
            if (PrAction("Confirm") || Pr(Keys.Space)) ActivateMainMenuItem(mainMenuIndex);
        }

        // ---------- world queries ----------
        bool InGap(float x) { foreach (var g in L.Gaps) if (x > g.X && x < g.Y) return true; return false; }
        float FloorBelow(float x, float y, float z) { float best = InGap(x) ? 99999 : G; foreach (var p in L.Plats) if (x >= p.X && x <= p.X + p.W && z >= 1 && z <= 171 && p.Y >= y - 4 && p.Y < best) best = p.Y; return best; }
        static float DistXZ(float x1, float z1, float x2, float z2) => MathF.Sqrt((x2 - x1) * (x2 - x1) + (z2 - z1) * (z2 - z1));

        // ---------- effects ----------
        void Burst(float X, float Y, int n, Color col, float sp = 300, float sz = 3, float g = 600, float life = .6f, float z = 110)
        { for (int i = 0; i < n && pt.Count < 2200; i++) { float a = Rf(0, 6.283f), s = Rf(.2f, 1) * sp; pt.Add(new Part { X = X, Y = Y, Z = z, Vx = MathF.Cos(a) * s, Vy = MathF.Sin(a) * s, Vz = Rf(-.35f, .35f) * s, L = Rf(.4f, 1) * life, M = life, Sz = Rf(.5f, 1) * sz, C = col, G = g }); } }
        void Arc(float X, float Y, int f, float r, Color c, float l = .22f, float a = 0, float z = 110) => fx.Add(new Fx { K = "arc", X = X, Y = Y, Z = z, F = f, R = r, L = l, M = l, C = c, A = a });
        void Ring(float X, float Y, float r, Color c, float l = .5f, float z = 110) => fx.Add(new Fx { K = "ring", X = X, Y = Y, Z = z, R = r, L = l, M = l, C = c });
        void Smoke(float X, float Y, float vx, float z = 110) { if (pt.Count < 2200) pt.Add(new Part { X = X, Y = Y, Z = z, Vx = vx + Rf(-20, 20), Vy = Rf(-70, -15), L = Rf(.6f, 1.1f), M = 1.1f, Sz = Rf(12, 24), C = new Color(10, 5, 18), G = -30, Dk = true }); }

        // ---------- combat ----------
        void Die()
        {
            if (P.Dead) return; DismissSummon(false); P.Dead = true; P.Hp = 0; state = "dead"; Burst(P.X, P.Y - 60, 90, VI, 650, 6, 200, 1.7f, P.Z); for (int i = 0; i < 18; i++) Smoke(P.X, P.Y - 40, Rf(-120, 120), P.Z); shake = 30; Sfx("kill", P.X); if (musInst != null) musInst.Volume = .15f;
        }
        void HurtP(float d, int dir)
        {
            if (P.Inv > 0 || P.Dash > 0 || P.Dead || ult > 0) return;
            if (rotationT > 0)
            {
                Burst(P.X, P.Y - 70, 12, new Color(110, 235, 255), 250, 3, 0, .35f, P.Z);
                Ring(P.X, P.Y - 70, 120, new Color(110, 235, 255), .25f, P.Z);
                return;
            }
            if (susanooT > 0) d *= .72f;
            if (summonedCompanion != null)
            {
                string id = summonedCompanion.Definition.Id;
                bool guardian = summonedCompanion.Definition.Role == SummonRole.Tank || id == "giant-toad-warrior" || id == "toad-boss" || id == "colossal-turtle-guardian";
                float share = guardian ? .42f : .18f;
                float absorbed = d * share;
                summonedCompanion.Hp -= absorbed;
                d -= absorbed;
                if (summonedCompanion.Hp <= 0) DismissSummon(false);
            }
            P.Hp -= d; P.Inv = .9f; P.Vx = dir * 380; P.Vy = -300; P.Ground = false; P.Cb = 0; P.Atk = 0; P.Chg = 0; shake = 16; flash = .35f; hitstop = .06f;
            Burst(P.X, P.Y - 70, 24, RD, 420, 4, 600, .6f, P.Z); Sfx("hurt", P.X); if (P.Hp <= 0) Die();
        }
        void HitE(Foe e, float d, float kx)
        {
            if (e.Dead) return;
            if (susanooT > 0) d *= 1.6f;
            e.Aggro = true; e.Hp -= d; e.Flash = .1f; e.Vx = kx * (e.Boss ? .15f : 1); if (!e.Boss) e.Vy = -220; if (e.St == 1 && !e.Boss) { e.St = 3; e.Tm = .5f; }
            P.En = Math.Min(100, P.En + d * .3f); hitstop = Math.Max(hitstop, .05f); shake = Math.Max(shake, d > 30 ? 18 : 8);
            Burst(e.X, e.Y - 60 * e.S, 16, WH, 450, 3, 500, .35f, e.Z); Burst(e.X, e.Y - 60 * e.S, 12, RD, 320, 4, 700, .5f, e.Z); Sfx("hit", e.X);
            if (e.Hp <= 0)
            {
                e.Dead = true; kills++; Burst(e.X, e.Y - 60 * e.S, 30, VI, 160, 6, -260, 1.5f, e.Z); Burst(e.X, e.Y - 60 * e.S, 34, WH, 520, 3, 400, .5f, e.Z);
                NotifyAbilityUnlocks(progression.RecordEnemyDefeat()); AwardCurrency(e.Boss); SaveProgression();
                Ring(e.X, e.Y - 60, 100, VI, .6f, e.Z); shake = Math.Max(shake, e.Boss ? 38 : 14); Sfx("kill", e.X);
                if (e.Boss) { bossDead = true; hitstop = .4f; flash = .9f; banS = "BOSS DEFEATED - THE SOUL GATE OPENS"; banT = 3.5f; Ring(e.X, e.Y - 100, 900, OR, 1f, e.Z); Sfx("boom", e.X); }
            }
        }
        void SlashHit()
        {
            float r = P.Cb == 2 ? 240 : 185, dm = new[] { 12f, 14f, 28f }[P.Cb];
            string weapon = progression.Profile.EquippedWeaponId;
            float weaponBonus = weapon == "lightning" ? 1.5f : weapon == "chakra" ? 1.3f : weapon == "katana" ? 1.18f : 1f;
            float sageBonus = sageT > 0 ? (progression.Profile.EquippedSageModeId == "sage.sixpaths" ? 1.65f : 1.35f) : 1f;
            dm *= UpgradeMultiplier("scythe") * weaponBonus * sageBonus;
            Color slashColor = weapon == "lightning" ? new Color(180, 230, 255) : weapon.Length > 0 ? CY : P.Cb == 2 ? VI : CY;
            Arc(P.X, P.Y - 70, P.F, r * (P.Cb == 2 ? 1.1f : .9f), slashColor, .25f, new[] { -.35f, .35f, 0f }[P.Cb], P.Z);
            if (P.Cb == 2) { Ring(P.X, P.Y - 60, 190, slashColor, .4f, P.Z); Burst(P.X + P.F * 90, P.Y - 70, 28, slashColor, 520, 4, 600, .6f, P.Z); }
            PlayScytheAttack(P.X);
            foreach (var e in en) if (DistXZ(e.X, e.Z, P.X, P.Z) < r + 20 * e.S && Math.Abs(e.Y - P.Y) < 160) HitE(e, dm * (1 + lv * .05f), P.F * (P.Cb == 2 ? 700 : 380));
            foreach (var p in proj) if (!p.Own && p.K != "pillar" && DistXZ(p.X, p.Z, P.X, P.Z) < r && Math.Abs(p.Y - (P.Y - 70)) < 120) { p.L = 0; P.En = Math.Min(100, P.En + 6); Burst(p.X, p.Y, 14, CY, 300, 3, 600, .6f, p.Z); }
        }
        void Explode(Proj p)
        {
            float R = 210 * p.R / 30; Ring(p.X, p.Y, R, CY, .5f, p.Z); Ring(p.X, p.Y, R * .6f, WH, .35f, p.Z); Burst(p.X, p.Y, 80, CY, 800, 5, 100, .8f, p.Z); Burst(p.X, p.Y, 30, WH, 600, 3, 0, .5f, p.Z);
            shake = 24; flash = .2f; hitstop = .08f; Sfx("ras", p.X);
            foreach (var e in en) if (DistXZ(e.X, e.Z, p.X, p.Z) < R + 20 && Math.Abs(e.Y - p.Y) < R) HitE(e, p.Dmg, Sg(e.X - p.X) * 600);
        }
        void Wave(float x, float z, int dir, float zdir, float spd = 480) => proj.Add(new Proj { K = "wave", X = x, Y = G - 30, Z = z, Vx = dir * spd, Vz = zdir * spd, L = 2.2f, R = 34, Dmg = 16 });
        void Slam(Foe e)
        {
            Wave(e.X, e.Z, 1, 0); Wave(e.X, e.Z, -1, 0); Ring(e.X, G - 10, 280, e.Kind == 2 ? OR : RD, .5f, e.Z); Burst(e.X, G - 10, 40, OR, 500, 5, 500, .6f, e.Z); shake = 24; Sfx("boom", e.X);
            if (DistXZ(P.X, P.Z, e.X, e.Z) < 180 && P.Ground) HurtP(22, Sg(P.X - e.X));
        }

        // ---------- enemies ----------
        void UpdE(Foe e, float dt)
        {
            if (e.GenjutsuT > 0)
            {
                e.GenjutsuT = Math.Max(0, e.GenjutsuT - dt);
                e.Vx = 0; e.Vz = 0; e.St = 3; e.Tm = Math.Max(e.Tm, .1f);
                return;
            }
            float d = P.X - e.X, dz = P.Z - e.Z, ad = DistXZ(P.X, P.Z, e.X, e.Z); e.Ph += dt; e.Flash -= dt;
            if (e.PoisonT > 0)
            {
                e.PoisonT = Math.Max(0, e.PoisonT - dt); e.PoisonCd -= dt;
                if (e.PoisonCd <= 0) { e.PoisonCd = .3f; if (!e.Dead) HitE(e, e.PoisonDmg, 0); }
            }
            if (e.BurnT > 0)
            {
                e.BurnT = Math.Max(0, e.BurnT - dt); e.BurnCd -= dt;
                if (e.BurnCd <= 0) { e.BurnCd = .35f; if (!e.Dead) HitE(e, e.BurnDmg, 0); }
            }
            if (!e.Aggro) { if (ad < (e.Boss ? 950 : 680) && !P.Dead) e.Aggro = true; else { e.F = d > 0 ? 1 : -1; return; } }
            if (e.St != 2 && e.St != 5) { e.F = d >= 0 ? 1 : -1; e.Yaw = MathF.Atan2(dz, d); }
            e.Vy += 2300 * dt; e.Y += e.Vy * dt; if (e.Y >= G) { e.Y = G; if (e.Vy > 0) e.Vy = 0; }
            e.X = Cl(e.X + e.Vx * dt, e.A + 30, e.B - 30); e.Z = Cl(e.Z + e.Vz * dt, -270, 490); if (e.St != 2) { e.Vx *= MathF.Pow(.02f, dt); e.Vz *= MathF.Pow(.02f, dt); }
            e.Tm -= dt; e.Mv = 0; int T = e.Type;
            if (e.Boss) { UpdBoss(e, dt, d, dz, ad); return; }
            if (e.St == 0)
            {
                if (T == 2) { int w = ad < 360 ? -1 : ad > 560 ? 1 : 0; float step = w * e.Sp * dt / Math.Max(1, ad); e.X = Cl(e.X + d * step, e.A + 30, e.B - 30); e.Z = Cl(e.Z + dz * step, -270, 490); e.Mv = w; if (e.Tm <= 0 && ad < 720) { e.St = 1; e.Tm = .6f; Sfx("tele", e.X); } }
                else
                {
                    float rg = T == 1 ? 125 : 260;
                    if (ad > (T == 1 ? 100 : 120) && (ad > rg || e.Tm > 0)) { e.X = Cl(e.X + d / ad * e.Sp * dt, e.A + 30, e.B - 30); e.Z = Cl(e.Z + dz / ad * e.Sp * dt, -270, 490); e.Mv = 1; }
                    if (e.Tm <= 0 && ad <= rg) { e.St = 1; e.Tm = T == 1 ? .65f : .4f; Sfx("tele", e.X); }
                }
            }
            else if (e.St == 1 && e.Tm <= 0)
            {
                if (T == 0) { e.St = 2; e.Air = 0; e.Vy = -680; e.Vx = Cl(d / .59f, -720, 720); e.Vz = Cl(dz / .59f, -520, 520); Sfx("dash", e.X); }
                else if (T == 1)
                {
                    Arc(e.X + e.F * 20, e.Y - 70, e.F, 130, new Color(255, 64, 32), .22f, 0, e.Z); Sfx("slash", e.X);
                    if (ad < 160 && Math.Abs(P.Y - e.Y) < 130) HurtP(16, e.F); e.St = 3; e.Tm = .8f;
                }
                else
                {
                    float len = Math.Max(1, MathF.Sqrt(d * d + dz * dz + 900)); proj.Add(new Proj { K = "arrow", X = e.X + e.F * 30, Y = e.Y - 80, Z = e.Z, Vx = d / len * 640, Vy = ((P.Y - 70) - (e.Y - 80)) / len * 640, Vz = dz / len * 640, L = 2.5f, R = 10, Dmg = 10 });
                    e.St = 3; e.Tm = .7f; Sfx("arrow", e.X);
                }
            }
            else if (e.St == 2)
            {
                e.Air += dt; fx.Add(new Fx { K = "ghost", X = e.X, Y = e.Y, Z = e.Z, F = e.F, L = .2f, M = .2f, C = RD, E = true });
                if (!e.Did && ad < 70 && Math.Abs(P.Y - e.Y) < 130) { e.Did = true; HurtP(13, e.F); }
                if (e.Air > .15f && e.Y >= G) { e.St = 3; e.Tm = .6f; e.Vx = 0; e.Did = false; }
            }
            else if (e.St == 3 && e.Tm <= 0) { e.St = 0; e.Tm = Rf(.7f, 1.5f); }
            e.Walk += dt * (e.Mv != 0 ? 1 : 0);
        }
        void UpdBoss(Foe e, float dt, float d, float dz, float ad)
        {
            int k = e.Kind;
            if (e.St == 0)
            {
                float rg = k == 1 ? 470 : 200; if (ad > rg) { e.X = Cl(e.X + d / ad * e.Sp * dt, e.A + 30, e.B - 30); e.Z = Cl(e.Z + dz / ad * e.Sp * dt, -270, 490); e.Mv = 1; } else if (k == 1 && ad < 230) { e.X = Cl(e.X - d / ad * e.Sp * .8f * dt, e.A + 30, e.B - 30); e.Z = Cl(e.Z - dz / ad * e.Sp * .8f * dt, -270, 490); e.Mv = -1; }
                if (e.Tm <= 0) { e.St = 1; e.Cnt++; e.Tm = .85f; Sfx("tele", e.X); }
            }
            else if (e.St == 1 && e.Tm <= 0)
            {
                int v = (int)e.Cnt % 3;
                if (k == 0) { if (v == 0) { e.Vy = -950; e.Vx = Cl(d * 1.1f, -650, 650); e.Vz = Cl(dz * 1.1f, -520, 520); e.St = 2; e.Air = 0; Sfx("slash3", e.X); } else { Slam(e); e.St = 3; e.Tm = 1.1f; } }
                else if (k == 1)
                {
                    if (v == 0) { e.St = 4; e.Tm = .45f; Sfx("tele", e.X); }
                    else { float aim = MathF.Atan2(dz, d); for (int i = -2; i <= 2; i++) { float a = aim + i * .22f; float len = Math.Max(1, MathF.Sqrt(ad * ad + 900)); proj.Add(new Proj { K = "orb", X = e.X + e.F * 30, Y = e.Y - 110, Z = e.Z, Vx = MathF.Cos(a) * 430, Vy = ((P.Y - 70) - (e.Y - 110)) / len * 430, Vz = MathF.Sin(a) * 430, L = 3, R = 16, Dmg = 12 }); } Sfx("arrow", e.X); e.St = 3; e.Tm = 1.2f; }
                }
                else
                {
                    if (v == 0) { Wave(e.X, e.Z, 1, 0, 600); Wave(e.X, e.Z, -1, 0, 600); Ring(e.X, G, 300, OR, .5f, e.Z); shake = 18; Sfx("boom", e.X); e.St = 3; e.Tm = 1.1f; }
                    else if (v == 1) { for (int i = -2; i <= 2; i++) proj.Add(new Proj { K = "pillar", X = P.X + i * 140, Y = G, Z = P.Z, L = 1.7f, T0 = .95f + Math.Abs(i) * .08f, R = 40, Dmg = 20 }); Sfx("tele", e.X); e.St = 3; e.Tm = 1.4f; }
                    else { e.Vy = -950; e.Vx = Cl(d * 1.1f, -650, 650); e.Vz = Cl(dz * 1.1f, -520, 520); e.St = 2; e.Air = 0; Sfx("slash3", e.X); }
                }
            }
            else if (e.St == 2) { e.Air += dt; if (e.Air > .25f && e.Y >= G) { Slam(e); e.St = 3; e.Tm = 1f; e.Vx = 0; } }
            else if (e.St == 4) { e.Al = Math.Max(0, e.Al - dt * 4); if (e.Tm <= 0) { e.X = Cl(P.X - P.F * 150, e.A + 30, e.B - 30); e.Z = Cl(P.Z + Rf(-120, 120), -240, 460); e.F = P.F; e.Yaw = MathF.Atan2(P.Z - e.Z, P.X - e.X); e.St = 5; e.Tm = .4f; e.Al = 1; Burst(e.X, e.Y - 80, 36, VI, 400, 5, 0, .6f, e.Z); } }
            else if (e.St == 5 && e.Tm <= 0) { Arc(e.X + e.F * 20, e.Y - 90, e.F, 200, VI, .25f, 0, e.Z); Sfx("slash3", e.X); if (DistXZ(P.X, P.Z, e.X, e.Z) < 220 && Math.Abs(P.Y - e.Y) < 140) HurtP(22, e.F); e.St = 3; e.Tm = .9f; }
            else if (e.St == 3 && e.Tm <= 0) { e.St = 0; e.Tm = Rf(.4f, .9f); }
            e.Walk += dt * (e.Mv != 0 ? 1 : 0);
        }

        // ---------- player update ----------
        bool Dn(Keys k) => cur.IsKeyDown(k);
        bool Pr(Keys k) => cur.IsKeyDown(k) && prev.IsKeyUp(k);

        void UpdatePlay(float dt)
        {
            levelElapsed += dt;
            if (banT > 0) banT -= dt;
            if (abilityMenuOpen) HandleAbilityMenuInput();
            if (shopMenuOpen || summoningMenuOpen) HandleShopInput();
            else if (!P.Dead)
            {
                if (PrAction("SummonCall")) SummonEquipped();
                if (PrAction("SummonDismiss")) DismissSummon(true);
            }
            eyeCooldown = Math.Max(0, eyeCooldown - dt); pushCooldown = Math.Max(0, pushCooldown - dt);
            genjutsuCooldown = Math.Max(0, genjutsuCooldown - dt); susanooCd = Math.Max(0, susanooCd - dt);
            sageCd = Math.Max(0, sageCd - dt); if (susanooT > 0) susanooT = Math.Max(0, susanooT - dt);
            shurikenCd = Math.Max(0, shurikenCd - dt); kenjutsuCd = Math.Max(0, kenjutsuCd - dt);
            hiddenCd = Math.Max(0, hiddenCd - dt); kunaiCd = Math.Max(0, kunaiCd - dt); hiddenT = Math.Max(0, hiddenT - dt);
            if (!shopMenuOpen && !summoningMenuOpen && !abilityMenuOpen && !P.Dead)
            {
                if (PrAction("EyePrimary")) UseEyeQ();
                if (PrAction("PowerSecondary")) UseEyeE();
                if (PrAction("ModeOrKenjutsu")) UseEyeF();
                if (PrAction("HiddenJutsu")) ActivateHiddenJutsu();
                if (PrAction("Kunai")) UseTeleportationKunai();
                if (PrAction("SageMode"))
                {
                    string sage = progression.Profile.EquippedSageModeId;
                    if (sage != "sage.toad" && sage != "sage.snake" && sage != "sage.sixpaths") { banS = "EQUIP A SAGE MODE IN THE SHOP"; banT = 1f; }
                    else if (sageCd <= 0) { sageT = sage == "sage.sixpaths" ? 12f : 10f; sageCd = 24f; banS = sage.ToUpperInvariant() + " ACTIVE"; banT = 1.2f; }
                }
            }
            float worldDt = dt;
            if (freezeT > 0) { worldDt = Math.Max(0, dt - freezeT); freezeT = Math.Max(0, freezeT - dt); }
            float side = (DnAction("MoveRight") ? 1 : 0) - (DnAction("MoveLeft") ? 1 : 0), forward = (DnAction("MoveForward") ? 1 : 0) - (DnAction("MoveBack") ? 1 : 0);
            float fwdX = -MathF.Sin(camYaw), fz = -MathF.Cos(camYaw), rx = MathF.Cos(camYaw), rz = -MathF.Sin(camYaw);
            float mx = rx * side + fwdX * forward, mz = rz * side + fz * forward, ml = MathF.Sqrt(mx * mx + mz * mz);
            if (ml > 1) { mx /= ml; mz /= ml; }
            if (byakuganScout)
            {
                // The scout camera leaves the player in place while the world keeps moving.
                // Keep the stationary body safe until scouting ends.
                P.Inv = Math.Max(P.Inv, .15f);
                scoutX = Cl(scoutX + side * 720 * dt, 0, L.GateX + 250);
                scoutZ = Cl(scoutZ + forward * 620 * dt, -260, 480);
                cam = Cl(scoutX - W * .5f, 0, Math.Max(0, L.GateX + 400 - W)); camZ = scoutZ; camY = 0;
            }
            if (!P.Dead && !summoningMenuOpen && !shopMenuOpen && !abilityMenuOpen && !byakuganScout)
            {
                P.Inv -= dt; P.Dcd -= dt; P.Kcd -= dt; P.CloneCd -= dt; P.CbT -= dt; bool was = P.Ground; float pvy = P.Vy;
                if (P.Dash > 0) { P.Dash -= dt; P.X += MathF.Cos(P.Yaw) * 1200 * dt; P.Z += MathF.Sin(P.Yaw) * 1200 * dt; P.Vy = 0; fx.Add(new Fx { K = "ghost", X = P.X, Y = P.Y, Z = P.Z, F = P.F, L = .3f, M = .3f, C = PU }); }
                else
                {
                    float sl = P.Atk > 0 ? .72f : 1, accel = Math.Min(1, dt * (P.Ground ? 14 : 6));
                    P.Vx += (mx * 420 * sl - P.Vx) * accel; P.Vz += (mz * 420 * sl - P.Vz) * accel;
                    if (ml > .1f && P.Atk <= 0) { P.Yaw = MathF.Atan2(mz, mx); if (Math.Abs(mx) > .15f) P.F = mx > 0 ? 1 : -1; }
                    if (P.Ground) P.Coy = .1f; else { P.Coy -= dt; if (P.Coy <= 0 && P.J == 0) P.J = 1; }
                    if (PrAction("Jump") && P.J < 2) { P.Vy = -850; P.J++; P.Ground = false; Burst(P.X, P.Y, 14, PU, 240, 3, 300, .4f, P.Z); Sfx("jump", P.X); }
                    float prevY = P.Y; P.Vy += 2300 * dt; P.X += P.Vx * dt; P.Z += P.Vz * dt; P.Y += P.Vy * dt; P.Ground = false;
                    if (P.Vy >= 0)
                    {
                        float best = 1e9f; if (!InGap(P.X) && prevY <= G + 1 && P.Y >= G) best = G;
                        foreach (var pl in L.Plats) if (P.X >= pl.X && P.X <= pl.X + pl.W && P.Z >= 1 && P.Z <= 171 && prevY <= pl.Y + 1 && P.Y >= pl.Y && pl.Y < best) best = pl.Y;
                        if (best < 1e8f) { P.Y = best; P.Vy = 0; P.Ground = true; P.J = 0; }
                    }
                }
                if (P.Ground && !was && pvy > 450) { Sfx("land", P.X); for (int i = 0; i < 6; i++) pt.Add(new Part { X = P.X + Rf(-20, 20), Y = P.Y - 4, Z = P.Z + Rf(-20, 20), Vx = Rf(-80, 80), Vy = Rf(-70, -15), L = .8f, M = .8f, Sz = 12, C = new Color(10, 5, 18), G = -30, Dk = true }); }
                if (P.Y > G + 380) { Sfx("fall", P.X); Die(); }
                P.X = Cl(P.X, 40, bossDead ? L.GateX + 60 : L.GateX - 90); P.Z = Cl(P.Z, -260, 480); P.Run = P.Ground && MathF.Sqrt(P.Vx * P.Vx + P.Vz * P.Vz) > 60;
                if (P.Ground && P.Y >= G - 6)
                    for (int spikeIndex = 0; spikeIndex < L.Spikes.Count; spikeIndex++)
                    {
                        if (disabledSpikeIndices.Contains(spikeIndex)) continue;
                        Vector2 spike = L.Spikes[spikeIndex];
                        if (P.X <= spike.X || P.X >= spike.Y) continue;
                        for (float z = -250; z <= 450; z += 70)
                            if (Math.Abs(P.Z - z) < 34) { HurtP(30, -P.F); P.Vy = -620; break; }
                        break;
                    }
                if ((P.Run || P.Dash > 0 || P.Atk > 0) && rnd.NextDouble() < .6) pt.Add(new Part { X = P.X - MathF.Cos(P.Yaw) * Rf(10, 34), Y = P.Y - Rf(0, 50), Z = P.Z - MathF.Sin(P.Yaw) * Rf(10, 34), Vx = Rf(-20, 20), Vy = Rf(-70, -15), L = .8f, M = .8f, Sz = 12, C = new Color(10, 5, 18), G = -30, Dk = true });
                if (PrAction("Dodge") && P.Dcd <= 0) { P.Dash = .17f; P.Dcd = .65f; P.Inv = Math.Max(P.Inv, .3f); Burst(P.X, P.Y - 60, 22, PU, 300, 4, 600, .6f, P.Z); Sfx("dash", P.X); }
                if (P.Atk > 0)
                {
                    float dur = P.Cb == 2 ? .42f : .28f; P.AtkT += dt; P.Atk -= dt; float atp = P.AtkT / dur;
                    var pts = ScytheGeom(P.X, P.Y, P.F, 1, ScytheAng(atp, P.Cb, false, false, false, t), atp, out _, out _, out _);
                    float bob = (Math.Abs(P.Vx) > 60 && P.Ground ? MathF.Abs(MathF.Sin(t * 13)) * 4 : MathF.Sin(t * 2.2f) * 3) + 4;
                    trail.Add(new Trl { A = ScythePoint(pts[6], bob), B = ScythePoint(pts[12], bob), L = .3f, Brk = P.AtkT <= dt * 1.5f });
                    if (rnd.NextDouble() < .8) { var spark = ScythePoint(pts[12], bob); pt.Add(new Part { X = spark.X, Y = spark.Y, Z = spark.Z, Vz = Rf(-40, 40), Vx = Rf(-40, 40), Vy = Rf(-80, -20), L = .5f, M = .5f, Sz = 5, C = P.Cb == 2 ? VI : CY, G = -60 }); }
                    if (P.Hit && P.AtkT > (P.Cb == 2 ? .17f : .11f)) { P.Hit = false; SlashHit(); }
                }
                if (mouseCur.LeftButton == ButtonState.Pressed && mousePrev.LeftButton == ButtonState.Released && P.Dash <= 0 && (P.Atk <= 0 || P.AtkT > .18f)) { P.Cb = P.CbT > 0 ? (P.Cb + 1) % 3 : 0; P.Atk = P.Cb == 2 ? .42f : .28f; P.AtkT = 0; P.Hit = true; P.CbT = .75f; }
                if (mouseCur.RightButton == ButtonState.Pressed && mousePrev.RightButton == ButtonState.Released)
                {
                    AbilityDefinition rasengan = AbilityCatalog.Get("rasengan");
                    if (!progression.IsUsable(rasengan.Id))
                    {
                        banS = "RASENGAN LOCKED  [TAB FOR REQUIREMENTS]"; banT = 1.2f;
                    }
                    else if (P.Kcd <= 0 && P.En >= rasengan.ResourceCost)
                    {
                        P.En -= rasengan.ResourceCost;
                        P.Kcd = rasengan.CooldownSeconds * Math.Max(.55f, 1 - progression.GetUpgradeLevel(rasengan.Id) * .07f);
                        float dx = MathF.Cos(P.Yaw), dz = MathF.Sin(P.Yaw);
                        proj.Add(new Proj { K = "ras", Own = true, X = P.X + dx * 42, Y = P.Y - 76, Z = P.Z + dz * 42, Vx = dx * 650, Vy = 0, Vz = dz * 650, L = 1.25f, R = 34, Dmg = 56 * UpgradeMultiplier(rasengan.Id) });
                        Burst(P.X + dx * 48, P.Y - 76, 24, CY, 360, 5, 0, .45f, P.Z + dz * 48); Sfx("ras", P.X);
                    }
                    else if (progression.IsUsable(rasengan.Id) && P.Kcd <= 0)
                    { banS = "NEED " + rasengan.ResourceCost + " SOUL ENERGY"; banT = 1.1f; }
                }
                bool ePowerEquipped = HasEye("byakugan") || HasEye("jougan") || HasEye("mangekyo") || HasEye("ems");
                if (PrAction("PowerSecondary") && !ePowerEquipped)
                {
                    AbilityDefinition clone = AbilityCatalog.Get("clone");
                    if (!progression.IsUsable(clone.Id))
                    {
                        banS = "SHADOW CLONE LOCKED  [TAB FOR REQUIREMENTS]"; banT = 1.2f;
                    }
                    else if (clones.Count > 0)
                    { banS = "SHADOW CLONE ALREADY ACTIVE"; banT = 1.1f; }
                    else if (P.CloneCd > 0)
                    { banS = "SHADOW CLONE COOLDOWN"; banT = 1.1f; }
                    else if (P.En >= clone.ResourceCost)
                    {
                        P.En -= clone.ResourceCost;
                        P.CloneCd = clone.CooldownSeconds * Math.Max(.55f, 1 - progression.GetUpgradeLevel(clone.Id) * .07f);
                        clones.Add(new Clone { X = P.X - MathF.Sin(P.Yaw) * 90, Z = P.Z + MathF.Cos(P.Yaw) * 90, Yaw = P.Yaw, F = P.F, L = 5 + progression.GetUpgradeLevel(clone.Id) * .4f, Cd = .2f });
                        Burst(P.X, G - 60, 24, new Color(223, 233, 255), 300, 6, -100, .7f, P.Z);
                        Sfx("clone", P.X);
                    }
                    else { banS = "NEED " + clone.ResourceCost + " SOUL ENERGY"; banT = 1.1f; }
                }
                // checkpoints
                for (int i = 0; i < L.Checks.Count; i++) if (!lit[i] && P.X > L.Checks[i]) { lit[i] = true; checkX = L.Checks[i]; checkZ = P.Z; P.Hp = Math.Min(P.Mh, P.Hp + 40); Burst(checkX, G - 60, 50, L.Th.Accent, 400, 5, -200, 1.2f, P.Z); Ring(checkX, G - 60, 200, L.Th.Accent, .8f, P.Z); Sfx("check", checkX); banS = "CHECKPOINT"; banT = 1.6f; }
                // soul pickups
                foreach (var s in L.Souls) if (!s.Got && DistXZ(P.X, P.Z, s.X, 110) < 70 && Math.Abs(P.Y - 60 - s.Y) < 90) { s.Got = true; P.En = Math.Min(100, P.En + 15); Burst(s.X, s.Y, 20, VI, 300, 4, -100, .6f, 110); Sfx("soul", s.X); }
                // gate
                if (L.Mission.CompletionCondition == "boss-defeated-and-soul-gate-reached" && bossDead && P.X >= L.GateX - 20)
                {
                    state = "win"; winT = 0;
                    NotifyAbilityUnlocks(progression.CompleteMission(lv, L.Mission.Id, levelElapsed));
                    SummoningEconomy.AwardMissionCompletion(progression.Profile, L.Mission);
                    SaveProgression(); Sfx("win", P.X);
                }
            }
            if (worldDt > 0)
            {
            if (ult > 0)
            {
                ult -= worldDt;
                if (!ultHit && ult < 1)
                {
                    ultHit = true; flash = 1; shake = 45; foreach (var e in en) if (Math.Abs(e.X - P.X) < 1100) HitE(e, e.Boss ? 120 : 95, Sg(e.X - P.X) * 500);
                    for (int i = 0; i < 6; i++) Arc(P.X + (i - 2.5f) * 220, P.Y - 130, i % 2 == 0 ? -1 : 1, 300 + i * 20, VI, .5f, Rf(-.6f, .6f));
                    Ring(P.X, P.Y - 80, 900, VI, .8f); Burst(P.X, P.Y - 80, 160, VI, 1200, 6, 0, 1);
                }
            }
            if (ult <= .9f) foreach (var e in en) UpdE(e, worldDt);
            en.RemoveAll(e => e.Dead);
            UpdateSummonedCompanion(worldDt);
            foreach (var k in clones)
            {
                k.L -= worldDt; k.T += worldDt; k.Cd -= worldDt; k.Mv = 0; Foe tg = null; float bd = 9e9f;
                foreach (var e in en) { float d = DistXZ(e.X, e.Z, k.X, k.Z); if (d < bd) { bd = d; tg = e; } }
                if (tg != null && bd < 900)
                {
                    float dx = tg.X - k.X, dz = tg.Z - k.Z; k.F = dx >= 0 ? 1 : -1; k.Yaw = MathF.Atan2(dz, dx);
                    if (bd > 110) { k.X += dx / bd * 330 * worldDt; k.Z = Cl(k.Z + dz / bd * 330 * worldDt, -260, 480); k.Mv = 1; }
                    else if (k.Cd <= 0) { k.Cd = .55f; k.At = 0; Arc(k.X + k.F * 30, G - 70, k.F, 130, new Color(223, 233, 255), .2f, 0, k.Z); HitE(tg, 10 * UpgradeMultiplier("clone"), k.F * 300); }
                }
                else if (DistXZ(P.X, P.Z, k.X, k.Z) > 140) { float dx = P.X - k.X, dz = P.Z - k.Z, d = DistXZ(P.X, P.Z, k.X, k.Z); k.X += dx / d * 330 * worldDt; k.Z = Cl(k.Z + dz / d * 330 * worldDt, -260, 480); k.Yaw = MathF.Atan2(dz, dx); k.Mv = 1; }
                if (k.At >= 0) { k.At += worldDt; if (k.At > .28f) k.At = -1; }
                if (k.L <= 0) Burst(k.X, G - 60, 25, new Color(223, 233, 255), 250, 5, -100, .6f, k.Z);
            }
            clones.RemoveAll(k => k.L <= 0);
            foreach (var p in proj)
            {
                p.L -= worldDt;
                if (p.K == "pillar") { p.T0 -= worldDt; if (p.T0 <= 0 && p.T0 > -.45f && DistXZ(P.X, P.Z, p.X, p.Z) < 45 && P.Y > G - 320) HurtP(p.Dmg, Sg(P.X - p.X)); continue; }
                p.X += p.Vx * worldDt; p.Y += p.Vy * worldDt; p.Z += p.Vz * worldDt;
                if (p.Own)
                {
                    pt.Add(new Part { X = p.X, Y = p.Y, Z = p.Z, Vx = Rf(-30, 30), Vy = Rf(-30, 30), L = .3f, M = .3f, Sz = p.K == "ras" ? p.R * .5f : 3, C = CY });
                    foreach (var e in en)
                        if (!e.Dead && p.L > 0 && DistXZ(p.X, p.Z, e.X, e.Z) < 26 * e.S + p.R && p.Y > e.Y - 125 * e.S && p.Y < e.Y + 10)
                        {
                            if (p.K == "ras") { Explode(p); p.Spent = true; }
                            else
                            {
                                HitE(e, p.Dmg, Sg(p.Vx) * 250);
                                if (!e.Dead && p.K == "fire")
                                {
                                    e.BurnT = Math.Max(e.BurnT, 1.75f);
                                    e.BurnDmg = Math.Max(e.BurnDmg, p.Dmg * .11f);
                                    e.BurnCd = .35f;
                                    Burst(e.X, e.Y - 60 * e.S, 10, new Color(255, 116, 38), 190, 4, -25, .45f, e.Z);
                                }
                                else if (!e.Dead && !e.Boss && (p.K == "water" || p.K == "lightning"))
                                {
                                    e.St = 3;
                                    e.Tm = p.K == "lightning" ? .8f : .5f;
                                }
                            }
                            p.L = 0;
                        }
                }
                else
                {
                    if (p.K == "wave") Burst(p.X, G - 5, 2, new Color(255, 64, 32), 100, 5, -300, .4f, p.Z);
                    if (DistXZ(p.X, p.Z, P.X, P.Z) < p.R + 16 && p.Y > P.Y - 115 && p.Y < P.Y + 10 && p.L > 0) { HurtP(p.Dmg, Sg(p.Vx)); p.L = 0; }
                }
                if (p.X < -100 || p.X > L.GateX + 900 || p.Y > G + 50) p.L = 0;
            }
            foreach (var p in proj) if (p.L <= 0 && p.K == "ras" && !p.Spent) { p.Spent = true; Explode(p); }
            proj.RemoveAll(p => p.L <= 0);
            }
            if (sageT > 0)
            {
                sageT = Math.Max(0, sageT - dt);
                float regen = progression.Profile.EquippedSageModeId == "sage.snake" ? 5f : progression.Profile.EquippedSageModeId == "sage.sixpaths" ? 4f : 1.5f;
                P.Hp = Math.Min(P.Mh, P.Hp + regen * dt);
            }
        }

        void UpdFx(float dt)
        {
            foreach (var p in pt) { p.L -= dt; p.Vy += p.G * dt; p.X += p.Vx * dt; p.Y += p.Vy * dt; p.Z += p.Vz * dt; if (p.Dk) { p.Vx *= .98f; p.Vz *= .98f; p.Sz += dt * 8; } }
            pt.RemoveAll(p => p.L <= 0); foreach (var f in fx) f.L -= dt; fx.RemoveAll(f => f.L <= 0); foreach (var q in trail) q.L -= dt; trail.RemoveAll(q => q.L <= 0);
            float dy = L.Th.Style == 0 ? .15f : -1;
            for (int i = 0; i < 120; i++) { amb[i, 0] += amb[i, 2] * dt + MathF.Sin(t + amb[i, 1] * .02f) * .4f; amb[i, 1] += amb[i, 3] * dy * dt; if (amb[i, 1] > H) amb[i, 1] = -5; if (amb[i, 1] < -5) amb[i, 1] = H; if (amb[i, 0] < 0) amb[i, 0] = W; if (amb[i, 0] > W) amb[i, 0] = 0; }
            shake = Math.Max(0, shake - 70 * dt); flash = Math.Max(0, flash - dt * 1.5f);
        }

        protected override void Update(GameTime gt)
        {
            prev = cur; cur = Keyboard.GetState(); mousePrev = mouseCur; mouseCur = Mouse.GetState(); float dt = Math.Min(.033f, (float)gt.ElapsedGameTime.TotalSeconds); t += dt;
            bool enteredPlayThisFrame = false;
            if (PrAction("Back"))
            {
                if (capturingSettingBinding) { capturingSettingBinding = false; banS = "KEY BINDING CANCELLED"; banT = 1.2f; }
                else if (state == "title" && titlePage == "levels")
                {
                    titlePage = "maps";
                    selectedMenuMap = selectedMenuLevel / MissionCatalog.MissionsPerMap;
                }
                else if (state == "title" && titlePage == "maps")
                {
                    levelSelectOpen = false; titlePage = "main"; PreviewMenuLevel(lv);
                }
                else if (state == "title" && titlePage != "main")
                {
                    levelSelectOpen = false; titlePage = "main"; PreviewMenuLevel(lv);
                }
                else if (state == "play" && (abilityMenuOpen || shopMenuOpen || summoningMenuOpen))
                { abilityMenuOpen = shopMenuOpen = summoningMenuOpen = false; }
                else Exit();
            }
            if (!capturingSettingBinding && Pr(Keys.F11)) { settings.Fullscreen = !settings.Fullscreen; ApplyVideoSettings(); SaveSettings(); }
            if (!capturingSettingBinding && Pr(Keys.F6)) { settings.GraphicsQuality = (settings.GraphicsQuality + 1) % QualityScales.Length; ApplyVideoSettings(); SaveSettings(); banS = settings.GraphicsQuality == 3 ? "ULTRA GRAPHICS  -  2X RENDER" : "RENDER QUALITY  " + (int)(renderScale * 100) + "%"; banT = 1.5f; }
            if (!capturingSettingBinding && Pr(Keys.F7)) { camYaw = -MathHelper.PiOver2; camPitch = .19f; camDistance = 880; camMouseIdle = 0; banS = "CAMERA RESET"; banT = 1.2f; }
            if (!capturingSettingBinding && Pr(Keys.F9)) SetTestMode(!testMode);
            if (state == "play" && PrAction("Shop"))
            {
                shopMenuOpen = !shopMenuOpen; abilityMenuOpen = false; summoningMenuOpen = false;
                if (shopMenuOpen) selectedShopIndex = Math.Max(0, selectedShopIndex);
            }
            if (state == "play" && PrAction("Abilities")) { abilityMenuOpen = !abilityMenuOpen; shopMenuOpen = false; summoningMenuOpen = false; }
            if (state == "play" && PrAction("Summons"))
            {
                summoningMenuOpen = !summoningMenuOpen;
                shopMenuOpen = false; abilityMenuOpen = false;
                if (summoningMenuOpen)
                {
                    int equippedIndex = 0;
                    for (int i = 0; i < SummoningCatalog.All.Count; i++)
                        if (SummoningCatalog.All[i].Id == progression.Profile.EquippedSummonId) { equippedIndex = i; break; }
                    selectedShopCategory = (int)ShopCategory.Summons;
                    selectedShopIndex = equippedIndex;
                }
            }
            if (state == "title")
            {
                HandleTitleMenuInput();
            }
            else if (state == "launch")
            {
                launchDelay -= dt;
                if (launchDelay <= 0) { MissionDefinition mission = MissionCatalog.Get(queuedMenuLevel); LoadLevel(queuedMenuLevel, mission.SpawnX, mission.SpawnZ); state = "play"; enteredPlayThisFrame = true; }
            }
            else if (state == "dead") { if (PrAction("Confirm")) { LoadLevel(lv, checkX, checkZ); state = "play"; enteredPlayThisFrame = true; } }
            else if (state == "win")
            {
                winT += dt;
                if (PrAction("Confirm") && winT > 1)
                {
                    if (lv >= MissionCatalog.All.Count - 1)
                    {
                        SaveProgression(); state = "end"; winT = 0;
                    }
                    else
                    {
                        lv = Math.Min(MissionCatalog.All.Count - 1, lv + 1); SaveProgression(); LoadLevel(lv, 250); state = "play"; enteredPlayThisFrame = true;
                    }
                }
            }
            else if (state == "end") { if (PrAction("Confirm")) { LoadLevel(0, 250); state = "title"; titlePage = "main"; } }
            if (state == "play" && !enteredPlayThisFrame) { if (hitstop > 0) { hitstop -= dt; dt *= .1f; } UpdatePlay(dt); }
            UpdateMenuMusic();
            UpdFx(dt); base.Update(gt);
        }
    }
}
