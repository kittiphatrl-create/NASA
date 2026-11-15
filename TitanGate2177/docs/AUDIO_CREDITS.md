# Audio credits

Audio has several sources and licenses. The Kenney and SRG774 packs below are recorded as CC0; user-supplied recordings are listed separately and must not be assumed to be CC0.

## Music: `Assets/Resources/Audio/Music/`

From the **Dark Sci-Fi Audio Pack** by **SRG774**, CC0 1.0, https://opengameart.org/content/dark-sci-fi-audio-pack (downloaded 2026-11-15).

| File in game | Original file | Used for |
|---|---|---|
| `sector.mp3` | `sector_0.mp3` | calm music in the base and on the ship |
| `urgent.mp3` | `urgent_0.mp3` | tension: incidents, the 60-second grab, low health, dark nights |
| `airy.mp3` | `airy_0.mp3` | solar storm (the day before and the day itself) |
| `transmission.mp3` | `transmission_1.mp3` | end screen |
| `victory.mp3` | `victory_4.mp3` | rescued |

## Sound effects: `Assets/Resources/Audio/KenneySciFi/`

From **Sci-Fi Sounds 1.0** by **Kenney** (www.kenney.nl), CC0. Mirror: https://opengameart.org/content/sci-fi-sounds. License text: `docs/Kenney-SciFi-Sounds-LICENSE.txt`.

| File | Used for |
|---|---|
| `doorOpen_000.ogg`, `doorClose_000.ogg` | sliding doors |
| `computerNoise_000–002.ogg` | consoles and panels |
| `impactMetal_000.ogg`, `impactMetal_002.ogg` | repairs and using things |
| `forceField_000.ogg` | storm shelter |
| `engineCircular_000.ogg` | rover |
| `spaceEngineLow_000.ogg` | ship rumble during the grab |
| `thrusterFire_000.ogg` | trips outside, lifeboat launch |
| `lowFrequency_explosion_000.ogg` | the debris impact |

## Door sound: `Assets/Resources/Audio/Doors/door.mp3`

"space-ship-door-open" (file `freesound_community-space-ship-door-open-47688.mp3`) by freesound_community. The user supplied it on 2026-11-15. It plays when sliding doors open and when they close. Check the license on the source page (Pixabay content license) before the final submission.

## UI sounds

The interface sounds are by Kenney (CC0). See `docs/Kenney-Interface-Sounds-LICENSE.txt`.

## User-selected chapter soundtrack — 15 November 2026

The user supplied these original MP3 files and specified the scene assignments. They are copied without editing into `Assets/Resources/Audio/UserScore/`. This section records provenance, not a grant of redistribution rights. Source URLs, artist credits for the untagged tracks, and applicable licenses have not been provided/verified. Confirm these before including the recordings in a public submission or downloadable build; do not describe them as CC0 or SI-generated audio.

| In-game file | User-supplied source filename | Cue |
|---|---|---|
| `menu.mp3` | `เพลงเมนเมนู.mp3` | Main menu (replaced the opening track there on 15 November 2026). |
| `opening.mp3` | `atlasaudio-sci-fi-ambient-587603.mp3` | Opening cutscene. |
| `ship.mp3` | `tunetank-sci-fi-ambient-347390.mp3` | Playable ship chapter, impact, supply scramble, and escape up to Mars atmospheric entry. |
| `ending.mp3` | `Cornfield Chase.mp3` | Ending, including the win/loss cinematic and result screen; plays once. |
| `mars.mp3` | `เพลงตอนดาวอังคาร.mp3` (replaced `Blind Spots.mp3` on 15 November 2026) | Starts at the Mars atmospheric-entry shot, continues into the base, and is restored by Continue/direct base entry. |

Routing: `GameFlow.Score.cs`; two-source crossfades: `StageMusicPlayer.cs`. Chapter tracks use the existing Master and Ambience controls, stay at pitch 1 even in accelerated QA, and suppress the old music/stingers while active. Machine, UI, door, footstep, and alarm effects retain their own effects channel.

Source hashes, duration, and routing are also recorded in `docs/USER_SOUNDTRACK_2026-11-15.json`.

## User-selected effects — 15 November 2026

| In-game file | User-supplied source filename | Use |
|---|---|---|
| `Audio/UserEffects/item_collected.mp3` | `alexis_gaming_cam-item-collected-1-367087.mp3` | Successful supply/key-card collection, storage finds and expedition rewards. One playback voice prevents long tails stacking during rapid pickups; unsuccessful/full-hand attempts do not play it. Ordinary UI/card/handling cues remain separate. |
| `Audio/UserEffects/ship_alarm.mp3` | `alex_jauk-spaceship-alarm-ambience-192220.mp3` | Loops from the ship's impact emergency through the supply scramble; stops on escape or world cleanup. Other short alarm cues are suppressed while this ship loop is active. |

Both originals were supplied by the user. Their source URLs and license evidence were not supplied/verified here; they are not automatically CC0. Both effects follow Master and Effects settings, independently of the chapter music.

## Playback polish — 15 November 2026

`Audio/UserEffects/item_collected_game.wav` is a 1.15-second, +9 dB edit of the retained user-supplied `item_collected.mp3`, with short fades. It is now used for collection and completed-work feedback. This edit does not change the original recording’s rights or provenance. Details and hashes: `AUDIO_POLISH_2026-11-15.md/.json`.

Landing engine/parachute effects reuse the existing Kenney Sci-Fi recordings. Entry-air and Mars wind beds are procedural noise generated by AI-assisted Sfx.cs code; the base establishing shot reuses the existing synthesized hum. No additional downloaded audio assets were introduced.
