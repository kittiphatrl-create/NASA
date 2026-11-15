# LAST DOCK — Space Apps submission copy

Prepared for team Overtimepw on 15 November 2026. This copy describes the current Last Dock implementation in F:/NASA/TitanGate2177. It does not describe the earlier three-day robot/camera-map concept. Copy only the text beneath the relevant form field; the editor notes and verification checklist are not submission prose.

## Project Name

LAST DOCK: Junior Astronaut Mission Trainer

## Summary

We built LAST DOCK, a story-driven survival game that introduces children aged 7–12 to the challenges of keeping a space habitat running. After a spacecraft accident, the player collects supplies, escapes to Mars, and takes shelter in an abandoned base while waiting seven days for rescue.

Each day offers only three work units, so repairing a machine, finding supplies, and preparing for a solar storm all compete for the same limited time. A working oxygen supply is only part of the problem: carbon dioxide, heat, water, and electrical power also need attention.

We used NASA resources on life-support systems and space hazards to shape these decisions. Short explanations connect in-game problems to real science. With Thai and English support, LAST DOCK aims to make space science approachable through choices and consequences, giving young players a reason to ask how the systems around them actually work.

## Project Details

We started with a simple question: how can a child understand the difficulty of living in space without reading a textbook first? Our answer was to put those ideas inside a survival game, where understanding a machine helps the player make a better decision.

LAST DOCK begins aboard the spacecraft Kestrel. When debris damages the ship, the player has 60 seconds to gather supplies and reach the escape pod. Carrying space is limited, so the player cannot take everything. Batteries, food, oxygen, spare parts, and specialist equipment can lead to different advantages after landing. The things collected during the escape become part of the supplies available in the base.

On Mars, the goal is to keep an abandoned habitat running until rescue can arrive. The base section is turn-based, with three work units available each day. Walking around and reading panels are free, allowing the player to understand a problem before spending time on it. Repairs, searches, and expeditions use work units. Some faults become worse as actions pass, so a useful task can still be the wrong task to do first.

The systems are connected. The oxygen generator needs water and power. The recycler helps recover water, while solar panels supply energy and lose efficiency as dust builds up. Machines wear down and need maintenance. A broken carbon-dioxide scrubber is a different problem from a stopped oxygen generator: adding oxygen does not remove the carbon dioxide people breathe out. Cooling matters too, because overheating can interrupt communication with the rescue ship. The player can inspect the nightly power forecast and switch systems on or off, choosing what the remaining battery supply can support.

We wanted the choices to have understandable consequences. A permanent repair may cost valuable parts, while a temporary solution can keep the player safe but create another cost later. The player can search outside, recover equipment, prepare shelter for a solar storm, and work towards restoring communication. Dinner adds another decision: use a normal meal, risk an unlabelled food box, or skip eating. Repeated hunger reduces the next day's working capacity. These are simplified game rules, designed to make trade-offs visible rather than reproduce an engineering or medical model.

NASA resources informed the life-support problems, including carbon-dioxide removal, oxygen generation by electrolysis, water recovery, thermal control, and solar-powered electrical systems. NASA's reporting on a solar storm observed at Mars also helped ground the environmental hazards. Short science explanations appear alongside incident outcomes and in the game's reference material. The fictional setting, event probabilities, rescue timetable, and numerical costs remain clearly separate from the real scientific principles.

We developed the game in Unity using C#, UI Toolkit, and the Input System. The presentation combines pixel-styled environments with Blender-based character and environment artwork. The survival rules are separated from the presentation in a C# simulation, making individual rules easier to test. Thai and English text are included so that language is less of a barrier for the learners we are designing for.

Our intended impact is to help children recognise that a habitat survives through several systems working together. We want players to leave with questions such as why air needs to circulate, why batteries matter even when solar panels work, and how fixing one problem can affect another. The current project is a playable prototype. Its educational value and difficulty still need evaluation with children and educators; we do not claim a measured learning improvement.

## Use of Super Intelligence (SI)

Suggested disclosure text, to be aligned with the team's final asset inventory:

We used AI assistants to support programming, debugging, design exploration, and drafting project documentation. Our team directed the game concept and selected and integrated the proposed changes. Visual development also involved scripted Blender workflows. NASA publications were used as scientific references; we did not use AI-generated information as a substitute for those sources. AI use should be read alongside the asset credits and SI disclosures supplied with the final project and demonstration.

Editor note: code and text assistance are confirmed by this development record. Check the team's actual tools and visual/audio asset provenance before selecting additional SI categories. The grayscale robot concept images are AI-generated design studies, not evidence of an implemented feature. If included in a demo or project gallery, they require the visible SI indicator described in the supplied form. Do not state that watermarks or complete asset metadata are already present without checking them.

## NASA Data / Resources

Use each resource title as Link Text and its complete URL as Link URL. These are NASA resources used for the game's scientific grounding, not a claim of live API integration.

