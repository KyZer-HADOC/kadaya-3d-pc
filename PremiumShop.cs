using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kadaya
{
    public enum ShopItemKind { Eye, Weapon, SageMode, SusanooVariant }

    public sealed class ShopItem
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public ShopItemKind Kind { get; private set; }
        public string Currency { get; private set; }
        public int Price { get; private set; }

        internal ShopItem(string id, string name, string description, ShopItemKind kind, string currency, int price)
        { Id = id; Name = name; Description = description; Kind = kind; Currency = currency; Price = price; }
    }

    public static class PremiumShopCatalog
    {
        private static readonly ReadOnlyCollection<ShopItem> items = new ReadOnlyCollection<ShopItem>(new List<ShopItem>
        {
            new ShopItem("eye.sharingan", "Three-Tomoe Sharingan", "Fireball Jutsu and improved enemy reading.", ShopItemKind.Eye, "diamonds", 80),
            new ShopItem("eye.mangekyo", "Mangekyo Sharingan", "Genjutsu disables one target; bosses resist sooner.", ShopItemKind.Eye, "diamonds", 130),
            new ShopItem("eye.byakugan", "Byakugan", "Free camera scouting and an all-direction chakra Rotation.", ShopItemKind.Eye, "diamonds", 100),
            new ShopItem("eye.kakashi", "Kakashi Eye", "Switch among Fire, Water, Wind, Earth, and Lightning attacks.", ShopItemKind.Eye, "diamonds", 180),
            new ShopItem("eye.ems", "Eternal Mangekyo Sharingan", "Activate a damage-boosting Susanoo transformation.", ShopItemKind.Eye, "diamonds", 220),
            new ShopItem("eye.jougan", "Jougan", "Future Vision reveals hazards; Time Freeze lasts 1.5 seconds.", ShopItemKind.Eye, "diamonds", 200),
            new ShopItem("eye.rinnegan", "Rinnegan", "Almighty Push shocks enemies and clears nearby traps.", ShopItemKind.Eye, "diamonds", 260),
            new ShopItem("weapon.katana", "Moonlit Katana", "A sharp sword with a faster, stronger melee combo.", ShopItemKind.Weapon, "money", 450),
            new ShopItem("weapon.chakra", "Chakra Blade", "A balanced chakra-edged sword.", ShopItemKind.Weapon, "money", 800),
            new ShopItem("weapon.lightning", "Lightning Blade", "A high-impact lightning weapon.", ShopItemKind.Weapon, "diamonds", 90),
            new ShopItem("sage.toad", "Toad Sage Mode", "Boosts melee damage and movement while active.", ShopItemKind.SageMode, "diamonds", 120),
            new ShopItem("sage.snake", "Snake Sage Mode", "Boosts ranged damage and grants regeneration.", ShopItemKind.SageMode, "diamonds", 140),
            new ShopItem("sage.sixpaths", "Six Paths Sage Mode", "A legendary, short-lived combat enhancement.", ShopItemKind.SageMode, "diamonds", 240),
            new ShopItem("susanoo.azure", "Azure Susanoo", "Change the Susanoo avatar's aura.", ShopItemKind.SusanooVariant, "diamonds", 100),
            new ShopItem("susanoo.crimson", "Crimson Susanoo", "Change the Susanoo avatar's aura.", ShopItemKind.SusanooVariant, "diamonds", 100)
        });

        public static IReadOnlyList<ShopItem> All { get { return items; } }
        public static ShopItem Get(string id)
        {
            for (int i = 0; i < items.Count; i++) if (string.Equals(items[i].Id, id, StringComparison.Ordinal)) return items[i];
            return null;
        }
    }

    public sealed class PremiumShopService
    {
        private readonly ProgressionProfile profile;
#if DEBUG
        private readonly bool testMode;
#endif
        public PremiumShopService(ProgressionProfile profile, bool testMode = false)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
#if DEBUG
            this.testMode = testMode;
#endif
            if (this.profile.OwnedContentIds == null) this.profile.OwnedContentIds = new List<string>();
            if (this.profile.CurrencyBalances == null) this.profile.CurrencyBalances = new Dictionary<string, int>(StringComparer.Ordinal);
            if (this.profile.UpgradeLevels == null) this.profile.UpgradeLevels = new Dictionary<string, int>(StringComparer.Ordinal);
        }

        public bool IsOwned(string id)
        {
            if (PremiumShopCatalog.Get(id) == null) return false;
#if DEBUG
            if (testMode) return true;
#endif
            return profile.OwnedContentIds.Contains(id);
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

        public bool TryPurchase(string id, out string reason)
        {
            ShopItem item = PremiumShopCatalog.Get(id);
            if (item == null) { reason = "Unknown shop item."; return false; }
            if (IsOwned(id)) { reason = "Already owned."; return false; }
            int balance = GetBalance(item.Currency);
            if (balance < item.Price) { reason = "Not enough " + item.Currency + "."; return false; }
            SetBalance(item.Currency, balance - item.Price);
            profile.OwnedContentIds.Add(item.Id);
            if (item.Kind == ShopItemKind.Eye) profile.EquippedEyeId = item.Id.Substring("eye.".Length);
            if (item.Kind == ShopItemKind.Weapon) { profile.EquippedWeaponId = item.Id.Substring("weapon.".Length); profile.EquippedItemId = profile.EquippedWeaponId; }
            if (item.Kind == ShopItemKind.SageMode) profile.EquippedSageModeId = item.Id;
            if (item.Kind == ShopItemKind.SusanooVariant) profile.EquippedSusanooVariantId = item.Id;
            reason = item.Name + " unlocked and equipped.";
            return true;
        }

        public bool TryEquip(string id, out string reason)
        {
            ShopItem item = PremiumShopCatalog.Get(id);
            if (item == null) { reason = "Unknown shop item."; return false; }
            if (!IsOwned(id)) { reason = "Purchase this item first."; return false; }
            if (item.Kind == ShopItemKind.Eye) profile.EquippedEyeId = item.Id.Substring("eye.".Length);
            else if (item.Kind == ShopItemKind.Weapon) { profile.EquippedWeaponId = item.Id.Substring("weapon.".Length); profile.EquippedItemId = profile.EquippedWeaponId; }
            else if (item.Kind == ShopItemKind.SageMode) profile.EquippedSageModeId = item.Id;
            else profile.EquippedSusanooVariantId = item.Id;
            reason = item.Name + " equipped.";
            return true;
        }

        public int GetSusanooLevel()
        {
#if DEBUG
            if (testMode) return 9;
#endif
            int level;
            return profile.UpgradeLevels.TryGetValue("susanoo.duration", out level) ? Math.Max(0, Math.Min(9, level)) : 0;
        }

        public float GetSusanooDuration() { return Math.Min(12f, 3f + GetSusanooLevel()); }
        public int GetSusanooUpgradeCost() { return 15 + GetSusanooLevel() * 10; }

        public bool TryUpgradeSusanoo(out string reason)
        {
            if (!IsOwned("eye.ems")) { reason = "Unlock Eternal Mangekyo Sharingan first."; return false; }
            int level = GetSusanooLevel();
            if (level >= 9) { reason = "Susanoo duration is at maximum."; return false; }
#if DEBUG
            if (testMode) { reason = "Test Mode already has the maximum Susanoo duration."; return false; }
#endif
            int shards = GetBalance("shards"), cost = GetSusanooUpgradeCost();
            if (shards < cost) { reason = "Not enough Shards."; return false; }
            SetBalance("shards", shards - cost);
            profile.UpgradeLevels["susanoo.duration"] = level + 1;
            reason = "Susanoo duration upgraded to " + GetSusanooDuration() + " seconds.";
            return true;
        }

        private int GetBalance(string currency)
        { int value; return profile.CurrencyBalances.TryGetValue(currency, out value) ? Math.Max(0, value) : 0; }
        private void SetBalance(string currency, int value) { profile.CurrencyBalances[currency] = Math.Max(0, value); }
    }
}
