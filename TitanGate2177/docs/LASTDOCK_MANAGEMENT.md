# LAST DOCK — management core (v1, 2026-11-15)

The owner's direction: the heart of the game is **management**. Four systems only (no recruiting crew: it does not
fit a 7-day week). Turn-based as before: 7 days × 3 work units (WU). Walking and reading are free; actions cost 1 WU.
Switching power on/off is free (it is planning, not work). All numbers live in `StationConfig`
(`Assets/Scripts/Core/LastDock/StationSimulation.cs`); rules are covered by `Assets/Tests/EditMode/LastDockTests.cs`.

## 1. Power budget
- Each night: `battery += solar − (base load 1 + every system that is ON)`. Battery holds at most **12** (start 6 + kit).
- Solar tonight = **6 × (1 − dust)**, +1 with the portable panel; ×0.3 on the solar-storm day. Dust rises **+10 %**
  a night (+35 % in the storm), max 90 %. *Brush the panels* (airlock, 1 WU, not in the storm) sets dust to 0.
- Switchable systems and their nightly draw: CO₂ scrubber **1**, O₂ maker **2**, water recycler **1**,
  heater **1**, beacon **1** (once built). At the start scrubber, O₂ maker and heater are on (5 with
  base load = break-even on day 1); everything on costs 6 (7 with the beacon), so the player must choose.
- If the battery cannot pay tonight, systems switch off by themselves in this order: beacon, recycler,
  heater, O₂ maker, scrubber (each logged). Still short → crew −1 (cold and dark).

## 2. Machine condition (0–100 %)
- Scrubber, O₂ maker, air fan, coolant loop, recycler start between **60 % and 90 %** (an old station, seeded).
- Every night a running machine wears **−7 %** (fan and coolant always run; coolant −6 % more when heat is high).
  A switched-off machine does not wear. At 0 % it stops.
- From day 2 one breakdown check a day, at dawn or after the first action, on the running machine in the worst
  shape: chance **0 %** at ≥ 60 %, **25 %** at 40–59 %, **55 %** at 20–39 %, **90 %** below 20 %.
- **Service** (at the machine, 1 WU, no parts): +40 %. **Repair** a broken machine: 1 part, back to 45 %.
  Preventive work is cheaper than repair — that is the lesson.
- With no machine at risk, a hull **leak** can still happen (20 %).
- The board and machines show the % and "may break N %"; a soft "!" marks a running machine under 40 %.

## 3. Production chain (water is the shared input; start 8 water)
- Crew drinks **1 water** a night (none left: crew −1).
- **O₂ maker** (electrolysis): ON + working + water → uses 1 water a night, O₂ tanks +1.
  Switched off or dry → **−1 O₂ tank a night** (a planned cost). **Broken** → −1 every step (an emergency).
- **Recycler**: ON + working → +1 water a night.
- **Ice field** trip (airlock, 1 WU, uses today's one trip): **+3 water**.
- **Heater** off at night → cold → one fewer WU tomorrow.
- Storage lockers can also hold a 2-water container.

## 4. Rescue — 3 signals, plus 2 battery for the docking port on day 7
- **Radio route**: board (Comms, 1 part) → power test (Comms, 1 battery, coolant loop must work) → antenna
  (airlock job, not in the storm). Then *send location*: 1 WU + 1 battery each. The radio repair kit does board and test.
- **Beacon route**: **2** rover parts (old rover wreck, one per safe trip) + 1 part → *build the beacon* (airlock).
  It sends 1 signal at once and **+1 every night** it is switched on and paid for. Rover parts build the rover
  (3) **or** the beacon (2).

## Forecast (station board, free to read)
Solar, draw, battery after tonight, and how many days O₂, water and food last with the current plan. The HUD shows
water and tonight's power balance; the top line warns when tonight's plan is short or O₂/water run out tomorrow.

## Balance (bots, 50 seeds, even kit 2/2/2/2) — `BalanceReport`
Radio route 39/50 rescued, beacon route 28/50, ignoring everything 0/50. Tests require ≥ 12/20 for both routes.

## Removed
- **No greenhouse in the station** (owner, 2026-11-15). Food comes only from the lifeboat kit, the station's
  stock, lockers, unlabelled boxes and frozen food; the dinner screen decides who eats. The ship's lab plants stay.

## What stays
Leaks, power dip on send, storm night shelter, rover, mystery food, raiders/dark nights, special ship items,
hands-on timing game for repairs and sending, save/continue by replay.
