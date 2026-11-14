// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do guests feel like people when they have many lines, remember how you treat them, and can be asked to help or to leave?
// Date: 2026-11-13
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Conversation banks. Guests in the base: a greeting that depends on friendship, stories about themselves,
    /// a Mars fact from their own job, and replies for asking help or asking them to leave. Visitors at the
    /// peephole: extra answers after the clue questions (the monsters answer a little wrong).
    /// Lines are (English, Thai). Characters may be redrawn later; the lines describe personality, not looks.
    /// </summary>
    public partial class GameFlow
    {
        sealed class TalkBank
        {
            public (string en, string th)[] hello;   // upset, ok, fond
            public (string en, string th)[] story, fact;
            public (string en, string th) help, leave, thanks;
        }

        static (string, string) P(string en, string th) => (en, th);

        static readonly Dictionary<string, TalkBank> Talks = new Dictionary<string, TalkBank>
        {
            ["tik"] = new TalkBank
            {
                hello = new[] { P("...Hi. I'm still a bit hungry, you know.", "...หวัดดี ยังหิวอยู่นิดนึงนะ รู้ไหม"), P("Hiii! Need anything delivered? Tik delivers!", "หวัดดีจ้า! มีอะไรให้ส่งไหม ติ๊กส่งให้!"), P("My favourite base-mate! I polished the door handle for you. Twice!", "เพื่อนร่วมฐานคนโปรด! ฉันขัดลูกบิดประตูให้แล้ว สองรอบด้วย!") },
                story = new[]
                {
                    P("I carry parcels between domes. Once I bounced all the way down a crater. The parcel was fine. Mostly.", "ฉันส่งพัสดุระหว่างโดม เคยกลิ้งตกหลุมอุกกาบาตทั้งตัว พัสดุไม่เป็นไร...ส่วนใหญ่นะ"),
                    P("Slimes like me soak up water and slowly give it back. That's why I bring you a little every day.", "สไลม์อย่างฉันดูดน้ำไว้แล้วค่อย ๆ ปล่อยออก ฉันเลยเอาน้ำมาให้ทุกวันไง"),
                    P("My mum says I'm clumsy. I say I'm 'fast in many directions'.", "แม่บอกว่าฉันซุ่มซ่าม ฉันบอกว่าฉัน 'ไวหลายทิศทาง' ต่างหาก"),
                },
                fact = new[]
                {
                    P("A day on Mars is called a sol. It's 24 hours and 39 minutes. Extra time for naps!", "วันหนึ่งบนดาวอังคารเรียกว่า 'โซล' ยาว 24 ชั่วโมง 39 นาที ได้นอนกลางวันเพิ่ม!"),
                    P("Mars gravity is about a third of Earth's. That's why I bounce so well.", "แรงโน้มถ่วงดาวอังคารประมาณหนึ่งในสามของโลก ฉันเลยเด้งเก่งไง"),
                },
                help = P("On my way! Try not to step on me.", "ไปเดี๋ยวนี้! ระวังอย่าเหยียบฉันนะ"),
                leave = P("Oh... okay. I'll deliver myself somewhere else. Bye-bye.", "อ้อ... โอเค งั้นฉันส่งตัวเองไปที่อื่นละกัน บ๊ายบาย"),
                thanks = P("Thanks for keeping me! I'll be the bounciest helper ever.", "ขอบใจที่ให้อยู่นะ! ฉันจะเป็นผู้ช่วยที่เด้งที่สุดเลย"),
            },
            ["mu"] = new TalkBank
            {
                hello = new[] { P("Mu... keeps to its tank today.", "มู...ขออยู่ในถังเงียบ ๆ วันนี้"), P("Blub. Hello. The base is warm.", "บลุบ สวัสดี ฐานนี้อุ่นดี"), P("Blub blub! Mu is happy you are here.", "บลุบ บลุบ! มูดีใจที่มีเธออยู่") },
                story = new[]
                {
                    P("Mu drifts from ice lake to ice lake. Under the ice the water stays liquid. Mu sleeps there.", "มูล่องจากทะเลสาบน้ำแข็งหนึ่งไปอีกที่หนึ่ง ใต้น้ำแข็งน้ำยังเป็นของเหลว มูนอนที่นั่น"),
                    P("Mu is shy. Mu sometimes takes things when scared. Mu is trying to stop.", "มูขี้อาย บางทีตอนกลัว มูก็หยิบของไป มูกำลังพยายามเลิกอยู่"),
                    P("Mu's tank is old. Mu keeps promises better than tanks keep water.", "ถังของมูเก่าแล้ว แต่มูรักษาสัญญาเก่งกว่าถังเก็บน้ำนะ"),
                },
                fact = new[]
                {
                    P("There is ice under the Mars ground, and at the poles. Orbiters saw it with radar.", "ใต้ผิวดาวอังคารและที่ขั้วดาวมีน้ำแข็ง ยานโคจรใช้เรดาร์มองเห็น"),
                    P("Water boils very fast in thin Mars air. Mu stays in the tank.", "ในอากาศบาง ๆ ของดาวอังคาร น้ำเดือดเร็วมาก มูเลยต้องอยู่ในถัง"),
                },
                help = P("Mu comes. Mu holds the bolts.", "มูมาแล้ว มูถือน็อตให้"),
                leave = P("Mu understands. Mu will float on.", "มูเข้าใจ มูจะล่องต่อไป"),
                thanks = P("Blub. Mu will remember the kindness.", "บลุบ มูจะจำความใจดีนี้ไว้"),
            },
            ["nok"] = new TalkBank
            {
                hello = new[] { P("Systems... low. Food... would help.", "ระบบ...ต่ำ อาหาร...ช่วยได้"), P("Good day. Nok is ready to serve.", "สวัสดีครับ น็อกพร้อมให้บริการ"), P("Friend detected. Nok's happiness module is at maximum.", "ตรวจพบเพื่อน โมดูลความสุขของน็อกเต็มร้อยครับ") },
                story = new[]
                {
                    P("Nok was built to clean solar panels. Then Nok learned to repair. Then Nok learned to worry.", "น็อกถูกสร้างมาเช็ดแผงโซลาร์ แล้วก็เรียนซ่อม แล้วก็เรียนรู้ที่จะเป็นห่วง"),
                    P("Nok's core is alive, like moss. It needs a little food. Strange for a robot, yes.", "แกนของน็อกมีชีวิตเหมือนตะไคร่ ต้องกินนิดหน่อย แปลกสำหรับหุ่นยนต์ใช่ไหมครับ"),
                    P("Nok keeps a spare dish part. Nok kept it for someone kind.", "น็อกเก็บชิ้นส่วนจานสัญญาณสำรองไว้ เก็บไว้ให้คนที่ใจดีครับ"),
                },
                fact = new[]
                {
                    P("Dust storms on Mars can cover the whole planet and dim the Sun for weeks. Solar panels lose power.", "พายุฝุ่นบนดาวอังคารคลุมได้ทั้งดาว บังแสงอาทิตย์เป็นสัปดาห์ แผงโซลาร์จะได้ไฟน้อยลง"),
                    P("The Opportunity rover stopped in 2018 when a dust storm covered its panels.", "ยาน Opportunity หยุดทำงานในปี 2018 เพราะพายุฝุ่นคลุมแผงโซลาร์"),
                },
                help = P("Nok is moving to the job. Estimated arrival: soon.", "น็อกกำลังไปที่งาน เวลาถึงโดยประมาณ: อีกไม่นานครับ"),
                leave = P("Understood. Nok will find another base. Thank you for the time.", "รับทราบครับ น็อกจะหาฐานอื่น ขอบคุณสำหรับเวลาที่ผ่านมา"),
                thanks = P("Gratitude logged. Saved forever.", "บันทึกความขอบคุณแล้ว เก็บไว้ตลอดไปครับ"),
            },
            ["ree"] = new TalkBank
            {
                hello = new[] { P("The plants and I are both thirsty today.", "วันนี้ทั้งฉันและต้นไม้หิวน้ำ"), P("Morning. The leaves look brave today.", "อรุณสวัสดิ์ ใบไม้วันนี้ดูใจสู้ดีนะ"), P("You water people like you water plants. Gently. Thank you.", "เธอดูแลคนเหมือนรดน้ำต้นไม้ อ่อนโยนดี ขอบใจนะ") },
                story = new[]
                {
                    P("I grow moss in old cans on the cliffs. Moss is patient, like me.", "ฉันปลูกตะไคร่ในกระป๋องเก่าบนหน้าผา ตะไคร่อดทน เหมือนฉัน"),
                    P("My family traded seeds for three generations. A good seed is worth more than gold.", "ครอบครัวฉันแลกเมล็ดพันธุ์กันมาสามรุ่น เมล็ดดี ๆ มีค่ามากกว่าทอง"),
                    P("When the wind is quiet, I talk to the greenhouse. It listens better than people.", "ตอนลมสงบ ฉันคุยกับเรือนกระจก มันฟังเก่งกว่าคนอีก"),
                },
                fact = new[]
                {
                    P("NASA's Veggie garden on the space station grew lettuce, and astronauts ate it in 2015.", "สวน Veggie ของ NASA บนสถานีอวกาศปลูกผักกาดได้ และนักบินอวกาศได้กินในปี 2015"),
                    P("Mars soil has salts called perchlorates. You must wash it before plants can grow well.", "ดินดาวอังคารมีเกลือเปอร์คลอเรต ต้องล้างก่อนถึงจะปลูกพืชได้ดี"),
                },
                help = P("I'm coming. Hands are for helping.", "กำลังไป มือมีไว้ช่วยกัน"),
                leave = P("Then I'll go. Plant something for me.", "งั้นฉันไปนะ ปลูกอะไรสักต้นไว้ให้ฉันด้วย"),
                thanks = P("That warms me more than sunlight.", "อุ่นใจกว่าแดดอีก"),
            },
            ["aya"] = new TalkBank
            {
                hello = new[] { P("I'm... fine. Just tired and hungry.", "ฉัน...ไม่เป็นไร แค่เหนื่อยกับหิว"), P("Hi! Want to see how the seedlings are doing?", "หวัดดี! อยากดูต้นกล้าไหม?"), P("Partner! The greenhouse misses you. So do I.", "คู่หู! เรือนกระจกคิดถึงเธอ ฉันก็ด้วย") },
                story = new[]
                {
                    P("My expedition's rover broke down in a sandstorm. I walked here with my seed box.", "รถสำรวจของคณะฉันพังกลางพายุทราย ฉันเลยเดินมาที่นี่พร้อมกล่องเมล็ดพันธุ์"),
                    P("When I was small I grew beans in a cup at school. Now I grow them on another planet.", "ตอนเด็ก ๆ ฉันปลูกถั่วในแก้วที่โรงเรียน ตอนนี้ปลูกบนอีกดาวแล้ว"),
                    P("Plants make oxygen and eat our carbon dioxide. They are tiny life-support machines.", "พืชผลิตออกซิเจนและกินคาร์บอนไดออกไซด์ของเรา มันคือเครื่องช่วยชีวิตตัวจิ๋ว"),
                },
                fact = new[]
                {
                    P("Red and blue LED light is enough for plants. That's why grow lamps look pink.", "แสง LED สีแดงกับน้ำเงินพอสำหรับพืช ไฟปลูกพืชเลยดูเป็นสีชมพู"),
                    P("NASA's MOXIE on Perseverance made oxygen from Mars air, a little at a time.", "เครื่อง MOXIE บนยาน Perseverance ของ NASA ผลิตออกซิเจนจากอากาศดาวอังคารได้ทีละนิด"),
                },
                help = P("Coming! Gloves on.", "กำลังไป! ใส่ถุงมือแล้ว"),
                leave = P("I see. I'll take my seeds and try the next base.", "เข้าใจแล้ว ฉันจะเอาเมล็ดพันธุ์ไปลองฐานถัดไป"),
                thanks = P("We make a good team, you know.", "เราเป็นทีมที่ดีนะ รู้ไหม"),
            },
            ["jules"] = new TalkBank
            {
                hello = new[] { P("Can't fix much on an empty stomach.", "ท้องว่างแบบนี้ซ่อมอะไรไม่ค่อยไหว"), P("Hey. Anything rattling? I'll fix it.", "เฮ้ มีอะไรสั่นแปลก ๆ ไหม เดี๋ยวซ่อมให้"), P("You're the best crewmate I've had. And I've had robots.", "เธอเป็นเพื่อนร่วมทีมที่ดีที่สุดที่เคยมี ขนาดเคยทำงานกับหุ่นยนต์มาแล้วนะ") },
                story = new[]
                {
                    P("I keep the dome pumps running in the south colony. Pumps are like people: listen and they tell you what's wrong.", "ฉันดูแลปั๊มของโดมฝั่งใต้ ปั๊มก็เหมือนคน ฟังดี ๆ มันจะบอกว่าเป็นอะไร"),
                    P("First rule of fixing things far from Earth: carry spares. Second rule: carry more spares.", "กฎข้อแรกของการซ่อมของไกลโลก: พกอะไหล่ ข้อสอง: พกอะไหล่เพิ่มอีก"),
                    P("I lost a bet once and had to clean a recycler with a toothbrush. Never again.", "เคยแพ้พนันต้องล้างเครื่องรีไซเคิลด้วยแปรงสีฟัน ไม่เอาอีกแล้ว"),
                },
                fact = new[]
                {
                    P("Astronauts do 'spacewalks' in suits that are like tiny spaceships, with their own air and cooling.", "ชุดอวกาศเหมือนยานอวกาศจิ๋ว มีอากาศและระบบระบายความร้อนในตัว"),
                    P("3D printers on the space station can print spare tools, so you don't wait months for a delivery.", "บนสถานีอวกาศมีเครื่องพิมพ์ 3 มิติพิมพ์เครื่องมือสำรองได้ ไม่ต้องรอส่งของหลายเดือน"),
                },
                help = P("On it. Hand me that wrench when I ask.", "จัดให้ เดี๋ยวขอประแจแล้วส่งให้ด้วยนะ"),
                leave = P("Fair. Supplies are supplies. Good luck out here.", "เข้าใจ เสบียงก็คือเสบียง โชคดีนะ"),
                thanks = P("Don't get soppy. ...Thanks, though.", "อย่าซึ้งนักสิ ...แต่ก็ขอบใจนะ"),
            },
            ["pip"] = new TalkBank
            {
                hello = new[] { P("Pip sad. Pip tummy empty.", "ปิ๊ปเศร้า ท้องปิ๊ปว่าง"), P("Pip copy you! *waves the same way*", "ปิ๊ปทำตาม! *โบกมือแบบเดียวกัน*"), P("Pip love! Pip love base! Pip love you!", "ปิ๊ปรัก! รักฐาน! รักเธอ!") },
                story = new[]
                {
                    P("Pip was a drop. Then Pip was a puddle. Now Pip is Pip!", "ปิ๊ปเคยเป็นหยดน้ำ แล้วเป็นแอ่ง ตอนนี้เป็นปิ๊ปแล้ว!"),
                    P("Pip learns by copying. You walk, Pip walks. You fix, Pip fixes!", "ปิ๊ปเรียนรู้จากการทำตาม เธอเดิน ปิ๊ปเดิน เธอซ่อม ปิ๊ปซ่อม!"),
                    P("Pip has a rock friend. Rock is quiet. Pip talks for both.", "ปิ๊ปมีเพื่อนเป็นก้อนหิน หินเงียบ ปิ๊ปเลยพูดแทนทั้งคู่"),
                },
                fact = new[]
                {
                    P("Mars sky red in day... and BLUE at sunset! Pip saw a photo!", "ฟ้าดาวอังคารสีแดงตอนกลางวัน...แต่ตอนตะวันตกเป็นสีฟ้า! ปิ๊ปเห็นในรูป!"),
                    P("Mars has two tiny moons! Phobos and Deimos! Pip waves at them.", "ดาวอังคารมีดวงจันทร์จิ๋วสองดวง! โฟบอสกับไดมอส! ปิ๊ปโบกมือให้"),
                },
                help = P("Pip help! Pip copy!", "ปิ๊ปช่วย! ปิ๊ปทำตาม!"),
                leave = P("Pip go... Pip copy leaving. Bye.", "ปิ๊ปไป...ปิ๊ปทำตามการจากลา บาย"),
                thanks = P("Pip says thank you too! Thank you!", "ปิ๊ปก็ขอบคุณ! ขอบคุณ!"),
            },
            ["kira"] = new TalkBank
            {
                hello = new[] { P("Ehehe... my stomach is growling louder than the storm.", "เหะ ๆ ...ท้องร้องดังกว่าพายุอีก"), P("Heya! Want to see my plant diary? It has stickers!", "หวัดดี! อยากดูไดอารี่ต้นไม้ของฉันไหม มีสติกเกอร์ด้วยนะ!"), P("Best friend on Mars! I made you a dust-flower. It's dust. But it's a flower!", "เพื่อนรักบนดาวอังคาร! ฉันทำดอกไม้ฝุ่นให้ มันเป็นฝุ่น...แต่ก็เป็นดอกไม้นะ!") },
                story = new[]
                {
                    P("I'm a cadet too! I got lost on a field trip. Ehehe. Please don't tell my teacher.", "ฉันก็เป็นนักเรียนนักบินเหมือนกัน! หลงตอนทัศนศึกษา เหะ ๆ อย่าบอกครูนะ"),
                    P("I want to be the first gardener on the Moon AND on Mars. Why choose?", "อยากเป็นคนสวนคนแรกบนดวงจันทร์และดาวอังคาร ทำไมต้องเลือกล่ะ"),
                    P("My big sister flies cargo ships. She says space is mostly waiting. I hate waiting!", "พี่สาวฉันขับยานขนส่ง บอกว่าอวกาศส่วนใหญ่คือการรอ ฉันเกลียดการรอ!"),
                },
                fact = new[]
                {
                    P("Did you know astronauts exercise about two hours a day? Weak gravity makes muscles and bones weaker.", "รู้ไหม นักบินอวกาศออกกำลังกายวันละประมาณสองชั่วโมง เพราะแรงโน้มถ่วงต่ำทำให้กล้ามเนื้อและกระดูกอ่อนแอ"),
                    P("Plants can grow toward light even in low gravity. They know which way is 'up' by the lamp!", "พืชยังเอนเข้าหาแสงได้ในที่แรงโน้มถ่วงต่ำ มันรู้ว่าทางไหนคือ 'ข้างบน' จากหลอดไฟ!"),
                },
                help = P("Kira to the rescue!", "คิระมาช่วยแล้ว!"),
                leave = P("Oh... okay. I'll be brave. Bye-bye!", "อ้อ...ก็ได้ ฉันจะกล้าหาญ บ๊ายบาย!"),
                thanks = P("Eee! You're the best!", "กรี๊ด! เธอดีที่สุดเลย!"),
            },
            ["rex"] = new TalkBank
            {
                hello = new[] { P("Not in the mood. A meal would change that.", "ไม่ค่อยมีอารมณ์ ข้าวสักมื้อคงช่วยได้"), P("Yo. Your battery bank hums off-key, by the way.", "โย่ แบตเตอรี่ฐานเธอเสียงเพี้ยนนะ บอกไว้ก่อน"), P("You're alright. I don't say that to many people.", "เธอโอเคเลย ฉันไม่ค่อยพูดแบบนี้กับใครหรอก") },
                story = new[]
                {
                    P("Grew up fixing rovers in a junkyard dome. Everything there was broken. I liked that.", "โตมาซ่อมรถสำรวจในโดมเศษเหล็ก ทุกอย่างที่นั่นพังหมด ฉันชอบนะ"),
                    P("The shades? Mars sun is weak, but dust glare isn't. Also they look cool.", "แว่นดำน่ะเหรอ แดดดาวอังคารอ่อน แต่ฝุ่นสะท้อนแสงแยงตา แล้วมันก็ดูเท่ด้วย"),
                    P("I don't trust easy. You fed me, though. That counts.", "ฉันไม่ไว้ใจใครง่าย ๆ แต่เธอให้ข้าวฉัน มันมีความหมายนะ"),
                },
                fact = new[]
                {
                    P("Nights on Mars can drop below minus 70 °C. Heaters are life support too, not luxury.", "กลางคืนบนดาวอังคารหนาวถึงลบ 70 องศา เครื่องทำความร้อนก็คือระบบช่วยชีวิต ไม่ใช่ของฟุ่มเฟือย"),
                    P("Curiosity runs on a nuclear battery called an RTG. No sun needed. Wish we had one.", "ยาน Curiosity ใช้แบตเตอรี่นิวเคลียร์ RTG ไม่ต้องใช้แดด อยากได้สักอันจัง"),
                },
                help = P("Fine. Show me what's broken.", "ได้ ไหนดูซิว่าอะไรพัง"),
                leave = P("Figured. No hard feelings. Watch your fuses.", "นึกแล้ว ไม่ได้โกรธนะ ระวังฟิวส์ด้วยล่ะ"),
                thanks = P("...Yeah. Me too.", "...อืม ฉันก็เหมือนกัน"),
            },
            ["bizz"] = new TalkBank
            {
                hello = new[] { P("Bzz... wings droopy... need snack...", "บซซ...ปีกตก...ขอของว่าง..."), P("Bzzz! Any flowers today? Even tiny ones?", "บซซซ! วันนี้มีดอกไม้ไหม ดอกจิ๋ว ๆ ก็ได้!"), P("BZZZ! Best hive-mate! Bizz does a happy dance!", "บซซซ! เพื่อนร่วมรังที่ดีที่สุด! บิซซ์เต้นดีใจ!") },
                story = new[]
                {
                    P("Bizz's guild pollinates greenhouses across Mars. No bees, no beans!", "สมาคมของบิซซ์ผสมเกสรให้เรือนกระจกทั่วดาวอังคาร ไม่มีผึ้ง ไม่มีถั่ว!"),
                    P("Bizz dances to tell friends where flowers are. Like a map, but wiggly.", "บิซซ์เต้นเพื่อบอกเพื่อนว่าดอกไม้อยู่ไหน เหมือนแผนที่ แต่ดุ๊กดิ๊ก"),
                    P("Bizz is scared of dust storms. Bizz hides in a teapot.", "บิซซ์กลัวพายุฝุ่น เลยไปหลบในกาน้ำชา"),
                },
                fact = new[]
                {
                    P("Many Earth plants need pollinators to make fruit. In greenhouses, people sometimes do it with a brush!", "พืชบนโลกหลายชนิดต้องมีผู้ผสมเกสรถึงจะออกผล ในเรือนกระจกบางทีคนก็ใช้พู่กันทำแทน!"),
                    P("Honey never spoils. It would make a great space food. Bzz!", "น้ำผึ้งไม่เน่าเสีย เหมาะเป็นอาหารอวกาศมาก บซซ!"),
                },
                help = P("Bzz! Bizz flies there!", "บซซ! บิซซ์บินไปเดี๋ยวนี้!"),
                leave = P("Bzz... okay. Bizz finds a new flower.", "บซซ...ก็ได้ บิซซ์ไปหาดอกไม้ใหม่"),
                thanks = P("Bzzz-thank you!", "บซซซ-ขอบคุณ!"),
            },
        };

        static readonly TalkBank DefaultTalk = new TalkBank
        {
            hello = new[] { P("I could use a meal.", "อยากได้ข้าวสักมื้อ"), P("Hello. It's good to have company.", "สวัสดี มีเพื่อนอยู่ด้วยก็ดีนะ"), P("I'm glad I knocked on your door.", "ดีใจที่มาเคาะประตูฐานนี้") },
            story = new[] { P("I've walked a long way across the red sand.", "ฉันเดินข้ามทรายแดงมาไกลมาก") },
            fact = new[] { P("Mars is about half the size of Earth.", "ดาวอังคารมีขนาดประมาณครึ่งหนึ่งของโลก") },
            help = P("I'll help.", "ฉันจะช่วย"), leave = P("All right. Goodbye.", "ก็ได้ ลาก่อน"), thanks = P("Thank you too.", "ขอบคุณเหมือนกัน"),
        };

        /// <summary>Extra peephole answers after the clue questions: (question, answer). Monsters answer a little wrong.</summary>
        static readonly Dictionary<string, (string en, string th)[]> PeepChat = new Dictionary<string, (string, string)[]>
        {
            ["tik"] = new[] { P("If I let you in, what will you do?", "ถ้าให้เข้ามา จะช่วยอะไรได้บ้าง?"), P("Deliver stuff! Carry stuff! Bring you water every day!", "ส่งของ! ขนของ! เอาน้ำมาให้ทุกวัน!"), P("What is it like out there?", "ข้างนอกเป็นยังไงบ้าง?"), P("Windy and dusty. I got sand in places slimes shouldn't have places.", "ลมแรง ฝุ่นเยอะ ทรายเข้าไปในที่ที่สไลม์ไม่ควรมีด้วยซ้ำ") },
            ["mu"] = new[] { P("If I let you in, what will you do?", "ถ้าให้เข้ามา จะช่วยอะไรได้บ้าง?"), P("Mu... stays quiet. Mu is good at quiet.", "มู...จะอยู่เงียบ ๆ มูเก่งเรื่องเงียบ"), P("Why do you look away?", "ทำไมหลบตาล่ะ?"), P("Mu is shy. And... Mu is very hungry. Very.", "มูขี้อาย แล้วก็...มูหิวมาก มากจริง ๆ") },
            ["zee"] = new[] { P("What do you sell?", "ขายอะไรบ้าง?"), P("Air, food, batteries. Fair price! Fair-ish.", "อากาศ อาหาร แบตเตอรี่ ราคายุติธรรม! เกือบ ๆ ยุติธรรม"), P("Why should I trust you?", "ทำไมต้องเชื่อใจคุณ?"), P("Because I want you alive to buy more next week!", "เพราะฉันอยากให้เธอรอดไปซื้อของฉันอีกอาทิตย์หน้าไง!") },
            ["nok"] = new[] { P("If I let you in, what will you do?", "ถ้าให้เข้ามา จะช่วยอะไรได้บ้าง?"), P("Nok can repair, clean and keep watch at night.", "น็อกซ่อม ทำความสะอาด และเฝ้ายามกลางคืนได้ครับ"), P("Are you broken?", "คุณเสียอยู่หรือเปล่า?"), P("A little. Leg servo squeaks. Nok is embarrassed.", "นิดหน่อยครับ มอเตอร์ขามีเสียงเอี๊ยด น็อกอายมาก") },
            ["shade"] = new[] { P("Why do you hide your face?", "ทำไมต้องปิดหน้า?"), P("The cold. Only the cold. Open the door.", "เพราะหนาว แค่หนาว เปิดประตูเถอะ"), P("Where are you from?", "มาจากไหน?"), P("From the place behind the hill. Everyone is from there. Open the door.", "จากที่หลังเนินนั่น ทุกคนก็มาจากที่นั่น เปิดประตูเถอะ") },
            ["ree"] = new[] { P("What do you carry?", "มีอะไรติดตัวมาบ้าง?"), P("Seeds, moss and patience.", "เมล็ดพันธุ์ ตะไคร่ และความอดทน"), P("What will you do here?", "มาที่นี่จะทำอะไร?"), P("Help your greenhouse grow. Plants like calm people.", "ช่วยให้เรือนกระจกของเธอโต พืชชอบคนใจเย็น") },
            ["mimic"] = new[] { P("Tik, is that you?", "ติ๊ก ใช่เธอหรือเปล่า?"), P("Yes. Tik. Tik is Tik. Deliver... deliver you.", "ใช่ ติ๊ก ติ๊กคือติ๊ก ส่ง...ส่งเธอ"), P("What did I give you last time?", "ครั้งก่อนฉันให้อะไรเธอ?"), P("Food. Water. Everything. Everything. Open.", "อาหาร น้ำ ทุกอย่าง ทุกอย่าง เปิดสิ") },
            ["aya"] = new[] { P("What happened to your team?", "ทีมของคุณเป็นอะไรไป?"), P("Our rover broke in a storm. I walked for two days.", "รถสำรวจพังกลางพายุ ฉันเดินมาสองวัน"), P("What can you do here?", "มาอยู่ที่นี่ทำอะไรได้บ้าง?"), P("Make your plants grow twice as fast. Promise.", "ทำให้ผักของเธอโตเร็วขึ้นสองเท่า สัญญา") },
            ["jules"] = new[] { P("What can you fix?", "ซ่อมอะไรได้บ้าง?"), P("Pumps, fuses, leaks. Most things that hiss or spark.", "ปั๊ม ฟิวส์ รอยรั่ว เกือบทุกอย่างที่มีเสียงฟู่หรือมีประกายไฟ"), P("Why are you out alone?", "ทำไมออกมาคนเดียว?"), P("Looking for a part. Found you instead.", "ออกมาหาอะไหล่ แต่ดันเจอเธอแทน") },
            ["pip"] = new[] { P("Who are you?", "เธอเป็นใคร?"), P("Pip! Who are you? *copies your head tilt*", "ปิ๊ป! แล้วเธอเป็นใคร? *เอียงหัวตาม*"), P("Are you alone?", "มาคนเดียวเหรอ?"), P("Pip and rock friend. Rock is shy.", "ปิ๊ปกับเพื่อนก้อนหิน หินขี้อาย") },
            ["kira"] = new[] { P("Are you lost?", "หลงทางเหรอ?"), P("Maaaybe. Ehehe. The map was upside down!", "อาจจะนะ เหะ ๆ แผนที่กลับหัวน่ะ!"), P("What can you do here?", "มาอยู่แล้วทำอะไรได้บ้าง?"), P("Grow snacks! I have seeds in every pocket.", "ปลูกขนม! ฉันมีเมล็ดพันธุ์ทุกกระเป๋าเลย") },
            ["rex"] = new[] { P("What do you want?", "ต้องการอะไร?"), P("A roof for a few nights. I'll keep your power running.", "ที่ซุกหัวนอนสักสองสามคืน แลกกับดูแลระบบไฟให้"), P("Can I trust you?", "ไว้ใจคุณได้ไหม?"), P("Scan me if you want. I'd do the same.", "สแกนดูก็ได้ ถ้าเป็นฉันก็ทำแบบนั้น") },
            ["vela"] = new[] { P("What are you selling?", "ขายอะไร?"), P("Only good things, darling. Air, water, power.", "มีแต่ของดีจ้ะ อากาศ น้ำ ไฟฟ้า"), P("Why are you so kind?", "ทำไมใจดีจัง?"), P("Kindness is good business. And you look like you need it.", "ความใจดีคือธุรกิจที่ดีจ้ะ แล้วเธอก็ดูเหมือนต้องการมัน") },
            ["bizz"] = new[] { P("What are you?", "เธอคืออะไร?"), P("A pollinator! Bzz! Bizz makes flowers into food!", "ผู้ผสมเกสร! บซซ! บิซซ์เปลี่ยนดอกไม้เป็นอาหาร!"), P("What do you need?", "ต้องการอะไร?"), P("Somewhere warm. And maybe a little honey-water.", "ที่อุ่น ๆ แล้วก็น้ำผึ้งผสมน้ำนิดหน่อย") },
        };

        TalkBank BankOf(string id) => Talks.TryGetValue(id, out var b) ? b : DefaultTalk;
        string TL((string en, string th) l) => TT(l.en, l.th);
        readonly Dictionary<string, int> _talkStory = new Dictionary<string, int>(), _talkFact = new Dictionary<string, int>();

        /// <summary>Talking to a guest in the base: a greeting that depends on how they feel about you, then topics.</summary>
        void CrewTalk(BSpot spot)
        {
            if (!int.TryParse(spot.id.Substring(5), out int index) || index >= _life.State.guests.Count) return;
            string id = _life.State.guests[index];
            _crewWaveUntil = Time.time + 1.8f;
            if (_bDone.Add("talk_" + id)) _life.State.morale = Mathf.Min(100, _life.State.morale + 4);
            CrewTalkMenu(id, spot, true);
        }

        void CrewTalkMenu(string id, BSpot spot, bool greet)
        {
            var bank = BankOf(id);
            int f = Friend(id);
            var hello = bank.hello[f <= -1 ? 0 : f >= 3 ? 2 : 1];
            var lines = new List<(string, string)> { (id, greet ? TL(hello) : TT("Anything else?", "มีอะไรอีกไหม?")) };
            var replies = new List<(string, System.Action)>();
            replies.Add((TT("Tell me about yourself", "เล่าเรื่องตัวเองหน่อย"), () =>
            {
                int i = _talkStory.TryGetValue(id, out var n) ? n : 0; _talkStory[id] = i + 1;
                var s = bank.story[i % bank.story.Length];
                BShowVN(id, new List<(string, string)> { (null, TT("Tell me about yourself.", "เล่าเรื่องตัวเองให้ฟังหน่อย")), (id, TL(s)) },
                    new List<(string, System.Action)> { (TT("I see", "อ๋อ อย่างนี้นี่เอง"), () => CrewTalkMenu(id, spot, false)) });
            }));
            replies.Add((TT("What do you know about Mars?", "รู้อะไรเกี่ยวกับดาวอังคารบ้าง?"), () =>
            {
                int i = _talkFact.TryGetValue(id, out var n) ? n : 0; _talkFact[id] = i + 1;
                var s = bank.fact[i % bank.fact.Length];
                BShowVN(id, new List<(string, string)> { (null, TT("What do you know about Mars?", "รู้อะไรเกี่ยวกับดาวอังคารบ้าง?")), (id, TL(s)) },
                    new List<(string, System.Action)> { (TT("Good to know", "น่ารู้ดี"), () => CrewTalkMenu(id, spot, false)) });
            }));
            var job = CardJob ?? (_emType == "power" ? Jobs["em_power"] : _emType == "leak" ? Jobs["em_leak"] : null);
            var jobSpot = job != null ? BSpotById(job.spot) : null;
            if (jobSpot != null && !_crewCall.ContainsKey(id))
                replies.Add((TT("Can you help me with a job?", "ช่วยงานหน่อยได้ไหม?"), () =>
                {
                    CrewCall(id, jobSpot.stand);
                    BShowVN(id, new List<(string, string)> { (id, TL(bank.help)) }, new List<(string, System.Action)> { (TT("Thanks!", "ขอบใจ!"), null) });
                }));
            if (!_bDone.Contains("vn_" + id))
                replies.Add((TT("Thanks for being here", "ขอบใจนะที่อยู่ด้วยกัน"), () =>
                {
                    BFriendTalk(id);
                    BShowVN(id, new List<(string, string)> { (id, TL(bank.thanks)) }, new List<(string, System.Action)> { (TT("See you", "ไว้เจอกัน"), null) });
                }));
            replies.Add((TT("Sorry... could you leave the base?", "ขอโทษนะ...ช่วยออกจากฐานได้ไหม"), () =>
            {
                BShowVN(id, new List<(string, string)> { (null, TT("We are short of supplies. I'm sorry, but could you leave?", "เสบียงเราไม่พอจริง ๆ ขอโทษนะ ช่วยออกไปได้ไหม")) },
                    new List<(string, System.Action)>
                    {
                        (TT("Yes, I'm sure", "ใช่ ตัดสินใจแล้ว"), () => GuestLeave(id)),
                        (TT("No, stay", "ไม่ อยู่ต่อเถอะ"), () => CrewTalkMenu(id, spot, false)),
                    });
            }));
            replies.Add((TT("See you around", "ไว้คุยกันใหม่นะ"), null));
            BShowVN(id, lines, replies);
        }

        /// <summary>You asked a guest to leave: one fewer mouth. Friends leave a small gift; upset guests take food.</summary>
        void GuestLeave(string id)
        {
            if (!_life.State.guests.Remove(id)) return;
            var bank = BankOf(id);
            _crewCall.Remove(id); _crewHere.Remove(id);
            if (_jobDelegateGuest == id) JobRelease();
            int f = Friend(id);
            if (f >= 2) { _life.ApplyTask(new Stock { water = 1 }); _ui.Toast(TT(LbName(id) + " left you a little water as a goodbye.", LbName(id) + " ทิ้งน้ำไว้ให้เล็กน้อยเป็นของลา"), GameUI.CGreen); }
            else if (f <= -1 && _life.State.stock.food >= 1) { _life.ApplyTask(new Stock { food = -1 }); _ui.Toast(TT(LbName(id) + " took some food on the way out.", LbName(id) + " หยิบอาหารติดมือไปด้วยตอนออกไป"), GameUI.CRed); }
            _life.State.morale = Mathf.Max(0, _life.State.morale - 6);
            _sfx.Play("open");
            BShowVN(id, new List<(string, string)> { (id, TL(bank.leave)) }, new List<(string, System.Action)> { (TT("Goodbye", "ลาก่อน"), null) });
            BRefreshProps();
        }
    }
}
