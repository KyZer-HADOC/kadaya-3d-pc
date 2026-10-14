# KADAYA: THE LAST SHADOW

An offline 3D dark-fantasy ninja action game built with C# and MonoGame. The campaign defines 45 selectable missions across three procedural realms. Preserve and extend the existing third-person camera, combat, enemy AI, summon behavior, progression profile, and save format.

## Run and Windows installer

Install the .NET 8 SDK and run `dotnet run` from the repository root.

Every push to `main` runs the Windows build and test workflow. A successful run publishes a self-contained `win-x64` build, checks the compiled audio content, and packages `output/setup.exe`. Download the `KADAYA-setup` artifact from the latest successful Actions run.

## Main menu and missions

The main menu offers **Continue** when the active profile is readable, plus **Start**, **Shop**, **Upgrade**, **Settings**, and **Exit**. Start opens separate map and level selectors. All 45 missions can be selected immediately; campaign progression is still recorded, but does not lock missions. The selector shows all 15 missions for a chosen map, each mission's boss and reward, cleared status, and best completion time. Completing an encounter saves its stable mission ID and time. Replays remain available and improve the best time without counting the mission completion twice. The selector can start a new campaign while preserving permanent shop ownership, currencies, and upgrades.

The main menu keeps the existing 3D Reaper and realm scene running behind its interface. The three available environments are Blood Moon Forest, Haunted Graveyard, and Burning Temple. The renderer now adds separate distant silhouettes for forest canopies, graveyard mausoleums/tombstones, and temple roofs/columns/braziers. Those pieces use the existing generated meshes; they are not authored map-art assets. Each mission has a stable ID, map and local level number, named objective, difficulty, route configuration, checkpoint cadence, boss identity, player spawn, completion condition, and in-game completion reward. Mission routes are composed from encounter, gap, spike, climb, ambush, and reward sections. The current mission implementation reuses the existing procedural environment and combat rules; it does not include 45 handcrafted environment meshes or bespoke boss AI for every mission.

### Mission catalog and route layouts

Route codes list the gameplay sections in order: `E` enemy encounter, `G` gap crossing, `S` spike hazard, `C` elevated platform climb, `A` ambush formation, `R` reward platform. The runtime prefixes the route with the map index (`0:`, `1:`, or `2:`). Every map has 15 named definitions and its own visual theme. The shared route builder varies section sequence, seeded dimensions, enemy formations, difficulty, checkpoints, and rewards.

