# itch.io: 9:16, FHD reference, native display resolution

## Build

1. Stop Play mode and save scene edits.
2. Choose **Tools > Don't Pop It > Select itch.io 9x16 Native Profile**. Wait for the Web platform switch/import to finish.
3. Choose **Tools > Don't Pop It > Build itch.io Web Release**.
4. Upload the resulting ZIP from `Builds/ItchWeb/` to itch.io as a playable HTML5 game. `index.html` is at the ZIP root.

The saved profile is `Assets/Settings/Build Profiles/Itch.io 9x16 Native.asset`. It uses MainMenuV.1 followed by GameSceneV.1, IL2CPP, Gzip, and release settings. Its scene list is independent of the global scene list. The build script uses this profile directly.

## itch.io embed options

- Use **Embed in page** with a **540 × 960** viewport, or **Click to launch in fullscreen**.
- Enable **Fullscreen Button** for the embedded version. The template also includes its own fullscreen button.
- Keep scrollbars disabled.

## Resolution behavior

1080 × 1920 is the portrait FHD reference size. The template fits the game to a centered 9:16 frame, with side borders on landscape displays. Unity automatically matches its rendering buffer to the frame's physical device pixels, including the browser's current pixel ratio. There is no 1920-pixel height limit or forced low-DPI setting.

Fullscreen examples at 100% browser zoom:

| Physical display | Game render area |
| --- | --- |
| 1080 × 1920 portrait | 1080 × 1920 |
| 1440 × 2560 portrait | 1440 × 2560 |
| 2160 × 3840 portrait | 2160 × 3840 |
| 2560 × 1440 landscape | 810 × 1440 |
| 3840 × 2160 landscape | 1215 × 2160 |

Windowed/embedded rendering follows the available frame size. OS display scaling is accounted for through device pixel ratio. Fullscreen changes and iframe resizing recalculate the portrait frame; moving between displays uses Unity's current device pixel ratio.

The browser controls fullscreen permission. If the template's fullscreen request is blocked by the iframe, use itch.io's fullscreen button.

Sources: [Unity canvas sizing](https://docs.unity.com/en-us/engine/6000.7/manual/platform-specific/webgl/develop/canvas-size), [itch.io HTML5 export and fullscreen options](https://itch.io/docs/creators/html5).
