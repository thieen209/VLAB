# Guidance quality audit

2026-10-20. Bounded source/state/visual audit of the current Ngua Man VLAB project using the Unity bug-investigation workflow. This is not a user-comprehension study or physical-phone/headset acceptance. Existing Subject34 native32-journey /47-attempt receipts and Runtime49 UI launch checks establish a behavior baseline; they do not establish that instructions are good enough for a first-time student.

## Sources and actual visual evidence

- Current `LessonGuide.cs`, `ReadinessView.cs`, `GuideCue.cs`, `VlabApp.cs`, `ScienceModels.cs`, `ApparatusRig.cs`, `PhysicsMechanicsDriver.cs`, `ChemistryDriver.cs`, `BiologyDriver.cs` and supplied research briefs/catalog. Actual and staged LessonGuide hashes matched before this audit's changes.
- Supplied plan `VLAB UI UX Plan/Plan/02-FLOWS-AND-DATA.md`: preview needs objective/preparation; Activity needs readable current-step guidance, real observations and the entire apparatus visible. Supplied research steps remain distinct from supported native operations; full-brief completion must not be inferred from one saved trial.
- Inspected actual images: `Evidence/Runtime/13-P01-stock-VR-ready.png`, `14-C01-guided-initial-meniscus.png`, `26-P01-animated-instruction.png`, `27-C01-animated-meniscus-guide.png`, `28-ready-pendulum-VI-landscape-phone.png`. These are Editor renders, not photographs through a viewer. They cover P01/C01 and readiness presentation, not all32 lessons visually.

## Critical findings and staged corrections

1. **M05 wrong inactive variant.** Preparation reference step2 always targets `rod100`; `ConfigureCrank` deactivates nonselected80/100/120mm rods. Select80 or120mm and Show me where points to the inactive100mm item. Staged LessonGuide now chooses the configured rod key.
2. **M06 wrong inactive variant.** Preparation reference step2 always targets the knife follower; native follower-type control can activate the roller and deactivate the knife. Staged LessonGuide now chooses the active roller when fitted.
3. **C01 contradictory Vietnamese recovery.** Native endpoint text explicitly retains overshot titres as rejected evidence; Current guidance agrees, but `ExplainFailure` previously said “Không giữ lượt này”. Staged copy now directs the learner to retain the excluded titre/reason, then use a fresh aliquot; it explicitly excludes that titre from the three valid readings.

Following root review, the same staged target correction also covers P06 sticking/elastic attachments, C05 selected50/100mL vessel and M07 selected caliper/micrometer. Staged `ScienceModels.cs` changes only P03/P06 preparation wording, with operation counts and parameters retained. Source geometry, authority gates, numerical outputs and saved data are untouched. Root must integrate and execute `yield return GuidanceQualityChecks.Run(Check);`. It builds real native fixtures for all three rod lengths, knife→roller→knife, P06/C05/M07 variants, wording correspondence and C01 recovery strings. These new checks have **not been executed by this audit agent**. Passing them will not certify the modal's user-visible highlighting or novice comprehension.

## Other concrete source/UI issues still requiring decisions

- **P06 instrumentation mismatch — staged correction:** old preparation says “Lắp hai xe và cổng / Mount carts and gates”, while `TrackKit(false,true,false)` supplies no photogates. Wording now names the carts/track. Readings remain cart/contact-state samples. Sticking-mode preparation now targets active `latch` rather than hidden `bumperA`.
- **P03 material-selection wording — staged correction:** old preparation says “Chọn bề mặt”, but native tile finish0/wood does not offer visual replacement. Wording now names the declared friction coefficients that the learner can set; source geometry/materials remain intact.
- **C05 chosen-vessel target — staged correction:** generic references now follow the chosen50/100mL vessel, matching the already-correct special fill/invert branch.
- **M07 chosen-tool target — staged correction:** generic references now follow caliper/micrometer mode. Both objects existed, but the old guide pointed to the wrong selected tool.
- **P08 height step target — staged correction:** reference “Đo chiều cao đứng” now points to the actual source height ruler rather than cart. Native variant check added; the learner still needs the correct vertical datum distinguished from track distance.
- **Readiness objectives — staged correction:** P01's captured text says “Khám phá nhịp chuyển động của con lắc”, omitting what to measure, compare and retain. All32 bilingual goals in staged `ReadinessView.cs` now name a measurable SDK-supported task; maximum65characters. Layout/callbacks are unchanged. The preview remains a room photograph, not a close-up of the selected experiment. Native text geometry/physical readability must still be checked.
- **Shared Ready wording — staged correction:** C01 actual capture says “Select Release / Start”, the same language used for releasing the pendulum. Staged LessonGuide now uses “Bắt đầu lượt đo / Start trial” for SDK lessons and keeps the pendulum's release wording. Root is matching the Activity button label. Starting observation remains distinct from recording a result.
- **Show me where hides the thing being taught:** C01 actual meniscus capture presents a large readings/control modal over most of the physical setup. The highlighted button is legible, but the burette scale/flask cannot be observed alongside it. A first-time learner sees a command and a number without enough instrument context. Root should reserve a visible instrument region or deliberate observation view; merely highlighting another button is insufficient.
- **Physical scale reading is still unresolved:** in P01 actual activity capture, ruler ticks and timer face are tiny at the room camera distance. The whole pendulum is visible, but the image does not prove a pupil can identify the pivot, measure length or read the instrument through inexpensive optics. Use readable inspection without changing or rescaling original geometry.
- **Waiting lacks a useful explanation:** B03 default soak is600simulation seconds; B04/B05 require actual sample equilibration then manually spaced gas points. Guide text describes the waits but does not give a specific remaining interval/temperature difference in the task itself. Do not silently skip those gates; show what is being waited for and what observation can be made meanwhile.

