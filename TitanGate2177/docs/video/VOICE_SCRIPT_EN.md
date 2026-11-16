# LAST DOCK — Voice-over script (Overtimepw, Space Apps 2026 Bangkok)

Target length: **about 6:25** (estimate at 135 words/min incl. pauses) (allowed 5:00–7:00).
Scenes 1, 2 and 8 happen in Discord with all five members. Scenes 3–7 use **one narrator voice**.

| Tag | Who | Role in the video | Job given in Scene 2 *(แก้ให้ตรงงานจริง)* |
|---|---|---|---|
| **P1** | [ชื่อ — หัวหน้าทีม] | joins Discord first, assigns jobs | Team lead & game design |
| **P2** | Frame [นามสกุล] | says "let's do NASA Space Apps", invites the others | Gameplay programming |
| **P3** | [ชื่อ] | member | Art — pixel art & Blender |
| **P4** | [ชื่อ] | member | Science research — NASA sources |
| **P5** | [ชื่อ] | member | Story, sound & video |
| **NARRATOR** | [ใครก็ได้ 1 คน] | Scenes 3–7 | — |

Recording tips
- Read slowly and clearly, about 130–140 words per minute. Leave a short breath between lines.
- Quiet room, phone close to the mouth. One file per speaker per scene, e.g. `P1_S2.wav`, `NAR_S4.wav`.
- Lines in *(brackets)* are acting notes, not words to read.
- If a line feels unnatural, change the words but keep the meaning.

---

## SCENE 1 — Discord: the team gathers (0:00–0:35)

*[Screen: Discord voice channel "Overtimepw". P1 alone. Join sound.]*

**P1:** *(tired, alone)* Okay… nobody's here yet. Classic.

*[Join sound — P2 (Frame) appears]*

**P2:** Bro. Bro! Are you awake?

**P1:** I'm here. What's up?

**P2:** Let's do the NASA Space Apps Challenge.

**P1:** Us? A NASA challenge?

**P2:** Yeah! We make games, right? Let's make a game about surviving in space.

**P1:** *(pause)* …Okay. I'm in. But we need more people.

**P2:** Already on it.

*[Ping! P3 joins.]*

**P3:** Hey! Frame said something about Mars?

*[Ping! P4 joins.]*

**P4:** Did someone say NASA?

*[Ping! P5 joins.]*

**P5:** I'm here. What did I miss?

**P2:** Welcome to team Overtimepw.

**P5:** Overtime… yeah, that sounds about right.

*[Everyone laughs.]*

---

## SCENE 2 — Discord: the lead hands out jobs (0:35–1:20)