| Level | Blood Moon Forest (ID — mission, route) | Haunted Graveyard (ID — mission, route) | Burning Temple (ID — mission, route) |
| ---: | --- | --- | --- |
| 01 | `blood-moon-forest-01` — The Red Trail (`EGSCAREG`) | `haunted-graveyard-01` — Graveside Arrival (`SACREGGE`) | `burning-temple-01` — Ember Gate (`CGEASRSG`) |
| 02 | `blood-moon-forest-02` — Rootbound Crossing (`ECRAGESE`) | `haunted-graveyard-02` — The Broken Wall (`GRASECGS`) | `burning-temple-02` — The Outer Stairs (`RAGESCGS`) |
| 03 | `blood-moon-forest-03` — Moonlit Watch (`GSEACREG`) | `haunted-graveyard-03` — Lanterns Below (`CEGASRGE`) | `burning-temple-03` — Fallen Courtyard (`ESCGARGE`) |
| 04 | `blood-moon-forest-04` — Bamboo Ambush (`SCGERAEG`) | `haunted-graveyard-04` — Tombkeeper's Path (`RSGCEAGE`) | `burning-temple-04` — The Furnace Walk (`GSCREAGE`) |
| 05 | `blood-moon-forest-05` — Shrine of Ash (`AEGCRSEG`) | `haunted-graveyard-05` — Mausoleum Steps (`AGCRSESG`) | `burning-temple-05` — Bridge of Cinders (`AREGSCGS`) |
| 06 | `blood-moon-forest-06` — The Split Canopy (`RSCGEAEG`) | `haunted-graveyard-06` — The Sunken Yard (`EGRSACGE`) | `burning-temple-06` — Temple Approach (`SEGRACGE`) |
| 07 | `blood-moon-forest-07` — Warden's Hollow (`CAGSEERG`) | `haunted-graveyard-07` — Pale Watchers (`SGAERCGS`) | `burning-temple-07` — The Red Gallery (`CGRAESGS`) |
| 08 | `blood-moon-forest-08` — Crimson Footbridge (`SGCREAEG`) | `haunted-graveyard-08` — Cryptside Crossing (`CRESGAGE`) | `burning-temple-08` — Collapsed Sanctum (`ASCEGRGE`) |
| 09 | `blood-moon-forest-09` — The Hunter's Route (`EASGCRGE`) | `haunted-graveyard-09` — The Bell Tower Ruin (`GESCARSG`) | `burning-temple-09` — The Flamekeeper's Rise (`GECASRSG`) |
| 10 | `blood-moon-forest-10` — Old Growth Ruins (`GCRSEAAG`) | `haunted-graveyard-10` — Widow's Courtyard (`ASGCREGE`) | `burning-temple-10` — Ashfall Arcade (`SRGCEAGE`) |
| 11 | `blood-moon-forest-11` — Bloodroot Pass (`SREAGCEG`) | `haunted-graveyard-11` — The Hollow Procession (`RCEGASGS`) | `burning-temple-11` — The Burning Nave (`EACSGRGE`) |
| 12 | `blood-moon-forest-12` — The Silent Clearing (`ACGERSGE`) | `haunted-graveyard-12` — Ashen Mausoleums (`SEACGRGE`) | `burning-temple-12` — Shogun's Courtyard (`RSGAECGS`) |
| 13 | `blood-moon-forest-13` — Moonfall Ravine (`EGARCSEG`) | `haunted-graveyard-13` — The Last Lantern (`GCASERGS`) | `burning-temple-13` — The Inner Crucible (`GCESARSG`) |
| 14 | `blood-moon-forest-14` — The Reaper's Approach (`RAEGSCGE`) | `haunted-graveyard-14` — Wraithbound Causeway (`EASRCGGE`) | `burning-temple-14` — Hall of Falling Embers (`AEGCRGSE`) |
| 15 | `blood-moon-forest-15` — Warlord's Moon (`CGSEARAEG`) | `haunted-graveyard-15` — The Wraith's Crown (`CRGSEAGE`) | `burning-temple-15` — The Fire Shogun (`SGRCEAGE`) |

## Default controls

| Action | Default |
| --- | --- |
| Move | W / A / S / D |
| Jump / dodge | Space / Left Shift |
| Camera orbit / zoom | Mouse / wheel |
| Scythe melee combo / Rasengan | Left mouse / right mouse |
| Eye primary / secondary | Q / E |
| Eye mode or Kenjutsu | F |
| Hidden Jutsu / Teleportation Kunai / Sage Mode | C / R / Z |
| Shop / Summon category / ability archive | P / M / Tab |
| Call / dismiss summon | G / X |
| Menu navigation | Arrow keys, Enter, mouse |
| Buy / upgrade | B / U |
| Graphics quality / camera reset / fullscreen | F6 / F7 / F11 |

Settings can rebind the listed keyboard actions. Mouse buttons remain the melee and Rasengan controls. Settings navigation uses Page Up/Page Down for sections, arrows to change a selected setting, Enter to toggle or capture a key, and Escape to go back. Escape exits from the main menu.

## Abilities and progression

All seven core ability entries are connected to combat and the existing profile progression:

| Ability | Unlock requirement | Effect |
| --- | --- | --- |
| Scythe Combo | Starting ability | Three-hit melee combo |
| Shuriken Jutsu | 3 enemy defeats | Camera-aimed three-shuriken spread |
| Kenjutsu | Shuriken and 8 total defeats | Forward cutting attack |
| Hidden Jutsu | Kenjutsu and mission 1 complete | Temporary evasion / invulnerability |
| Shadow Clone | Hidden Jutsu and 6 defeats after mission 1 | Combat clone for 5 seconds |
| Teleportation Kunai | Clone and mission 2 complete | Safe blink or enemy-targeted teleport |
| Rasengan | Kunai and 5 defeats after mission 2 | Chakra projectile and area impact |

The ability archive shows each requirement, cooldown, energy cost, availability, and upgrade level. Ability upgrades spend Shards, improve the existing combat effect, cap at level 5, and persist in the profile.

## Shop catalog and in-game currency

