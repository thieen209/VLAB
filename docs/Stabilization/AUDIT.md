# VLAB stabilization audit — 13 September 2026

Authoritative project: `VLAB`, Unity **6000.5.6f1**. Starting commit **199f1cc** on `integration/unified-vlab-20260910`; clean working tree before audit. Repair branch: `fix/vlab-final-unification`. Recovery folders outside the project are historical copies and remain untouched.

## Evidence and reproducible causes

All five images in `bugthatneeedtobefixed` and all seven attached style references were visually inspected. `bug-matrix.json` records the 20 acceptance requirements and every screenshot's disposition. `scene-inventory.json` records serialized script and prefab dependencies for all 12 build scenes; it is a source inventory, not a runtime singleton assertion.

The fresh baseline passed **108 EditMode** and **46 production PlayMode** cases. Their reports and rendered camera views are preserved in `TestResults/Stabilization`. Two new regression cases then failed before repair: opening the molecule activity disables the actual floor collider, and pause does not recenter following a 90-degree head turn. Existing passing tests did not cover these invariants.

| Area | Observed architecture | Repair target |
|---|---|---|
| Physics | Original commit `d0c8c78` uses the XRI Starter Assets origin with **NearFarInteractor** controllers, far UI interaction and manipulation controls. | Preserve this native two-hand concept and use one shared prefab/configuration across all labs. |
| Pointer regression | Later `VLabSharedPointer` checks only `XRRayInteractor`, misses native near/far hands, and adds a detached model/ray. | Recognize native near/far interaction, prevent duplicate custom/native routing, retain one fallback for decoded controller and desktop input. |
| Chemistry | Separate desktop camera and legacy inactive XR rig; scene-specific presentation loader. | Integrated application owns viewer lifecycle; keep standalone compatibility outside its normal flow. |
| Biology/Engineering | Own cameras and movement roots with shared semantic input, but older screen HUD layouts. | Shared rig and instruction styling; preserve original microscope and circuit logic. |
| Pause | Placement only on open; center ray misses bench edges; no follow cone; world geometry intersects the panel. | Full rectangular volume checks, safe distance, unscaled soft recenter and deliberate UI priority. |
| Supplementary lessons | Global renderer/collider shutdown, including floor; movement disabled; procedural replacement room placed along gaze. | Serialized apparatus boundaries and fixed bench anchors; preserve room, floor, locomotion and player pose. |
| Physics entry | Additive hub includes a large selection panel and console. | Compact bench prompt; six experiments selected from shared pause menu. |

## Visual direction

The reference images contribute compact cyan gradient section headings, floating spatial layers, slender rays, restrained dark backgrounds and prominent elevated branding. VLAB keeps its own blue/cyan/green/orange identity, existing licensed fonts and Vietnamese glyph support. Chemistry's original furniture and room remain the visual baseline. Biology's existing microscope/storage assets and Engineering's circuit/tool assets should remain visible during every lesson; the empty supplementary room is removed.

## Verification boundaries

No hardware result is inferred from Editor tests. The repository's existing decoded controller provider is an input adapter, not proof of BLE packet transport. Android build and physical phone/controller performance must be recorded separately. Unity's current Editor log also contains licensing/account network errors; those are retained in baseline evidence and must not be confused with successful runtime verification.

No required acceptance item is marked complete at this stage.
