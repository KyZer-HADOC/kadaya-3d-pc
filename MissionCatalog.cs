using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kadaya
{
    public sealed class MissionMapDefinition
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }

        internal MissionMapDefinition(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }

    public sealed class MissionDefinition
    {
        public int Index { get; private set; }
        public string Id { get; private set; }
        public string MapId { get; private set; }
        public int MapIndex { get; private set; }
        public int LevelNumber { get; private set; }
        public string Name { get; private set; }
        // Route characters map to encounter, gap, spikes, climb, ambush, and reward sections.
        public string RouteCode { get; private set; }
        public int Difficulty { get; private set; }
        public int CheckpointInterval { get; private set; }
        public int BossKind { get; private set; }
        public string BossName { get; private set; }
        public string Objective { get; private set; }
        public float SpawnX { get; private set; }
        public float SpawnZ { get; private set; }
        public string CompletionCondition { get; private set; }
        public int RewardMoney { get; private set; }
        public int RewardDiamonds { get; private set; }
        public int RewardShards { get; private set; }
        public bool IsBossMilestone { get; private set; }

        internal MissionDefinition(int index, int mapIndex, int levelNumber, string name, string routeCode)
        {
            MissionMapDefinition map = MissionCatalog.Maps[mapIndex];
            Index = index;
            Id = map.Id + "-" + levelNumber.ToString("00");
            MapId = map.Id;
            MapIndex = mapIndex;
            LevelNumber = levelNumber;
            Name = name;
            RouteCode = routeCode;
            Difficulty = levelNumber;
            CheckpointInterval = 2 + ((levelNumber + mapIndex) % 3);
            BossKind = levelNumber == 15 ? mapIndex : (levelNumber + mapIndex) % 3;
            IsBossMilestone = levelNumber % 5 == 0 || levelNumber == 15;
            BossName = MissionCatalog.GetBossName(mapIndex, levelNumber, BossKind);
            Objective = "Defeat " + BossName + " and reach the Soul Gate.";
            SpawnX = 250;
            SpawnZ = 110;
            CompletionCondition = "boss-defeated-and-soul-gate-reached";
            RewardMoney = 120 + levelNumber * 35 + mapIndex * 80;
            RewardDiamonds = 3 + levelNumber + mapIndex * 2;
            RewardShards = 2 + levelNumber / 3 + mapIndex;
        }
    }

    public static class MissionCatalog
    {
        public const int MissionsPerMap = 15;

        private static readonly ReadOnlyCollection<MissionMapDefinition> maps =
            new ReadOnlyCollection<MissionMapDefinition>(new List<MissionMapDefinition>
            {
                new MissionMapDefinition("blood-moon-forest", "BLOOD MOON FOREST", "Moonlit woods, root paths, and ruined forest shrines."),
                new MissionMapDefinition("haunted-graveyard", "HAUNTED GRAVEYARD", "Tombs, broken walls, and haunted crossing routes."),
                new MissionMapDefinition("burning-temple", "BURNING TEMPLE", "Temple stairs, collapsing platforms, and firelit arenas.")
            });

        private static readonly string[][] names =
        {
            new[] { "The Red Trail", "Rootbound Crossing", "Moonlit Watch", "Bamboo Ambush", "Shrine of Ash", "The Split Canopy", "Warden's Hollow", "Crimson Footbridge", "The Hunter's Route", "Old Growth Ruins", "Bloodroot Pass", "The Silent Clearing", "Moonfall Ravine", "The Reaper's Approach", "Warlord's Moon" },
            new[] { "Graveside Arrival", "The Broken Wall", "Lanterns Below", "Tombkeeper's Path", "Mausoleum Steps", "The Sunken Yard", "Pale Watchers", "Cryptside Crossing", "The Bell Tower Ruin", "Widow's Courtyard", "The Hollow Procession", "Ashen Mausoleums", "The Last Lantern", "Wraithbound Causeway", "The Wraith's Crown" },
            new[] { "Ember Gate", "The Outer Stairs", "Fallen Courtyard", "The Furnace Walk", "Bridge of Cinders", "Temple Approach", "The Red Gallery", "Collapsed Sanctum", "The Flamekeeper's Rise", "Ashfall Arcade", "The Burning Nave", "Shogun's Courtyard", "The Inner Crucible", "Hall of Falling Embers", "The Fire Shogun" }
        };

        private static readonly string[][] routes =
        {
            new[] { "EGSCAREG", "ECRAGESE", "GSEACREG", "SCGERAEG", "AEGCRSEG", "RSCGEAEG", "CAGSEERG", "SGCREAEG", "EASGCRGE", "GCRSEAAG", "SREAGCEG", "ACGERSGE", "EGARCSEG", "RAEGSCGE", "CGSEARAEG" },
            new[] { "SACREGGE", "GRASECGS", "CEGASRGE", "RSGCEAGE", "AGCRSESG", "EGRSACGE", "SGAERCGS", "CRESGAGE", "GESCARSG", "ASGCREGE", "RCEGASGS", "SEACGRGE", "GCASERGS", "EASRCGGE", "CRGSEAGE" },
            new[] { "CGEASRSG", "RAGESCGS", "ESCGARGE", "GSCREAGE", "AREGSCGS", "SEGRACGE", "CGRAESGS", "ASCEGRGE", "GECASRSG", "SRGCEAGE", "EACSGRGE", "RSGAECGS", "GCESARSG", "AEGCRGSE", "SGRCEAGE" }
        };

        private static readonly string[][] bossNames =
        {
            new[] { "Oni Warlord", "Moon Wraith", "Bloodroot Brute", "The Red Daimyo" },
            new[] { "Gravebound Knight", "Wraith Lord", "Tomb Behemoth", "The Mourning King" },
            new[] { "Ashen Colossus", "Temple Wraith", "Fire Shogun", "The Temple Sovereign" }
        };

        private static readonly ReadOnlyCollection<MissionDefinition> missions = BuildMissions();

        public static IReadOnlyList<MissionMapDefinition> Maps { get { return maps; } }
        public static IReadOnlyList<MissionDefinition> All { get { return missions; } }

        public static MissionDefinition Get(int index)
        {
            if (index < 0 || index >= missions.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return missions[index];
        }

        public static MissionDefinition Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < missions.Count; i++)
                if (string.Equals(missions[i].Id, id, StringComparison.Ordinal)) return missions[i];
            return null;
        }

        public static string GetBossName(int mapIndex, int levelNumber, int bossKind)
        {
            int map = Math.Max(0, Math.Min(bossNames.Length - 1, mapIndex));
            if (levelNumber == 15) return bossNames[map][3];
            string name = bossNames[map][Math.Max(0, Math.Min(2, bossKind))];
            return levelNumber % 5 == 0 ? "Veteran " + name : name;
        }

        private static ReadOnlyCollection<MissionDefinition> BuildMissions()
        {
            var result = new List<MissionDefinition>(maps.Count * MissionsPerMap);
            for (int map = 0; map < maps.Count; map++)
                for (int level = 0; level < MissionsPerMap; level++)
                    result.Add(new MissionDefinition(result.Count, map, level + 1, names[map][level], map.ToString() + ":" + routes[map][level]));
            return new ReadOnlyCollection<MissionDefinition>(result);
        }
    }
}
