# Audio polish — 15 November 2026

Implemented in the live Unity project `F:/NASA/TitanGate2177`.

## Changes

- All four recorded footstep variants now use 0.75 of their previous gain. Their shared 120 ms cooldown also applies after random variant selection.
- Retained the original user MP3. Added `Audio/UserEffects/item_collected_game.wav`: first 1.15 seconds, +9 dB, 4 ms attack fade and 100 ms release fade. The source contained about 5.46 seconds of trailing silence after its initial effect.
- Collection and successful work use one dedicated 2D feedback voice at 0.95 gain. Music ducks to 42% for 1.1 seconds, then returns smoothly. Failed/cancelled actions retain failure feedback and do not play a reward.
- Prologue jobs, supply/keycard collection, stowing, base work/repairs, and completed expeditions produce reward feedback. Removed the second generic success cue that previously played over expedition rewards. Power-plan toggles remain ordinary UI feedback.
- Landing videos have continuous engine/entry-air layers; parachute deployment has a whoosh; braking engines lead into a touchdown thud at 65% of the landing shot; the empty-base establishing shot has wind and electrical ambience. Layers fade out on scene completion/skip, and are stopped immediately on world cleanup.
- Cinematic engine/air use Master + Effects. Base wind/electrical ambience use Master + Ambience. Previously selected chapter recordings and dialogue-beep preference are preserved.

## Verification

Used Unity MCP through the local bridge; no desktop mouse/keyboard automation. Captured the actual AudioListener output with the existing QA-only WalkthroughAudioCapture component.

At Master 0.8 / Effects 0.7 / Ambience 0.5:

- Footstep A/B peak ratio: 0.742 (about 25% lower amplitude).
- Collection A/B: original peak -21.33 dBFS; edited playback peak -8.96 dBFS; RMS improvement 11.81 dB.
- Collection and completion clips both play the imported 1.15-second edit.
- Effects mute sets feedback volume to zero.
- Normal-speed landing recording peak 0.457, below clipping. Observed braking engine and base ambience active, then all cinematic layers stopped at scene completion.

Recordings and raw measurements: `Logs/audio-polish-20261115/landing.wav`, `effects-ab.wav`, `effects-measurement.json`, `landing-states.json`, `effects-markers.txt`. The A/B recording is isolated sound verification, separate from the automated gameplay pass.

The existing automated gameplay pass completed the real prologue/scramble, all landing shots, base work, seven days, rescue and ending: **errors=0, checksFailed=0**, seed 12. Handler observations confirmed the edited reward playing for ship collection and base repair_radio, radio_antenna, seal_room, send, search, prepare_shelter, service_scrubber, ice, clean_panels, repair_thermal, service_fan and repair_o2gen. QA accelerates time and bypasses the timing minigame; the separate landing recording used normal cinematic durations.

Additional MCP contract checks passed: departure engine, actual Skip button callback, stop on skip, base wind, Ambience mute, Master mute, music duck/recovery and menu cleanup. Unity Console contained zero errors. Player preferences were backed up before testing and restored afterwards.

## Areas still deliberately light on sound

- Dialogue/caption text has no spoken voice track. Dialogue beeps remain disabled as previously requested.
- Off-screen expeditions use departure/return/vehicle and reward sounds, rather than continuous footsteps/tool foley outside.
- Dinner has selection/UI feedback; there is no separate eating or utensil performance.
- Quiet idle rooms mostly share the existing base hum; individual machines do not all have distinct continuous operating loops.

## Provenance

The collection edit is derived from the user-supplied Alexis Gaming Cam recording; this is editing, not a newly generated recording. Engine rumble and parachute thruster/whoosh reuse existing Kenney Sci-Fi recordings (CC0, existing credits). Wind/entry-air layers are deterministic procedural noise in Sfx.cs, implemented with AI assistance; the base bed reuses the existing synthesized hum. No new downloaded third-party audio was added.

Windows build refreshed successfully: `Builds/Win/TitanGate2177.exe`; build report `Succeeded errors=0`, 15 November 2026, 18:37 local time.
