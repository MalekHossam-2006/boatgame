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
