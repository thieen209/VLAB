# Separate derived electrochemistry and filtration supports

These are new product fixtures, not supplied VLAB library assets. `ScientificFixtureSupport.cs` creates their individually named solids under the product apparatus parent, outside the original instrument hierarchy. It does not edit or rescale original meshes. Its owned nonmetallic URP material is destroyed by `OnDestroy`; fixture objects and colliders are destroyed with their apparatus parent.

## Local source provenance

Authoritative library: `E:/VLAB/4 LABS/Delivery/VLAB_Library_2026-10-19/Sources`.

- `electrodes/v001/docs/UNITY_HANDOFF.md` and `metadata.json`: original Cu/Zn plate 60 × 20 × 1 mm; electrical contact 55 mm above bottom; graphite rod 5 × 80 mm, contact 75 mm. Original active renderer bounds and contact sockets locate each clamp.
- `beaker/v001/docs/UNITY_HANDOFF.md` and `metadata.json`: original hollow 250 mL beaker, nominal body diameter 70 mm, height 95 mm; original glass MeshCollider supplies actual rim contacts. SDK metadata supplies liquid height.
- `funnel/v001/docs/UNITY_HANDOFF.md` and `metadata.json`: original 60 mm mouth, 60 mm stem, 8 mm outside / 5 mm inside stem diameter; outlet at root, native PaperSocket at 56.069873 mm; actual glass collider determines fork contact height.
- `filter_paper/v001/docs/UNITY_HANDOFF.md`: original folded cone, 45 mm slant / 38.97 mm height. `VlabFunnel.TrySeatPaper` performs its accepted original paper seating.

## C02 / C03 electrode holders

Each vessel receives one explicitly insulating polymer rim bridge, shared by the two C03 electrodes. Each electrode has a separate downward hanger, opposing face jaws and side connection. The original short plate cannot simply be raised to the top of the 95 mm beaker while retaining its existing immersion. The downward holder grips above liquid, leaving the original electrical contact exposed. The holder is not a conductive busbar and has no electrical terminal or cable component.

The bridge is 4 mm thick / 12 mm deep; opposing jaws are 3 mm deep / 8 mm tall, sized from the actual plate/rod bounds. Two raycasts to the original glass rim establish support. Validation checks current bridge coverage of rim points, hanger/jaw connections, current X span and Z face contact, and terminal accessibility. Existing independent electrode immersion/separation and electrical routing checks remain in the Chemistry driver. Cathode removal for rinsing/drying remains part of C03; the empty holder can remain on the beaker.

## B06 / B07 filtration holder

At the filtering action, the original upright funnel moves over the original receiving beaker. Two 3 mm-thick fork arms have a 10 mm central opening clearing the source 8 mm stem; two short risers rest on raycast-confirmed glass rim contacts. Upward raycasts against the original funnel glass determine its supported underside. The native SDK seats the folded paper. The original outlet points downward into the receiver and stays above retained liquid. Its current geometry is rechecked before starting the experiment.

The unprepared funnel may rest separately with the other kit. A prepared capture is required to judge this assembly; an all-installed screenshot is not evidence that filtration has been prepared.

## Acceptance and limits

`ScientificFixtureChecks.RunPrepared` operates on an actually prepared rig: C02/C03 after step 3 (before C03 cathode removal), B06 after step 2, B07 after step 3. It checks fixture count, one bridge per beaker, insulating material, source unit scales and current support paths; deliberately detaches the instrument in X and the fixture in X by 12 mm, requires rejection, and restores each exact pose. Prospective receiver volumes are checked without changing liquid. `RejectFilterCapacity` runs before filtering (B06 after step 1 / B07 after step 2), fills the receiver deliberately, requires the filtering action to fail with both amounts unchanged, then restores its original amount. The product preflights receiver capacity and prospective outlet clearance before the single SDK volume transfer. Native execution and independent prepared-view review are required; source compilation alone is not acceptance.

These rigid fixtures do not simulate clamp force, glass stress, bolt preload, frictional slip, electrical breakdown or structural stability. Filtration retains the existing explicit volume transfer and qualitative lesson state; no physical flow-rate or filtration-grade measurement is added. Geometry/appearance is not evidence of a real-world safety certification or calibrated laboratory instrument.

## Integrated prototype verification — 20 October 2026

Layout94 passed these checks on actual UI-prepared C02/C03/B06/B07 rigs, including failed full-receiver actions retaining both volumes and current instrument/fixture detachment rejection with exact restoration. Independent source review and full-resolution prepared82 views closed the bounded holder/filtration findings. This checkpoint supersedes the earlier pending-integration wording only for those tested paths; the mechanical, flow-rate, calibration and hardware limits above remain.
