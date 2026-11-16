# LAST DOCK — Fish Audio script (พร้อมแท็กอารมณ์)

เวอร์ชันนี้ใช้กับ Fish Audio **S2** (แท็กอยู่ใน `[วงเล็บเหลี่ยม]` วางไว้หน้าคำหรือวลีที่ต้องการ) — คัดลอกข้อความในกล่องโค้ดแต่ละกล่องไปวางได้เลย 1 กล่อง = 1 ไฟล์เสียง

## วิธีใช้ (อ่านก่อน)

1. **เลือกเสียง 6 เสียง** ใน Fish Audio: P1, P2 (Frame), P3, P4, P5, NARRATOR ให้แต่ละคนเสียงต่างกันชัด ๆ (ถ้าจะโคลนเสียงเพื่อนในทีม ต้องให้เจ้าของเสียงอนุญาตก่อน)
2. **แทนที่ `[P3_NAME]` `[P4_NAME]` `[P5_NAME]`** ด้วยชื่อเล่นจริงก่อนกด generate ไม่อย่างนั้นมันจะอ่านออกมาตรง ๆ หรือคิดว่าเป็นแท็ก
3. **ถ้าบัญชีใช้โมเดล S1** (รุ่นเก่า): เปลี่ยน `[...]` เป็น `(...)` และย้ายแท็กไปไว้**ต้นประโยค**เท่านั้น เช่น `(whispering) Here's a lesson...`
4. ห้ามใส่แท็กเกิน 2 อันติดกัน และอย่าใส่แท็กที่ขัดกันในประโยคเดียว (เช่น whispering + shouting)
5. ถ้าเสียงไม่ได้อารมณ์ ลอง generate ใหม่ 2–3 รอบ แล้วเลือกอันที่ดีที่สุด
6. บรรทัด **ALL**: generate ทั้ง 5 เสียงแยกกัน แล้วซ้อนกันในตัวตัดต่อ (เลื่อนกันสัก 0.05–0.1 วิ ให้ฟังเป็นหลายคน)

คำที่เขียนให้ TTS อ่านถูก: `2177` → "twenty-one seventy-seven", CO₂ → "C O two", ECLSS → "E C L S S", C# → "C sharp", 3D → "three-D", ชื่อทีม → "Overtime P W" (ถ้าทีมอ่านชื่อทีมแบบอื่น ให้แก้ตรงนี้)

แท็กที่ใช้: `[whispering]` กระซิบ · `[shouting]` ตะโกน · `[excited]` · `[tired]` · `[sigh]` · `[surprised]` · `[confident]` · `[laughing]` · `[chuckle]` · `[serious]` · `[calm]` · `[pause]`

---

## SCENE 1 — Discord: the team gathers (0:00–0:35)

`P1_S1_01.wav` — P1
```
[tired][sigh] Okay... nobody's here yet. Classic.
```

`P2_S1_01.wav` — P2 (Frame) · **กระซิบ → ตะโกน**
```
[whispering] Bro. [shouting] Bro! Are you awake?
```

`P1_S1_02.wav` — P1
```
[tired] I'm here. What's up?
```

`P2_S1_02.wav` — P2
```
[excited] Let's do the NASA Space Apps Challenge.
```

`P1_S1_03.wav` — P1
```
[surprised] Us? A NASA challenge?
```

`P2_S1_03.wav` — P2
```
[excited] Yeah! We make games, right? Let's make a game about surviving in space.
```

`P1_S1_04.wav` — P1
```
[pause] Okay. [confident] I'm in. But we need more people.
```

`P2_S1_04.wav` — P2
```
[confident] Already on it.
```

`P3_S1_01.wav` — P3
```
[excited] Hey! Frame said something about Mars?
```

`P4_S1_01.wav` — P4 · **กระซิบ → ตะโกน**
```
[whispering] Did someone say... [shouting] NASA?
```

`P5_S1_01.wav` — P5
```
[surprised] I'm here. What did I miss?
```

`P2_S1_05.wav` — P2
```
[excited] Welcome to team Overtime P W.
```

`P5_S1_02.wav` — P5
```
[sigh] Overtime... [chuckle] yeah, that sounds about right.
```

`ALL_S1_laugh.wav` — ทุกเสียง (ซ้อนกัน)
```
[laughing] Ha ha ha!
```

