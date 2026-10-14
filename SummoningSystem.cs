using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kadaya
{
    public enum SummonCategory
    {
        Toad,
        Dog,
        Bird,
        Snake,
        Slug,
        Turtle,
        Wolf,
        Panther,
        Dragon,
        Shadow,
        Legendary
    }

    public enum SummonRole
    {
        Combat,
        Tank,
        Scout,
        Healer,
        Ambush,
        Legendary
    }

    public sealed class SummoningDefinition
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public SummonCategory Category { get; private set; }
        public SummonRole Role { get; private set; }
        public string Description { get; private set; }
        public int DiamondPrice { get; private set; }
        public int EnergyCost { get; private set; }
        public float CooldownSeconds { get; private set; }
        public float DurationSeconds { get; private set; }
        public float MaximumHealth { get; private set; }
        public float Damage { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackInterval { get; private set; }
        public float MoveSpeed { get; private set; }
        public float Size { get; private set; }
        public float HealingPerSecond { get; private set; }
        public float SpecialDamage { get; private set; }
        public int PrimaryRgb { get; private set; }
        public int AccentRgb { get; private set; }
        public int MaximumUpgradeLevel { get; private set; }
        public int BaseUpgradeShardCost { get; private set; }

        internal SummoningDefinition(string id, string name, SummonCategory category, SummonRole role,
            string description, int diamondPrice, float duration, float cooldown, float health, float damage,
            float range, float interval, float speed, float size, float healing, float specialDamage,
            int primaryRgb, int accentRgb, int energyCost, int maximumUpgradeLevel, int baseUpgradeShardCost)
        {
            Id = id; Name = name; Category = category; Role = role; Description = description;
            DiamondPrice = diamondPrice; EnergyCost = Math.Max(0, energyCost);
            DurationSeconds = duration; CooldownSeconds = cooldown; MaximumHealth = health; Damage = damage;
            AttackRange = range; AttackInterval = interval; MoveSpeed = speed; Size = size;
            HealingPerSecond = healing; SpecialDamage = specialDamage; PrimaryRgb = primaryRgb; AccentRgb = accentRgb;
            MaximumUpgradeLevel = Math.Max(0, maximumUpgradeLevel); BaseUpgradeShardCost = Math.Max(0, baseUpgradeShardCost);
        }
    }

    public sealed class SummonUpgradeStats
    {
        public int Level { get; private set; }
        public float DurationMultiplier { get; private set; }
        public float HealthMultiplier { get; private set; }
        public float DamageMultiplier { get; private set; }
        public float SpeedMultiplier { get; private set; }
        public float CooldownMultiplier { get; private set; }
        public float HealingMultiplier { get; private set; }
        public float SpecialDamageMultiplier { get; private set; }

        internal SummonUpgradeStats(int level)
        {
            Level = Math.Max(0, level);
            DurationMultiplier = 1f + Level * .08f;
            HealthMultiplier = 1f + Level * .2f;
            DamageMultiplier = 1f + Level * .15f;
            SpeedMultiplier = 1f + Level * .1f;
            CooldownMultiplier = Math.Max(.55f, 1f - Level * .08f);
            HealingMultiplier = 1f + Level * .18f;
            SpecialDamageMultiplier = 1f + Level * .2f;
        }
    }

    public static class SummoningCatalog
    {
        private static readonly ReadOnlyCollection<SummoningDefinition> definitions =
            new ReadOnlyCollection<SummoningDefinition>(new List<SummoningDefinition>
            {
                Make("small-toad", "Small Toad", SummonCategory.Toad, SummonRole.Combat, "Quick close-range tongue strike.", 25, 18, 28, 80, 12, 110, 1.15f, 330, .72f, 0, 28, 0x517D4B, 0xB6D76A),
                Make("giant-battle-toad", "Giant Battle Toad", SummonCategory.Toad, SummonRole.Tank, "Heavy leap, knockback, and partial player protection.", 60, 22, 36, 220, 24, 145, 1.6f, 260, 1.55f, 0, 50, 0x397A59, 0xD39A55),
                Make("armored-toad", "Armored Toad", SummonCategory.Toad, SummonRole.Tank, "Armored body absorbs more incoming damage.", 50, 24, 34, 280, 18, 125, 1.45f, 230, 1.42f, 0, 40, 0x53636C, 0xC7B47A),
                Make("legendary-toad", "Legendary Toad", SummonCategory.Toad, SummonRole.Legendary, "A powerful leap followed by a chakra shockwave.", 120, 28, 45, 360, 36, 190, 1.55f, 310, 1.85f, 0, 90, 0x437C51, 0xF3C66A),
                Make("toad-boss", "Toad Boss Summon", SummonCategory.Toad, SummonRole.Legendary, "Colossal guard with a broad ground slam.", 180, 30, 52, 520, 48, 230, 1.8f, 240, 2.2f, 0, 125, 0x3A6847, 0xDF9F4B),
                Make("tracking-dog", "Ninja Tracking Dog", SummonCategory.Dog, SummonRole.Scout, "Fast tracker that seeks and interrupts enemies.", 20, 20, 28, 72, 14, 105, .85f, 470, .65f, 0, 24, 0x8B654B, 0xE5D5A7),
                Make("ninja-dog-pack", "Pack of Ninja Dogs", SummonCategory.Dog, SummonRole.Combat, "Two coordinated hounds strike the same target.", 45, 22, 34, 135, 11, 125, .9f, 420, .9f, 0, 42, 0x74604E, 0xD2C2A6),
                Make("armored-combat-dog", "Armored Combat Dog", SummonCategory.Dog, SummonRole.Tank, "Armored hound holds the line and staggers foes.", 55, 24, 36, 190, 22, 120, 1.25f, 330, .95f, 0, 42, 0x555563, 0xD77B54),
                Make("elite-hound", "Elite Summoning Hound", SummonCategory.Dog, SummonRole.Ambush, "Elite pounce can briefly pin ordinary enemies.", 110, 26, 42, 230, 34, 175, 1.35f, 440, 1.12f, 0, 68, 0x393C52, 0xD7D7EB),
                Make("scout-bird", "Scout Bird", SummonCategory.Bird, SummonRole.Scout, "Reveals nearby enemies and makes fast dive strikes.", 20, 22, 27, 70, 12, 140, .9f, 450, .72f, 0, 26, 0x527C91, 0xC6E8F4),
                Make("giant-combat-bird", "Giant Combat Bird", SummonCategory.Bird, SummonRole.Combat, "Aerial dive attacks hit hard and knock targets back.", 65, 24, 38, 190, 28, 165, 1.25f, 390, 1.35f, 0, 56, 0x475C86, 0xE4A44F),
                Make("eagle-summon", "Eagle Summon", SummonCategory.Bird, SummonRole.Ambush, "Swift eagle dives repeatedly at isolated enemies.", 55, 22, 34, 145, 22, 155, .9f, 510, 1.03f, 0, 48, 0x795E40, 0xF0D28A),
                Make("legendary-sky-bird", "Legendary Sky Bird", SummonCategory.Bird, SummonRole.Legendary, "Sky-borne dive and wide chakra gust.", 130, 28, 46, 285, 42, 210, 1.35f, 470, 1.85f, 0, 100, 0x355F9A, 0xC9F4FF),
                Make("combat-snake", "Small Combat Snake", SummonCategory.Snake, SummonRole.Ambush, "Rapid bite and short poison damage-over-time.", 20, 18, 28, 78, 10, 95, .62f, 390, .58f, 0, 22, 0x56804A, 0xB7D86A),
                Make("giant-snake", "Giant Snake", SummonCategory.Snake, SummonRole.Combat, "Heavy bite constricts and briefly immobilizes foes.", 70, 25, 40, 255, 32, 150, 1.3f, 300, 1.5f, 0, 64, 0x4B7651, 0xCBBF70),
                Make("armored-snake", "Armored Snake", SummonCategory.Snake, SummonRole.Tank, "Scale armor and a punishing coil strike.", 58, 26, 38, 310, 24, 135, 1.4f, 260, 1.4f, 0, 52, 0x555B50, 0xB9A77A),
                Make("legendary-serpent", "Legendary Serpent", SummonCategory.Snake, SummonRole.Legendary, "Ancient serpent sweeps a broad area with venom.", 130, 30, 48, 390, 44, 205, 1.4f, 360, 2f, 0, 108, 0x3E704C, 0xE0D274),
                Make("giant-boss-serpent", "Giant Boss Serpent", SummonCategory.Snake, SummonRole.Legendary, "Boss-scale serpent coils through the enemy line.", 190, 32, 55, 560, 55, 245, 1.65f, 310, 2.3f, 0, 145, 0x285942, 0xD4C26A),
                Make("healing-slug", "Healing Slug", SummonCategory.Slug, SummonRole.Healer, "Restores health over time and grants light protection.", 45, 24, 36, 150, 0, 0, 1, 390, .88f, 5.5f, 0, 0xB4D8D2, 0xEAF9E7),
                Make("giant-turtle", "Giant Turtle Summon", SummonCategory.Turtle, SummonRole.Tank, "Slow guardian reduces damage and shield-bashes nearby foes.", 100, 28, 45, 480, 27, 165, 1.65f, 205, 1.75f, 0, 58, 0x527D5B, 0xC3A66C),
                Make("wolf-pack", "Wolf Pack Summon", SummonCategory.Wolf, SummonRole.Combat, "Two wolves flank and coordinate quick attacks.", 50, 22, 34, 155, 13, 115, .72f, 480, .96f, 0, 36, 0x626573, 0xC4D4E8),
                Make("panther", "Panther Summon", SummonCategory.Panther, SummonRole.Ambush, "A dark pounce interrupts enemies and strikes hard.", 55, 22, 35, 160, 29, 160, 1.25f, 500, 1.05f, 0, 56, 0x262633, 0x9D89CB),
                Make("dragon", "Dragon Summon", SummonCategory.Dragon, SummonRole.Legendary, "Aerial rush releases a short-range fire burst.", 150, 28, 48, 360, 42, 210, 1.4f, 420, 1.8f, 0, 105, 0x813A32, 0xFFB94D),
                Make("shadow-beast", "Shadow Beast Summon", SummonCategory.Shadow, SummonRole.Legendary, "Dark-red shadow beast lashes out with a fear pulse.", 125, 27, 44, 340, 38, 190, 1.35f, 410, 1.7f, 0, 105, 0x501322, 0xFF344A),
                Make("giant-toad-warrior", "Giant Toad Warrior", SummonCategory.Legendary, SummonRole.Legendary, "Legendary warrior leaps and guards its summoner.", 220, 32, 54, 660, 62, 250, 1.5f, 330, 2.35f, 0, 155, 0x39724D, 0xF0C66A),
                Make("ancient-serpent", "Ancient Serpent", SummonCategory.Legendary, SummonRole.Legendary, "Ancient coil can bind even a powerful target briefly.", 250, 34, 58, 690, 68, 275, 1.65f, 360, 2.5f, 0, 175, 0x2E634D, 0xDFDA87),
                Make("sky-king-bird", "Sky King Bird", SummonCategory.Legendary, SummonRole.Legendary, "A sovereign dive shakes the battlefield with a gust.", 230, 32, 56, 620, 72, 280, 1.55f, 510, 2.3f, 0, 185, 0x354C91, 0xD8F8FF),
                Make("colossal-turtle-guardian", "Colossal Turtle Guardian", SummonCategory.Legendary, SummonRole.Legendary, "Colossal shell shields the player and crushes foes.", 240, 36, 60, 900, 58, 285, 1.8f, 220, 2.7f, 0, 160, 0x426A58, 0xD4BC7A),
                Make("shadow-emperor", "Shadow Emperor Beast", SummonCategory.Legendary, SummonRole.Legendary, "Original Kadaya summon; unleashes a dark-red signature pulse.", 300, 36, 62, 820, 78, 300, 1.55f, 450, 2.45f, 0, 220, 0x3C091A, 0xFF263F)
            });

        public static IReadOnlyList<SummoningDefinition> All { get { return definitions; } }

        public static SummoningDefinition Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < definitions.Count; i++)
                if (string.Equals(definitions[i].Id, id, StringComparison.Ordinal)) return definitions[i];
            return null;
        }

        private static SummoningDefinition Make(string id, string name, SummonCategory category, SummonRole role,
            string description, int price, float duration, float cooldown, float health, float damage, float range,
            float interval, float speed, float size, float healing, float special, int primary, int accent,
            int energyCost = -1, int maximumUpgradeLevel = 5, int baseUpgradeShardCost = 8)
        {
            return new SummoningDefinition(id, name, category, role, description, price, duration, cooldown,
                health, damage, range, interval, speed, size, healing, special, primary, accent,
                energyCost < 0 ? (role == SummonRole.Legendary ? 30 : 18) : energyCost,
                maximumUpgradeLevel, baseUpgradeShardCost);
        }
    }

    public static class SummoningEconomy
    {
        public const int EnemyMoney = 25;
        public const int BossMoney = 150;
        public const int EnemyDiamonds = 2;
        public const int EnemyShards = 1;
        public const int BossDiamonds = 10;
        public const int BossShards = 5;

        public static void AwardEnemyDefeat(ProgressionProfile profile, bool boss)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.CurrencyBalances == null) profile.CurrencyBalances = new Dictionary<string, int>(StringComparer.Ordinal);
            int balance;
            profile.CurrencyBalances.TryGetValue("money", out balance);
            profile.CurrencyBalances["money"] = Math.Max(0, balance) + (boss ? BossMoney : EnemyMoney);
            profile.CurrencyBalances.TryGetValue("diamonds", out balance);
            profile.CurrencyBalances["diamonds"] = Math.Max(0, balance) + (boss ? BossDiamonds : EnemyDiamonds);
            profile.CurrencyBalances.TryGetValue("shards", out balance);
            profile.CurrencyBalances["shards"] = Math.Max(0, balance) + (boss ? BossShards : EnemyShards);
        }
    }

    public sealed class SummoningService
    {
#if DEBUG
        private readonly bool testMode;
#endif
        public ProgressionProfile Profile { get; private set; }

        public SummoningService(ProgressionProfile profile, bool testMode = false)
        {
            Profile = profile ?? new ProgressionProfile();
#if DEBUG
            this.testMode = testMode;
#endif
            if (Profile.OwnedContentIds == null) Profile.OwnedContentIds = new List<string>();
            if (Profile.UpgradeLevels == null) Profile.UpgradeLevels = new Dictionary<string, int>(StringComparer.Ordinal);
            if (Profile.CurrencyBalances == null) Profile.CurrencyBalances = new Dictionary<string, int>(StringComparer.Ordinal);
            if (Profile.EquippedSummonId == null) Profile.EquippedSummonId = string.Empty;
        }

        public bool IsTestMode
        {
            get
            {
#if DEBUG
                return testMode;
#else
                return false;
#endif
            }
        }

        public bool IsOwned(string id)
        {
            if (SummoningCatalog.Get(id) == null) return false;
#if DEBUG
            if (testMode) return true;
#endif
            return Profile.OwnedContentIds.Contains(OwnershipId(id));
        }

        public int GetUpgradeLevel(string id)
        {
            SummoningDefinition definition = SummoningCatalog.Get(id);
            if (definition == null) return 0;
#if DEBUG
            if (testMode) return definition.MaximumUpgradeLevel;
#endif
            int level;
            if (!Profile.UpgradeLevels.TryGetValue(UpgradeId(id), out level)) return 0;
            return Math.Max(0, Math.Min(definition.MaximumUpgradeLevel, level));
        }

        public SummonUpgradeStats GetStats(string id)
        {
            return new SummonUpgradeStats(GetUpgradeLevel(id));
        }

        public int GetUpgradeShardCost(string id)
        {
            SummoningDefinition definition = SummoningCatalog.Get(id);
            return definition == null ? 0 : definition.BaseUpgradeShardCost * (GetUpgradeLevel(id) + 1);
        }

        public bool TryPurchase(string id, out string reason)
        {
            SummoningDefinition definition = SummoningCatalog.Get(id);
            if (definition == null) { reason = "Unknown summon."; return false; }
            if (IsOwned(id)) { reason = "Already owned."; return false; }
            int diamonds = GetBalance("diamonds");
            if (diamonds < definition.DiamondPrice) { reason = "Not enough Diamonds."; return false; }
            SetBalance("diamonds", diamonds - definition.DiamondPrice);
            Profile.OwnedContentIds.Add(OwnershipId(id));
            reason = "Summon unlocked.";
            return true;
        }

        public bool TryEquip(string id, out string reason)
        {
            if (!IsOwned(id)) { reason = "Unlock this summon first."; return false; }
            Profile.EquippedSummonId = id;
            reason = "Summon equipped.";
            return true;
        }

        public bool TryUnequip(string id, out string reason)
        {
            if (!IsOwned(id)) { reason = "Unlock this summon first."; return false; }
            if (Profile.EquippedSummonId != id) { reason = "Summon is not equipped."; return false; }
            Profile.EquippedSummonId = string.Empty;
            reason = "Summon unequipped.";
            return true;
        }

        public bool TryUpgrade(string id, out string reason)
        {
            SummoningDefinition definition = SummoningCatalog.Get(id);
            if (definition == null) { reason = "Unknown summon."; return false; }
            if (!IsOwned(id)) { reason = "Unlock this summon first."; return false; }
            int level = GetUpgradeLevel(id);
            if (level >= definition.MaximumUpgradeLevel) { reason = "Maximum level reached."; return false; }
            int cost = definition.BaseUpgradeShardCost * (level + 1);
            int shards = GetBalance("shards");
            if (shards < cost) { reason = "Not enough Shards."; return false; }
            SetBalance("shards", shards - cost);
            Profile.UpgradeLevels[UpgradeId(id)] = level + 1;
            reason = "Summon upgraded.";
            return true;
        }

        public static string OwnershipId(string id) { return "summon." + (id ?? string.Empty); }
        public static string UpgradeId(string id) { return "summon." + (id ?? string.Empty); }

        private int GetBalance(string currency)
        {
            int balance;
            return Profile.CurrencyBalances.TryGetValue(currency, out balance) ? Math.Max(0, balance) : 0;
        }

        private void SetBalance(string currency, int value)
        {
            Profile.CurrencyBalances[currency] = Math.Max(0, value);
        }
    }
}
