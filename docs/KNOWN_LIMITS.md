# Acceptance still required

The source SDK journeys and offline-record checks pass, but the complete research assignment is broader than one recordable trial. Bilingual journal follow-ups now list required repeats/controls and current kit limits. These are material limits, not completed requirements:

- P02: the authored launcher's muzzle height changes with angle; independent height / h0=0 investigations and the complementary-angle equality are unsupported. Actual geometry is retained and its height is recorded (readonly display bound increased to1.2m to contain the measured muzzle). The original Physics room floor is5.10x3.90m, so not every original5–85degree/1–10m/s setting can be performed physically. The unchanged seated launcher aims across the real floor; the actual receiving mat must be fully supported. Trials crossing the room envelope, ceiling or known worktops are refused before preparation/Start with Vietnamese guidance and unchanged speed/angle inputs; no unsupported shot is recorded. This envelope/worktop sampling is not a complete furniture collision solver. Isolated numerical tests use an explicitly labelled synthetic receiving floor and do not establish real-room acceptance.
- B01: schematic microscope imagery; focus/objective are illustrative. No drawing tool or optical calibration.
- B02: four manually selected illustrative states; no calibrated concentration response or biological kinetics. Keep same-cell counts, without inferring dose-response from the images.
- B04/B05: the sample cools after transfer from the bath. Model histories are labelled; this does not reproduce an isothermal reaction bath.
- B07: alcohol temperature is recorded but does not quantitatively change the crude precipitate model.
- M06: original fixed cam profile retained; no lift/rise-angle variation beyond the supplied geometry. Knife/roller comparisons are supported. The supplied motor/cam axes differ by23mm and the guide sleeve has no supplied stand; the prototype adds separately derived motor and guide supports, documented in [DERIVED_M06_FIXTURE.md](DERIVED_M06_FIXTURE.md). Original apparatus meshes/scales are preserved. Contact/axis/clearance checks do not validate fasteners, vibration, stress or loaded stability.
- Other named extensions are disclosed in each journal follow-up: some apparatus diagnostics, endpoint-work measurements, repeated orientation sampling, visible electrolysis gas/colour and cooling extrapolation are narrower than the research assignment.
- Standard OS-paired HID gamepad input is integrated and synthetic Input System states passed. A bespoke ESP32/BLE protocol, its calibration and physical hardware are not certified.
- No physical Android phone/Cardboard headset was attached for stereo, touch/trigger, QR profile, comfort, suspend/resume or performance acceptance. Editor pictures are monoscopic renders of the actual world-space UI.

An older debug development APK predates the integrated 32-lesson build and is superseded. Release credentials stay outside the project and repository. Do not uninstall an existing app to bypass a signature conflict without exporting any wanted local progress first.

## Source constraints confirmed

The original research `research/experiments.py` B02 (lines190–198) asks for concentration/time comparisons. Delivered `Sources/specimen_plasmolysis/v001/docs/UNITY_HANDOFF.md`, `cells.json` and `metadata.json` explicitly describe four prescribed untimed states, with no concentration/rate claim; the SDK only selects these states. The research B02 photo manifest does not supply concentration/time-linked observations of the same cells. Manual same-ID state comparisons are supported; calibrated dose-response is not supplied or implemented.

M06 (lines321–329) asks for h/beta changes. Delivered `Sources/cam/v001/docs/profile.json`/`profile.csv` author one25mm-base,10mm-lift cam with120/60/120/60degree sectors. `QA_CamKnife` is an inspection wedge, not another cam variant. Changing only reported numbers would disconnect the graph from the original mesh. No unmodified alternate h/beta apparatus was found. Original geometry remains preserved.

## Derived supports in this prototype

Original apparatus meshes and scales are retained. Additional, explicitly named rigid fixtures carry the M01/M04 transmissions, M06 cam/guide/gauge, M08 raised beam/gauge, P07/P08 movable photogates and C02/C03 electrodes; B06/B07 receive split funnel supports. Their provenance and exact contact-test scopes are in DERIVED_M06_FIXTURE.md and DERIVED_SCIENCE_FIXTURES.md. These are prototype integration parts, not supplied original assets or validated mechanical/electrical hardware. Geometry tests do not establish fastener strength, glass stress, friction, torque stability or calibrated flow. Ordinary stored pipettes, droppers, thermometer, stirrer and unused pestle retain original geometry in supported rest poses.


## Latest negative verification addendum

Fresh119 Windows Subjects verification failed after the separately successful Core333 run; offline file-replacement errors leave B01/M07/M08 reopened coverage incomplete. Cause remains unresolved. Old47-record success is historical, not current119 clean-clone persistence proof. See [the current negative-evidence report](VERIFICATION_119_WINDOWS_PERSISTENCE.md). The0.1.1 APK/tag/code are unchanged; production readiness is not claimed.
