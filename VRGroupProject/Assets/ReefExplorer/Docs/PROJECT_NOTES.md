# Reef Rescue — Project Notes

## Problem, concept and objectives

A trainee diver’s first independent survey: Reef Buoy Seven went silent. Restore the signal, recover the previous (simulated) survey, finish today’s animal observations, and return a water sample before the expedition moves on.

Measurable objectives:

1. Restore the buoy by inserting the power cell.
2. Scan clownfish, sea turtle and ray once each across three zones.
3. Fill and return the sample bottle, submit the dive log, and view the comparison.

## Why VR suits this experience

VR supports presence through pointing, grabbing and looking around a 3D reef. Desktop mode keeps the same mission playable without a headset.

## Locomotion and tracking origin

- Default: teleportation + snap turning (Starter Assets XR Origin).
- Tracking origin: Starter Assets default (seated or standing).
- Comfort: no forced camera shake/head bob; fog and particles kept modest.

Headset comfort was **not** validated in the automated setup session.

## Immersion / D.I.C.E. (simple notes)

- **Sensory**: blue-green fog, particles, station hum, buoy signal, scanner tones.
- **Challenge**: find cell, aim scanner, fill and return bottle.
- **Narrative**: silent buoy + Maya guidance.
- **Social**: single-player coursework (group development).

## Architecture

- `MissionController`: state machine including `RepairBuoy`.
- Interactions: scanner, bottle, zones, bottle socket, buoy power socket, respawn.
- Survey: ScriptableObject species/baseline, analyzer, JSON saver.
- Input: exclusive XR Origin vs DesktopPlayer.
- UI: world-space board + HUD + pause/settings.
- Editor menus: build, validate, batch setup.

## Advanced feature — recovered survey comparison

Restoring the buoy unlocks baseline data for the ending comparison. Observations are compared by species/zone with educational disclaimers. Logs save under `Application.persistentDataPath/DiveLogs`.

## Asset and audio credits

See `ASSET_CREDITS.md`. Procedural WAV tones from Editor tools. Unity URP + XRI samples. No paid third-party models claimed in this build.

## AI assistance

See `AI_ASSISTANCE.md`.

## Known limitations

- Play Mode / build / headset tests must be run by the team.
- Animals are stylized primitives with wander motion.
- Procedural audio is functional, not production scoring.
- Ordinary 3D audio positioning (no binaural spatializer plugin).
