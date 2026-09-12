# VLAB visual style and implementation

Source snapshot: 12 September 2026. This guide records the implemented shared treatment and its intended use. It does not assert that every retained material has been replaced or that device performance has been measured.

## Shared palette

The canonical UI colors are in `Assets/VLAB/MainMenu/Runtime/VLABUI.cs`.

| Token | Value | Use |
|---|---|---|
| Background | `#0D1117` | Main dark backdrop |
| Surface | `#142230` | Panels, cards, workbench surfaces |
| Cyan | `#00B4D8` | Primary controls, section ribbons, workbench edge and pointer |
| White | `#F8F9FA` | Main text |
| Muted | `#ABC2CF` | Supporting text |

A valid fallback pointer uses a pale green highlight. High-contrast mode makes it white and increases line width. Scientific apparatus uses additional colors to distinguish meaningful parts: oxygen red, hydrogen white, carbon dark grey; orange/cyan gears and forces; green/yellow/blue/purple/orange cell structures. Color is accompanied by labels, current instructions and result text rather than acting as the only cue.

## Typography and supplied assets

`VLABMenuAssets` binds the shared Liberation Sans font, `VLAB_Vietnamese` TMP activity font and `VLAB Controller Visual` prefab. The runtime uses these shared assets instead of substituting unrelated fonts and controller models per lesson. TMP labels share one activity-owned font material with a white face color; component color then supplies the intended readable tint. Equal-color apparatus parts also share materials. Owned material instances are destroyed with the activity.

Existing physics apparatus, chemistry equipment, microscope and LED assets are retained. The supplementary cell, molecule, vessels and lever use simple original geometry; the gears use generated original extruded silhouettes. The menu's distant scientific points and orbit lines are also generated geometry. External rhythm-game assets, branding, audio and models are not part of this treatment.

Vietnamese is the lesson language. Menu/document localization remains distinct from lesson localization, as stated in Settings. Check accented letters, superscripts, subscripts, degree signs, arrows and multiplication/minus symbols when changing font atlases.

## Spatial composition

- The main application canvas uses world space. It is placed when opening the menu; it does not chase the user's head each frame. Lab pause placement uses a forward collision check to avoid placing the panel through nearby apparatus.
- The menu presents the VLAB brand, four subjects, settings and guidance against a dark stage. Its environment uses a static distant-point mesh and three thin orbit lines, with no continuously simulated particle field.
- A supplementary activity places a fixed upright workbench along the actual entry view ray. The camera position and orientation remain unchanged. The instruction board sits to the left and the apparatus to the right so both fit the entry composition.
- The board displays objective, a concise theory synopsis, a full-theory action, current instruction, result and navigation/reset controls. Full explanation remains accessible through Help; long prose must not silently overflow a short board field.
- Controls retain clear labels and a consistent cyan accent. Hover/press feedback comes from `VLABUI` colors or `LabInteractable`; activation should be explicit.

After modifying placement, capture both a neutral and a downward-pitched entry view. Verify the whole activity, theory action and reset/menu controls remain visible. Recenter must not continuously move already placed panels.

## Scientific model conventions

The plant-cell model is a deliberately open cutaway. It identifies a photosynthetic leaf cell, not every possible plant cell; the explanation distinguishes it from onion epidermal specimens. Organelle sizes and colors are explanatory conventions.

The water activity uses a ball-and-stick model with O at the center and two H atoms. Its bent geometry is approximately 104.5 degrees. The alternative straight arrangement is an explicitly testable incorrect answer.

Qualitative chemistry uses schematic vessels, liquid levels, a controlled transfer indicator and white AgCl particles. It compares a known chloride sample with a known nitrate negative control. Colors/geometry are an educational display; the lesson does not calculate solubility equilibria.

Gear tooth shape and lever tilt are simplified. The displayed relationships come from tooth counts and moments, not from collision-driven mechanical simulation. Preserve these explanations when refining the art.

## Materials, light and mobile cost

The new apparatus and workbench use built-in `Standard` materials; the pointer and menu stage use `Unlit/Color`. URP's presence in Packages does not mean the current scenes use a URP asset. Avoid an unplanned render-pipeline migration or blanket replacement of serialized materials.

The menu uses a solid dark camera background, fog and disabled HDR. Existing lab lighting is retained; the new workbench does not install another persistent lighting or post-processing system. Imported scene lighting still needs visual review in its actual runtime context.

Application quality controls call chemistry `LabPreferences.Apply`: Low disables MSAA and shadows; Balanced uses 4× MSAA, hard shadows, medium shadow resolution and 20 m shadow distance; the exposed Detailed setting uses 4× MSAA, all shadows, high resolution and 35 m distance. A legacy highest setting remains decodable in preferences but is not one of the three cycling choices in the shared UI.

Prefer small bounded meshes, opaque apparatus, simple surfaces and reused font/controller assets. Generated materials and custom meshes must have a clear scene/activity owner. Do not describe nominal settings or a 60 Hz frame-rate request as measured on-device performance.

## Visual verification checklist

Inspect actual Menu and each lesson capture for readable Vietnamese text and scientific glyphs, visible pointer and reticle, clear hover/press feedback, no pink materials, no duplicate controller graphics, apparatus separated from its instruction board, and visible scientific results after completion. Repeat after reset, pause/resume, lesson switching and main-menu reentry.

On an Android viewer, additionally inspect both eyes, distortion/profile selection, practical text readability, comfortable panel depth, head recenter, sustained frame timing and heat. Record actual outcomes in the test matrix rather than treating this checklist as a completed test.
