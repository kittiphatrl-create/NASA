# Habitat visual pass

User requests the supplied navy/orange industrial pixel reference, multiple looks, distinct room functions, deeper materials and complete gameplay preservation. Implement on the existing layout; do not relocate doors, collision, quests, restoration state or NPC stations.

Three native sprite palettes: navy/orange (default), ceramic research, oxidized expedition. Higher-resolution authored sprites preserve world bounds/pivots. Quiet floors, structured wall panels, controlled wear, strong dark silhouettes, amber task lighting and cyan displays. Colored room labels also include readable short codes and symbols. No generated-image floorplan is pasted over gameplay.

Extend the shared puzzle surface and control materials, including actual cast shadows and bevels, rather than claiming a frame alone is a complete redraw. Keep interaction bounds, pointer routing, completion callbacks and the no-text success indicator intact. Use existing Sarabun typography from the previous pass.

Verification: compare identical-camera runtime views for all three palettes; validate wall/floor sprite world dimensions and collision/spot signatures across themes; exercise all nine puzzle completion paths at 720p/1080p; run EditMode tests and a fresh abandoned-base playthrough. References: user image, https://eastwardgame.com/media/ and https://www.playstarmancer.com/media. Video https://www.youtube.com/watch?v=hOlZjUSRqiY (previously sampled; not newly claimed as fully watched).

## Implemented visual scope

- Default navy/amber skin; ceramic research and oxidized expedition palettes available through the art showcase route. The variants are review previews, not a new player settings option.
- Authored 64-pixel floor/wall tiles and higher-resolution console/table sprites retain the original world bounds and pivots. Wall/floor contact shadows are separate non-interactive renderers. Room codes supplement color, and room lights follow restoration power state.
- The nine repair panels share an inset metal backplate, bevels and control shadows. Wire cables have cast shadows; matching endpoints use A–D in addition to color. Existing mechanics, bounds and completion callbacks are retained.
- Sarabun Thai/Latin typography and the no-text completion lamp/rail from the previous polish pass are retained.
- Existing character art and other furniture remain. This is a materials/lighting pass over the current layout, not a claim that every prop has been redrawn or that the reference has been reproduced exactly.

## Review frames

The `art` route deliberately stages arrival and all-restoration flags at the same camera for palette comparison. These are runtime frames, not painted concepts or evidence of a full playthrough. Staging does not replay each cleaning animation; some old junk/decals can still be visible in these staged frames. The independent full-play log records actual progression.

- [Navy / amber](../production/qa/evidence/habitat-art-2026-11-14/art/theme_0_restored.png)
- [Ceramic research](../production/qa/evidence/habitat-art-2026-11-14/art/theme_1_restored.png)
- [Oxidized expedition](../production/qa/evidence/habitat-art-2026-11-14/art/theme_2_restored.png)
- [Arrival without restored power](../production/qa/evidence/habitat-art-2026-11-14/art/theme_0_arrival.png)
- [Wire panel](../production/qa/evidence/habitat-art-2026-11-14/typography-1280/wires_complete.png)
- [Pipe panel](../production/qa/evidence/habitat-art-2026-11-14/typography-1920/pipes_working.png)

Fresh build: `Builds/Story/TitanGate2177.exe`. Reproduce palette review with `-autoshot -artshowcase -force-d3d11 -screen-fullscreen 0 -screen-width 1280 -screen-height 720`. Use a normal rendered window for image captures; a hidden Windows player produced black captures and those frames were replaced before archiving visual evidence.

## Verification limits

EditMode: 94 passed, 0 failed. Palette route: 12 passed, no runtime errors. Thai typography and nine puzzle completion paths: 38 checks at 1280×720 and 38 at 1920×1080, no runtime errors. This route invokes the existing puzzle solvers/callbacks; it does not manually drag every control. All 48 archived visual-review PNGs were checked as non-black, with selected map, wire, filter, pipe and radio panels visually inspected.

Fresh independent full play: rescued on day 5 in 206.2 seconds with health 2, O2 6.5, water 1.5, food 6 and power 3. Final errors 0, failed checks 0. It covered the intro, ship/tutorial and sofa impact, five supplies, landing, restoration, food, storm sheltering and radio rescue. This is automated regression evidence, not a claim of manual play or a complete balance/fun assessment. [Full log](../production/qa/evidence/habitat-art-2026-11-14/full-play-log.txt); [result and build/source fingerprints](../production/qa/evidence/habitat-art-2026-11-14/result.json).
