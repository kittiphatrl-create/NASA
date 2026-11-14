# ตรวจการใช้ข้อมูล NASA — 14 พฤศจิกายน 2026

## คำตอบ

เกมใช้ความรู้และข้อมูลอ้างอิง NASA แบบ offline จริง แต่ยังไม่ใช่ระบบเชื่อม NASA API สด และยังยืนยันไม่ได้ว่าผ่านข้อกำหนด challenge ใดโดยไม่เห็นโจทย์เฉพาะ

| สิ่งที่พบ | จุดใช้งาน | สถานะ |
|---|---|---|
| Solar flare ย้อนหลัง 6 แถว | `RealFlares` ใน `GameFlow.Base.cs`, แสดงในฐาน | hard-coded records; ยังไม่มี raw DONKI response/event-ID manifest |
| X9.0 วันที่ 2024-10-03 | หนึ่งตัวอย่างของ records | วันและระดับยืนยันจาก [NASA](https://science.nasa.gov/blogs/solar-cycle-25/2024/10/03/sun-releases-strong-solar-flare-6/) |
| X8.7 วันที่ 2024-05-14 | หนึ่งตัวอย่างของ records | วันและระดับยืนยันจาก [NASA SVS](https://svs.gsfc.nasa.gov/14593) |
| MOXIE | เครื่องผลิตออกซิเจนและหน้าแหล่งความรู้ | หลักการอ้าง NASA; ผลผลิต/ราคาไฟในเกมเป็น tuning ไม่ใช่เครื่องจริง ([NASA](https://www.nasa.gov/solar-system/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/)) |
| ECLSS | งานเครื่องรีไซเคิลและความรู้ยังชีพ | หลักการอ้าง [NASA ECLSS](https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/); ไม่ใช่ engineering simulation ที่รับรองความถูกต้อง |
| Curiosity RAD | ความรู้รังสี/Science archive | ต้องกำกับช่วงเวลาของข้อมูล; [NASA release](https://www.nasa.gov/news-release/nasa-rover-results-include-first-age-measurement-on-mars-and-help-for-human-exploration/) รายงาน 0.67 mSv/day ในช่วง Aug 2012–Jun 2013 ขณะที่ข้อความในเกมอ้าง paper 0.64 และปัด 0.7 ต้องตรวจ citation ให้ตรงกัน |
| Radiation meter ในฐานล่าสุด | `GameFlow.Radiation.cs` | **หน่วยเกม ไม่ใช่ mSv**: ทุก 12 หน่วยเสีย 1 health; ออกนอกฐาน 15 วินาทีเพิ่ม 1 หน่วย นี่คือกติกาเกม |
| จัมป์ เอเลี่ยน เผ่าพันธุ์ สแกนผู้มาเยือน อรุณ และกู้ภัย 7 วัน | เนื้อเรื่อง | เรื่องแต่ง ไม่ใช่ข้อค้นพบ NASA |

## ขาดอะไรถ้าต้องแสดงหลักฐานการใช้ NASA datasets

1. ชื่อและ URL challenge/ปีที่สมัคร เพื่อแยก requirement จริงจากความเข้าใจของทีม
2. Dataset URL/API query, event IDs, retrieval date, units, raw snapshot และ source manifest ของ records ที่ใช้
3. อธิบายว่าแต่ละ record มีผลต่อเกมตรงไหน ปัจจุบันการแสดงวัน/ระดับ flare ไม่ได้พิสูจน์ว่าความรุนแรงของ gameplay คำนวณจากข้อมูล NASA
4. แยก observed / scientific principle / approximation / game balance / fiction ใน UI และเอกสาร
5. ตรวจค่ารังสีและ citation ให้ตรงช่วงวัด ห้ามนำค่าหนึ่งไปอ้างเป็นค่าคงที่ทุกพื้นที่/ทุกเวลา

[DONKI](https://ccmc.gsfc.nasa.gov/tools/DONKI/) และ [NASA CCMC API index](https://ccmc.gsfc.nasa.gov/tools/API/) เป็นจุดเริ่มต้นสำหรับ snapshot ที่ทำซ้ำได้ ไม่จำเป็นต้องสร้างเว็บหรือให้ผู้เล่นออนไลน์เพื่อใช้ข้อมูลเหล่านี้

งานรอบนี้เป็นการตรวจและส่งต่อ ไม่ได้เพิ่ม API หรือแก้สมดุลเกม
