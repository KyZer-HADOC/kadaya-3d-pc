using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kadaya.Tests
{
    static class Program
    {
        static void Main()
        {
            VerifySpriteFontTextFallback();
            float yawDelta = CameraMotion.MouseYawDelta(100, 1);
            if (Math.Abs(yawDelta + .15f) > .0001f) throw new Exception("Mouse yaw sensitivity changed unexpectedly.");

            float before = 3.13f;
            float after = CameraMotion.ApproachYaw(before, -3.13f, .016f, 2.4f);
            if (Math.Abs(CameraMotion.WrapAngle(-3.13f - after)) >= Math.Abs(CameraMotion.WrapAngle(-3.13f - before)))
                throw new Exception("Automatic camera follow did not take the shortest path across the angle boundary.");

            if (CameraMotion.ApproachYaw(before, -3.13f, 0, 2.4f) != before)
                throw new Exception("Camera follow must remain stable when no frame time has elapsed.");

            VerifyAbilityCatalogAndInitialUnlock();
            VerifyMissionCatalogAndCompletion();
            VerifyMilestoneUnlockOrder();
            VerifySameThresholdMissionAndKillOrder();
            VerifyTestModeGrantIsTemporary();
            VerifyNewGameReset();
            VerifyLegacySaveMigrationPreservesOldFile();
            VerifyUnreadableProfileIsProtected();
            VerifyProfileJsonRoundTrip();
            VerifyNormalAndTestPathsAreSeparate();
            VerifyNormalAndTestProfileSaveIsolation();
            VerifySummoningCatalogAndOwnership();
            VerifySummonUpgradesUseShards();
#if DEBUG
            VerifyTestModeSummoningGrantsAreTemporary();
#endif
            VerifySummoningEconomyRewardsPersist();
            VerifyShopAndEyeOwnership();
            VerifyAbilityAndSusanooShardUpgrades();
            VerifyDevelopmentTestModeSettings();
            VerifyAbilityMenuRows();
            VerifyShopMenuModel();
            VerifyGameSettingsPersistenceAndBindings();

            Console.WriteLine("Camera motion checks passed.");
            Console.WriteLine("45-mission catalog checks passed.");
            Console.WriteLine("Ability progression checks passed.");
        }

        static void VerifyMissionCatalogAndCompletion()
        {
            Require(MissionCatalog.Maps.Count == 3, "The campaign must define exactly three maps.");
            Require(MissionCatalog.All.Count == 45, "The campaign must define exactly forty-five missions.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var layouts = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < MissionCatalog.All.Count; i++)
            {
                MissionDefinition mission = MissionCatalog.Get(i);
                Require(mission.Index == i && ids.Add(mission.Id), "Every mission must have a stable unique ID and correct index.");
                Require(mission.MapIndex == i / MissionCatalog.MissionsPerMap && mission.LevelNumber == i % MissionCatalog.MissionsPerMap + 1,
                    "Missions must be grouped into three consecutive sets of fifteen.");
                Require(layouts.Add(mission.RouteCode), "Every mission must provide a distinct route configuration.");
                Require(mission.Name.Length > 0 && mission.Objective.Length > 0 && mission.Difficulty >= 1 && mission.Difficulty <= 15,
                    "Every mission must have a name, objective, and valid difficulty.");
                Require(mission.SpawnX >= 0 && mission.CompletionCondition == "boss-defeated-and-soul-gate-reached",
                    "Every mission must define a valid player spawn and completion condition.");
                Require(mission.RewardMoney > 0 && mission.RewardDiamonds > 0 && mission.RewardShards > 0,
                    "Every mission must offer positive in-game rewards.");
                foreach (char section in mission.RouteCode.Substring(2))
                    Require("EGSCAR".IndexOf(section) >= 0, "Mission routes may only contain supported gameplay section types.");
            }

            ProgressionProfile profile = new ProgressionProfile();
            ProgressionService progression = new ProgressionService(profile);
            progression.CompleteMission(44, "burning-temple-15", 125f);
            Require(profile.CompletedMissionIds.Contains("burning-temple-15") && profile.CompletedMissions == 1,
                "Mission completion must persist an ID and count unique missions.");
            Require(profile.UnlockedLevel == 44 && Math.Abs(profile.BestMissionTimes["burning-temple-15"] - 125f) < .001f,
                "Completing the final mission must retain campaign progress and a best time.");
            progression.CompleteMission(44, "burning-temple-15", 90f);
            Require(profile.CompletedMissions == 1 && Math.Abs(profile.BestMissionTimes["burning-temple-15"] - 90f) < .001f,
                "Replaying a mission must not double-count completion and should improve its best time.");

            ProgressionProfile rewardProfile = new ProgressionProfile();
            MissionDefinition rewardMission = MissionCatalog.Get(44);
            SummoningEconomy.AwardMissionCompletion(rewardProfile, rewardMission);
            Require(rewardProfile.CurrencyBalances["money"] == rewardMission.RewardMoney &&
                rewardProfile.CurrencyBalances["diamonds"] == rewardMission.RewardDiamonds &&
                rewardProfile.CurrencyBalances["shards"] == rewardMission.RewardShards,
                "Mission rewards must be credited to the persisted in-game economy.");
            rewardProfile.CurrencyBalances["money"] = int.MaxValue;
            SummoningEconomy.AwardMissionCompletion(rewardProfile, rewardMission);
            Require(rewardProfile.CurrencyBalances["money"] == int.MaxValue, "Mission reward balances must saturate instead of overflowing.");
        }

        static void VerifySpriteFontTextFallback()
        {
            string safe = SpriteFontText.ToSupportedGlyphs("↑ / ↓ SELECT · BACK — ← →");
            Require(safe == "^ / v SELECT | BACK - < >", "Menu symbols must be converted to glyphs supported by the bundled SpriteFont.");
            foreach (char character in safe)
                Require(character >= ' ' && character <= '~', "Every normalized SpriteFont character must be in the bundled ASCII glyph range.");
        }

        static void VerifyAbilityCatalogAndInitialUnlock()
        {
            var profile = new ProgressionProfile();
            var progression = new ProgressionService(profile);

            Require(AbilityCatalog.All.Count == 7, "The catalog must contain all seven core abilities.");
            Require(progression.IsUnlocked("scythe"), "Scythe must be unlocked for a new profile.");
            Require(progression.IsUsable("scythe"), "The starting Scythe must be usable.");

            string[] locked = { "shuriken", "kenjutsu", "hidden", "clone", "kunai", "rasengan" };
            foreach (string id in locked)
            {
                Require(!progression.IsUnlocked(id), id + " must start locked.");
                Require(!progression.IsUsable(id), id + " must not be usable while locked.");
            }

            Require(progression.IsUsable("clone") == false, "A locked but implemented Clone must still be unavailable.");
            Require(AbilityCatalog.Get("shuriken").IsImplemented, "Shuriken must have a functional gameplay implementation.");
            Require(AbilityCatalog.Get("kenjutsu").IsImplemented && AbilityCatalog.Get("hidden").IsImplemented && AbilityCatalog.Get("kunai").IsImplemented,
                "Every core ability shown in the progression menu must be implemented.");
            Require(AbilityCatalog.Get("clone").IsImplemented, "Shadow Clone must remain implemented.");
            Require(AbilityCatalog.Get("rasengan").IsImplemented, "Rasengan must remain implemented.");
            Require(AbilityCatalog.Get("clone").ResourceCost == 40, "Clone cost must be 40 soul energy.");
            Require(Math.Abs(AbilityCatalog.Get("clone").CooldownSeconds - 12f) < .001f, "Clone cooldown must be 12 seconds.");
            Require(AbilityCatalog.Get("rasengan").ResourceCost == 35, "Rasengan cost must be 35 soul energy.");
            Require(Math.Abs(AbilityCatalog.Get("rasengan").CooldownSeconds - 1.1f) < .001f, "Rasengan cooldown must be 1.1 seconds.");
            Require(AbilityCatalog.Get("unknown") == null, "Unknown ability IDs must return null.");
        }

        static void VerifyMilestoneUnlockOrder()
        {
            var progression = new ProgressionService(new ProgressionProfile());

            for (int i = 1; i <= 8; i++)
            {
                IReadOnlyList<string> newlyUnlocked = progression.RecordEnemyDefeat();
                Require(newlyUnlocked.Contains("shuriken") == (i == 3), "Shuriken must unlock exactly at defeat 3.");
                Require(newlyUnlocked.Contains("kenjutsu") == (i == 8), "Kenjutsu must unlock exactly at defeat 8, after Shuriken.");
            }

            IReadOnlyList<string> firstMissionUnlocks = progression.CompleteMission();
            Require(firstMissionUnlocks.Contains("hidden"), "Hidden Jutsu must unlock when mission 1 completes after Kenjutsu.");
            for (int i = 1; i <= 6; i++)
            {
                IReadOnlyList<string> newlyUnlocked = progression.RecordEnemyDefeat();
                Require(newlyUnlocked.Contains("clone") == (i == 6), "Shadow Clone must unlock at 6 defeats after mission 1.");
            }

            IReadOnlyList<string> secondMissionUnlocks = progression.CompleteMission();
            Require(secondMissionUnlocks.Contains("kunai"), "Teleportation Kunai must unlock after mission 2 and Clone.");
            for (int i = 1; i <= 5; i++)
            {
                IReadOnlyList<string> newlyUnlocked = progression.RecordEnemyDefeat();
                Require(newlyUnlocked.Contains("rasengan") == (i == 5), "Rasengan must unlock at 5 defeats after mission 2.");
            }

            foreach (string id in new[] { "scythe", "shuriken", "kenjutsu", "hidden", "clone", "kunai", "rasengan" })
                Require(progression.IsUnlocked(id), id + " should be unlocked after completing its milestones.");
            Require(progression.IsUsable("shuriken") && progression.IsUsable("kenjutsu") && progression.IsUsable("hidden") &&
                    progression.IsUsable("clone") && progression.IsUsable("kunai") && progression.IsUsable("rasengan"),
                "Every implemented ability must become usable after its progression milestones unlock it.");
        }

        static void VerifySameThresholdMissionAndKillOrder()
        {
            var missionBeforeKill = new ProgressionService(new ProgressionProfile());
            for (int i = 0; i < 7; i++) missionBeforeKill.RecordEnemyDefeat();
            IReadOnlyList<string> beforeKill = missionBeforeKill.CompleteMission();
            Require(!beforeKill.Contains("kenjutsu") && !beforeKill.Contains("hidden"),
                "Mission 1 must not bypass the eight-defeat Kenjutsu requirement.");
            IReadOnlyList<string> killAfterMission = missionBeforeKill.RecordEnemyDefeat();
            Require(killAfterMission.Count == 2 && killAfterMission[0] == "kenjutsu" && killAfterMission[1] == "hidden",
                "When defeat 8 follows mission 1, Kenjutsu must unlock before Hidden Jutsu.");

            var killBeforeMission = new ProgressionService(new ProgressionProfile());
            for (int i = 0; i < 7; i++) killBeforeMission.RecordEnemyDefeat();
            IReadOnlyList<string> eighthDefeat = killBeforeMission.RecordEnemyDefeat();
            Require(eighthDefeat.Count == 1 && eighthDefeat[0] == "kenjutsu",
                "Defeat 8 must unlock Kenjutsu without skipping to Hidden Jutsu before mission 1.");
            IReadOnlyList<string> missionAfterKill = killBeforeMission.CompleteMission();
            Require(missionAfterKill.Count == 1 && missionAfterKill[0] == "hidden",
                "When mission 1 follows defeat 8, Hidden Jutsu must unlock after Kenjutsu.");
        }

        static void VerifyTestModeGrantIsTemporary()
        {
            var profile = new ProgressionProfile();
            var normal = new ProgressionService(profile);
            var unlockedBefore = new List<string>(profile.UnlockedAbilityIds);
            var upgradesBefore = new Dictionary<string, int>(profile.UpgradeLevels);
            var testMode = new ProgressionService(profile, true);

#if DEBUG
            foreach (AbilityDefinition ability in AbilityCatalog.All)
            {
                bool shouldBeAvailable = ability.IsImplemented;
                Require(testMode.IsUnlocked(ability.Id) == shouldBeAvailable, ability.Id + " Test Mode unlock state is incorrect.");
                Require(testMode.IsUsable(ability.Id) == shouldBeAvailable, ability.Id + " Test Mode usability is incorrect.");
                if (ability.IsImplemented)
                    Require(testMode.GetUpgradeLevel(ability.Id) == ability.MaximumUpgradeLevel,
                        ability.Id + " must use its registered maximum upgrade in Test Mode.");
            }
#else
            Require(!testMode.IsUsable("clone") && !testMode.IsUsable("rasengan"),
                "Release builds must not activate the Debug-only Test Mode grants.");
#endif

            Require(!normal.IsUnlocked("clone") && !normal.IsUnlocked("rasengan"), "Test Mode must not grant abilities to the normal service.");
            Require(SameItems(unlockedBefore, profile.UnlockedAbilityIds), "Test Mode must not persist its grants.");
            Require(SameDictionary(upgradesBefore, profile.UpgradeLevels), "Test Mode must not persist maximum upgrades.");
        }

        static void VerifyNewGameReset()
        {
            var profile = new ProgressionProfile();
            var progression = new ProgressionService(profile);
            for (int i = 0; i < 8; i++) progression.RecordEnemyDefeat();
            progression.CompleteMission();
            for (int i = 0; i < 6; i++) progression.RecordEnemyDefeat();
            progression.CompleteMission();
            profile.OwnedContentIds.Add("eye.byakugan");
            profile.OwnedContentIds.Add("weapon.katana");
            profile.OwnedContentIds.Add("sage.toad");
            profile.OwnedContentIds.Add("summon.small-toad");
            profile.EquippedEyeId = "byakugan";
            profile.EquippedItemId = "katana";
            profile.EquippedWeaponId = "katana";
            profile.EquippedSageModeId = "sage.toad";
            profile.EquippedSummonId = "small-toad";
            profile.UpgradeLevels["clone"] = 2;
            profile.UpgradeLevels["summon.small-toad"] = 2;
            profile.CurrencyBalances["money"] = 150;
            profile.CurrencyBalances["diamonds"] = 30;
            profile.CurrencyBalances["shards"] = 20;

            progression.ResetNewGame();

            Require(profile.UnlockedLevel == 0 && profile.CompletedMissions == 0, "New Game must reset campaign progress.");
            Require(profile.TotalEnemyDefeats == 0 && profile.DefeatsSinceMission1 == 0 && profile.DefeatsSinceMission2 == 0,
                "New Game must reset defeat milestones.");
            Require(SameItems(new List<string> { "scythe" }, profile.UnlockedAbilityIds), "New Game must retain only the starting Scythe unlock.");
            Require(profile.OwnedContentIds.Count == 4 && profile.OwnedContentIds.Contains("summon.small-toad") && profile.OwnedContentIds.Contains("eye.byakugan") &&
                profile.EquippedEyeId == "byakugan" && profile.EquippedWeaponId == "katana" && profile.EquippedSageModeId == "sage.toad" && profile.EquippedSummonId == "small-toad",
                "New Game must preserve permanent shop purchases and equipment.");
            Require(profile.UpgradeLevels.Count == 2 && profile.UpgradeLevels["clone"] == 2 && profile.UpgradeLevels["summon.small-toad"] == 2 &&
                profile.CurrencyBalances["money"] == 150 && profile.CurrencyBalances["diamonds"] == 30 && profile.CurrencyBalances["shards"] == 20,
                "New Game must preserve permanent upgrades and all wallet balances.");
        }

        static void VerifyLegacySaveMigrationPreservesOldFile()
        {
            RunWithTempDirectory(root =>
            {
                string[] levels = { "-7", "1", "99" };
                int[] expectedLevels = { 0, 1, 2 };
                for (int i = 0; i < levels.Length; i++)
                {
                    string caseRoot = Path.Combine(root, "legacy-" + i);
                    Directory.CreateDirectory(caseRoot);
                    string profilePath = Path.Combine(caseRoot, "Kadaya", "save.json");
                    string legacyPath = Path.Combine(caseRoot, "Kadaya", "save.txt");
                    Directory.CreateDirectory(Path.GetDirectoryName(legacyPath));
                    byte[] original = Encoding.UTF8.GetBytes(levels[i]);
                    File.WriteAllBytes(legacyPath, original);

                    ProgressionProfile migrated = ProgressionStore.Load(profilePath, legacyPath);

                    Require(migrated.UnlockedLevel == expectedLevels[i], "Legacy level must clamp to the supported 0-2 range.");
                    Require(migrated.CompletedMissions == expectedLevels[i], "Legacy level must infer completed missions.");
                    Require(original.SequenceEqual(File.ReadAllBytes(legacyPath)), "Migration must leave save.txt byte-for-byte unchanged.");
                    Require(File.Exists(profilePath), "A valid legacy save must produce the new JSON profile.");
                }

                string invalidRoot = Path.Combine(root, "invalid-legacy");
                Directory.CreateDirectory(invalidRoot);
                string invalidProfilePath = Path.Combine(invalidRoot, "Kadaya", "save.json");
                string invalidLegacyPath = Path.Combine(invalidRoot, "Kadaya", "save.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(invalidLegacyPath));
                byte[] invalidContent = Encoding.UTF8.GetBytes("not-a-level");
                File.WriteAllBytes(invalidLegacyPath, invalidContent);
                ProgressionProfile fresh = ProgressionStore.Load(invalidProfilePath, invalidLegacyPath);
                Require(fresh.UnlockedLevel == 0 && fresh.CompletedMissions == 0, "Invalid legacy content must start a fresh profile.");
                Require(invalidContent.SequenceEqual(File.ReadAllBytes(invalidLegacyPath)), "Invalid legacy content must remain untouched.");

                string precedenceRoot = Path.Combine(root, "json-precedence");
                Directory.CreateDirectory(precedenceRoot);
                string precedenceProfilePath = Path.Combine(precedenceRoot, "Kadaya", "save.json");
                string precedenceLegacyPath = Path.Combine(precedenceRoot, "Kadaya", "save.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(precedenceLegacyPath));
                File.WriteAllText(precedenceLegacyPath, "2");
                var jsonProfile = new ProgressionProfile { TotalEnemyDefeats = 17 };
                ProgressionStore.Save(jsonProfile, precedenceProfilePath);
                ProgressionProfile selected = ProgressionStore.Load(precedenceProfilePath, precedenceLegacyPath);
                Require(selected.TotalEnemyDefeats == 17 && selected.UnlockedLevel == 0,
                    "An existing JSON profile must take precedence over save.txt.");
            });
        }

        static void VerifyProfileJsonRoundTrip()
        {
            RunWithTempDirectory(root =>
            {
                string profilePath = Path.Combine(root, "Kadaya", "save.json");
                var original = new ProgressionProfile
                {
                    UnlockedLevel = 2,
                    CompletedMissions = 1,
                    TotalEnemyDefeats = 19,
                    DefeatsSinceMission1 = 6,
                    DefeatsSinceMission2 = 0,
                    CompletedMissionIds = new List<string> { "blood-moon-forest-01", "haunted-graveyard-03" },
                    BestMissionTimes = new Dictionary<string, float> { ["blood-moon-forest-01"] = 93.5f },
                    EquippedEyeId = "eye.sharingan",
                    EquippedItemId = "shop.kunai"
                };
                original.UnlockedAbilityIds.Add("clone");
                original.OwnedContentIds.Add("eye.sharingan");
                original.UpgradeLevels["clone"] = 3;
                original.CurrencyBalances["shards"] = 24;

                ProgressionStore.Save(original, profilePath);
                ProgressionProfile loaded = ProgressionStore.Load(profilePath, Path.Combine(root, "missing", "save.txt"));

                Require(loaded.SchemaVersion == original.SchemaVersion, "JSON round-trip must preserve schema version.");
                Require(loaded.UnlockedLevel == 2 && loaded.CompletedMissions == 1 && loaded.TotalEnemyDefeats == 19,
                    "JSON round-trip must preserve campaign and defeat progress.");
                Require(loaded.DefeatsSinceMission1 == 6 && loaded.DefeatsSinceMission2 == 0,
                    "JSON round-trip must preserve post-mission defeat counters.");
                Require(loaded.CompletedMissionIds.Contains("haunted-graveyard-03") &&
                    Math.Abs(loaded.BestMissionTimes["blood-moon-forest-01"] - 93.5f) < .001f,
                    "JSON round-trip must preserve stable mission completion IDs and best times.");
                Require(SameItems(original.UnlockedAbilityIds, loaded.UnlockedAbilityIds), "JSON round-trip must preserve unlocked IDs.");
                Require(SameItems(original.OwnedContentIds, loaded.OwnedContentIds), "JSON round-trip must preserve owned content.");
                Require(loaded.EquippedEyeId == "eye.sharingan" && loaded.EquippedItemId == "shop.kunai",
                    "JSON round-trip must preserve equipped IDs.");
                Require(SameDictionary(original.UpgradeLevels, loaded.UpgradeLevels), "JSON round-trip must preserve upgrades.");
                Require(SameDictionary(original.CurrencyBalances, loaded.CurrencyBalances), "JSON round-trip must preserve currencies.");
                Require(!loaded.IsReadOnlyRecovery, "The recovery-only save guard must not be persisted in valid profiles.");
            });
        }

        static void VerifyUnreadableProfileIsProtected()
        {
            RunWithTempDirectory(root =>
            {
                string profilePath = Path.Combine(root, "Kadaya", "save.json");
                Directory.CreateDirectory(Path.GetDirectoryName(profilePath));
                byte[] damagedBytes = Encoding.UTF8.GetBytes("{ this is not valid JSON");
                File.WriteAllBytes(profilePath, damagedBytes);

                ProgressionProfile recovered = ProgressionStore.Load(profilePath, null);
                Require(recovered.IsReadOnlyRecovery, "An unreadable existing profile must be marked read-only to protect its source data.");
                Require(damagedBytes.SequenceEqual(File.ReadAllBytes(profilePath)), "Loading an unreadable profile must not alter its source bytes.");

                bool saveRejected = false;
                try { ProgressionStore.Save(recovered, profilePath); }
                catch (InvalidOperationException) { saveRejected = true; }
                Require(saveRejected, "A recovered read-only profile must not overwrite the unreadable source file.");
                Require(damagedBytes.SequenceEqual(File.ReadAllBytes(profilePath)), "A rejected recovery save must leave the unreadable source bytes unchanged.");
            });
        }

        static void VerifyNormalAndTestPathsAreSeparate()
        {
            string root = Path.Combine("app-data", "root");
            string normalPath = ProgressionStore.GetNormalProfilePath(root);
            string testPath = ProgressionStore.GetTestProfilePath(root);
            string legacyPath = ProgressionStore.GetLegacySavePath(root);
            Require(normalPath == Path.Combine(root, "Kadaya", "save.json"), "Normal profile path must use Kadaya/save.json.");
            Require(testPath == Path.Combine(root, "Kadaya", "test-save.json"), "Test profile path must use Kadaya/test-save.json.");
            Require(legacyPath == Path.Combine(root, "Kadaya", "save.txt"), "Legacy profile path must use Kadaya/save.txt.");
            Require(!string.Equals(normalPath, testPath, StringComparison.OrdinalIgnoreCase), "Normal and test profile paths must differ.");
        }

        static void VerifyNormalAndTestProfileSaveIsolation()
        {
            RunWithTempDirectory(root =>
            {
                string normalPath = ProgressionStore.GetNormalProfilePath(root);
                string testPath = ProgressionStore.GetTestProfilePath(root);
                var normal = new ProgressionProfile { TotalEnemyDefeats = 4 };
                normal.CurrencyBalances["shards"] = 12;
                ProgressionStore.Save(normal, normalPath);

                var test = new ProgressionProfile();
                var testProgression = new ProgressionService(test);
                for (int i = 0; i < 3; i++) testProgression.RecordEnemyDefeat();
                var testMode = new ProgressionService(test, true);
#if DEBUG
                Require(testMode.IsUsable("clone") && testMode.GetUpgradeLevel("clone") == AbilityCatalog.Get("clone").MaximumUpgradeLevel,
                    "Test Mode must provide temporary implemented-ability grants in the test profile.");
#else
                Require(!testMode.IsUsable("clone"), "Release builds must not enable Debug-only Test Mode grants.");
#endif
                ProgressionStore.Save(test, testPath);

                ProgressionProfile loadedNormal = ProgressionStore.Load(normalPath, null);
                ProgressionProfile loadedTest = ProgressionStore.Load(testPath, null);
                Require(loadedNormal.TotalEnemyDefeats == 4 && loadedNormal.CurrencyBalances["shards"] == 12,
                    "Saving Test Mode progression must leave normal profile data unchanged.");
                Require(loadedTest.TotalEnemyDefeats == 3 && loadedTest.UnlockedAbilityIds.Contains("shuriken"),
                    "Test profile progression must persist independently at the test path.");
                Require(!loadedTest.UnlockedAbilityIds.Contains("clone") && !loadedTest.UpgradeLevels.ContainsKey("clone"),
                    "Temporary Test Mode grants and maximum upgrades must not be written into the test profile.");
            });
        }

        static void VerifySummoningCatalogAndOwnership()
        {
            var profile = new ProgressionProfile();
            var summoning = new SummoningService(profile);
            Require(SummoningCatalog.All.Count == 29, "The summon catalog must contain the standard and legendary summons.");
            var ids = new List<string>();
            foreach (SummoningDefinition definition in SummoningCatalog.All)
            {
                Require(!ids.Contains(definition.Id), "Summon IDs must be unique: " + definition.Id);
                ids.Add(definition.Id);
                Require(!summoning.IsOwned(definition.Id), definition.Name + " must start locked in normal gameplay.");
            }
            Require(SummoningCatalog.Get("healing-slug").HealingPerSecond > 0 && SummoningCatalog.Get("shadow-emperor").SpecialDamage > 0,
                "Support and signature summons must carry distinct role stats.");

            string reason;
            SummoningDefinition first = SummoningCatalog.All[0];
            Require(!summoning.TryPurchase(first.Id, out reason), "A summon must not be purchasable without enough Diamonds.");
            profile.CurrencyBalances["diamonds"] = first.DiamondPrice;
            Require(summoning.TryPurchase(first.Id, out reason), "A summon must be purchasable with its configured Diamond price.");
            Require(summoning.IsOwned(first.Id) && profile.CurrencyBalances["diamonds"] == 0,
                "Purchasing must permanently record ownership and debit Diamonds.");
            Require(!summoning.TryPurchase(first.Id, out reason), "Permanent summon purchases must reject duplicates.");
            Require(summoning.TryEquip(first.Id, out reason) && profile.EquippedSummonId == first.Id,
                "Owned summons must be equippable and persist the equipped ID.");

            RunWithTempDirectory(root =>
            {
                string savePath = Path.Combine(root, "Kadaya", "save.json");
                ProgressionStore.Save(profile, savePath);
                ProgressionProfile loaded = ProgressionStore.Load(savePath, null);
                Require(loaded.OwnedContentIds.Contains("summon." + first.Id) && loaded.EquippedSummonId == first.Id,
                    "Purchased and equipped summoning data must round-trip through the profile save.");
            });
        }

        static void VerifySummonUpgradesUseShards()
        {
            var profile = new ProgressionProfile();
            var summoning = new SummoningService(profile);
            string reason;
            SummoningDefinition definition = SummoningCatalog.Get("small-toad");
            Require(!summoning.TryUpgrade(definition.Id, out reason), "Summons must be unlocked before their shard upgrades.");
            profile.CurrencyBalances["diamonds"] = definition.DiamondPrice;
            Require(summoning.TryPurchase(definition.Id, out reason), "Upgrade test setup must purchase the summon.");
            Require(!summoning.TryUpgrade(definition.Id, out reason), "Summon upgrades must require Shards.");
            profile.CurrencyBalances["shards"] = 100;
            Require(summoning.TryUpgrade(definition.Id, out reason), "An owned summon must upgrade with Shards.");
            Require(summoning.GetUpgradeLevel(definition.Id) == 1 && profile.CurrencyBalances["shards"] == 92,
                "The first summon upgrade must spend its configured Shard cost and persist its level.");
            Require(summoning.GetStats(definition.Id).DamageMultiplier > 1,
                "Summon upgrades must improve combat statistics.");
        }

        static void VerifyTestModeSummoningGrantsAreTemporary()
        {
            var profile = new ProgressionProfile();
            var testMode = new SummoningService(profile, true);
            foreach (SummoningDefinition definition in SummoningCatalog.All)
            {
                Require(testMode.IsOwned(definition.Id), definition.Name + " must be available in Test Mode.");
                Require(testMode.GetUpgradeLevel(definition.Id) == definition.MaximumUpgradeLevel,
                    definition.Name + " must be max-upgraded in Test Mode.");
            }
            Require(profile.OwnedContentIds.Count == 0 && profile.UpgradeLevels.Count == 0,
                "Test Mode must not write test ownership or upgrades into the normal profile.");
            Require(testMode.GetStats("shadow-emperor").SpecialDamageMultiplier > 1,
                "Legendary Test Mode summons must receive maximum special attack upgrades.");
        }

        static void VerifySummoningEconomyRewardsPersist()
        {
            RunWithTempDirectory(root =>
            {
                var profile = new ProgressionProfile();
                SummoningEconomy.AwardEnemyDefeat(profile, false);
                SummoningEconomy.AwardEnemyDefeat(profile, true);
                Require(profile.CurrencyBalances["diamonds"] == SummoningEconomy.EnemyDiamonds + SummoningEconomy.BossDiamonds,
                    "Enemy and boss defeats must award the configured Diamond amounts.");
                Require(profile.CurrencyBalances["shards"] == SummoningEconomy.EnemyShards + SummoningEconomy.BossShards,
                    "Enemy and boss defeats must award the configured Shard amounts.");
                Require(profile.CurrencyBalances["money"] == SummoningEconomy.EnemyMoney + SummoningEconomy.BossMoney,
                    "Enemy and boss defeats must award the configured Money amounts.");
                string savePath = Path.Combine(root, "Kadaya", "save.json");
                ProgressionStore.Save(profile, savePath);
                ProgressionProfile loaded = ProgressionStore.Load(savePath, null);
                Require(SameDictionary(profile.CurrencyBalances, loaded.CurrencyBalances),
                    "Earned Diamonds and Shards must persist through the existing profile save.");
            });
        }

        static void VerifyShopAndEyeOwnership()
        {
            var profile = new ProgressionProfile();
            var shop = new ShopService(profile);
            string[] eyeIds = { "sharingan", "mangekyo", "byakugan", "kakashi", "ems", "jougan", "rinnegan" };
            foreach (string id in eyeIds)
                Require(!shop.IsOwned("eye." + id), id + " must be locked until a normal player purchases it.");

            string reason;
            ShopItem katana = ShopCatalog.Get("weapon.katana");
            Require(!shop.TryPurchase(katana.Id, out reason), "Money shop items must reject an insufficient balance.");
            profile.CurrencyBalances["money"] = katana.Price;
            Require(shop.TryPurchase(katana.Id, out reason) && profile.EquippedWeaponId == "katana",
                "A purchased weapon must debit Money and equip through the existing profile.");
            Require(profile.CurrencyBalances["money"] == 0 && shop.IsOwned(katana.Id), "Weapon ownership and Money debit must persist in profile state.");

            ShopItem byakugan = ShopCatalog.Get("eye.byakugan");
            Require(PremiumShopCatalog.Get("weapon.katana") == katana && new PremiumShopService(profile).IsOwned(katana.Id),
                "The original public shop type names must remain available as compatibility aliases.");
            profile.CurrencyBalances["diamonds"] = byakugan.Price;
            Require(shop.TryPurchase(byakugan.Id, out reason) && profile.EquippedEyeId == "byakugan",
                "Eyes must unlock and equip through the earned-Diamonds shop.");
            Require(profile.CurrencyBalances["diamonds"] == 0 && shop.TryEquip("eye.byakugan", out reason),
                "Purchased eyes must remain owned and re-equippable.");

            RunWithTempDirectory(root =>
            {
                string path = Path.Combine(root, "Kadaya", "save.json");
                ProgressionStore.Save(profile, path);
                ProgressionProfile loaded = ProgressionStore.Load(path, null);
                Require(loaded.OwnedContentIds.Contains("eye.byakugan") && loaded.EquippedEyeId == "byakugan" && loaded.EquippedWeaponId == "katana",
                    "Shop ownership and equipped eye/weapon IDs must survive save and reload.");
            });

#if DEBUG
            var testProfile = new ProgressionProfile();
            var testShop = new ShopService(testProfile, true);
            foreach (ShopItem item in ShopCatalog.All)
                Require(testShop.IsOwned(item.Id), "Test Mode must provide immediate access to " + item.Id + ".");
            Require(testShop.GetSusanooLevel() == 9 && testShop.GetSusanooDuration() == 12f,
                "Test Mode must grant maximum Susanoo duration without normal purchases.");
            Require(testProfile.OwnedContentIds.Count == 0, "Test Mode shop access must not write test ownership into the profile.");
#endif
        }

        static void VerifyAbilityAndSusanooShardUpgrades()
        {
            var profile = new ProgressionProfile();
            var progression = new ProgressionService(profile);
            string reason;
            Require(!progression.TryUpgrade("shuriken", out reason), "Locked abilities must not be upgradeable.");
            for (int i = 0; i < 3; i++) progression.RecordEnemyDefeat();
            profile.CurrencyBalances["shards"] = progression.GetUpgradeShardCost("shuriken");
            int balance = profile.CurrencyBalances["shards"];
            Require(progression.TryUpgrade("shuriken", out reason) && progression.GetUpgradeLevel("shuriken") == 1 && profile.CurrencyBalances["shards"] == 0,
                "Unlocked abilities must consume Shards to upgrade.");

            var shop = new ShopService(profile);
            Require(!shop.TryUpgradeSusanoo(out reason), "Susanoo duration must require the Eternal Mangekyo eye.");
            ShopItem ems = ShopCatalog.Get("eye.ems");
            profile.CurrencyBalances["diamonds"] = ems.Price;
            Require(shop.TryPurchase(ems.Id, out reason) && Math.Abs(shop.GetSusanooDuration() - 3f) < .001f,
                "Susanoo must begin with its three-second base duration.");
            profile.CurrencyBalances["shards"] = 1000;
            for (int i = 0; i < 9; i++) Require(shop.TryUpgradeSusanoo(out reason), "Susanoo duration upgrade " + (i + 1) + " must spend Shards.");
            Require(shop.GetSusanooLevel() == 9 && Math.Abs(shop.GetSusanooDuration() - 12f) < .001f,
                "Susanoo duration must cap at twelve seconds after nine upgrades.");
            Require(!shop.TryUpgradeSusanoo(out reason) && shop.GetSusanooDuration() <= 12f,
                "Susanoo upgrades must stop at the twelve-second maximum.");
            Require(balance > 0, "Ability upgrades must expose a configured Shard cost.");
        }

        static void VerifyDevelopmentTestModeSettings()
        {
            RunWithTempDirectory(root =>
            {
                string settingsPath = DevelopmentSettings.GetPath(root);
                Require(settingsPath == Path.Combine(root, "Kadaya", "development.json"),
                    "Development settings must use Kadaya/development.json.");
                Require(!DevelopmentSettings.Load(settingsPath), "Missing settings must disable Test Mode.");

                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                File.WriteAllText(settingsPath, "not-json");
                Require(!DevelopmentSettings.Load(settingsPath), "Invalid settings must disable Test Mode.");

                File.WriteAllText(settingsPath, "{\"development_test_mode\":true}");
                Require(DevelopmentSettings.Load(settingsPath), "The true development_test_mode setting must enable Test Mode.");
                File.WriteAllText(settingsPath, "{\"development_test_mode\":false}");
                Require(!DevelopmentSettings.Load(settingsPath), "The false development_test_mode setting must disable Test Mode.");
            });
        }

        static void VerifyAbilityMenuRows()
        {
            var profile = new ProgressionProfile();
            profile.UpgradeLevels["scythe"] = 2;
            var normal = new ProgressionService(profile);
            IReadOnlyList<AbilityMenuRow> rows = AbilityMenuModel.Build(normal);
            Require(rows.Count == 7, "The ability menu must include all seven core entries.");

            AbilityMenuRow scythe = FindMenuRow(rows, "scythe");
            AbilityMenuRow shuriken = FindMenuRow(rows, "shuriken");
            AbilityMenuRow kenjutsu = FindMenuRow(rows, "kenjutsu");
            AbilityMenuRow hidden = FindMenuRow(rows, "hidden");
            AbilityMenuRow clone = FindMenuRow(rows, "clone");
            AbilityMenuRow kunai = FindMenuRow(rows, "kunai");
            AbilityMenuRow rasengan = FindMenuRow(rows, "rasengan");

            Require(scythe.Definition.Name == "Scythe Combo" && scythe.Definition.Binding == "LMB", "Scythe name and binding must be shown.");
            Require(shuriken.Definition.Binding == "Q" && kenjutsu.Definition.Binding == "F" && hidden.Definition.Binding == "C",
                "Later-stage keyboard bindings must come from the catalog.");
            Require(clone.Definition.Binding == "E" && kunai.Definition.Binding == "R" && rasengan.Definition.Binding == "RMB",
                "Clone, Kunai, and Rasengan bindings must come from the catalog.");
            Require(Math.Abs(clone.Definition.CooldownSeconds - 12f) < .001f && clone.Definition.ResourceCost == 40,
                "Clone cooldown and cost must be available to the menu.");
            Require(Math.Abs(rasengan.Definition.CooldownSeconds - 1.1f) < .001f && rasengan.Definition.ResourceCost == 35,
                "Rasengan cooldown and cost must be available to the menu.");
            Require(scythe.UpgradeLevel == 2, "The menu must show the current upgrade level.");
            Require(scythe.Status == "Available", "An unlocked implemented ability must be available.");
            Require(clone.Status == "Locked", "An unavailable implemented ability must be labeled Locked.");
            Require(shuriken.Status == "Locked", "An implemented but unavailable entry must be labeled Locked.");
            Require(shuriken.RequirementText.Contains("3 enemies"), "Shuriken must show its defeat requirement.");
            Require(kenjutsu.RequirementText.Contains("8 enemies") && kenjutsu.RequirementText.Contains("Shuriken"),
                "Kenjutsu must show its prerequisite and defeat requirement.");
            Require(hidden.RequirementText.Contains("mission 1") && clone.RequirementText.Contains("6 enemies after mission 1"),
                "Mission 1 and Clone requirements must be visible.");
            Require(kunai.RequirementText.Contains("mission 2") && rasengan.RequirementText.Contains("5 enemies after mission 2"),
                "Mission 2 and Rasengan requirements must be visible.");

            IReadOnlyList<AbilityMenuRow> testRows = AbilityMenuModel.Build(new ProgressionService(new ProgressionProfile(), true));
#if DEBUG
            Require(FindMenuRow(testRows, "clone").Status == "Available" && FindMenuRow(testRows, "rasengan").Status == "Available",
                "Test Mode must show implemented abilities as Available.");
            Require(FindMenuRow(testRows, "clone").UpgradeLevel == clone.Definition.MaximumUpgradeLevel,
                "Test Mode must show the maximum registered upgrade level.");
#else
            Require(FindMenuRow(testRows, "clone").Status == "Locked" && FindMenuRow(testRows, "rasengan").Status == "Locked",
                "Release builds must keep Debug-only Test Mode abilities locked until normal progression unlocks them.");
#endif
        }

        static void VerifyShopMenuModel()
        {
            Require(ShopMenuModel.Build(ShopCategory.Eyes).Count == 7, "The redesigned shop must list all seven implemented eye powers.");
            Require(ShopMenuModel.Build(ShopCategory.Swords).Count == 4, "The sword category must include the starting Scythe and all three catalog weapons.");
            Require(ShopMenuModel.Build(ShopCategory.Susano).Count == 2, "The Susano category must list the two implemented aura variants.");
            Require(ShopMenuModel.Build(ShopCategory.Special).Count == 10, "Special must include all seven core abilities and three Sage Modes.");
            Require(ShopMenuModel.Build(ShopCategory.Summons).Count == 29, "The summon category must include every implemented summon definition.");
            Require(ShopMenuModel.Build(ShopCategory.Special)[0].Kind == ShopMenuEntryKind.Ability &&
                ShopMenuModel.Build(ShopCategory.Summons)[28].Summon.Id == "shadow-emperor",
                "Shop entries must preserve their actual gameplay catalog records.");
        }

        static void VerifyGameSettingsPersistenceAndBindings()
        {
            var settings = new GameSettings();
            Require(settings.TrySetBinding("MoveForward", "H"), "A valid unused key must be assignable.");
            Require(!settings.TrySetBinding("MoveLeft", "H"), "A key already used by another action must be rejected.");
            Require(!settings.TrySetBinding("MoveLeft", "F11"), "A reserved system key must not be assignable.");
            Require(!settings.TrySetBinding("UnknownAction", "J"), "Unknown actions must not be persisted as controls.");

            settings.MasterVolume = 1.4f;
            settings.MusicVolume = .35f;
            settings.HudScale = 1.2f;
            settings.HudOffsetX = 35;
            settings.Muted = true;
            settings.Fullscreen = true;
            settings.GraphicsQuality = 2;
            settings.Normalize();
            Require(Math.Abs(settings.MasterVolume - 1f) < .001f, "Settings values must clamp to their valid range.");
            Require(settings.Muted && settings.Fullscreen && settings.GraphicsQuality == 2, "Boolean and graphics preferences must remain selectable.");

            RunWithTempDirectory(root =>
            {
                string path = GameSettingsStore.GetPath(root);
                GameSettingsStore.Save(settings, path);
                GameSettings loaded = GameSettingsStore.Load(path);
                Require(loaded.Muted && loaded.Fullscreen && Math.Abs(loaded.MusicVolume - .35f) < .001f,
                    "Audio and display settings must persist in their independent settings file.");
                Require(loaded.GetBinding("MoveForward") == "H" && loaded.HudOffsetX == 35 && Math.Abs(loaded.HudScale - 1.2f) < .001f,
                    "Key bindings and HUD preferences must persist.");
            });
        }

        static AbilityMenuRow FindMenuRow(IReadOnlyList<AbilityMenuRow> rows, string id)
        {
            foreach (AbilityMenuRow row in rows)
                if (row.Definition.Id == id) return row;
            throw new Exception("Ability menu row not found: " + id);
        }

        static void RunWithTempDirectory(Action<string> action)
        {
            string root = Path.Combine(Environment.CurrentDirectory, ".progression-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                action(root);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        static bool SameItems<T>(ICollection<T> expected, ICollection<T> actual)
        {
            if (expected.Count != actual.Count) return false;
            foreach (T value in expected)
                if (!actual.Contains(value)) return false;
            return true;
        }

        static bool SameDictionary(IDictionary<string, int> expected, IDictionary<string, int> actual)
        {
            if (expected.Count != actual.Count) return false;
            foreach (KeyValuePair<string, int> value in expected)
                if (!actual.TryGetValue(value.Key, out int actualValue) || actualValue != value.Value) return false;
            return true;
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