*[Still in Discord. Each time P1 names someone, cut to that member's photo popping up with a card: NAME · ROLE. After the last one, all five cards sit together on screen.]*

**P1:** Okay, team. Five of us. Let's split the work.

**P1:** Frame — you're on gameplay code. The survival rules, the machines, everything that can break.

**P2:** Got it. I'll break everything.

*[Card: Frame · Gameplay Programmer]*

**P1:** [P3] — you're on art. Pixel art, Blender models. Make Mars look good.

**P3:** On it, boss.

*[Card: P3 · Artist]*

**P1:** [P4] — science. Read the NASA documents and keep us honest.

**P4:** Copy that. No fake science.

*[Card: P4 · Science Research]*

**P1:** [P5] — story, sound, and our video.

**P5:** Roger. I'll make it sound like space.

*[Card: P5 · Story, Sound & Video]*

**P1:** And I'll lead the team and put everything together. Seven days on Mars… let's go!

**ALL:** Let's go!

*[Card: P1 · Team Lead & Game Design → all five cards together → hard cut to title.]*

---

## SCENE 3 — The idea (1:20–2:00)

*[Title card: LAST DOCK — Junior Astronaut Mission Trainer. Then Slides 2–3.]*

**NARRATOR:** This is LAST DOCK — Junior Astronaut Mission Trainer. A survival game that shows kids how a space habitat stays alive.

**NARRATOR:** We started with one question. How can a child understand life in space, without reading a textbook first?

**NARRATOR:** Life support is invisible. Air, water, heat and power — you only notice them when they break. So we made a game where they break, and the player has to decide what to fix first.

**NARRATOR:** It's made for kids aged seven to twelve, and it speaks both Thai and English.

---

## SCENE 4 — The game (2:00–3:55)

### 4a. Escape from the Kestrel

**NARRATOR:** The story begins in the year 2177, on the spacecraft Kestrel. Space debris hits the ship. You have sixty seconds to grab supplies and reach the escape pod.

**NARRATOR:** You can carry only three things at a time, and the pod holds eight. So you can't take everything. Batteries, food, oxygen, spare parts, a radio kit… what you save here changes what you can do later.

### 4b. The Mars base

**NARRATOR:** You land on Mars and hide in an abandoned base. Rescue is seven days away — and every day, you get only three work units.

**NARRATOR:** Walking around and reading panels are free. Repairs, searches and trips outside cost work units. And some problems get worse every time you act. So a good job can still be the wrong job to do first.

### 4c. Everything is connected

**NARRATOR:** Everything in the base is connected. The oxygen maker needs water and power. The recycler gets water back. Solar panels give power… but dust makes them weaker.

**NARRATOR:** Here's a lesson kids don't expect. If the CO2 scrubber breaks, more oxygen won't save you. You still have to remove the carbon dioxide you breathe out.

**NARRATOR:** At night, you check the power forecast and choose which systems stay on.

### 4d. Storm, dinner, rescue

**NARRATOR:** Then a solar storm is coming. Do you prepare the shelter… or fix the radio?

**NARRATOR:** Even dinner is a choice: a normal meal, a mystery box with no label, or nothing at all. Stay hungry too long, and you can do less tomorrow.

**NARRATOR:** Keep the base alive for seven days, bring back communication, and the rescue ship comes for you.

---

## SCENE 5 — The real science (3:55–4:45)

**NARRATOR:** We didn't make up the science. We learned it from NASA.

**NARRATOR:** Removing carbon dioxide, making oxygen from water, and recycling water come from NASA's life support system on the Space Station, called ECLSS. The heat problem is based on the station's cooling loops, and the power problem on its solar arrays and batteries.

**NARRATOR:** The storm is inspired by a real solar storm that NASA saw light up Mars. And the CO2 repair is a nod to Apollo 13.

**NARRATOR:** After each problem, a short science card explains the real thing behind it. The seven-day rescue, the numbers and the chances are game rules — but the ideas behind them are real.

---

## SCENE 6 — How we built it (4:45–5:40)

**NARRATOR:** We built LAST DOCK in Unity, with C#. The survival rules run in their own simulation, separate from the graphics. We wrote more than a hundred automated tests, and bot players that play by themselves to check the balance.

**NARRATOR:** Problems are picked by a fair "incident director": never the same fault twice in a row, and never one you have no time to answer.

**NARRATOR:** The art mixes pixel art drawn in code with characters rendered from Blender. The main menu is a live 3D scene — you can drag to spin the ship. And Thai text is shaped properly, so every vowel and tone mark sits in the right place.

**NARRATOR:** We used AI assistants for coding, debugging and writing documents. Our team led the design and chose what went into the game.

---

## SCENE 7 — Impact and next steps (5:40–6:05)

**NARRATOR:** We want kids to finish the game with questions. Why does air need to move? Why do batteries matter, even when the sun is shining?

**NARRATOR:** LAST DOCK is a playable prototype. We haven't tested it with children in a classroom yet. That's our next step: play it with real kids and teachers, and learn from them.

---

## SCENE 8 — Discord outro (6:05–6:25)

*[Back to the Discord call, all five online.]*

**P2:** So… we actually finished?

**P1:** We finished. Seven days on Mars.

**P5:** Felt more like seven days of overtime.

**ALL:** Thank you! We're Overtimepw!

*[Credits: five members with full names and roles, NASA resources, Unity, Blender, Kenney audio, SRG774 music, Google Fonts.]*
