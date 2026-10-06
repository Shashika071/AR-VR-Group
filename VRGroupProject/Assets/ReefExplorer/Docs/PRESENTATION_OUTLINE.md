# Presentation outline — Reef Rescue — The Silent Signal

Reported presentation window: **20–30 minutes** + live demo **≤ 10 minutes** including panel questions.  
Confirm against your module handbook if different.

Fill team names, real schedule history and reflections yourselves. Leave blanks until true.

## Slide plan + speaker notes

1. **Title and team**  
   - Title: Reef Rescue — The Silent Signal  
   - Unity 6000.6.4f1, URP, XRI 3.6.1, OpenXR  
   - Speakers: ________

2. **Actual contributions**  
   - List real ownership only (see README group suggestion).  
   - Names: ________

3. **Planned versus actual schedule**  
   - Planned milestones: ________  
   - Actual: ________ (do not invent)

4. **Problem**  
   - Trainee divers need a short guided first survey practice with clear goals.

5. **Vision**  
   - Calm underwater VR mission: restore buoy, record wildlife, return sample.

6. **Measurable objectives** (examples — keep to ≤3)  
   1. Player completes buoy restore + 3 scans + sample return in one session.  
   2. Desktop and VR share the same mission rules.  
   3. Ending shows simulated survey comparison + saved JSON log.

7. **Relevant theory**  
   - Presence / immersion; comfort locomotion; educational data ethics (no false decline claims).

8. **Concept and story**  
   - Maya / Reef Buoy Seven silent signal → power cell → baseline unlock → field survey.

9. **Design rationale**  
   - Teleport + snap turn for comfort.  
   - Amber incomplete / green complete visuals.  
   - Physical power-cell socket as meaningful XR interaction.

10. **Architecture, advanced feature, AI disclosure**  
    - `MissionController` state machine; shared VR/desktop logic.  
    - Advanced: recovered survey comparison + JSON dive log.  
    - AI: see `AI_ASSISTANCE.md`.

11. **Testing**  
    - Automated EditMode tests in `Assets/ReefExplorer/Tests`.  
    - Manual teammate sheet in `TEST_CHECKLIST.md` (results blank until run).  
    - Simulator limits: cannot prove headset comfort/presence.

12. **Challenges and solutions**  
    - Grab/simulator confusion (G vs click).  
    - Pink materials / missing assets → stylized primitives.  
    - Story alignment to buoy restore.

13. **Limitations**  
    - Primitive animals; procedural audio; headset not validated here.

14. **Conclusion and individual reflections**  
    - Reflections: ________ (personal, not invented)

15. **References and asset licences**  
    - Unity samples; procedural audio; `ASSET_CREDITS.md`.

16. **Live demo plan (≤10 min)**  
    1. Show mode select + briefing goals (30s).  
    2. Desktop: grab scanner, walk to buoy, insert PowerCell (2–3 min).  
    3. Scan one animal + show fact toast (1 min).  
    4. Jump/skip or continue to sample return + Submit Log (2–3 min).  
    5. Show comparison panel + JSON path (1 min).  
    6. Panel Q&A remainder.

## Why VR vs AR (brief)

VR surrounds the diver in a continuous underwater space suited to presence and two-handed tool use. AR would keep the real room visible, which fights the reef-immersion goal for this story. Use your course decision framework / D.I.C.E. notes from `PROJECT_NOTES.md`.
