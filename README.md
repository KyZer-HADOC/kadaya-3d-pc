# KADAYA (3D)

3D dark ninja action platformer (MonoGame / C#). You are The Grim Reaper.
Perspective 3D camera, lit + fogged scenery built from primitive shapes, bosses, checkpoints.
Run left to right through each level, cross pits, dodge spikes, cut down ninja / samurai / archers,
defeat the boss, step through the Soul Gate. Die and you restart from the last shrine of the SAME level (saved).

## Camera and quality
Move the mouse to orbit the 3D camera. After a short pause, it gently recenters behind your movement. Use the mouse wheel to zoom.
Press F7 to reset the camera, or F6 to cycle render quality from 100% to 125% and 150%.

## Run
1. Install the .NET 8 SDK: https://dotnet.microsoft.com/download
2. In this folder: `dotnet run`

## Windows installer
Every push to `main` builds `setup.exe` with GitHub Actions. Download it from the latest successful `Build KADAYA setup.exe` run under **Actions > Artifacts > KADAYA-setup**.

## Controls
W/A/S/D move in the camera direction | Space jump (x2) | Shift dodge | Mouse wheel zoom
LMB melee combo | E eye power / Shadow Clone | RMB Rasengan | Q eye power / Shuriken
F eye mode / Kenjutsu | C Hidden Jutsu | R Teleportation Kunai | Z Sage Mode
Tab ability archive | P Premium Shop | M Premium Summon Vault | F6 quality | F7 camera reset
In either shop: Up/Down select | B buy | Enter equip | U upgrade | G summon | X dismiss
Enter confirm / continue | N new game | F11 fullscreen | Esc quit

## Ability progression
The Scythe is the starting melee weapon and keeps its existing combo. Abilities unlock from enemy defeats and boss-gate completions:

| Ability | Requirement |
| --- | --- |
| Scythe Combo | Available from the start |
| Shuriken Jutsu | 3 total enemy defeats |
| Kenjutsu | Shuriken Jutsu and 8 total enemy defeats |
| Hidden Jutsu | Kenjutsu and completion of mission 1 |
| Shadow Clone | Hidden Jutsu and 6 enemy defeats after mission 1 |
| Teleportation Kunai | Shadow Clone and completion of mission 2 |
| Rasengan | Teleportation Kunai and 5 enemy defeats after mission 2 |

Shuriken, Kenjutsu, Hidden Jutsu, and Teleportation Kunai have working combat actions. Shadow Clone creates one copy for 5 seconds, costs 40 soul energy, and has a 12-second cooldown. Rasengan costs 35 soul energy and has a 1.1-second cooldown. The `Tab` archive shows requirements, costs, cooldowns, and upgrade levels; `U` upgrades the selected unlocked ability with Shards.

## Saves and development Test Mode
Normal progression is stored in `%AppData%\Kadaya\save.json`. The old `%AppData%\Kadaya\save.txt` level value is migrated the first time the JSON profile is created; migration leaves `save.txt` untouched. Press `N` from the title screen to start a new campaign and reset normal progression.

If an existing JSON profile cannot be read, the game preserves that file and blocks writes to the recovered in-memory defaults. A warning appears in-game; copy the unreadable save aside before replacing it manually. Write failures also show an in-game warning.

Debug builds can use a separate profile at `%AppData%\Kadaya\test-save.json`. Press `F9` in a Debug build to toggle Test Mode. It grants access to all eye powers, shop items, summons, and maximum upgrades without adding permanent purchases to the normal profile. To start in Test Mode, create `%AppData%\Kadaya\development.json` with:

```json
{
  "development_test_mode": true
}
```

Switching profiles saves each one to its own file; the setting, toggle, and Test Mode grants are excluded from Release builds. New Game resets campaign milestones while keeping permanent shop purchases, wallet balances, and upgrades.

## Premium Shop and eye powers
Press `P` in gameplay. Enemies award Money, Diamonds, and Shards; bosses award larger amounts. Buy weapons and Sage Modes with Money or Diamonds, and permanently unlock eyes with Diamonds. Shop ownership and equipment share the existing profile save. There are no real-money payments.

Equip an eye, then use its powers:

| Eye | Q | E | F |
| --- | --- | --- | --- |
| Three-Tomoe Sharingan | Camera-aimed Fireball with burning damage | Shadow Clone | — |
| Mangekyo Sharingan | Fireball | Genjutsu disables a target (bosses resist sooner) | — |
| Byakugan | Shuriken if no other eye is equipped | Rotation blocks incoming hits | Toggle independent level scouting camera |
| Kakashi Eye | Fire, Water, Wind, Earth, or Lightning attack | Shadow Clone | Cycle the five chakra types |
| Eternal Mangekyo | Fireball | Susanoo transformation | — |
| Jougan | Shuriken if no other eye is equipped | Selective 1.5-second world freeze | Toggle hazard/objective vision |
| Rinnegan | 10-meter 3D Almighty Push | Shadow Clone | — |

The Rinnegan push kills ordinary enemies and disables nearby spike hazards; bosses take damage and knockback. Susanoo starts at 3 seconds and upgrades to a 12-second maximum with Shards in the Premium Shop. Sword purchases change the held weapon and melee damage. Sage Modes activate with `Z` and improve damage or regeneration for a limited time.

## Premium summoning vault
Press `M` during gameplay to browse 29 permanent premium summons. Every summon begins locked in normal play and must be bought with Diamonds earned in-game (2 per ordinary enemy defeat, 10 per boss); no real-money purchases are used. `Enter` equips an owned summon, `G` calls the equipped/selected summon, and `X` dismisses it. One summon can be active at a time. `B` buys the selected entry and `U` upgrades an owned summon with Shards (8, then increasing by 8 per level, up to level 5). Ownership, equipped summon, all currencies, and upgrades are saved and survive `N` starting a new campaign.

Summons use role-specific procedural 3D placeholder models, movement, attacks, support effects, duration, health, cooldown, and upgrade stats. Toads/turtles guard and knock enemies back; dogs/wolves chase and interrupt; birds scout and dive; snakes bite, poison, and briefly bind ordinary foes; the slug heals; panthers ambush; dragons and legendary summons use area attacks. The Shadow Emperor has a dark-red aura and signature area pulse. `F9` Test Mode in Debug builds makes every summon available at maximum upgrade without granting normal-profile ownership or spending currency. Custom summon meshes, authored animation sets, and bespoke audio remain future asset work.

## Verification
Run the console checks with `dotnet run --project Kadaya.Tests/Kadaya.Tests.csproj`, and build Debug/Release with `dotnet build Kadaya.csproj` and `dotnet build Kadaya.csproj -c Release`. The GitHub Actions workflow builds the self-contained Windows installer.
