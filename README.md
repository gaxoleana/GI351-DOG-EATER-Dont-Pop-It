# Don't Pop It

Unity 2D game by DOG EATER. Event work for **BU Game On! by Revy Event** is on `codex/bu-game-on-revy`.

## Open the project

1. Install **Unity 6000.5.10f1** through Unity Hub. This is the version pinned in `ProjectSettings/ProjectVersion.txt`.
2. Include **Web Build Support** for the existing Web profile. Windows Build Support is needed for desktop builds.
3. Add this repository's root folder in Unity Hub and open it. Let Unity restore the packages and finish importing assets.
4. Open `Assets/V.1/Scene/MainMenuV.1.unity` and enter Play mode.

Hold **Space**, the **left mouse button**, or touch to blow the bubble; release to stop. **Escape** opens the pause menu during gameplay.

## Project layout

| Folder | Purpose |
| --- | --- |
| `Assets/V.1` | Current game scenes, scripts, prefabs, art, and sound |
| `Assets/Assets`, `Assets/Scenes` | Earlier prototype content; retained for the team |
| `Assets/Sample Assets`, `Assets/TextMesh Pro` | Imported assets and examples; retained for the team |
| `Assets/Settings` | Input actions, URP settings, and build profiles |
| `Assets/Editor` | Project validation menu and batch entry point |
| `Packages` | Dependency manifest and resolved package lockfile |
| `ProjectSettings` | Shared editor and player configuration |
| `tools` | Read-only repository checks |

`Library`, `Temp`, `Logs`, `Obj`, and `UserSettings` are local Unity output. Let Unity manage these folders; they do not belong in source control. Existing assets, example content, archive metadata, and generated performance-test resources are preserved until the team decides otherwise.

## Validate before sharing changes

From the repository root, with Python 3.10+ and Git installed:

```powershell
python tools/validate_project.py
git diff --check
```

The repository check catches manifest/lockfile mismatches, unresolved dependencies, missing `.meta` files, duplicate GUIDs, and invalid build scene paths or GUIDs. It reports existing archive metadata and performance-test resources as warnings and leaves them in place. It does not require `Library` or change any files.

In Unity, select **Tools > Don't Pop It > Validate Project**. This loads enabled build scenes in temporary preview scenes and checks their hierarchies and dependent prefabs for missing scripts, including inactive objects. Your open scenes are preserved. Results are written to `Logs/ProjectValidation.json`.

With the project closed in the editor, the same check can run from PowerShell:

```powershell
$unityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Unity.exe'
$projectPath = (Get-Location).Path
& $unityEditor -batchmode -nographics -quit -projectPath $projectPath `
    -executeMethod DontPopIt.Editor.ProjectValidation.RunBatch `
    -logFile "$projectPath/Logs/ProjectValidation.log"
```

An active Unity license is required. Batch validation fails with a nonzero Unity exit code when project validation fails. Check the log after the editor process exits.

For a release, also check Play mode: start a run, pause/resume, retry, return to the menu, change and reopen audio settings, and enter Play mode twice. Check a built player as well; editor validation does not prove a Web or desktop build works.

## Build and dependency essentials

- Build scenes are ordered **MainMenuV.1**, then **GameSceneV.1**.
- Use `Assets/Settings/Build Profiles/Web.asset` for Web builds and put output in `Builds/`. Web defaults use a 1080 × 1920 portrait canvas and decompression fallback. Serve the build over HTTP(S); test on the actual event browser.
- Keep the editor version pinned until an editor upgrade is agreed with the team. URP **17.5.0** matches the pinned Unity 6000.5 editor and its existing resolved dependencies.
- Update packages through Unity's Package Manager and review `manifest.json` and `packages-lock.json` together. Run both validators and smoke-test the game after updates. Existing preview packages and optional tools remain installed.
- Keep each asset's `.meta` file when moving or renaming it. Prefer moving assets inside Unity so serialized references keep their GUIDs.
- Unity assets use Force Text and Visible Meta Files. `.gitattributes` keeps source/YAML line endings consistent; binary art, sound, and fonts remain binary. No Git LFS setup is required by these changes.
- Preserve imported licenses and attribution alongside third-party assets. Agree on removals or reorganizations with the team first.
