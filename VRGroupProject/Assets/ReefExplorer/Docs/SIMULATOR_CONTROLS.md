# XR Device Simulator — Actual Bindings

Source assets (do not guess from memory):

- `Assets/Samples/XR Interaction Toolkit/3.6.1/XR Device Simulator/XR Device Simulator Controls.inputactions`
- `Assets/Samples/XR Interaction Toolkit/3.6.1/XR Device Simulator/XR Device Controller Controls.inputactions`

## Move / aim a controller

1. Hold **Left Shift** = manipulate **left** controller  
2. Hold **Space** = manipulate **right** controller  
3. Move the **mouse** to translate/rotate that controller  
4. Press **R** to toggle mouse translate vs rotate mode  
5. Scroll wheel can adjust along additional axes depending on mode  

## Grab vs activate

- **G** = Grip = **Select** = **Grab**
- **Left Mouse Button** = Trigger = **Activate** (use tool, not grab)

If the cube does not grab:

1. Confirm the diagnostics HUD shows hover on `Cube`
2. Confirm you pressed **G**, not left click
3. Confirm only one XR Origin exists
4. Aim the controller ray at the cube or move the controller into near-interaction range
