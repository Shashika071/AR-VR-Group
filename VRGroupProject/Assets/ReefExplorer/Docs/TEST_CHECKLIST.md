# Manual Test Checklist

Mark each item with Pass / Fail / Not run.

## A. VRTestScene grab (Editor simulator)

- [ ] Only one XR Origin in Hierarchy
- [ ] Only one AudioListener
- [ ] Diagnostics HUD appears (or press F3)
- [ ] Hold **Space**, move mouse so controller ray hits Cube
- [ ] Press **G** to grab (not left click)
- [ ] Cube moves with controller
- [ ] Release G to drop

## B. Mission — Desktop

- [ ] Open `Assets/ReefExplorer/Scenes/ReefExplorer.unity`
- [ ] Play, choose **Desktop**
- [ ] Controls text shows desktop bindings
- [ ] Start Dive
- [ ] Move with WASD through tutorial gate
- [ ] Pick up practice buoy (E / left click), drop with Q
- [ ] Activate held practice/scanner tool with left mouse
- [ ] Collect scanner and bottle
- [ ] Visit all three zones
- [ ] Scan clownfish, turtle, ray (hold left click while aiming)
- [ ] Duplicate scan is rejected with feedback
- [ ] Fill bottle at sample point (E)
- [ ] Return filled bottle to holder
- [ ] Incomplete submit blocked
- [ ] Complete submit shows comparison + JSON path
- [ ] Restart clears previous progress
- [ ] Pause / mute / quit controls respond

## C. Mission — Editor XR simulator

- [ ] Choose **VR / Simulator**
- [ ] Teleport and snap turn work on valid areas
- [ ] Grab/carry/activate/drop tools
- [ ] Full mission completable
- [ ] Lost tool returns via respawn when dropped out of bounds

## D. Build

- [ ] File → Build Settings includes ReefExplorer scene
- [ ] Windows standalone launches outside Editor
- [ ] Desktop mode can finish the mission in the build

## Verification status from automated work session

| Item | Status |
|------|--------|
| Project/package inspection | Done |
| Duplicate XR Origin removed in VRTestScene | Done in source |
| Grab scripts/diagnostics added | Done in source |
| Scene builder / data / audio generators | Done in source |
| Unity Play Mode grab test | **Not run here** |
| Full mission playthrough | **Not run here** |
| Windows build launch | **Not run here** |
