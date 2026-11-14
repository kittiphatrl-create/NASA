"""Approved mentor story and optional companion interactions, TH/EN."""
def extend_story(entries):
    def put(key, en, th):
        old = next((e for e in entries if e['k'] == key), None)
        if old is None: entries.append(dict(k=key, en=en, th=th))
        else: old.update(en=en, th=th)
    rows = [
        ('speaker.mentor', 'Commander Arun', 'ผู้การอรุณ'),
        ('story.intro.earth', '2177. Humanity has built homes beyond Earth. Every outpost still depends on air, water and the people who care for it.', 'ปี 2177 มนุษย์สร้างบ้านไกลจากโลก แต่ทุกสถานียังต้องพึ่งอากาศ น้ำ และผู้คนที่คอยดูแล'),
        ('story.intro.jump', 'Jump corridors connect distant settlements. The training ship Kestrel carries a new crew toward the Mars frontier.', 'เส้นทางจัมป์เชื่อมถิ่นฐานห่างไกล ยานฝึก Kestrel กำลังพาลูกเรือใหม่สู่ชายแดนดาวอังคาร'),
        ('story.intro.crew', 'You are the junior astronaut. Commander Arun has one rule: learn the ship, and look after everyone aboard.', 'คุณคือนักบินอวกาศฝึกหัด ผู้การอรุณมีกฎข้อเดียว รู้จักยานของเรา แล้วดูแลทุกชีวิตบนยาน'),
        ('story.mentor.welcome', 'Morning, cadet. Start with the pet\'s breakfast. Then carry the filter to the recycler, water the plants and check the radio. Small jobs keep a crew alive.', 'อรุณสวัสดิ์นักบิน เริ่มจากอาหารเจ้าตัวเล็กก่อน แล้วนำตัวกรองไปเครื่องรีไซเคิล รดน้ำต้นไม้ และตรวจวิทยุ งานเล็ก ๆ พวกนี้ทำให้ลูกเรือรอดนะ'),
        ('story.mentor.tip', 'Follow the gold marker. Walk close and press E to work. Keep your hands free when you can, and use the ship\'s knowledge cards when you need a clue.', 'ตามเครื่องหมายสีทอง เดินเข้าใกล้แล้วกด E ทำงาน จัดของในมือให้ดี ถ้าต้องการคำใบ้ เปิดสมุดความรู้ในยานได้เลย'),
        ('story.mentor.emergency', 'The launch release is jammed. I will hold it open here. Take the supplies, the pet and ORBI. You know the route.', 'ตัวปล่อยยานติดขัด ฉันจะคุมมันไว้ตรงนี้ พาเสบียง เจ้าตัวเล็ก และ ORBI ไปด้วย เธอรู้เส้นทางแล้ว'),
        ('story.escape.release', 'ARUN: Manual release engaged. I have to stay at this console. Get everyone into the escape pod. That is an order, cadet.', 'อรุณ: เปิดตัวปล่อยแบบมือแล้ว ฉันต้องอยู่ที่แผงนี้ พาทุกคนขึ้นยานกู้ชีพ นี่คือคำสั่งนะ นักบิน'),
        ('story.escape.signal', 'ARUN: Look after them… astronaut. The signal breaks. Kestrel disappears into the debris. His fate is unknown.', 'อรุณ: ดูแลทุกคนด้วย… นักบิน สัญญาณขาดหาย Kestrel ลับเข้าไปในกลุ่มเศษซาก ยังไม่มีใครรู้ชะตากรรมของเขา'),
        ('story.dock', 'ORBI locates an old Mars emergency habitat. Your one-room escape pod docks with its access tube. A different home. A chance to call for help.', 'ORBI พบฐานฉุกเฉินเก่าบนดาวอังคาร ยานกู้ชีพหนึ่งห้องเชื่อมกับท่อทางเข้าฐาน บ้านหลังใหม่ และโอกาสส่งสัญญาณขอความช่วยเหลือ'),
        ('ship.rest', 'ARUN: Good work. Take a short rest on the sofa. I will keep watch from the cockpit.', 'อรุณ: ทำได้ดี พักบนโซฟาสักนิดนะ ฉันจะเฝ้าระวังจากห้องนักบิน'),
        ('life.night.GuestDeparted', '{0} thanks the crew and leaves after the storm. Their short stay is over.', '{0} ขอบคุณทุกคนแล้วออกเดินทางหลังพายุ การพักชั่วคราวสิ้นสุดลงแล้ว'),
    ]
    for row in rows: put(*row)
    additions = {
        'life.card.visitor_mu.body': ('Mu rolls up in a portable water vessel. Bubbles carry a soft voice: "May I shelter here for one night? I can share water before I leave."', 'มิวเคลื่อนมาพร้อมถังน้ำพกพา เสียงนุ่มดังผ่านฟองอากาศ ขอหลบพายุสักคืนได้ไหม ก่อนจากไปฉันจะแบ่งน้ำให้'),
        'life.card.visitor_tik.body': ('Tik, a springy mint-coloured slime, balances a parcel on its head and tries to salute. A tiny courier badge wobbles inside the gel.', 'ทิก สไลม์สีมินต์เด้งดึ๋ง ทรงห่อพัสดุบนหัวแล้วพยายามทำวันทยหัตถ์ บัตรผู้ส่งของจิ๋วแกว่งอยู่ในตัวใส ๆ'),
        'life.card.visitor_zee.body': ('Zee is an adult human traveller in a pink expedition coat, with a star-shaped hair clip and far too many snack pouches. "Terrible weather for a picnic, right?"', 'ซีเป็นนักเดินทางมนุษย์วัยผู้ใหญ่ สวมเสื้อสำรวจสีชมพู ติดกิ๊บรูปดาวและห้อยถุงขนมเต็มตัว อากาศไม่เหมาะกับปิกนิกเลยเนอะ'),
        'life.card.visitor_nok.body': ('Nok, a patched service robot, projects a crooked smile. "Hospitality protocol: compliment the door. Your door is excellent." A spare component rattles in its chest.', 'นก หุ่นบริการปะซ่อม ฉายรอยยิ้มเอียง ๆ ระเบียบการทักทาย ชมประตู ประตูคุณยอดเยี่ยมมาก อะไหล่ชิ้นหนึ่งกระทบกันในอก'),
    }
    for key, texts in additions.items(): put(key, *texts)
    for row in [
        ('life.scan.mu.thermal', 'The water vessel holds a warm, stable core. The outer glass is cooler.', 'ถังน้ำรักษาอุณหภูมิแกนกลางคงที่ กระจกด้านนอกเย็นกว่า'),
        ('life.scan.mu.breath', 'Regular gill movement and bubbles through the water filter.', 'เหงือกขยับสม่ำเสมอ มีฟองอากาศผ่านตัวกรองน้ำ'),
        ('life.scan.nok.thermal', 'Heat around a damaged joint; a living repair core stays warm inside the chassis.', 'ความร้อนสูงรอบข้อต่อที่เสียหาย แกนซ่อมแซมมีชีวิตภายในโครงยังอุ่นอยู่'),
        ('life.scan.nok.breath', 'Gas exchange from the biological repair core. This android still needs breathable air.', 'แกนซ่อมแซมชีวภาพแลกเปลี่ยนก๊าซ แอนดรอยด์ตัวนี้ยังต้องใช้อากาศหายใจ'),
        ('life.scan.nok.heartbeat', 'A regular coolant-pump pulse, not a human heartbeat. System rhythm alone cannot prove intent.', 'จังหวะปั๊มหล่อเย็นสม่ำเสมอ ไม่ใช่หัวใจมนุษย์ จังหวะระบบเพียงอย่างเดียวบอกเจตนาไม่ได้'),
        ('life.card.visitor_nok.clue1', 'A joint is scorched. Beneath the patched panels, a small biological repair core glows.', 'ข้อต่อมีรอยไหม้ ใต้แผ่นปะซ่อมมีแกนซ่อมแซมชีวภาพเรืองแสงเล็ก ๆ'),
        ('life.card.visitor_nok.clue2', 'No external bags; its component compartment is built into the chassis.', 'ไม่มีสัมภาระภายนอก ช่องเก็บอะไหล่อยู่ในโครงตัว'),
        ('life.card.visitor_nok.admit.result', 'Nok rolls inside. Its living repair core needs air, water and nutrients while it recovers.', 'นกเคลื่อนเข้ามา แกนซ่อมแซมมีชีวิตต้องใช้อากาศ น้ำ และสารอาหารระหว่างฟื้นตัว'),
        ('life.card.visitor_nok.treat', 'Dress the exposed repair core with the first aid kit', 'ใช้ชุดปฐมพยาบาลปิดแผลแกนชีวภาพ'),
        ('life.card.visitor_nok.treat.result', 'The sterile dressing protects the living core. Nok salutes with its good arm and heads home.', 'ผ้าปิดแผลปลอดเชื้อป้องกันแกนมีชีวิต นกทำวันทยหัตถ์ด้วยแขนข้างดีแล้วกลับบ้าน'),
        ('life.card.visitor_nok.water.result', 'Nok refills its cooling loop, bows twice, and rolls toward the hills.', 'นกเติมน้ำเข้าวงจรหล่อเย็น โค้งขอบคุณสองครั้งแล้วเคลื่อนไปทางเนินเขา'),
        ('life.card.visitor_nok.hint.careful', 'Damaged panels and a repair core. Check the evidence and choose help we can afford.', 'แผ่นโครงเสียหายกับแกนซ่อมแซม ตรวจหลักฐานแล้วเลือกช่วยเท่าที่เสบียงเราพอ'),
        ('life.scan.pip.thermal', 'A warm core under a cool gel membrane. Different layers hold heat differently.', 'แกนกลางอุ่นใต้เยื่อเจลเย็น แต่ละชั้นเก็บความร้อนต่างกัน'),
        ('life.scan.pip.breath', 'Slow, regular gas exchange through the gel membrane.', 'แลกเปลี่ยนก๊าซช้าและสม่ำเสมอผ่านเยื่อเจล'),
    ]: put(*row)
    for entry in entries:
        if entry['k'] == 'life.end.Rescued.body':
            entry['en'] += '\nA broken transmission carries Arun\'s call sign. ORBI saves the coordinates. Your search is not over.'
            entry['th'] += '\nสัญญาณขาด ๆ มีรหัสเรียกของอรุณ ORBI บันทึกพิกัดไว้ การตามหาของคุณยังไม่จบ'
