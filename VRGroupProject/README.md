# Reef Rescue — The Silent Signal

Unity **6000.6.4f1** underwater VR coursework project (URP, Input System, XR Interaction Toolkit **3.6.1**, OpenXR **1.18.0**).

Folder for custom work: `Assets/ReefExplorer/`.

## What you do in the mission

1. Choose **Desktop** or **VR / Simulator**.
2. Complete a short training (move, grab, activate).
3. Pick up the **Scanner** and **SampleBottle**.
4. Walk to **Reef Buoy Seven**, pick up the **PowerCell**, insert it into the buoy socket.
5. Scan clownfish, sea turtle and ray in three reef areas.
6. Fill the bottle at the sample point, return it to the station holder, **Submit Log**.
7. Read the simulated survey comparison and Credits.

## Exact Unity steps (beginner)

1. Open the project in **Unity Hub** with editor **6000.6.4f1**.
2. Wait until the bottom-right spinner finishes (scripts compile).
3. Click menu **Reef Explorer → 0. Run Full Setup (Fix Grab + Build Scene)**.  
   Expected: dialog says Done. Scene `Assets/ReefExplorer/Scenes/ReefExplorer.unity` is created/refreshed.
4. Click **Reef Explorer → 6. Validate ReefExplorer Scene**.  
   Expected: list of OK lines for buoy, power cell, scanner, zones.
5. Open **Assets/ReefExplorer/Scenes/ReefExplorer.unity** (double-click in Project).
6. Press **Play**.
7. Click **Desktop**, then **Start Dive**.
8. Follow the on-screen objective text until mission complete.

**Important:** Stop Play before using Reef Explorer menus 0–6.

## Desktop controls (shown in HUD after mode select)

| Action | Binding |
|--------|---------|
| Move | WASD |
| Look | Hold Right Mouse |
| Pick up | E or Left Click |
| Activate scanner | Hold Left Click while holding scanner |
| Drop | Q |
| Fill bottle in sample zone | E |
| Hotkeys | 1 practice tool, 2 scanner, 3 bottle, 4 power cell |
| Pause / settings | Esc |

## VR / Simulator controls (Starter Assets Device Simulator)

| Action | Binding |
|--------|---------|
| Aim right controller | Hold **Space** + mouse |
| Aim left controller | Hold **Left Shift** + mouse |
| Grab | **G** |
| Activate | **Mouse Left Button** |
| Teleport / turn | Starter Assets locomotion on XR Origin |

Grab is **G**, not mouse click.

## Diagnostic grab scene

1. Open `Assets/VRTestScene`.
2. Play → hold **Space**, aim at Cube, press **G**.

## Build Windows standalone

1. Run setup menu 0 once (not in Play Mode).
2. **File → Build Settings**.
3. Ensure `Assets/ReefExplorer/Scenes/ReefExplorer.unity` is checked.
4. Platform **Windows**, **Build**.
5. Run the `.exe` and complete the mission with keyboard/mouse (no headset).

## Docs

- `Assets/ReefExplorer/Docs/REQUIREMENTS_CHECKLIST.md`
- `Assets/ReefExplorer/Docs/TEST_CHECKLIST.md`
- `Assets/ReefExplorer/Docs/PRESENTATION_OUTLINE.md`
- `Assets/ReefExplorer/Docs/AI_ASSISTANCE.md`
- `Assets/ReefExplorer/Docs/ASSET_CREDITS.md`
- `Assets/ReefExplorer/Docs/PROJECT_NOTES.md`

## Spatial audio note

World sounds use ordinary Unity 3D `AudioSource` / `PlayClipAtPoint` positioning. A dedicated binaural spatializer plugin is **not** configured.

## Group ownership suggestion (3–4 people)

1. XR/desktop input + grab/sockets  
2. Environment, animals, audio  
3. Mission flow + survey analysis  
4. UI, docs, testing, build  

Adjust to your actual team. Do not invent completed work.
