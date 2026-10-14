using System;
using System.Collections.Generic;

namespace Kadaya
{
    public enum ShopCategory { Eyes, Swords, Susano, Special, Summons }
    public enum ShopMenuEntryKind { StarterWeapon, ShopItem, Ability, Summon }

    public sealed class ShopMenuEntry
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public ShopMenuEntryKind Kind { get; private set; }
        public ShopItem ShopItem { get; private set; }
        public AbilityDefinition Ability { get; private set; }
        public SummoningDefinition Summon { get; private set; }

        internal ShopMenuEntry(string id, string name, string description, ShopMenuEntryKind kind,
            ShopItem shopItem = null, AbilityDefinition ability = null, SummoningDefinition summon = null)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            Kind = kind;
            ShopItem = shopItem;
            Ability = ability;
            Summon = summon;
        }
    }

    public static class ShopMenuModel
    {
        public static IReadOnlyList<ShopMenuEntry> Build(ShopCategory category)
        {
            var entries = new List<ShopMenuEntry>();
            if (category == ShopCategory.Summons)
            {
                foreach (SummoningDefinition summon in SummoningCatalog.All)
                    entries.Add(new ShopMenuEntry(summon.Id, summon.Name, summon.Description, ShopMenuEntryKind.Summon, summon: summon));
                return entries.AsReadOnly();
            }

            if (category == ShopCategory.Special)
                foreach (AbilityDefinition ability in AbilityCatalog.All)
                    entries.Add(new ShopMenuEntry(ability.Id, ability.Name, ability.Description, ShopMenuEntryKind.Ability, ability: ability));

            if (category == ShopCategory.Swords)
                entries.Add(new ShopMenuEntry("weapon.scythe", "Reaper Scythe", "Starting weapon; its three-hit melee combo is always available.", ShopMenuEntryKind.StarterWeapon));

            foreach (ShopItem item in ShopCatalog.All)
            {
                bool include = category == ShopCategory.Eyes && item.Kind == ShopItemKind.Eye ||
                    category == ShopCategory.Swords && item.Kind == ShopItemKind.Weapon ||
                    category == ShopCategory.Susano && item.Kind == ShopItemKind.SusanooVariant ||
                    category == ShopCategory.Special && item.Kind == ShopItemKind.SageMode;
                if (include) entries.Add(new ShopMenuEntry(item.Id, item.Name, item.Description, ShopMenuEntryKind.ShopItem, shopItem: item));
            }
            return entries.AsReadOnly();
        }

        public static string CategoryName(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.Eyes: return "EYES";
                case ShopCategory.Swords: return "SWORDS";
                case ShopCategory.Susano: return "SUSANO";
                case ShopCategory.Special: return "SPECIAL ABILITIES";
                case ShopCategory.Summons: return "SUMMONS";
                default: return "SHOP";
            }
        }
    }
}
