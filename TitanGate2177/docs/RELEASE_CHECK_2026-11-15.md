# LAST DOCK demo readiness — 15 November 2026

The current version is a playable demo. This is not a claim that every requested future feature is implemented or that no undiscovered bugs remain.

- Pulled `origin/main` with fast-forward-only before preparing this commit; the branch was already current at `70bb836`.
- All 147 Unity EditMode tests passed, zero failed or skipped. Includes resource management, winning plans, failure from neglect, survival rules, saving-related simulation behavior and the retained legacy systems. The focused LAST DOCK subset passed 53/53.
- Latest automated gameplay: prologue jobs, impact, supply scramble, landing, seven base days, rescue, ending. `errors=0 checksFailed=0`, seed 12. This QA pass accelerates time and bypasses the hand-timing minigame; it does not replace human usability testing.
- Normal-duration landing audio was separately recorded from the actual AudioListener. Collection/completion feedback, footsteps, cinematic skip cleanup, mute settings and music duck/recovery passed focused checks. See `AUDIO_POLISH_2026-11-15.md`.
- All 18 runtime cinematic videos match the corresponding Blender render outputs by SHA-256. A fresh Windows build including these videos succeeded at 19:05 local time, with zero build errors. Unchanged managed assemblies were reused by Unity’s incremental build.

The game has resource/time tradeoffs and tested winning/losing paths. Difficulty and enjoyment for children aged 7–12 still need feedback from actual players; automated success is not a measure of fun.

Audio scope: chapter music and the requested main scene/action effects are connected. There is no spoken dialogue, separate eating/utensil performance, or unique idle loop for every machine. These are polish gaps rather than blockers to playing the current demo.

Submission note: user-supplied chapter music and effects have documented provenance, but redistribution/license evidence has not been supplied for all recordings. In particular, do not treat the named soundtrack files as automatically licensed because they were supplied locally. Check `AUDIO_CREDITS.md` before making submission declarations about permission to use all external material.

This repository contains Unity source/assets. `Builds/` is ignored by Git; pushing this commit does not upload a downloadable Windows package or submit the competition form.
