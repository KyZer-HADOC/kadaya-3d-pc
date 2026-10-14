using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Kadaya
{
    public sealed class AbilityUnlockRule
    {
        public string RequiredAbilityId { get; private set; }
        public int MinimumTotalEnemyDefeats { get; private set; }
        public int RequiredCompletedMissions { get; private set; }
        public int MinimumDefeatsAfterRequiredMission { get; private set; }

        public AbilityUnlockRule(
            string requiredAbilityId,
            int minimumTotalEnemyDefeats,
            int requiredCompletedMissions,
            int minimumDefeatsAfterRequiredMission)
        {
            RequiredAbilityId = requiredAbilityId ?? string.Empty;
            MinimumTotalEnemyDefeats = Math.Max(0, minimumTotalEnemyDefeats);
            RequiredCompletedMissions = Math.Max(0, requiredCompletedMissions);
            MinimumDefeatsAfterRequiredMission = Math.Max(0, minimumDefeatsAfterRequiredMission);
        }
    }

    public sealed class AbilityDefinition
    {
        public string Id { get; private set; }
        public string ParentId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Binding { get; private set; }
        public string ResourceId { get; private set; }
        public int ResourceCost { get; private set; }
        public float CooldownSeconds { get; private set; }
        public int MaximumUpgradeLevel { get; private set; }
        public bool IsImplemented { get; private set; }
        public AbilityUnlockRule UnlockRule { get; private set; }

        public AbilityDefinition(
            string id,
            string parentId,
            string name,
            string description,
            string binding,
            string resourceId,
            int resourceCost,
            float cooldownSeconds,
            int maximumUpgradeLevel,
            bool isImplemented,
            AbilityUnlockRule unlockRule)
        {
            Id = id ?? string.Empty;
            ParentId = parentId ?? string.Empty;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            Binding = binding ?? string.Empty;
            ResourceId = resourceId ?? string.Empty;
            ResourceCost = Math.Max(0, resourceCost);
            CooldownSeconds = Math.Max(0, cooldownSeconds);
            MaximumUpgradeLevel = Math.Max(0, maximumUpgradeLevel);
            IsImplemented = isImplemented;
            UnlockRule = unlockRule ?? new AbilityUnlockRule(null, 0, 0, 0);
        }
    }

    public static class AbilityCatalog
    {
        private static readonly ReadOnlyCollection<AbilityDefinition> abilities =
            new ReadOnlyCollection<AbilityDefinition>(new List<AbilityDefinition>
            {
                new AbilityDefinition(
                    "scythe", "core", "Scythe Combo", "Starting melee combo.", "LMB", "soul_energy", 0, 0f, 5, true,
                    new AbilityUnlockRule(null, 0, 0, 0)),
                new AbilityDefinition(
                    "shuriken", "core", "Shuriken Jutsu", "Throw a camera-aimed shuriken spread.", "Q", "soul_energy", 8, 1.2f, 5, true,
                    new AbilityUnlockRule(null, 3, 0, 0)),
                new AbilityDefinition(
                    "kenjutsu", "core", "Kenjutsu", "A forward-cutting melee technique.", "F", "soul_energy", 14, 5f, 5, true,
                    new AbilityUnlockRule("shuriken", 8, 0, 0)),
                new AbilityDefinition(
                    "hidden", "core", "Hidden Jutsu", "Briefly vanish and evade incoming damage.", "C", "soul_energy", 20, 9f, 5, true,
                    new AbilityUnlockRule("kenjutsu", 0, 1, 0)),
                new AbilityDefinition(
                    "clone", "core", "Shadow Clone", "Create one short-lived combat clone.", "E", "soul_energy", 40, 12f, 5, true,
                    new AbilityUnlockRule("hidden", 0, 1, 6)),
                new AbilityDefinition(
                    "kunai", "core", "Teleportation Kunai", "Blink to a nearby enemy or forward safe ground.", "R", "soul_energy", 18, 7f, 5, true,
                    new AbilityUnlockRule("clone", 0, 2, 0)),
                new AbilityDefinition(
                    "rasengan", "core", "Rasengan", "Launch a concentrated chakra projectile.", "RMB", "soul_energy", 35, 1.1f, 5, true,
                    new AbilityUnlockRule("kunai", 0, 2, 5))
            });

        public static IReadOnlyList<AbilityDefinition> All
        {
            get { return abilities; }
        }

        public static AbilityDefinition Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < abilities.Count; i++)
                if (string.Equals(abilities[i].Id, id, StringComparison.Ordinal))
                    return abilities[i];
            return null;
        }
    }

    public sealed class ProgressionProfile
    {
        [JsonIgnore]
        public bool IsReadOnlyRecovery { get; set; }
        public int SchemaVersion { get; set; } = 1;
        public int UnlockedLevel { get; set; }
        public int CompletedMissions { get; set; }
        public int TotalEnemyDefeats { get; set; }
        public int DefeatsSinceMission1 { get; set; }
        public int DefeatsSinceMission2 { get; set; }
        public List<string> UnlockedAbilityIds { get; set; } = new List<string> { "scythe" };
        public List<string> CompletedMissionIds { get; set; } = new List<string>();
        public Dictionary<string, float> BestMissionTimes { get; set; } = new Dictionary<string, float>(StringComparer.Ordinal);
        public List<string> OwnedContentIds { get; set; } = new List<string>();
        public string EquippedEyeId { get; set; } = string.Empty;
        public string EquippedItemId { get; set; } = string.Empty;
        public string EquippedWeaponId { get; set; } = string.Empty;
        public string EquippedSageModeId { get; set; } = string.Empty;
        public string EquippedSusanooVariantId { get; set; } = string.Empty;
        public string EquippedChakraId { get; set; } = "fire";
        public string EquippedSummonId { get; set; } = string.Empty;
        public Dictionary<string, int> UpgradeLevels { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> CurrencyBalances { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class ProgressionService
    {
#if DEBUG
        private readonly bool testMode;
#endif

        public ProgressionProfile Profile { get; private set; }

        public ProgressionService(ProgressionProfile profile, bool testMode = false)
        {
            Profile = profile ?? new ProgressionProfile();
#if DEBUG
            this.testMode = testMode;
#endif
            NormalizeProfile();
            EvaluateUnlocks();
        }

        public bool IsUnlocked(string id)
        {
            AbilityDefinition ability = AbilityCatalog.Get(id);
            if (ability == null) return false;
#if DEBUG
            if (testMode && ability.IsImplemented) return true;
#endif
            return Profile.UnlockedAbilityIds.Contains(ability.Id);
        }

        public bool IsUsable(string id)
        {
            AbilityDefinition ability = AbilityCatalog.Get(id);
            return ability != null && ability.IsImplemented && IsUnlocked(ability.Id);
        }

        public int GetUpgradeLevel(string id)
        {
            AbilityDefinition ability = AbilityCatalog.Get(id);
            if (ability == null) return 0;
#if DEBUG
            if (testMode && ability.IsImplemented) return ability.MaximumUpgradeLevel;
#endif

            int level;
            if (!Profile.UpgradeLevels.TryGetValue(ability.Id, out level)) return 0;
            return Math.Max(0, Math.Min(ability.MaximumUpgradeLevel, level));
        }

        public int GetUpgradeShardCost(string id)
        {
            AbilityDefinition ability = AbilityCatalog.Get(id);
            return ability == null ? 0 : 6 * (GetUpgradeLevel(id) + 1);
        }

        public bool TryUpgrade(string id, out string reason)
        {
            AbilityDefinition ability = AbilityCatalog.Get(id);
            if (ability == null || !ability.IsImplemented) { reason = "Ability cannot be upgraded."; return false; }
            if (!IsUnlocked(id)) { reason = "Unlock this ability first."; return false; }
            int level = GetUpgradeLevel(id);
            if (level >= ability.MaximumUpgradeLevel) { reason = "Ability is at maximum level."; return false; }
#if DEBUG
            if (testMode) { reason = "Test Mode already has maximum upgrades."; return false; }
#endif
            int shards;
            Profile.CurrencyBalances.TryGetValue("shards", out shards);
            int cost = GetUpgradeShardCost(id);
            if (shards < cost) { reason = "Not enough Shards."; return false; }
            Profile.CurrencyBalances["shards"] = Math.Max(0, shards - cost);
            Profile.UpgradeLevels[id] = level + 1;
            reason = ability.Name + " upgraded.";
            return true;
        }

        public IReadOnlyList<string> RecordEnemyDefeat()
        {
            Profile.TotalEnemyDefeats = Math.Max(0, Profile.TotalEnemyDefeats) + 1;
            if (Profile.CompletedMissions >= 1)
                Profile.DefeatsSinceMission1 = Math.Max(0, Profile.DefeatsSinceMission1) + 1;
            if (Profile.CompletedMissions >= 2)
                Profile.DefeatsSinceMission2 = Math.Max(0, Profile.DefeatsSinceMission2) + 1;
            return EvaluateUnlocks();
        }

        public IReadOnlyList<string> CompleteMission()
        {
            Profile.CompletedMissions = Math.Min(MissionCatalog.All.Count, Math.Max(0, Profile.CompletedMissions) + 1);
            Profile.UnlockedLevel = Math.Max(Profile.UnlockedLevel, Math.Min(MissionCatalog.All.Count - 1, Profile.CompletedMissions));
            return EvaluateUnlocks();
        }

        public IReadOnlyList<string> CompleteMission(int missionIndex, string missionId, float elapsedSeconds)
        {
            if (missionIndex < 0 || missionIndex >= MissionCatalog.All.Count)
                throw new System.ArgumentOutOfRangeException(nameof(missionIndex));
            if (string.IsNullOrWhiteSpace(missionId))
                throw new System.ArgumentException("A stable mission ID is required.", nameof(missionId));
            if (!string.Equals(MissionCatalog.Get(missionIndex).Id, missionId, StringComparison.Ordinal))
                throw new System.ArgumentException("The mission ID does not match its catalog index.", nameof(missionId));

            if (!Profile.CompletedMissionIds.Contains(missionId))
            {
                Profile.CompletedMissionIds.Add(missionId);
                Profile.CompletedMissions = Math.Min(MissionCatalog.All.Count, Math.Max(0, Profile.CompletedMissions) + 1);
            }
            Profile.UnlockedLevel = Math.Max(Profile.UnlockedLevel, Math.Min(MissionCatalog.All.Count - 1, missionIndex + 1));
            if (elapsedSeconds > 0 && !float.IsNaN(elapsedSeconds) && !float.IsInfinity(elapsedSeconds))
            {
                float best;
                if (!Profile.BestMissionTimes.TryGetValue(missionId, out best) || elapsedSeconds < best)
                    Profile.BestMissionTimes[missionId] = elapsedSeconds;
            }
            return EvaluateUnlocks();
        }

        public void ResetNewGame()
        {
            Profile.SchemaVersion = 3;
            Profile.UnlockedLevel = 0;
            Profile.CompletedMissions = 0;
            Profile.CompletedMissionIds.Clear();
            Profile.BestMissionTimes.Clear();
            Profile.TotalEnemyDefeats = 0;
            Profile.DefeatsSinceMission1 = 0;
            Profile.DefeatsSinceMission2 = 0;
            Profile.UnlockedAbilityIds.Clear();
            Profile.UnlockedAbilityIds.Add("scythe");
            var permanentContent = new List<string>();
            for (int i = 0; i < Profile.OwnedContentIds.Count; i++)
                if (Profile.OwnedContentIds[i] != null)
                    permanentContent.Add(Profile.OwnedContentIds[i]);
            string equippedSummon = !string.IsNullOrEmpty(Profile.EquippedSummonId) &&
                permanentContent.Contains("summon." + Profile.EquippedSummonId) ? Profile.EquippedSummonId : string.Empty;
            string equippedEye = !string.IsNullOrEmpty(Profile.EquippedEyeId) && permanentContent.Contains("eye." + Profile.EquippedEyeId)
                ? Profile.EquippedEyeId : string.Empty;
            string equippedItem = !string.IsNullOrEmpty(Profile.EquippedItemId) && permanentContent.Contains(Profile.EquippedItemId.StartsWith("sage.", StringComparison.Ordinal)
                || Profile.EquippedItemId.StartsWith("susanoo.", StringComparison.Ordinal) ? Profile.EquippedItemId : "weapon." + Profile.EquippedItemId)
                ? Profile.EquippedItemId : string.Empty;
            string equippedWeapon = !string.IsNullOrEmpty(Profile.EquippedWeaponId) && permanentContent.Contains("weapon." + Profile.EquippedWeaponId) ? Profile.EquippedWeaponId : string.Empty;
            string equippedSage = !string.IsNullOrEmpty(Profile.EquippedSageModeId) && permanentContent.Contains(Profile.EquippedSageModeId) ? Profile.EquippedSageModeId : string.Empty;
            string equippedSusanoo = !string.IsNullOrEmpty(Profile.EquippedSusanooVariantId) && permanentContent.Contains(Profile.EquippedSusanooVariantId) ? Profile.EquippedSusanooVariantId : string.Empty;
            Profile.OwnedContentIds.Clear();
            Profile.OwnedContentIds.AddRange(permanentContent);
            Profile.EquippedEyeId = equippedEye;
            Profile.EquippedItemId = equippedItem;
            Profile.EquippedWeaponId = equippedWeapon;
            Profile.EquippedSageModeId = equippedSage;
            Profile.EquippedSusanooVariantId = equippedSusanoo;
            Profile.EquippedSummonId = equippedSummon;
            Profile.SchemaVersion = Math.Max(3, Profile.SchemaVersion);
            // Purchases, wallet balances and permanent item upgrades belong to the account profile,
            // so starting a new campaign must not erase them.
        }

        private void NormalizeProfile()
        {
            Profile.SchemaVersion = Math.Max(3, Profile.SchemaVersion);
            Profile.UnlockedLevel = Math.Max(0, Math.Min(MissionCatalog.All.Count - 1, Profile.UnlockedLevel));
            Profile.CompletedMissions = Math.Max(0, Math.Min(MissionCatalog.All.Count, Profile.CompletedMissions));
            Profile.TotalEnemyDefeats = Math.Max(0, Profile.TotalEnemyDefeats);
            Profile.DefeatsSinceMission1 = Math.Max(0, Profile.DefeatsSinceMission1);
            Profile.DefeatsSinceMission2 = Math.Max(0, Profile.DefeatsSinceMission2);
            if (Profile.UnlockedAbilityIds == null) Profile.UnlockedAbilityIds = new List<string>();
            if (Profile.CompletedMissionIds == null) Profile.CompletedMissionIds = new List<string>();
            if (Profile.BestMissionTimes == null) Profile.BestMissionTimes = new Dictionary<string, float>(StringComparer.Ordinal);
            var completedIds = new HashSet<string>(StringComparer.Ordinal);
            Profile.CompletedMissionIds.RemoveAll(id => MissionCatalog.Get(id) == null || !completedIds.Add(id));
            foreach (string id in new List<string>(Profile.BestMissionTimes.Keys))
                if (MissionCatalog.Get(id) == null || Profile.BestMissionTimes[id] <= 0 || float.IsNaN(Profile.BestMissionTimes[id]) || float.IsInfinity(Profile.BestMissionTimes[id]))
                    Profile.BestMissionTimes.Remove(id);
            if (Profile.OwnedContentIds == null) Profile.OwnedContentIds = new List<string>();
            if (Profile.UpgradeLevels == null) Profile.UpgradeLevels = new Dictionary<string, int>(StringComparer.Ordinal);
            if (Profile.CurrencyBalances == null) Profile.CurrencyBalances = new Dictionary<string, int>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(Profile.EquippedEyeId)) Profile.EquippedEyeId = string.Empty;
            if (string.IsNullOrEmpty(Profile.EquippedItemId)) Profile.EquippedItemId = string.Empty;
            if (string.IsNullOrEmpty(Profile.EquippedWeaponId)) Profile.EquippedWeaponId = string.Empty;
            if (string.IsNullOrEmpty(Profile.EquippedSageModeId)) Profile.EquippedSageModeId = string.Empty;
            if (string.IsNullOrEmpty(Profile.EquippedSusanooVariantId)) Profile.EquippedSusanooVariantId = string.Empty;
            if (string.IsNullOrEmpty(Profile.EquippedChakraId)) Profile.EquippedChakraId = "fire";
            if (string.IsNullOrEmpty(Profile.EquippedSummonId)) Profile.EquippedSummonId = string.Empty;
            if (!Profile.UnlockedAbilityIds.Contains("scythe")) Profile.UnlockedAbilityIds.Insert(0, "scythe");
        }

        private IReadOnlyList<string> EvaluateUnlocks()
        {
            var newlyUnlocked = new List<string>();
            for (int i = 0; i < AbilityCatalog.All.Count; i++)
            {
                AbilityDefinition ability = AbilityCatalog.All[i];
                if (Profile.UnlockedAbilityIds.Contains(ability.Id)) continue;
                if (!IsRuleSatisfied(ability.UnlockRule)) continue;
                Profile.UnlockedAbilityIds.Add(ability.Id);
                newlyUnlocked.Add(ability.Id);
            }
            return newlyUnlocked.AsReadOnly();
        }

        private bool IsRuleSatisfied(AbilityUnlockRule rule)
        {
            if (rule.RequiredAbilityId.Length > 0 && !Profile.UnlockedAbilityIds.Contains(rule.RequiredAbilityId))
                return false;
            if (Profile.TotalEnemyDefeats < rule.MinimumTotalEnemyDefeats)
                return false;
            if (Profile.CompletedMissions < rule.RequiredCompletedMissions)
                return false;
            if (rule.MinimumDefeatsAfterRequiredMission > 0)
            {
                int defeatsAfterMission = rule.RequiredCompletedMissions >= 2
                    ? Profile.DefeatsSinceMission2
                    : rule.RequiredCompletedMissions == 1
                        ? Profile.DefeatsSinceMission1
                        : Profile.TotalEnemyDefeats;
                if (defeatsAfterMission < rule.MinimumDefeatsAfterRequiredMission)
                    return false;
            }
            return true;
        }
    }
}
