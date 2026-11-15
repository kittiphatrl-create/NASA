# Main-menu branch integration — 15 November 2026

Integrated `origin/ui/title-menu-font` (42b0183) into the current main lineage (d01e924), retaining the new docking-ring menu, ship/Mars scene layers, fonts, input controls and save-card metadata.

## Integration decisions

- Preserved both sides of Sfx.cs: varied-pitch menu cues plus the current collection/completion voice, softer footsteps, chapter score and landing ambience.
- Retained main's existing GUID for GameFlow.Dinner.cs.meta. The added menu code references GameFlow by type; no new asset references the alternate GUID.
- Connected submenu/departure music ducking to StageMusicPlayer.ContextGain. The user's opening recording remains continuous into the opening cinematic, and the older/menu-specific tracks cannot overlap it.
- Fixed a departure-frame ordering issue discovered by the focused Continue check: after a menu action detaches the menu and starts the next chapter, its scheduled callback now returns immediately. This prevents the departing menu from leaving the new Mars track at 40% volume.
- Saved all 42 pre-existing local render/backup files and the pre-merge Git index under Logs/menu-merge-backup-20261115. SHA-256 checks confirmed all 42 original local files remained unchanged. They are not part of this merge commit.

## Checks

Unity MCP only, without desktop mouse/keyboard automation. The first focused check exposed the departure music issue above; a fresh check after the fix is the acceptance result. A separate early retry overlapped Editor script recompilation and was stopped/restarted after compilation finished; live script hot-reload is not an accepted gameplay test.

- All 147 EditMode tests passed after the branch was combined.
- All 17 focused menu checks passed: EN/TH, no-save Continue disabled, Start enabled, settings, music duck/recovery, valid-save Continue, scene/music cleanup, and Start entering the opening cinematic through its real callback. Screenshots and JSON: Logs/menu-merge-20261115/.
- Full automated gameplay reached rescue on day 7 (seed 12): `errors=0 checksFailed=0`, including the prologue, all cutscenes, scramble, base actions and save restoration. QA accelerates time and bypasses the hand-timing minigame. The menu’s actual Start and Continue callbacks were verified separately above.
- Settings screenshot was captured while its modal was visible and reviewed, alongside the English ship menu and Thai Mars menu.
- Fresh Windows build succeeded with zero build errors at 19:28 local time: `Builds/Win/TitanGate2177.exe`. The game assembly and resource bundle were rebuilt for the merged menu.

The user's saved preferences/progress are backed up separately and restored after QA.