The Shop has **Eyes**, **Swords**, **Susano**, **Special Abilities**, and **Summons** categories. Shop entries use their existing gameplay catalog records. Eyes, weapons, Sage Modes, Susano aura variants, and summons can be bought with Money or Diamonds earned by playing. There are no real-money purchases, premium accounts, subscriptions, or online checks.

Enemies award 25 Money, 2 Diamonds, and 1 Shard. Bosses award 150 Money, 10 Diamonds, and 5 Shards. Unlocked missions can be replayed to earn more currency. Purchases, equipped items, currencies, and upgrades are saved. Special abilities unlock from the mission and enemy-defeat progression shown in the archive; they are not sold as shop purchases.

### Eyes

| Eye | Price | Implemented action |
| --- | ---: | --- |
| Three-Tomoe Sharingan | 80 Diamonds | Fireball and enemy health reading |
| Mangekyo Sharingan | 130 Diamonds | Fireball and Genjutsu; bosses resist sooner |
| Byakugan | 100 Diamonds | Shuriken, Rotation defense, and scouting camera |
| Kakashi Eye | 180 Diamonds | Five chakra attacks; switch Fire, Water, Wind, Earth, and Lightning |
| Eternal Mangekyo Sharingan | 220 Diamonds | Fireball and damage-reducing, damage-boosting Susano transformation |
| Jougan | 200 Diamonds | Shuriken, short time freeze, and hazard/objective vision |
| Rinnegan | 260 Diamonds | Almighty Push damages enemies, interrupts projectiles, and disables nearby spikes |

Three-Tomoe Sharingan, Mangekyo, and Eternal Mangekyo share the enemy-health-reading passive. Powers use the existing configured eye/ability controls shown in Settings and the HUD.

### Swords and special items

The Reaper Scythe is the starting weapon. The three purchasable swords are Moonlit Katana (450 Money), Chakra Blade (800 Money), and Lightning Blade (90 Diamonds). They use the existing 3D weapon rendering and melee damage modifiers.

The three implemented Sage Modes are Toad (120 Diamonds), Snake (140 Diamonds), and Six Paths (240 Diamonds). Their timed effects increase scythe damage and regenerate health while active: Toad regenerates slowly, Snake regenerates fastest, and Six Paths lasts longest with the strongest damage boost. Two Susano variants are currently implemented: Azure and Crimson (100 Diamonds each). They change the aura color of the existing Susano form; they are not separate character-specific armored models.

## Summons

All 29 current summon definitions appear in the Summons shop and share the existing ownership, equip, call, dismiss, upgrade, and save systems:

- **Toads:** Small Toad (25), Giant Battle Toad (60), Armored Toad (50), Legendary Toad (120), Toad Boss Summon (180).
- **Dogs:** Ninja Tracking Dog (20), Pack of Ninja Dogs (45), Armored Combat Dog (55), Elite Summoning Hound (110).
- **Birds:** Scout Bird (20), Giant Combat Bird (65), Eagle Summon (55), Legendary Sky Bird (130).
- **Snakes:** Small Combat Snake (20), Giant Snake (70), Armored Snake (58), Legendary Serpent (130), Giant Boss Serpent (190).
- **Other:** Healing Slug (45), Giant Turtle Summon (100), Wolf Pack Summon (50), Panther Summon (55), Dragon Summon (150), Shadow Beast Summon (125).
- **Legendary:** Giant Toad Warrior (220), Ancient Serpent (250), Sky King Bird (230), Colossal Turtle Guardian (240), Shadow Emperor Beast (300).

Prices are Diamonds. One companion can be active at a time. Toads and turtles guard or knock enemies back; dogs and wolves chase and interrupt; birds scout and dive; snakes bite, poison, and bind; the slug heals; panthers ambush; dragons and legendary summons use stronger area attacks. Shard upgrades improve the existing health, damage, duration, speed, healing, and cooldown stats, up to each entry's level cap.

**Visual limitation:** summon bodies and their movement are procedural game geometry and animation states. These catalog entries do not include bespoke creature meshes, authored animation sets, or dedicated summon voice/music assets.

## Upgrade screen

The Upgrade screen covers every currently supported upgrade path:

