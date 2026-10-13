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
W/A/S/D move in the camera direction | Space jump (x2) | Left mouse button scythe combo
Shift dodge | E shadow clones | Right mouse button Rasengan (35 soul energy) | Mouse wheel zoom
Enter confirm / continue | N new game | F6 render quality | F7 reset camera | F11 fullscreen | Esc quit

