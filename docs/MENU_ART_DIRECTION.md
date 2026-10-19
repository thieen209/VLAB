# Home: source comparison and acceptance

Authoritative layout: the user's `E:/VLAB/branding/UI demo nice/main.pdf`, page1. Visual reference: supplied `References/Game Inspiration/beat saber - Beat-Saber12292022-040103-50396.jpg`. Later user instructions override the older `home.png` and the previously curved prototype.

The sketch establishes a centred original VLAB logo above four equal portrait subject cards, followed by four small circular utilities. Each subject has a centred title near its top and one large scientific silhouette. The sketch explicitly circles the Beat Saber figures where they interrupt the card boundaries. A real laboratory photograph changes on focus.

Beat Saber's useful detail is structural: title, flat card surface and large contour illustration form one shape. The figure removes part of the lower card surface and its border. It is not a glossy object floating over a complete rectangle. Its restrained edge and large negative spaces keep the title readable. The active card has a clearly coloured face while the others stay quiet. The photograph also contains perspective from the viewer's head; reproducing that unequal projected size would contradict the user's later request for four equal apparent sizes.

Implementation contract:

- Four identical268x432 resting hit rectangles, one common baseline and plane. Keep enough gap for the drawing and3.5% focus growth without touching a neighbour.
- Centre each subject title horizontally in its upper zone. Use Chakra Petch Bold with verified Vietnamese glyphs, retain Inter for longer reading.
- Build the lower card surface around each scientific contour and open edge-crossing cuts to the exterior. Remove both surface triangles and the corresponding outer-frame segments; the original rectangle must not reappear behind the overflowing drawing. No arbitrary universal frame gap or glossy cutout image.
- Use consistent fine strokes, decisive boundary crossings and restrained subject colours. Check both the complete menu and a small thumbnail, not just isolated icons.
- Real ray focus slightly enlarges the card, strengthens its edge and gently tints its face. Use muted slate surfaces, warm light symbols/titles and restrained accents. The face follows the saved UI opacity: new profiles and Appearance defaults use80%, adjustable55–100%; preserve an existing saved choice. Symbols and titles remain readable. Reduced motion keeps the size stable. Each card chooses its matching current-project captured laboratory.
- Preserve the blurred current-project room background, transparent horizontally centred logo lowered within its upper zone, four round utilities and writable notebook. Remove the Home lesson-resume control. Keep the separate one-tap VR entry available.
- Settings categories form one connected top segmented rail, with six equal72px transparent hit areas inside a single rounded host and one selected underline. Do not draw six separate outlined boxes. Controls and outputs must remain readable in Vietnamese, English and large-text mode.

Native layout/input assertions support these checks; user visual acceptance and physical headset comfort remain separate.
