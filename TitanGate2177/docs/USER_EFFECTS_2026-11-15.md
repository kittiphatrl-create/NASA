# Requested collection and ship-alarm effects

Implemented in `F:/NASA/TitanGate2177` on 15 November 2026. Original file hashes and provenance: USER_EFFECTS_2026-11-15.json.

- **Item Collected:** the supplied Alexis Gaming Cam file plays for successful ship supply pickups, carrying-task pickups, key-card acquisition, positive storage finds, and successful expedition loot. It uses a dedicated one-shot voice with a short cooldown so the 6.456-second tails do not stack. UI cards, character pokes, ordinary handling, and failed collection attempts keep their existing cue routing.
- **Spaceship Alarm:** the supplied Alex Jauk ambience loops from impact through the timed scramble. It stops on lifeboat escape and world cleanup, including returning to the menu. Other short alarm cues are suppressed while this long ship alarm is playing. Base incident alarms retain their existing routing.
- Both use Master and Effects controls; chapter music remains on its existing music/ambience path.

## Verification

An automated ship-to-Mars gameplay sequence was started through Unity MCP, seed 12, 3x simulation speed. It used the existing AutoPlay handlers; the run was intentionally stopped after verifying arrival in the base, not played through another full seven-day campaign.

Observed and recorded:

1. The `ship_alarm` clip was actively playing/looping during impact and the scramble.
2. Successful collection invoked the `item_collected` clip and playback advanced.
3. The alarm stopped when the scramble ended and remained stopped on Mars.
4. Turning Effects to zero muted both supplied effects immediately.
5. Returning to the main menu stopped an active ship alarm.

Evidence: `Logs/user-sfx-2026-11-15/effects-flow-result.json`, `final-effects-contract.json`, and the `ship-check/` runtime log and screenshots. Code compiled without console errors during testing. This verifies routing/playback state, not a complete human listening review or every possible inventory edge case.

Routing backups: `Logs/user-sfx-backup-20261115/`. Game preferences were backed up and restored after testing. No public upload or Git push was performed. Licensing notes remain in AUDIO_CREDITS.md.
