# LAST DOCK: Science Notes

This file lists every science statement the game shows the player. For each one it gives the NASA source and says how the game simplifies it. The statements are written in `Assets/Scripts/Game/GameFlow.LastDock.cs` (`LdIncidentOf(...).fact`).

The game is for ages 7–12, so each fact is one or two sentences. The fact appears when the player solves an incident, in the incident log on the station board, and on the end screen.

## Facts shown in game

| # | Statement in the game (EN) | Source (NASA) | How the game simplifies it |
|---|---|---|---|
| A, CO₂ scrubber | The ISS removes CO₂ from the air with molecular sieves. Earlier spacecraft like Apollo soaked it up with lithium hydroxide canisters. | ISS ECLSS: "Carbon dioxide is removed using molecular sieves" (https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/). Apollo 13 LiOH canisters and the "mail box" adapter (https://www.nasa.gov/history/afj/ap13fj/15day4-mailbox.html; https://www.nasa.gov/image-article/putting-square-peg-round-hole/) | CO₂ is a level (normal/high/critical), not an item. The broken scrubber raises CO₂ by one level step per action. The "absorber canister" is a one-shot LiOH-style item that lowers CO₂ by 3 steps. |
| B, Thermal | The ISS pumps liquid ammonia through loops to big radiators outside, which radiate the heat away into space. | ISS Active Thermal Control overview (https://www.nasa.gov/wp-content/uploads/2021/02/473486main_iss_atcs_overview.pdf). EATCS pump paper, NTRS 20110023292 (https://ntrs.nasa.gov/citations/20110023292) | Heat is a level. A broken loop raises it each action, faster while transmitting. At "high" the transmitter trips. Higher still, it drains a battery. "Switch off spare machines" is the game's version of shedding electrical load. |
| C, Leak | In 2018 the ISS crew traced a leak to a hole about 2 mm wide in a docked Soyuz and plugged it with epoxy on gauze. | ISS status, 30 Aug 2018 (https://www.nasa.gov/blogs/spacestation/2018/08/30/international-space-station-status-3/). Statement on the leak investigation (https://www.nasa.gov/news-release/statement-on-international-space-station-leak-investigation/) | Air loss is shown as oxygen tanks going down. "Close the hatch" isolates the module, which then can't be used until it is patched. "Detach storage" is a game-only choice that loses the module for good. |
| D, Fan | Air hardly mixes by itself in microgravity, so pockets of breathed-out CO₂ can form. ISS sleep stations aim a fan at the astronaut's face. | NASA Human Research Roadmap, SANS evidence report 2022: local CO₂ pockets from poor air mixing, and sleep-station fans aimed at the face (https://humanresearchroadmap.nasa.gov/evidence/reports/SANS%20EB%202022%20FINAL_8-15-22.pdf) | When the fan stops, CO₂ rises even though the scrubber works. This is CO₂ (D) independent of O₂ (F). The "backup fan circuit" costs 1 extra battery per night. |
| Storm, Mars surface | In May 2024 NASA's Curiosity rover measured a jump in radiation on the surface of Mars during a big solar storm. | "NASA Watches Mars Light Up During Epic Solar Storm" (https://www.nasa.gov/solar-system/planets/mars/nasa-watches-mars-light-up-during-epic-solar-storm/). Curiosity's RAD saw its largest surge since landing in 2012 (about 8,100 micrograys, roughly 30 chest X-rays). | One storm per run, on a seeded day, and ORBI warns the day before. During the storm every site outside is a hot spot. That night the crew loses 1 heart unless the shelter is powered (1 battery). |
| E, Power | The ISS circles Earth every 90 minutes and spends about 30 of them in shadow; batteries charged by the solar arrays keep it running. | Overview of ISS Electrical Power, NTRS 20160014034 (https://ntrs.nasa.gov/api/citations/20160014034/downloads/20160014034.pdf) | One "night" turn models a day: solar gives +1 battery and the station's base load takes −1. A power dip during a send asks the player to choose: use the battery, pause air and cooling for a moment, or cancel the send. |
| F, O₂ generator | The ISS makes oxygen by electrolysis: electricity splits water (H₂O) into oxygen and hydrogen. | ISS ECLSS: the cell stack "electrolyzes, or breaks apart, water … yielding oxygen and hydrogen" (https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/) | The generator splits the station's service water, so water is not a fifth resource. When it stops, the O₂ tanks drop by 1 per action. "Move slowly" saves that step's oxygen. CO₂ stays normal because the scrubber still works. |

## Other background sources from the brief

- NASA Crew Systems: https://www.nasa.gov/reference/crew-systems/
- JSC Life Support Subsystems: https://www.nasa.gov/reference/jsc-life-support-subsystems/
- Ames Air Revitalization: https://www.nasa.gov/ames/space-biosciences/bioengineering-branch/air-revitalization-at-arc/
- NTRS 20000032541 (ISS ECLSS): https://ntrs.nasa.gov/citations/20000032541

## Game abstractions that are not real NASA facts

- One site outside is a radiation hot spot each day, and a radiation meter shows which one. Real radiation is not on-off at fixed spots; this is a simple way to teach checking before going out.
- Unlabelled food can be spoiled (25%). This is a game rule; real space food is packed to last.
- Strangers who follow the rover, and "something at the door" on dark nights, are **fiction**. The game says so on screen ("story" / "เรื่องแต่ง").
- The rover's parts, charge cost and solar upgrade are game numbers.

- No food is made from electricity. Food only comes from the scavenge, the station's stock, and storage searches.
- Health is three hearts. Each one stands for "the crew got hurt" (critical CO₂, oxygen gone, hunger, a flat battery at night). It is not a medical model.
- "Take a part from the transmitter" is a game rule. It lets the player out of a no-parts dead end at a known cost (signal −1).
- Times and amounts (3 work units a day, 7 days, 2 battery to open the docking port) are game numbers, not real ones.

## How it was checked

On 2026-11-15 each statement above was checked against the linked page or its search summary. Two claims were rewritten after that check:

- "Zeolite beds" became "molecular sieves", the ECLSS page's wording.
- The "CO₂ bubble around your head" became "pockets of CO₂" with the sleep-station fan detail, the Human Research Roadmap wording.