## All32 source/state coverage

“Aligned” below means the inspected guide points to a supported native task/gate in source. It is not a visual or novice-usability pass. Every row has a short title and generic preparation reference; most preparation steps are explicit virtual operations, not freehand scientific manipulations. Native observation/recording limits remain in `EXPERIMENT_FINAL_AUDIT.md`.

| ID | Current actionable guidance | Finding / boundary |
|---|---|---|
| P01 | Install stock pieces; preparation; release; count same-direction crossings; record gate | Source aligned; identify pivot-to-centre length and improve readable timer/ruler context. Original720mm stand retained; long strings must stay outside tabletop edge. |
| P02 | Target/launcher/ball preparation; first-impact observation | Aligned with first impact; independent launch-height control remains unavailable. |
| P03 | Meter pull increments then steady sliding | Running controls align; staged wording now names configurable coefficients rather than unsupported material replacement. |
| P04 | Track/flag/timer; wait for both complete gate cycles | Aligned; keep50mm flag and500mm spacing explicit rather than implying alternate geometry. |
| P05 | Unloaded length then load; ten-cycle gate | Aligned; generic run text omits explicit ten-cycle target; supported spring travel still governs load. |
| P06 | Two-cart preparation and post-contact observation | Staged gates wording/active-attachment fixes; contact samples remain explicitly distinct from photogate data. |
| P07 | String/hanger setup; both gates before bottom stop | Aligned; task should explain bottom-stop failure and valid travel range in learner language. |
| P08 | Track incline/height/release; gate observation | Staged height reference points at actual ruler; stalled observations correctly lack invented gate speeds. |
| C01 | Initial meniscus; fresh aliquot/indicator; flow, swirl, endpoint persistence, final/titre | Rich state-specific sequence; staged rejected-titre recovery fix. Large modal obscures the physical scale/context. |
| C02 | Electrode/bridge/meter; voltage/probe reversal | Supported; concentration/temperature groups remain separate trials, not an automatic completed study. |
| C03 | Target time then off/rinse/dry/final mass | Controls/gates align; initial weighing is a virtual preparation action. No complete gas/colour rendering claim. |
| C04 | Reselect AgNO3 then dose two controls and NaCl | Aligned with explicit reagent-selection requirement; doses are qualitative units, not mL/mol. |
| C05 | Pipette/volumetric transfer; water-to-mark/invert controls | Staged generic reference now resolves chosen vessel, matching current fill step. |
| C06 | Buffer4→rinse→buffer7→rinse; probe sample and BTB | Specific sequence aligns; guide remembers successful controls, not a real calibration certificate. |
| C07 | Sealed gas route; observe volume/time | Supported; source-defined illustrative kinetics and syringe capacity must remain visible limitations. |
| C08 | Read acid/base initial temperatures; observe≥20s | Specific temperature controls align; different starting temperatures/cup heat capacity must be retained in interpretation. |
| B01 | Low-objective focus; select region and name≥3 structures | Supported schematic classification; guide must not imply calibrated real microscopy. |
| B02 | Baseline20–30IDs; salt wait; classify sameIDs; replace water; recovery wait | Aligned with manual classifications and timing gates; source states do not establish quantitative concentration kinetics. |
| B03 | Read three identified initial masses; soak; blot and final-weigh byID | Aligned;600s default wait needs explicit expectation/remaining time. Reweighing oneID is not independent replication. |
| B04 | Sample equilibration; mix;≥3 gas points spanning≥20s | Aligned; tell sample temperature rather than implying an isothermal reaction bath after transfer. |
| B05 | Equilibrate/mix;≥3 timed gas readings | Aligned; observe lag and separate controls; CO2 identity is assumed, not chemically assayed. |
| B06 | Load capillary→deposit→dry; develop≥30s; mark front | Specific controls align; pigment identities/Rf calibration beyond authored bands remain unavailable. |
| B07 | Sequential extraction; wait≥10s; spool if enabled | Aligned; no-alcohol controls may legitimately produce no spoolable material; crude precipitate is not purity/mass. |
| B08 | Select unclassified globalIDs; choose stage/unclear; reach100 | Aligned with manual counting and no answer preselection; atlas crops are one schematic, not independent fields. |
| M01 | Native train preparation; observe signed ratio | Supported stock12/24/36 train; no freely mismatched modules or loaded-efficiency measurement. |
| M02 | Pivot/load/horizontal reference; moment observation | Supported signed moments including declared beam weight; horizontal pose is not a dynamic balance demonstration. |
| M03 | Route/setup; repeated10mm free-end pulls until lift reached | Explicit pull control aligns; supporting strands, not wheel count, determine ideal advantage. |
| M04 | Pulley/belt/motor setup; observe output | Supported stock fixed-length configurations; no measured tension or loaded efficiency. |
| M05 | Crank/rod/slider setup; one-cycle traces | Staged active80/100/120mm rod target fix; kinematics only, no force inference. |
| M06 | Cam/follower/gauge; one-cycle observation | Staged active knife/roller target fix; lift10mm and sectors120/60/120/60 are fixed. |
| M07 | Zero/place/contact virtual preparation; corrected reading | Staged reference follows selected caliper/micrometer. Ratchet is virtual. |
| M08 | Supports/section/gauge/load; explicit unload check | Unload control aligns with record gate; loaded reading is retained by authority rather than an independent learner-entered scale reading. |

