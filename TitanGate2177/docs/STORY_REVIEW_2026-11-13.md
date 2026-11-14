# Opening story — proposal awaiting review

Historical proposal. The user approved the missing-mentor variant; see [implemented behavior](STORY_IMPLEMENTED_2026-11-13.md) for the current opening and interaction changes.

1. In 2177, humanity operates orbital settlements and jump corridors. Introduce Earth, the network and Kestrel through illustrated scenes rather than a long text panel.
2. Introduce the junior astronaut, an experienced pilot, ORBI and the selected pet aboard Kestrel. The pilot demonstrates procedures and establishes a personal connection before the accident.
3. Quiet routine: caring for the pet, watering plants, carrying equipment and resting on the sofa. Each requires a readable action pose and a clear return to movement.
4. A debris strike disables Kestrel. The experienced pilot stays at the controls to release the escape craft. The user must decide whether the pilot's death is confirmed or their fate remains unknown.
5. The junior astronaut gathers supplies during the emergency and evacuates with the pet and ORBI in a compact, one-room escape craft. Show the separation from Kestrel.
6. The escape craft docks with a pre-existing Mars emergency habitat. Its docking module remains distinguishable from the larger habitat and the original ship.
7. The seven-day survival chapter combines repairs, limited work hours, companions and visitors seeking temporary refuge. Visitors need distinct silhouettes, motives and stay durations.

## Character proposals

- Aya: adult human botanist with a recognizable ponytail, plant satchel and practical clothing.
- Jules: adult human mechanic with rolled sleeves, tools and a different body/hair silhouette.
- Pip: tiny gelatinous courier; squash/stretch movement and floating parcels rather than a recolored humanoid.
- Water visitor: translucent aquatic explorer in a portable water vessel; bubbles and gestures establish identity.
- Adult anime-inspired traveler: expressive face, distinctive expedition jacket and playful dialogue; original design.
- Meme-like service robot: mismatched salvaged panels and an overly formal personality.

Temporary shelter requests should specify departure conditions, such as the end of a solar storm. Permanent admission remains a separate choice.

## Interaction and animation acceptance targets

- Pet proximity: cute species icon and E prompt; play, stay and follow commands. Play is optional and must not trap movement or advance story unexpectedly.
- Seated poses for sofa/chairs; legs and feet match the seat height. Stand up on reachable floor before movement resumes.
- Watering, repair, planting, lifting, carrying and placing equipment use identifiable action poses/props, not a standing sprite with text alone.
- Interruptions, cutscene skips, pause and emergency transitions release action locks and restore reachable positions.
- Distinguish Kestrel's full working ship, the small escape craft and the Mars habitat in layout, props and palette.

## Immediate bug patch

SImpact teleported the cadet into the sofa collider for the rest sequence, then re-enabled movement without restoring a floor position. The patch saves the pre-seat position and restores it after the impact cinematic, before the scramble begins. AutoShip now checks floor clearance and a direct movement step before any automatic pathfinding can conceal the regression.
