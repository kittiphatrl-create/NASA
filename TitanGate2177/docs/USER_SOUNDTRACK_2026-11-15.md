# User soundtrack integration — 15 November 2026

Implemented in the active project `F:/NASA/TitanGate2177`, not the older Codex worktree gameplay snapshot. Original MP3 bytes are preserved; hashes are in USER_SOUNDTRACK_2026-11-15.json.

| User file | Game phase | Playback |
|---|---|---|
| 3. atlasaudio-sci-fi-ambient-587603.mp3 | Title/menu and opening cutscene | Loops; moving from title into the opening does not restart it. |
| 4. tunetank-sci-fi-ambient-347390.mp3 | First playable chapter on Kestrel, impact, scramble, lifeboat departure | Loops until Mars atmospheric entry. |
| 5. Cornfield Chase.mp3 | Ending cinematic and result screen | Plays once for either ending; returning to the title changes back to the opening track. |
| 6. Blind Spots.mp3 | Mars entry (`l02_entry`) and the Mars base | Loops; entering the base or using Continue does not restart an already active Mars cue. |

Files 1 and 2 have no new effect assignment in this request. Existing alarm, pickup, and other effect routing is retained.

## Implementation

- `StageMusicPlayer.cs` owns two dedicated 2D music sources with 1.2-second unscaled crossfades. Pitch stays at 1, independent of simulation speed. Ending music does not loop.
- Music follows the existing `Master × Ambience` controls with a 0.72 mix gain. Setting Master to zero mutes immediately, including during a fade. SFX use the existing effects path.
- `GameFlow.Score.cs` routes the cues. Narrow hooks in menus, the ship intro, cinema shot entry, base start, and ending cover normal transitions, cinematic skipping, Continue, direct base start, and restart.
- Legacy adaptive music layers and victory/transmission stingers are suppressed while a requested chapter cue owns the soundtrack. Missing custom tracks fall back to the existing route without throwing.
- All four long tracks use Streaming import and background loading. No downloaded codecs, external playback service, or scene replacement is needed.

## Verification

- Unity imported all four AudioClips successfully. Hashes match the user-supplied originals.
- Opening playback was active at a nonzero elapsed time; repeated Opening requests preserved playback position. Master mute worked. Pitch was 1.
- Existing full-game AutoPlay was started through Unity MCP, seed 12, simulation speed 3x. It reached **Rescued on day 7**, with runtime `errors=0 checksFailed=0`.
- The runtime log recorded `Ship -> Mars -> Ending -> Mars`; the final Mars switch is the test's restart check. Opening was already active before the log started.
- That full run preceded a final narrow adjustment making legacy music layers immediately silent. Subsequent focused checks and build results are recorded with the evidence; do not imply the complete game was replayed again after that adjustment.
- Final focused checks passed: all three legacy layers became immediately silent; Mars and Ending clips loaded and played; Ending did not loop and advanced to 21.57 seconds; returning to the main menu selected and played Opening. Console errors remained zero.
- Final Windows build: `Succeeded errors=0`, updated `Builds/Win/TitanGate2177.exe` and its player data. The executable itself can retain its older timestamp because Unity reuses the engine launcher; `TitanGate2177_Data/Managed/TitanGate.Game.dll` was freshly built on 15 November at 17:55 (Asia/Bangkok).
- This is programmatic playback/routing verification, not a claim that a human performed a complete listening review.

Evidence: `Logs/score-2026-11-15/mcp-full-run/autoshot-log.txt`; the worktree also retains focused MCP diagnostics. Original routing files were backed up under `Logs/score-backup-20261115/` before edits. No commit, push, or public upload was performed.

Source credit and license status: see AUDIO_CREDITS.md. User-provided files are not automatically CC0, and this integration does not establish redistribution rights.