## Acceptance criteria before calling guidance ready for low-cost phone VR

1. For every lesson and supported variant, a learner can state the immediate task, locate the correct active item/control and recognize success without guessing hidden controls. Native guide-target checks must include variants, not only dictionary counts.
2. Vietnamese steps name what is actually supplied and do not promise freehand manipulation, alternate material, optical calibration or unavailable geometry. Starting, observing, retaining a titre and saving an experiment record are distinct actions.
3. At normal and large text, the current task and success condition fit at the intended viewing distance; complete lines are reachable without tiny targets. Readable in-view observations must be available without obscuring the relevant setup.
4. For P01, confirm original rod/clamp/string/bob geometry, the current0.2–1.0m range, both swing directions through0–60°, worktop clearance and continuous visibility at extremes. Preserve native stock geometry; do not solve framing by stretching the stand. Existing geometric checks are a baseline, not a stability/comfort test through a viewer.
5. Record a fresh phone-installed run using gaze/trigger/touch: one simple timed experiment, one multistep titration and one classification lesson, then all32 launch/return paths and variant cues. Check text, targets, observation coexistence, recenter, reduced motion, pause/resume and no accidental double actions.
6. A novice Vietnamese-speaking student should complete the next task and explain what was measured using only the app. Observe actual errors and revise wording; no such learner test was performed here. Do not award mastery or full research completion from trial counts.

No Unity process, build, source-project copy or publication was launched by this audit agent. All staged fixes and their native checks remain pending root integration/execution at this document update. Changed staged code is limited to `LessonGuide.cs`, the two preparation strings in `ScienceModels.cs`, the32bilingual goal strings in `ReadinessView.cs`, and new `GuidanceQualityChecks.cs`.

## Independent practice and optional help follow-up

The follow-up source review found that `hints=false` suppresses the live detailed step/card override, while Station tools → Guidance and Show me where → Guidance remain reachable. The Guidance window's explicit locator remains usable without enabling persistent hints. Successful instrument controls still call `Guide.Applied` regardless of hints, so asking for help later retains knowledge of what the learner already did. Preparation/start/record authority gates remain enforced with hints off; disabling coaching must not silently accept an incomplete experiment. These are source-reviewed properties, not a newly executed hints-off phone test.

The guide itself observes the session and builds text/targets. It does not perform instrument actions, choose biological classifications, advance time or save a measurement. Native regression additions snapshot the real M05 fixture's phase, preparation operation, elapsed time, dose, recorded result, parameters, authority state and all local transforms before/after requested guidance in Ready and Running phases. These checks are staged for root execution; they do not prove the corresponding property across every biology/chemistry UI branch.

Two further staged improvements support independent judgement:

- Replaced the B01 error message's exact virtual focus coordinate with low-power, adjust-until-structures-clear coaching. It retains the schematic/calibration limitation and does not reveal a cell classification or a supposed universal focus distance.
- Added bilingual observable preparation checks for every lesson:103 native preparation operations across32 lessons. Examples are a seated string/bob, a clear gate, the intended receiving vessel, separate initial temperatures, or retained sample identity. They replace the counter-increased success text. Where native preparation requires later equilibration or a separate mix action, wording explicitly says so rather than claiming the reaction already occurred. They are reference criteria, not an automatic certificate that the learner understood the step.

Static validation passed: every criterion sequence has exactly the corresponding native operation count in both languages and no empty item. `GuidanceQualityChecks.Run` additionally checks metadata coverage, the nonrevealing focus message and read-only guidance snapshots. **These new regressions have not been executed by this agent.** Root owns activity/help presentation and fresh UI/runtime execution. Actual learner autonomy, scientific reasoning, Android input and comfort still require observed student/device use; no professor-quality or complete-research score is assigned from code or assertion counts.