| Link Text | Link URL | Connection to the game |
|---|---|---|
| NASA — Environmental Control and Life Support Systems (ECLSS) | https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/ | Carbon-dioxide removal, oxygen generation by electrolysis, and water recovery. |
| NASA — Water Recovery Milestone on the International Space Station | https://www.nasa.gov/missions/station/nasa-achieves-water-recovery-milestone-on-international-space-station/ | Context for the recycler's educational explanation; the reported milestone concerns a particular ISS recovery configuration, not a fixed efficiency for the fictional base. |
| NASA — Active Thermal Control System Overview | https://www.nasa.gov/wp-content/uploads/2021/02/473486main_iss_atcs_overview.pdf | Coolant loops and radiators inform the thermal-control incident. |
| NASA — International Space Station Status, 30 August 2018 | https://www.nasa.gov/blogs/spacestation/2018/08/30/international-space-station-status-3/ | The real leak and repair example used in the incident's science explanation. |
| NASA — Putting a Square Peg in a Round Hole (Apollo 13) | https://www.nasa.gov/image-article/putting-square-peg-round-hole/ | Historical carbon-dioxide absorber canisters and improvised life-support repair. The old Flight Journal link now redirects to a general index. |
| NASA — Overview of ISS Electrical Power | https://ntrs.nasa.gov/api/citations/20160014034/downloads/20160014034.pdf | Solar arrays, battery storage, and power during orbital darkness. |
| NASA — Watches Mars Light Up During Epic Solar Storm | https://www.nasa.gov/solar-system/planets/mars/nasa-watches-mars-light-up-during-epic-solar-storm/ | A real Mars radiation event informs the storm/shelter scenario; hazard probabilities are game rules. |
| NASA Human Research Roadmap — SANS Evidence Report (2022) | https://humanresearchroadmap.nasa.gov/evidence/reports/SANS%20EB%202022%20FINAL_8-15-22.pdf | Background on air mixing and local CO2 exposure in spacecraft. The application to Mars is simplified. |

Link check on 15 November: the SANS PDF could not be retrieved by the web checker; confirm its anonymous accessibility before submission. Other source listings do not imply that every page has been independently checked for every scientific claim.

The current base simulation's oxygen generator is described as splitting water, following the ECLSS example. Do not describe this implementation as a functioning MOXIE model. Do not reuse the old DONKI-data claims without checking whether those records affect the current Last Dock route.

## Space Agency Partner & Other Data / Resources

List tools and assets actually included, together with their public source and license information. This is a starting inventory, not a completed rights audit.

| Link Text | Link URL | Use |
|---|---|---|
| Unity | https://unity.com/ | Game engine, C# runtime, UI, and build tools. |
| Blender | https://www.blender.org/ | Character, environment, and cinematic asset workflows. |
| Kenney — Interface Sounds | https://kenney.nl/assets/interface-sounds | UI audio; project includes a CC0 license file. |
| Kenney — Sci-Fi Sounds | https://kenney.nl/assets/sci-fi-sounds | Sci-fi effects; project includes a CC0 license file. |
| Kenney — RPG Audio | https://kenney.nl/assets/rpg-audio | Handling effects are loaded from Audio/KenneyRPG in Sfx.cs; project includes a CC0 license file. |
| SRG774 — Dark Sci-Fi Audio Pack | https://opengameart.org/content/dark-sci-fi-audio-pack | Background music; recorded as CC0 in project credits. |
| Google Fonts — Sarabun | https://fonts.google.com/specimen/Sarabun | Thai/English typography; include the font's license. |
| Google Fonts — Chakra Petch | https://fonts.google.com/specimen/Chakra+Petch | Thai/English typography; include the font's license. |
| MCP for Unity | https://github.com/CoplayDev/unity-mcp | Development/testing bridge, not a player-facing feature. |

Also list the AI tools actually used by the team and any other incorporated code, models, images, fonts, or audio. The user-supplied door effect is identified in docs/AUDIO_CREDITS.md as freesound_community's space-ship-door-open-47688.mp3 from Pixabay; its exact original public source URL/license still needs confirmation. The credits file's opening statement that all third-party audio is CC0 does not establish this separate file's rights.

## Project Demonstration

The supplied submission form requests either a public video of no more than 30 seconds or a presentation of no more than seven slides, including the title/introduction. This document does not create or publish that demo.

Suggested 30-second storyboard using footage from the actual current build:

| Time | Visual | Narration |
|---|---|---|
| 0–5 s | Title, ship impact, brief supply collection | We built LAST DOCK, a survival game for young space explorers. |
| 5–11 s | Transition to the base and its work-unit display | After escaping a damaged ship, you have seven days to keep a Mars base running. |
| 11–20 s | CO2 incident, repair choice, power forecast | Every action uses time. Air, water, heat, and electricity affect one another, so you have to choose what to fix first. |
| 20–26 s | Science explanation and a restored system | NASA resources connect these decisions to real life-support science. |
| 26–30 s | Rescue/result and project title | Learn by making choices, seeing the consequences, and trying again. |

Use genuine footage rather than the proposed grayscale robot scenes. Add any SI-generated indicators required by the supplied form. Keep essential text large enough for a short demo. Check the actual exported duration; a script alone is not proof of a compliant video.

## Project Link

Repository remote found in the local project: https://github.com/kittiphatrl-create/NASA

Anonymous web access returned 404 during preparation. This may be a private repository or another access problem. It is not verified as a valid public submission link. Provide a public project/release or public downloadable build link and test it signed out. A standalone Windows game does not require a separate website, but its download and instructions must be accessible to judges without requesting permission.

## Checks before copying the final form

- Use Last Dock's current seven-day, three-work-unit rules. The three-day, robot-controlled, first-person, monochrome redesign remains a design proposal.
- Keep public project and demo URLs separate; verify both anonymously.
- Confirm the exact challenge and local-event rules against the team's official page. The attached form, not this document, is the source for the stated 30-second/seven-slide and public-link requirements.
- Complete the real SI options, visible labels, code/data metadata, and third-party credits. Do not copy a claim of completed disclosure if it has not been implemented.
- Check the supplied door-sound license and any models/images whose provenance is missing.
- Review the fresh playtest report. Pending or missing cinematic assets should not be described as finished animation.
- Saving a project is separate from submitting it for judging, according to the supplied form. This task does not submit the project or accept any terms on the team's behalf.
