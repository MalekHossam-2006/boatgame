# Development Log

This log records every step of the prototype: what changed, how it was done, and
which files were touched. Each step matches one git commit on the branch
`feature/ship-map-barrels`, so you can open the commit to see the exact diff.

---

## Step 0 - Upgrade the project to Unity 6000.4.7f1

**Goal:** open the project in the Unity version the team has installed (Unity 6.4).

**How it was done:**
- The repository was created with Unity `6000.3.8f1`. It was opened with Unity
  Hub in `6000.4.7f1`, and Unity upgraded the project automatically.
- Unity bumped the package versions to the ones that ship with 6.4
  (URP 17.4.0, Input System 1.19.0, AI Navigation 2.0.12, ...).
- No game code changed in this step.

**Files:** `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`,
`Packages/packages-lock.json`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`,
`ProjectSettings/EditorBuildSettings.asset`.

**Team note:** everyone must open the project with **Unity 6000.4.7f1** from now on.
Opening it with an older version can break the packages.

---

## Step 1 - Bring reusable parts from our pirate prototype

**Goal:** reuse what already worked in our earlier pirate prototype
(`D:\Unity Project\PirateGame\My project (3)`) instead of starting from zero.

**What was copied as-is:**
| File | What it does |
|---|---|
| `Assets/ThirdParty/KenneyPirateKit/` | Low-poly 3D models (ships, barrels, islands, palms, rocks) by Kenney, CC0 license (`License.txt` included). Copied with their `.meta` files so Unity keeps the same asset IDs. |
| `Assets/Scripts/Ocean/Ocean.cs` | Builds a low-poly water mesh and moves its vertices with 4 summed sine waves. `HeightAt(position)` returns the water height so other objects can float. The mesh follows the ship, so the sea looks endless. |
| `Assets/Scripts/Ocean/Bobber.cs` | Makes a small object (a barrel) sit on the waves and slowly spin. |
| `Assets/Scripts/Camera/TopDownCamera.cs` | Camera that looks down at the ship from a fixed angle, follows it smoothly, looks a bit ahead of where the ship is going, and zooms with the mouse wheel. |

**What was simplified (rewritten from the pirate version):**
| File | Change |
|---|---|
| `Assets/Scripts/Ship/ShipController.cs` | The pirate version used 3 "sail levels" you step through with W/S. For this game we wanted simple controls, so the sail levels were replaced by a continuous `Throttle` (-1..1): hold W to speed up, release to slow down, hold S to reverse. Added `mapRadius` so the ship cannot leave the play area. Kept the floating code (the ship model tilts with the waves and leans into turns) and the obstacle push-out (the ship slides off islands and rocks). |
| `Assets/Scripts/Ship/PlayerShipInput.cs` | Removed the cannon keys (Q/E/T). Now: W/S or arrows = move, A/D or arrows = steer, R = restart the level. Uses the new Input System, which the project already had. |

**Editor tooling (not part of the game):**
- `Assets/Editor/ClaudeBridge/ClaudeBridge.cs` + `ClaudeTools/bridge.py`: a small
  editor-only HTTP server (localhost only, protected by a token kept in the
  git-ignored `UserSettings/` folder). The AI assistant (Claude) used it to
  recompile, build scenes, enter Play mode and take screenshots while
  developing. It lives in an `Editor` folder, so it is never included in a game build.
- `.gitignore`: ignore `ClaudeTools/out/` (screenshots).

**Not copied on purpose:** cannons, enemy ships, health, the main menu. They are
planned for later milestones and would make the first prototype too complex.

**Check:** Unity recompiled with no errors.
