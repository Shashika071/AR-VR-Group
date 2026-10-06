# Manual Test Checklist — Reef Rescue

Tester name: ________  
Date: ________  
Machine: ________  
Mode tested: Desktop / XR Simulator / Standalone (circle one)

Mark each item Pass / Fail / Not run. Do not invent results.

## A. VRTestScene grab (Editor simulator)

- [ ] Only one XR Origin in Hierarchy
- [ ] Only one AudioListener
- [ ] Hold **Space**, aim at Cube, press **G** to grab (not left click)
- [ ] Cube moves with controller; release G to drop

## B. Mission — Desktop

- [ ] Open `Assets/ReefExplorer/Scenes/ReefExplorer.unity`
- [ ] Play → **Desktop** → **Start Dive**
- [ ] Briefing shows buoy / wildlife / sample goals
- [ ] Tutorial: WASD through gate; grab practice tool; activate
- [ ] Pick up **Scanner** and **SampleBottle**
- [ ] Follow path markers to **MonitoringBuoy** (amber)
- [ ] Pick up **PowerCell**, insert into **BuoyPowerSocket**
- [ ] Buoy turns green; Maya feedback appears; baseline unlocked
- [ ] Visit Coral Garden, Seagrass Crossing, Sandy Passage
- [ ] Scan clownfish, turtle, ray (hold Left Click while aiming)
- [ ] Each scan shows a short educational fact
- [ ] Duplicate scan rejected
- [ ] Fill bottle at SampleZone (E); label/liquid updates
- [ ] Return filled bottle to BottleHolder
- [ ] Incomplete Submit Log explains what remains
- [ ] Complete Submit shows comparison + JSON path + thank-you
- [ ] Restart resets buoy, tools, observations
- [ ] Esc pause: resume / volume / mute / quit respond

## C. Mission — Editor XR simulator

- [ ] Choose **VR / Simulator**
- [ ] Teleport + snap turn work
- [ ] Grab/activate power cell into buoy socket
- [ ] Full mission completable
- [ ] Lost tool / power cell respawns when dropped out of bounds

## D. Build

- [ ] Build Settings includes ReefExplorer scene
- [ ] Windows `.exe` launches outside Editor
- [ ] Desktop mode finishes the mission in the build

## Limits

Simulator testing cannot prove headset comfort, presence, physical reach or real-world scale.

## Automated session (do not treat as teammate testing)

| Item | Status |
|------|--------|
| Package / script implementation | Done in source |
| Unity Play Mode full run | **Not run here** |
| Windows build launch | **Not run here** |
| Headset validation | **Not run** |