- All seven core abilities (up to level 5), with their combat-effect modifiers.
- Owned summons (up to each summon definition's level cap, currently level 5).
- Susano duration (base 3 seconds, up to 12 seconds at level 9).

Ability upgrades cost `6 x (current level + 1)` Shards. Summon upgrade cost is the entry's base cost multiplied by `current level + 1` (base 8 Shards, maximum level 5). Susano duration upgrades cost `15 + 10 x current level` Shards and require the Eternal Mangekyo Sharingan to be owned. All effects and balances persist in the existing progression save.

Weapons, eyes, Sage Modes, and aura variants do not currently have separate upgrade levels; their existing equipment effects are shown in the Shop. The Upgrade screen does not add duplicate or cosmetic-only upgrade controls.

## Settings and saves

Settings save separately to `%AppData%\Kadaya\settings.json`; campaign progression remains in `%AppData%\Kadaya\save.json` and the legacy `%AppData%\Kadaya\save.txt` migration is retained.

Settings include master/music/effects volume and mute, HUD visibility toggles and scale/offset, keyboard rebinds with duplicate/reserved-key rejection and restore defaults, 1280x720 / 1600x900 / 1920x1080 resolution, fullscreen/windowed, VSync, Low 100% / High 125% / Very High 150% / Ultra 200% internal render scale, and camera sensitivity. New settings default to Ultra. Ultra also rebuilds the procedural sphere and round meshes at higher tessellation; if the GPU cannot allocate its render targets, the game falls back to the next supported preset and saves that choice. The higher render scale costs GPU memory and frame time, so use a lower preset on modest PCs. Changes apply immediately. F6 cycles quality, F7 resets the camera, and F11 toggles fullscreen.

Unreadable progression files are preserved and protected from being overwritten. Test Mode is available in Debug and Release: toggle it with F9 or Settings → Camera → Test Mode. It grants every implemented ability, shop item, summon, and maximum upgrade level without writing those grants into a save. Test Mode always uses `%AppData%\Kadaya\test-save.json`; normal play uses `%AppData%\Kadaya\save.json`. Test Mode starts disabled each launch, and starting a new campaign while it is enabled resets only the test profile.

## Audio and icon

The existing audio files are compiled through `Content/Content.mgcb` and loaded through the MonoGame content manager:

- `Content/Audio/MainMenu_Music.mp3` loops while the title menu is active and stops on game entry.
- `Content/Audio/Reaper_Scythe_Attack.wav` plays once when the scythe's combat hit event fires.
- `Content/Audio/Reaper_Transformation.mp3` plays once when Eternal Mangekyo activates Susano.

Audio volume and mute settings apply to the menu song, imported effects, and existing procedural effects. The existing audio code has 19 procedural sound-effect definitions and three generated, looping level-music tracks. Missing imported files fall back to procedural sounds where an event has a fallback.

The existing `Content/UI/kadaya_icon.ico` is used for the Windows executable and Inno Setup installer icon. The menu logo and particles are drawn with existing game primitives; no additional PNG or duplicate icon file is introduced.

## Catalog audit and remaining asset work

Fully wired gameplay systems currently include the seven core abilities, seven eye entries, three shop swords plus the starting Scythe, three Sage Modes, two Susano aura variants, all 29 summon definitions, currency rewards, saved ownership/equipment, and supported upgrade effects. Catalog items that have no underlying separate character/model variant are not presented as implemented: only Azure and Crimson Susano color variants exist, and no Itachi, Sasuke, Shisui, Madara, Obito/Kakashi, Indra, Hagoromo, or Sarada-specific Susano models are currently included.

The player, enemies, realms, weapons, summon previews, and supernatural effects use the project's existing primitive/procedural 3D rendering. The added realm landmarks also use those procedural meshes. No external character/environment assets were imported, so no third-party model licence or attribution is claimed. Dedicated authored character meshes, rigged skeletal animation packs, authored boss models/attack sets, and extra authored icons are not present in the current content. The Shop uses procedural previews from the real catalog entries rather than claiming missing artwork exists.

## Verification commands

```powershell
dotnet run --project Kadaya.Tests/Kadaya.Tests.csproj -c Release
dotnet build Kadaya.csproj
dotnet build Kadaya.csproj -c Release
dotnet publish Kadaya.csproj -c Release -r win-x64 --self-contained true -o publish
```