---

## SCENE 2 — Discord: the lead hands out jobs (0:35–1:20)

`P1_S2_01.wav` — P1
```
[confident] Okay, team. Five of us. Let's split the work.
```

`P1_S2_02.wav` — P1
```
[confident] Frame. You're on gameplay code. The survival rules, the machines, everything that can break.
```

`P2_S2_01.wav` — P2
```
[chuckle] Got it. I'll break everything.
```

`P1_S2_03.wav` — P1
```
[confident] [P3_NAME]. You're on art. Pixel art, Blender models. Make Mars look good.
```

`P3_S2_01.wav` — P3
```
[excited] On it, boss!
```

`P1_S2_04.wav` — P1
```
[confident] [P4_NAME]. Science. Read the NASA documents and keep us honest.
```

`P4_S2_01.wav` — P4
```
[serious] Copy that. No fake science.
```

`P1_S2_05.wav` — P1
```
[confident] [P5_NAME]. Story, sound, and our video.
```

`P5_S2_01.wav` — P5 · **กระซิบ**
```
Roger. [whispering] I'll make it sound like space.
```

`P1_S2_06.wav` — P1 · **ตะโกนท้าย**
```
[confident] And I'll lead the team and put everything together. Seven days on Mars... [shouting] let's go!
```

`ALL_S2_letsgo.wav` — ทุกเสียง (ซ้อนกัน) · **ตะโกน**
```
[shouting][excited] Let's go!
```

---

## SCENE 3 — The idea (1:20–2:00) · NARRATOR

`NAR_S3_01.wav`
```
[confident] This is LAST DOCK. Junior Astronaut Mission Trainer. A survival game that shows kids how a space habitat stays alive.
```

`NAR_S3_02.wav`
```
[calm] We started with one question. [pause] How can a child understand life in space, without reading a textbook first?
```

`NAR_S3_03.wav`
```
[calm] Life support is invisible. Air, water, heat and power. You only notice them when they break. [pause] So we made a game where they break, and the player has to decide what to fix first.
```

`NAR_S3_04.wav`
```
[calm] It's made for kids aged seven to twelve, and it speaks both Thai and English.
```

---

## SCENE 4 — The game (2:00–3:55) · NARRATOR

`NAR_S4a_01.wav`
```
[calm] The story begins in the year twenty-one seventy-seven, on the spacecraft Kestrel. [serious] Space debris hits the ship! You have sixty seconds to grab supplies and reach the escape pod.
```

`NAR_S4a_02.wav`
```
[calm] You can carry only three things at a time, and the pod holds eight. So you can't take everything. Batteries, food, oxygen, spare parts, a radio kit. What you save here changes what you can do later.
```

`NAR_S4b_01.wav`
```
[calm] You land on Mars and hide in an abandoned base. Rescue is seven days away. And every day, you get only three work units.
```

`NAR_S4b_02.wav`
```
[calm] Walking around and reading panels are free. Repairs, searches and trips outside cost work units. [serious] And some problems get worse every time you act. So a good job can still be the wrong job to do first.
```

`NAR_S4c_01.wav`
```
[calm] Everything in the base is connected. The oxygen maker needs water and power. The recycler gets water back. Solar panels give power... but dust makes them weaker.
```

`NAR_S4c_02.wav` · **กระซิบ** (เหมือนบอกความลับ)
```
[whispering] Here's a lesson kids don't expect. [serious] If the C O two scrubber breaks, more oxygen won't save you. You still have to remove the carbon dioxide you breathe out.
```

`NAR_S4c_03.wav`
```
[calm] At night, you check the power forecast and choose which systems stay on.
```

`NAR_S4d_01.wav` · **กระซิบท้ายประโยค**
```
[serious] Then a solar storm is coming. Do you prepare the shelter... [whispering] or fix the radio?
```

`NAR_S4d_02.wav`
```
[calm] Even dinner is a choice. A normal meal, a mystery box with no label, or nothing at all. Stay hungry too long, and you can do less tomorrow.
```

`NAR_S4d_03.wav`
```
[confident] Keep the base alive for seven days, bring back communication, and the rescue ship comes for you.
```

---

## SCENE 5 — The real science (3:55–4:45) · NARRATOR

