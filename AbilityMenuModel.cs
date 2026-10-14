using System;
using System.Collections.Generic;

namespace Kadaya
{
    public sealed class AbilityMenuRow
    {
        public AbilityDefinition Definition { get; private set; }
        public string Status { get; private set; }
        public string RequirementText { get; private set; }
        public int UpgradeLevel { get; private set; }
        public bool IsUnlocked { get; private set; }

        internal AbilityMenuRow(AbilityDefinition definition, string status, string requirementText, int upgradeLevel, bool isUnlocked)
        {
            Definition = definition;
            Status = status;
            RequirementText = requirementText;
            UpgradeLevel = upgradeLevel;
            IsUnlocked = isUnlocked;
        }
    }

    public static class AbilityMenuModel
    {
        public static IReadOnlyList<AbilityMenuRow> Build(ProgressionService progression)
        {
            if (progression == null) throw new ArgumentNullException(nameof(progression));

            var rows = new List<AbilityMenuRow>(AbilityCatalog.All.Count);
            for (int i = 0; i < AbilityCatalog.All.Count; i++)
            {
                AbilityDefinition definition = AbilityCatalog.All[i];
                bool unlocked = progression.IsUnlocked(definition.Id);
                string status = !definition.IsImplemented
                    ? "Not yet implemented"
                    : unlocked ? "Available" : "Locked";
                rows.Add(new AbilityMenuRow(
                    definition,
                    status,
                    GetRequirementText(definition.UnlockRule),
                    progression.GetUpgradeLevel(definition.Id),
                    unlocked));
            }
            return rows.AsReadOnly();
        }

        private static string GetRequirementText(AbilityUnlockRule rule)
        {
            var requirements = new List<string>();
            if (rule.RequiredAbilityId.Length > 0)
            {
                AbilityDefinition requiredAbility = AbilityCatalog.Get(rule.RequiredAbilityId);
                if (requiredAbility != null) requirements.Add("Unlock " + requiredAbility.Name);
            }
            if (rule.MinimumTotalEnemyDefeats > 0)
                requirements.Add("Defeat " + rule.MinimumTotalEnemyDefeats + " enemies total");
            if (rule.RequiredCompletedMissions > 0)
                requirements.Add("Complete mission " + rule.RequiredCompletedMissions);
            if (rule.MinimumDefeatsAfterRequiredMission > 0)
                requirements.Add("Defeat " + rule.MinimumDefeatsAfterRequiredMission + " enemies after mission " + rule.RequiredCompletedMissions);
            return requirements.Count == 0 ? "Available from the start" : string.Join(" and ", requirements);
        }
    }
}
