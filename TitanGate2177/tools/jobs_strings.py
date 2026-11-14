# Strings for repair jobs, the toolbox, radiation and the updated guide (round 9).
def extend_jobs(entries):
    def put(key, en, th):
        old = next((e for e in entries if e['k'] == key), None)
        if old is None: entries.append(dict(k=key, en=en, th=th))
        else: old.update(en=en, th=th)
    rows = [
        ('base.place.toolbox', 'toolbox in storage', 'ตู้เครื่องมือในคลัง'),
        ('base.salvage.scrap', 'Scrap metal with usable parts inside.', 'เศษเหล็กที่ยังมีอะไหล่ใช้ได้อยู่ข้างใน'),
        ('base.em.flare.hit', 'The storm caught you outside the shelter. Your radiation dose jumped.', 'พายุสุริยะมาถึงตอนคุณยังไม่อยู่ในที่หลบภัย รังสีสะสมพุ่งขึ้น'),
        ('base.em.flare.safe', 'The storm passed over the shelter.', 'พายุผ่านที่หลบภัยไปแล้ว'),
        ('base.guide.6.body', 'Look through the peephole. Talk as much as you like and use both scanners (they are free): living beings are warm and breathe. Guests help, but each one eats every day. You can ask a guest to leave.', 'ส่องตาแมว คุยได้เรื่อย ๆ และใช้เครื่องสแกนได้ทั้งสองแบบ (ไม่จำกัด) สิ่งมีชีวิตจริงจะอุ่นและหายใจ แขกช่วยงานได้ แต่ทุกคนต้องกินทุกวัน ขอให้แขกออกจากฐานได้'),
        ('base.guide.7.title', 'Repairs and emergencies', 'งานซ่อมและเหตุฉุกเฉิน'),
        ('base.guide.7.body', 'Broken things wait at their place. Fetch the part from the toolbox in storage (Tab shows your bag), carry it there and solve the panel. Careless or slow fixes cost more. Call a friend over to make it easier. Solar storm: run to the storage shelter.', 'ของที่เสียจะรออยู่ที่จุดของมัน ไปหยิบอะไหล่ที่ตู้เครื่องมือในคลัง (กด Tab ดูกระเป๋า) ถือไปแล้วซ่อมให้เสร็จ ซ่อมพลาดหรือช้าจะเสียของมากขึ้น เรียกเพื่อนมาช่วยจะง่ายขึ้น พายุสุริยะ: วิ่งไปที่หลบพายุในคลัง'),
        ('base.guide.5.body', 'Open the airlock yourself. Your suit only holds air for a short walk, and every minute outside adds radiation dose: Mars has thin air and no global magnetic field. Nobody knocks while you are outside.', 'เปิดประตูปรับแรงดันเอง อากาศในชุดพอแค่เดินไม่ไกล และทุกนาทีข้างนอกจะได้รังสีสะสมเพิ่ม เพราะดาวอังคารมีบรรยากาศบางและไม่มีสนามแม่เหล็กหุ้มทั้งดาว ระหว่างอยู่ข้างนอกจะไม่มีใครมาเคาะประตู'),
    ]
    # round 10: no guide arrows any more, a "!" marks every place with something to do
    rows += [
        ('base.hud.controls', 'WASD or click to walk · Shift run · E use · Tab bag · P pause · F speed · ! = something to do', 'WASD/คลิกเพื่อเดิน · Shift วิ่ง · E ใช้ · Tab กระเป๋า · P หยุดเวลา · F เร่งเวลา · ! = มีงานรอ'),
        ('ship.intro.2', 'A ship only works if the crew keeps it running. Morning jobs first! {0} is hungry. Look for the ! sign.', 'ยานจะอยู่ได้ถ้าลูกเรือดูแลมันทุกวัน เริ่มงานเช้ากันก่อน! {0} หิวแล้ว มองหาเครื่องหมาย !'),
        ('ship.look.other', '{0}: not now. Look for the ! sign.', '{0}: ยังไม่ใช่ตอนนี้ มองหาเครื่องหมาย !'),
        ('ship.hud.lifeboat', 'Lifeboat: {0}/{1} — carry supplies to the bay (the ! sign) and click the orange pod', 'ยานชูชีพ: {0}/{1} — ขนของไปห้องจอด (ที่มีเครื่องหมาย !) แล้วคลิกยานสีส้ม'),
    ]
    rows += [
        ('base.place.air', 'oxygen maker (MOXIE)', 'เครื่องผลิตออกซิเจน (MOXIE)'),
        ('base.place.board', 'project board', 'บอร์ดโครงการ'),
    ]
    for k, en, th in rows: put(k, en, th)
