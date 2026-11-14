# Approved story and interaction implementation

User approved the missing-mentor story and requested implementation without further design questions. Game-first: every required clue stays in the existing in-game knowledge/archive system; a companion website remains optional and is not being built.

1. Extend the existing cinematic stage: Earth/jump network, cadet/mentor introduction, mentor emergency transmission, visible escape-pod separation and docking. Retain cutscene skip/restore behavior.
2. Add a named senior pilot as the on-ship teacher; preserve the current quest sequence and NASA facts. Avoid confirming death.
3. Implement authored seated, carrying, watering, repair, planting and pet-play poses. Use short bounded action states and restore movement on completion/interruption. Check impact floor clearance directly.
4. Add species proximity icon and optional play/stay/follow pet controls. Cosmetic commands spend neither supplies nor work hours; emergency evacuation restores following.
5. Differentiate visitor silhouettes and add temporary shelter behavior through existing guest simulation, preserving current visitor identifiers and save compatibility.
6. Build, run existing EditMode tests and actual ship/base/UX scenarios, capture the opening and action states, and report limitations honestly.

Approach: extend the existing procedural pixel sprites and GameFlow partials; no renderer migration, duplicate simulation, website service or external dependencies. Kestrel and Mars already have distinct layouts; preserve the single-room pod connected by a tube to the habitat.
