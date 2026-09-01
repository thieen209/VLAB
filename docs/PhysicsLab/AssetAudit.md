# VLAB Physics Lab Asset Audit

Audit date: 2026-08-30

Scope: every supported source file supplied in `D:\VLAB\files 3d lý`, plus the two non-sample furniture meshes already present in the Unity project. Originals were SHA-256 checked, copied byte-for-byte to `00_Source_Original`, and only the copies were expanded for inspection. No original was deleted or edited.

## Supplied source assets

| Source | Observed content | Evidence | Classification | Decision |
|---|---|---|---|---|
| `iron_stand_with_base_and_clamp_arrangement.glb` | Retort stand, base, vertical rod, horizontal rod, boss head, clamp screws | 36 nodes, 16 mesh parts, clean material split; overall height is only about 0.072 m because of an inherited 0.01 root scale | `NEEDS_CLEANUP`, `WRONG_SCALE` | Reuse after flattening the Sketchfab hierarchy, scaling to a believable 0.72 m height, applying transforms, renaming parts, and adding Unity anchors/colliders |
| `hookes-law-spring.zip` | Three static coil variants and a support/mass assembly | 5 meshes; spring lengths 5.81-15.27 m, unapplied scales around 0.586, roughly 9-10k polygons per spring | `WRONG_SCALE`, `NEEDS_SEPARATION` | Keep as reference. Replace the Unity-ready spring with a clean endpoint-driven coil because the source contains baked length variants rather than one functional spring |
| `lab-7.zip` | Complete pendulum demonstration merged into one object | One 30,992-vertex / 30,388-polygon mesh, 14 materials, stand/string/bob fused, non-applied 0.05 scale | `NEEDS_SEPARATION` | Preserve as reference only. It cannot support detachable bob, adjustable string, or efficient mobile-VR interaction without an excessive rebuild |
| `lab-10.zip` | Large electrical apparatus / meter assembly, not one of the six mechanics experiments | 23 objects, about 53k polygons, many duplicated material slots and non-English generic object names; working dimensions reach 31 m | `WRONG_SCALE`, `UNUSABLE` | Preserve. Do not import into the Physics Lab foundation because it is unrelated to the required experiment set |
| `ammeter.zip` | Analog ammeter with separate terminals/needle | 6 objects; terminal meshes are 23-27k polygons each; body dimensions exceed 4 m before cleanup | `WRONG_SCALE`, `UNUSABLE` | Preserve for a future electricity lab. It does not satisfy the required digital timer or force-meter roles |

## Existing Unity furniture

| Source | Observed content | Classification | Decision |
|---|---|---|---|
| `Assets/Layer2_Noi that/Lab Desk.../model.obj` | Detailed laboratory desk, 2,488 vertices / 2,466 polygons | `READY` for room dressing | Reuse in the room; keep functional experiment prefabs independent of it |
| `Assets/Layer2_Noi that/Table.../Table.fbx` | Low-poly generic table, 208 vertices / 170 polygons | `READY` after scene scale/orientation check | Reuse as the visual source for `Experiment_Table` when available; generator also creates a physics-safe root and collider hierarchy |

## Audit conclusion

The retort stand is the only supplied mechanics model suitable for direct cleanup and reuse. The supplied pendulum and spring are visually useful references but their fused/static construction conflicts with the required runtime behavior. All other functional instruments must be generated as separate, metric, game-ready assets.

## Source integrity

| File | SHA-256 |
|---|---|
| `ammeter.zip` | `D372AF04F1F9750E1055E173D7D455C6742B1C66B2014CF55C0D942FD214B0C8` |
| `hookes-law-spring.zip` | `F59D7B6985253512E565A63FA8357500B16791416A2E829DAC053B02BC6C4FD9` |
| `iron_stand_with_base_and_clamp_arrangement.glb` | `BBE8CADD3CECB406CED13BCF739B27CC47CE6FB7D628D679163E02D043804B01` |
| `lab-10.zip` | `A4BCD9CC8C500F8656B61C1AE7DBDB66F89506130BFFB99B7D53A0AFA747AD26` |
| `lab-7.zip` | `02283682959276F22D57D7F0C4958DD83E9DD343F0AF63F29D795FBF8E0D4703` |

