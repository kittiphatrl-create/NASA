// Mission Handbook content (in-game modal; no external URL yet).
// Real-world facts are simplified and approximate; Jump Drive is fictional.
namespace Last72
{
    public static class Handbook
    {
        public static readonly string[] Pages =
        {
            // 1. Life Support
            "<b><color=#4DBFFF>LIFE SUPPORT — Oxygen Recycling and CO₂ Removal</color></b>\n\n" +
            "<b>ในโลกจริง (ISS)</b>\n" +
            "• ออกซิเจนผลิตจากการแยกน้ำด้วยไฟฟ้า (Electrolysis): H₂O → H₂ + O₂\n" +
            "• CO₂ ที่นักบินหายใจออกถูกดักด้วยตัวกรองดูดซับ (เช่น zeolite) แล้วปล่อยทิ้งหรือนำไปใช้ต่อ\n" +
            "• ระบบ Sabatier รวม CO₂ กับ H₂ ได้น้ำกลับมาใช้ใหม่ — วนทรัพยากรให้ได้มากที่สุด\n" +
            "• ถ้า CO₂ สะสม: ปวดหัว สมาธิลด ตัดสินใจพลาด — อันตรายก่อนออกซิเจนจะหมดเสียอีก\n\n" +
            "<b>ในเกม</b>\n" +
            "• Oxygen Scrubber ตัน → O2 ลด 1.5/ชม. (หมดในราว 48 ชม.)\n" +
            "• ใส่ Spare Filter (อยู่ใน Maintenance Cabinet — ต้องใช้ Multitool เปิด) → O2 ลดเหลือ 0.2/ชม.\n" +
            "• ดูอัตราการลดและเวลาที่เหลือได้ที่ Emergency Panel\n\n" +
            "<color=#FFD27F>คำแนะนำ: ซ่อมระบบช่วยชีวิตก่อน — ทุกอย่างอื่นรอได้ แต่ลมหายใจรอไม่ได้</color>",

            // 2. Power
            "<b><color=#4DBFFF>POWER MANAGEMENT — Energy Allocation</color></b>\n\n" +
            "<b>ในโลกจริง</b>\n" +
            "• ยานส่วนใหญ่ใช้แผงโซลาร์ + แบตเตอรี่. ยิ่งไกลดวงอาทิตย์ แสงยิ่งน้อย —\n" +
            "  ที่ดาวเสาร์ได้แสงเพียงราว 1% ของที่โลก จึงมักใช้ RTG (พลังงานจากการสลายตัวของไอโซโทป) เช่นยาน Cassini\n" +
            "• เมื่อพลังงานจำกัด วิศวกรทำ Load Shedding: ตัดระบบไม่จำเป็นออก\n" +
            "  ลำดับความสำคัญ: ระบบช่วยชีวิต → การสื่อสาร/นำทาง → การทดลอง/ระบบเสริม\n" +
            "• อุปกรณ์ป้องกัน (Fuse/Breaker) ช่วยกันไม่ให้ความเสียหายลามไปทั้งระบบ\n\n" +
            "<b>ในเกม</b>\n" +
            "• PDU ฟิวส์ขาด → Power ลด 0.6/ชม. ใส่ Fuse (ต้องมี Multitool) → +15 และลดเหลือ 0.15/ชม.\n" +
            "• งานที่กินไฟ: Calibrate Nav -5, Transfer Reserve -4, Stabilize Jump -10, Override -2\n" +
            "• เหตุการณ์ Solar Particle Event ที่ T-30 ชม. ทำ Power -6 ถ้า PDU ยังไม่ได้ซ่อม\n" +
            "• THE JUMP ต้อง Power ≥ 35, MARS ต้อง ≥ 20\n\n" +
            "<color=#FFD27F>คำแนะนำ: ซ่อม PDU เร็วเท่าไร ไฟที่เหลือไว้ใช้ตอนท้ายก็ยิ่งมาก</color>",

            // 3. Mars Survival
            "<b><color=#4DBFFF>MARS SURVIVAL — Radiation Protection and Emergency Habitat</color></b>\n\n" +
            "<b>ในโลกจริง</b>\n" +
            "• ดาวอังคารไม่มีสนามแม่เหล็กโลกปกคลุมทั้งดาว และบรรยากาศบางมาก (ความดันราว 1% ของโลก)\n" +
            "  → รังสีคอสมิกและอนุภาคจากดวงอาทิตย์ลงถึงพื้นได้มากกว่าบนโลกมาก\n" +
            "• เครื่อง RAD บนรถ Curiosity วัดรังสีที่พื้นผิวได้ราว 0.7 mSv/วัน — สูงกว่าบนโลกหลายร้อยเท่า\n" +
            "• วิธีป้องกัน: กำบังด้วยดิน Regolith หรือน้ำ, ใช้ถ้ำลาวา/ใต้ดิน, หลบในห้องป้องกันช่วยพายุสุริยะ\n" +
            "• ที่อยู่อาศัยต้องปิดผนึกและเพิ่มแรงดัน ป้องกันฝุ่นละเอียดซึ่งกัดกร่อนอุปกรณ์\n\n" +
            "<b>ในเกม (ARES-7 เป็นสถานีสมมติ)</b>\n" +
            "• MARS FALLBACK = Ending ที่สำเร็จ ต้อง: Final Preparation, Navigation READY, Propellant ≥ 30, Power ≥ 20\n" +
            "• ไม่ต้องซ่อม Jump Drive — เป็นทางรอดเมื่อเชื้อเพลิงไม่พอสำหรับ Jump (≥ 65)\n\n" +
            "<color=#FFD27F>คำแนะนำ: การยอมเปลี่ยนแผนเพื่อให้ลูกเรือรอด คือการตัดสินใจของผู้บังคับการที่ดี</color>"
        };
    }
}
