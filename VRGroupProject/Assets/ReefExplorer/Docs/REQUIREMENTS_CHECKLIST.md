# Reef Rescue — Requirements Traceability Checklist

Authoritative sources compared:
- This coursework prompt (**Reef Rescue — The Silent Signal**)
- Existing project code under `Assets/ReefExplorer` (previously titled Reef Explorer — The Missing Survey)

## Prompt vs previous project title

| Topic | Prompt | Existing before this pass | Status |
|---|---|---|---|
| Title / story | Reef Rescue — Silent Signal, buoy power cell, Maya | Missing Survey, no buoy repair | **Aligned in code/UI this pass** |
| Mission goals | Restore buoy + survey + sample | Survey + sample only | **Buoy restore added** |
| Advanced feature | Recovered survey comparison after buoy | Comparison existed at submit | **Unlocked after buoy + shown at ending** |

Statuses below mean: **Done in source**, **Partial**, **Manual verify needed**, or **Not claimed**.

| # | Requirement | Implementation | Evidence | Verify | Status |
|---|---|---|---|---|---|
| 1 | XRI + OpenXR | Packages in `Packages/manifest.json` | `com.unity.xr.interaction.toolkit` 3.6.1, `com.unity.xr.openxr` 1.18.0 | Package Manager | Done in source |
| 2 | Named action-based input | Starter Assets XR Origin + Desktop Input System keys | XR Rig prefab; `DesktopPlayerController` | Play VR + Desktop | Manual verify |
| 3 | Teleport + ~30° snap turn defaults | Starter Assets locomotion | XR Origin prefab | Simulator teleport/turn | Manual verify |
| 4 | Grab / activate / sockets | `XRGrabInteractable`, scanner activate, bottle + buoy sockets | `Scanner`, `SampleBottle`, `PowerCell`, `BottleHolder`, `BuoyPowerSocket` | Full mission Play | Manual verify |
| 5 | Plausible physics | Rigidbodies + colliders + ToolRespawn | Interaction scripts | Drop/respawn | Manual verify |
| 6 | World-space UI | `MissionCanvas` | Scene builder + `WorldMissionBoard` | Read board in Play | Manual verify |
| 7 | Spatial / 3D audio | `GameAudioHub` + buoy signal + station hum | `PlayClipAtPoint` / spatial `AudioSource` | Hear cues in Play | Manual verify (ordinary 3D, not binaural spatializer) |
| 8 | Advanced survey comparison | `SurveyAnalyzer`, baseline SO, ending panel, JSON | `Scripts/Survey/*`, dive log path | Complete mission | Done in source |
| 9 | Clear start/end | Mode select → briefing → … → Results/Credits | `MissionController` states | Full run | Manual verify |
| 10 | Simulator testing | VRTestScene + Device Simulator | Docs/TEST_CHECKLIST | Teammate run | Manual verify |
| 11 | Windows standalone + desktop fallback | Build settings scene + DesktopPlayer | README build section | File → Build | Manual verify |
| 12 | Asset credits | In-app Credits + docs | `ASSET_CREDITS.md`, board Credits text | Credits button | Done in source |
| 13 | AI disclosure | Docs record | `AI_ASSISTANCE.md` | Presentation | Done in source |
| 14 | Independent teammate testing | Blank checklist | `TEST_CHECKLIST.md` | Teammates fill | Not claimed |
| 15 | Arrival briefing text | Board copy | `WorldMissionBoard` | Read board | Done in source |
| 16 | Interactive training | Move / grab / activate steps | Tutorial states + gates | Tutorial run | Manual verify |
| 17 | Restore silent buoy | Power cell + socket + amber/green buoy | `MonitoringBuoy`, `PowerCell`, `BuoyPowerSocket` | Insert cell | Manual verify |
| 18 | Three reef areas | Coral / Seagrass / Sandy labels + animals | Zones in scene builder | Visit zones | Manual verify |
| 19 | Scanner hold + aim + LOS + duration | `ScannerTool` | Interaction script | Scan animals | Manual verify |
| 20 | Educational fact per species | Species description feedback | `SpeciesDefinition` + mission feedback | Scan once | Manual verify |
| 21 | Water sample fill + label | `SampleZone` / `SampleBottle` | Scripts | Fill bottle | Manual verify |
| 22 | Return + submit incomplete message | Holder + `BuildIncompleteMessage` | MissionController | Incomplete submit | Manual verify |
| 23 | Ending panel + thank-you | Results text | `WorldMissionBoard.OnSubmitted` | Submit log | Manual verify |
| 24 | Restart resets mission | `RestartMission` + events | MissionController + animal/buoy reset | Restart button | Manual verify |
| 25 | Pause / settings volumes | Pause menu sliders (if wired) | `PauseMenuController` | Esc pause | Partial (sliders need scene wiring on rebuild) |
| 26 | Automated tests | EditMode tests | `Tests/*` | Window → General → Test Runner | Manual verify |
| 27 | Presentation support | Outline + demo plan | `PRESENTATION_OUTLINE.md` | Team fill names | Done in source (content skeleton) |

## Not invented

- No teammate names, test dates, or performance numbers are filled in.
- No headset comfort claims.
- No paid Envato assets claimed in this build (stylized primitives + Unity samples + procedural audio).
