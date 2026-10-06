# Reef Explorer — The Missing Survey

Unity 6 (6000.6.4f1) underwater VR coursework project using URP, Input System, XR Interaction Toolkit 3.6.1 and OpenXR.

## Quick start

1. Open this folder in **Unity Hub** with editor **6000.6.4f1**.
2. Wait for scripts to compile.
3. In the menu bar click:
   - **Reef Explorer → 0. Run Full Setup (Fix Grab + Build Scene)**
   - **Reef Explorer → 4. Apply Cute Fish Pack Models** (uses `Assets/Cute Fish Pack - Feb 2020`, CC0)
4. Test grab:
   - Open `Assets/VRTestScene`
   - Press Play
   - Hold **Space** (right controller), aim at the Cube, press **G** to grab
5. Play the mission:
   - Open `Assets/ReefExplorer/Scenes/ReefExplorer.unity`
   - Press Play
   - Choose **Desktop** or **VR / Simulator**, then **Start Dive**

## Simulator controls (from installed XR Device Simulator input actions)

These are the actual bindings in  
`Assets/Samples/.../XR Device Simulator/XR Device Simulator Controls.inputactions` and  
`.../XR Device Controller Controls.inputactions`:

| Action | Binding | Meaning |
|--------|---------|---------|
| Manipulate Left | Left Shift (hold) | Move/aim left controller with mouse |
| Manipulate Right | Space (hold) | Move/aim right controller with mouse |
| Mouse Delta | Mouse move | Translate or rotate active device |
| Toggle mouse transform mode | R | Switch translate/rotate mouse mode |
| Grip / Select (grab) | **G** | Grab hovered/pointed interactable |
| Trigger / Activate | **Mouse Left Button** | Activate held tool (scanner) |
| Primary / Secondary | B / N | Controller buttons |
| WASD / QE | Move simulated devices / height | Not player locomotion |

Important: **mouse click is Activate, not Grab**.

## Desktop controls

| Action | Key |
|--------|-----|
| Move | WASD |
| Look | Hold Right Mouse (or locked cursor) |
| Pick up | E or Left Click |
| Activate scanner | Hold Left Click while holding scanner |
| Drop | Q |
| Fill bottle in sample zone | E |
| Pause | Esc |

## Project layout

Custom content lives under `Assets/ReefExplorer/`:

- `Scripts/` mission, interaction, input, survey, UI, audio
- `Editor/` scene/grab setup menus
- `Scenes/` generated `ReefExplorer.unity`
- `Data/` species + baseline ScriptableObjects
- `Docs/` project notes and test checklist

`Assets/VRTestScene.unity` remains the grab/diagnostics scene.

## Tracking origin / comfort

Uses the Starter Assets XR Origin defaults (teleport + snap turn). Suitable for seated or standing play. Comfort and presence were **not** validated on a physical headset in the automated setup session.

## Survey analysis

Baseline and species definitions are ScriptableObjects. Completing the mission writes a JSON dive log to:

`%userprofile%/AppData/LocalLow/<Company>/<Product>/DiveLogs/`

Results are labelled as simulated educational data.

## Build (Windows)

1. Run the Reef Explorer setup menu once.
2. **File → Build Settings**
3. Ensure `Assets/ReefExplorer/Scenes/ReefExplorer.unity` is enabled (setup adds it).
4. Platform: Windows
5. **Build** to a folder outside the repo (for example `C:\Builds\ReefExplorer\`)
6. Launch the `.exe` and complete the mission in Desktop mode

Build output folders should stay out of git (`Build/`, `Builds/` are ignored).

## Tests

- Edit Mode tests: `Assets/ReefExplorer/Tests`
- Manual checklist: `Assets/ReefExplorer/Docs/TEST_CHECKLIST.md`
- Design notes: `Assets/ReefExplorer/Docs/PROJECT_NOTES.md`

## Suggested group ownership

1. **Interaction / XR** — grab, scanner, sockets, simulator docs  
2. **Environment / audio** — scene art pass, mixer, particles  
3. **Mission / data** — state machine, survey analysis, JSON  
4. **UI / testing** — panels, checklist, Windows build verification  

## Milestone commit suggestions

1. Fix VRTestScene grab + diagnostics  
2. Mission scripts + survey data model  
3. Generated ReefExplorer scene + tools  
4. Desktop mode + UI polish  
5. Docs, checklist, build instructions  

## Optional .NET IntelliSense fix

If Cursor shows `Assembly-CSharp.csproj` load warnings, install the  
[.NET Framework 4.7.1 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net471).