`NAR_S5_01.wav`
```
[serious] We didn't make up the science. We learned it from NASA.
```

`NAR_S5_02.wav`
```
[calm] Removing carbon dioxide, making oxygen from water, and recycling water come from NASA's life support system on the Space Station, called E C L S S. The heat problem is based on the station's cooling loops, and the power problem on its solar arrays and batteries.
```

`NAR_S5_03.wav`
```
[calm] The storm is inspired by a real solar storm that NASA saw light up Mars. And the C O two repair is a nod to Apollo thirteen.
```

`NAR_S5_04.wav`
```
[calm] After each problem, a short science card explains the real thing behind it. The seven-day rescue, the numbers and the chances are game rules. [confident] But the ideas behind them are real.
```

---

## SCENE 6 — How we built it (4:45–5:40) · NARRATOR

`NAR_S6_01.wav`
```
[confident] We built LAST DOCK in Unity, with C sharp. The survival rules run in their own simulation, separate from the graphics. We wrote more than a hundred automated tests, and bot players that play by themselves to check the balance.
```

`NAR_S6_02.wav`
```
[calm] Problems are picked by a fair incident director. Never the same fault twice in a row, and never one you have no time to answer.
```

`NAR_S6_03.wav`
```
[calm] The art mixes pixel art drawn in code with characters rendered from Blender. The main menu is a live three-D scene. You can drag to spin the ship. And Thai text is shaped properly, so every vowel and tone mark sits in the right place.
```

`NAR_S6_04.wav` — ประโยคเปิดเผยการใช้ AI (เพิ่ม "voice-over" เพราะใช้ Fish Audio)
```
[calm] We used AI tools for coding, debugging, writing documents, and this voice-over. Our team led the design and chose what went into the game.
```

---

## SCENE 7 — Impact and next steps (5:40–6:05) · NARRATOR

`NAR_S7_01.wav`
```
[calm] We want kids to finish the game with questions. Why does air need to move? Why do batteries matter, even when the sun is shining?
```

`NAR_S7_02.wav`
```
[calm] LAST DOCK is a playable prototype. We haven't tested it with children in a classroom yet. [confident] That's our next step. Play it with real kids and teachers, and learn from them.
```

---

## SCENE 8 — Discord outro (6:05–6:25)

`P2_S8_01.wav` — P2
```
[surprised] So... we actually finished?
```

`P1_S8_01.wav` — P1
```
[laughing] We finished. Seven days on Mars.
```

`P5_S8_01.wav` — P5
```
[tired][sigh] Felt more like seven days of overtime.
```

`ALL_S8_thanks.wav` — ทุกเสียง (ซ้อนกัน) · **ตะโกน**
```
[shouting][excited] Thank you! We're Overtime P W!
```

---

## จุดกระซิบ / ตะโกน สรุป

| ไฟล์ | ใคร | แบบ | ทำไม |
|---|---|---|---|
| `P2_S1_01` | Frame | กระซิบ → ตะโกน | ปลุกเพื่อนตอนดึก เรียกเสียงหัวเราะเปิดคลิป |
| `P4_S1_01` | P4 | กระซิบ → ตะโกน "NASA?" | มุกตอนจอยเข้าห้อง |
| `P5_S2_01` | P5 | กระซิบ | "make it sound like space" เสียงลึกลับ |
| `P1_S2_06` | P1 | ตะโกนท้าย | ปลุกพลังทีมก่อนเข้าเกม |
| `ALL_S2_letsgo` | ทุกคน | ตะโกน | ปิดซีนแนะนำทีม |
| `NAR_S4c_02` | ผู้บรรยาย | กระซิบ | บทเรียน CO₂ ที่เด็กไม่คาดคิด ดึงความสนใจกรรมการ |
| `NAR_S4d_01` | ผู้บรรยาย | กระซิบท้าย | ทางเลือกที่ยาก "...or fix the radio?" |
| `ALL_S8_thanks` | ทุกคน | ตะโกน | ปิดคลิป |

ช่วงนำเสนอเนื้อหา (ซีน 3–7) ใช้กระซิบแค่ 2 ที่ ที่เหลือพูดเรียบ ๆ ชัด ๆ เพื่อให้กรรมการฟังรู้เรื่อง
