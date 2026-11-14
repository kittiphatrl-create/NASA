// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a short rescue scene and a big star summary make the ending feel earned (instead of a wall of text)?
// Date: 2026-11-14
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// The ending of the abandoned-base chapter: a rescue scene (ship lands at dusk, everyone walks aboard, it
    /// lifts off), then one summary screen: stars, one big line, four big numbers, how everyone felt, and buttons.
    /// A lost run shows why in one line and a tip, with "retry from this morning".
    /// </summary>
    public partial class GameFlow
    {
        /// <summary>Living conditions 0-100: the cadet's needs, health and how full the base stores are.</summary>
        int RestoreLiving()
        {
            if (_restore == null || _life == null) return 0;
            var st = _life.State;
            float needs = (_restore.Food + _restore.Water + _restore.Rest) / 3f;
            float health = 100f * st.health / Mathf.Max(1, LC.health);
            float stores = Mathf.Clamp01((st.stock.o2 / 8f + st.stock.water / 6f + st.stock.food / 6f) / 3f) * 100f;
            return Mathf.RoundToInt(needs * 0.4f + health * 0.4f + stores * 0.2f);
        }

        int RestoreStars()
        {
            if (_life.State.outcome != LifeOutcome.Rescued) return 0;
            int living = RestoreLiving(), stars = 1;
            if (living >= 50 && _life.State.health >= 2) stars++;
            if (living >= 70 && _life.State.health >= 3 && _life.State.day <= 5) stars++;
            return stars;
        }

        /// <summary>The ship lands at dusk, the crew (pet and friends too) walks aboard, the ship lifts off. ~8 seconds, skippable.</summary>
        void RestoreRescueScene(System.Action done)
        {
            _sfx.Play("good");
            bool finished = false;
            void Finish() { if (finished) return; finished = true; _ui.HideScreen(); done(); }
            _ui.ShowScreen(root =>
            {
                root.style.alignItems = Align.FlexStart; root.style.justifyContent = Justify.FlexStart;
                var bg = new VisualElement(); bg.style.position = Position.Absolute; bg.style.left = 0; bg.style.top = 0; bg.style.width = 1920; bg.style.height = 1080;
                bg.style.backgroundImage = new StyleBackground(Art.RSky(false, false, _restore.Day)); root.Add(bg);
                // the base silhouette sits at x≈ 0.35-0.6 of the sky image; the landing pad to its right
                var ship = new VisualElement(); ship.style.position = Position.Absolute; ship.style.width = 200; ship.style.height = 260; ship.style.left = 1300;
                ship.style.backgroundImage = new StyleBackground(Art.RRescueShip(true)); root.Add(ship);
                var walkers = new List<VisualElement>();
                var who = new List<string> { "cadet" };
                if (_life.State.pet != null) who.Add("pet:" + _life.State.pet);
                foreach (var g in _life.State.guests) who.Add(g);
                for (int i = 0; i < who.Count; i++)
                {
                    var sp = who[i].StartsWith("pet:") ? Art.Pet(who[i].Substring(4)) : Art.Crew(who[i], 0, -1);
                    float k = (who[i].StartsWith("pet:") ? 70f : 150f) / sp.rect.height;
                    var w = new VisualElement(); w.style.position = Position.Absolute; w.style.width = sp.rect.width * k; w.style.height = sp.rect.height * k;
                    w.style.left = 760 - i * 90; w.style.top = 900 - sp.rect.height * k; w.style.backgroundImage = new StyleBackground(sp);
                    root.Add(w); walkers.Add(w);
                }
                var line = _ui.L(TT("The rescue ship is here. Time to go home.", "ยานกู้ภัยมาถึงแล้ว ถึงเวลากลับบ้าน"), 40, true, GameUI.CText);
                line.style.position = Position.Absolute; line.style.left = 0; line.style.right = 0; line.style.top = 80; line.style.unityTextAlign = TextAnchor.MiddleCenter;
                root.Add(line);
                var skip = _ui.B(TT("Skip", "ข้าม"), Finish, 20); skip.style.position = Position.Absolute; skip.style.right = 40; skip.style.bottom = 40; root.Add(skip);
                float t0 = Time.realtimeSinceStartup;
                root.schedule.Execute(() =>
                {
                    float t = Time.realtimeSinceStartup - t0;
                    // 0-3 s: the ship comes down; 3-6 s: everyone walks aboard; 6-8.5 s: lift off
                    float y = t < 3 ? Mathf.Lerp(-280, 640, Mathf.SmoothStep(0, 1, t / 3f)) : t < 6 ? 640 : Mathf.Lerp(640, -320, Mathf.Clamp01((t - 6) / 2.5f) * Mathf.Clamp01((t - 6) / 2.5f));
                    ship.style.top = y;
                    ship.style.backgroundImage = new StyleBackground(Art.RRescueShip(t < 3.1f || t > 6f));
                    for (int i = 0; i < walkers.Count; i++)
                    {
                        float k = Mathf.Clamp01((t - 3f - i * 0.25f) / 2.2f);
                        walkers[i].style.left = Mathf.Lerp(760 - i * 90, 1370, k);
                        walkers[i].style.opacity = k > 0.97f ? 0 : 1;
                    }
                    if (t > 6) line.text = GameUI.Thai(TT("Goodbye, old base. Thank you, ORBI.", "ลาก่อนฐานเก่า ขอบคุณนะ ORBI"));
                    if (t > 8.8f) Finish();
                }).Every(16).Until(() => finished);
            });
        }

        /// <summary>Restore-mode end screen: replaces the long diary summary.</summary>
        void RestoreEndShow()
        {
            var st = _life.State;
            bool won = st.outcome == LifeOutcome.Rescued;
            int stars = RestoreStars(), living = RestoreLiving();
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.width = 1300; box.style.alignItems = Align.Center;
                GameUI.PanelFrame(box, won ? GameUI.CAmber : GameUI.CRed);
                box.style.paddingLeft = box.style.paddingRight = 40; box.style.paddingTop = 26; box.style.paddingBottom = 28;
                if (won)
                {
                    var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginBottom = 6;
                    for (int i = 0; i < 3; i++)
                    {
                        var s = new Image { sprite = Art.RStar(i < stars), scaleMode = ScaleMode.ScaleToFit };
                        s.style.width = i == 1 ? 150 : 120; s.style.height = i == 1 ? 150 : 120; s.style.marginLeft = s.style.marginRight = 10; s.style.alignSelf = Align.FlexEnd;
                        row.Add(s);
                    }
                    box.Add(row);
                }
                string head = !won ? TT("Not this time... try again!", "ครั้งนี้ไม่รอด... ลองใหม่นะ!")
                    : stars == 3 ? TT("AMAZING! A real astronaut!", "สุดยอดมาก! นักบินอวกาศตัวจริง!")
                    : stars == 2 ? TT("Great job! Everyone got home safe.", "เก่งมาก! ทุกคนกลับบ้านอย่างปลอดภัย")
                    : TT("Made it, just barely! Everyone is worn out.", "รอดแบบหวุดหวิด! ทุกคนเหนื่อยล้าสุด ๆ");
                box.Add(_ui.L(head, 56, true, won ? GameUI.CAmber : GameUI.CRed));
                string mood = !won ? LoseReason()
                    : living >= 75 ? TT("Everyone ate well, slept well and is smiling.", "ทุกคนอิ่มท้อง นอนหลับสบาย และยิ้มได้")
                    : living >= 50 ? TT("Tired and a bit hungry, but safe and proud.", "เหนื่อยและหิวนิดหน่อย แต่ปลอดภัยและภูมิใจ")
                    : TT("Hungry, thirsty and exhausted... but alive.", "หิว กระหาย และหมดแรง... แต่ก็ยังรอด");
                var m = _ui.L(mood, 30, false, GameUI.CText); m.style.marginTop = 6; m.style.unityTextAlign = TextAnchor.MiddleCenter; box.Add(m);
                // four big numbers
                var tiles = new VisualElement(); tiles.style.flexDirection = FlexDirection.Row; tiles.style.marginTop = 22;
                void Tile(string big, string small, Color c)
                {
                    var t = new VisualElement(); t.style.width = 270; t.style.marginLeft = t.style.marginRight = 8; t.style.paddingTop = 12; t.style.paddingBottom = 12; t.style.alignItems = Align.Center;
                    t.style.backgroundColor = new Color(0.07f, 0.1f, 0.18f); GameUI.Border(t, c, 2, 14);
                    t.Add(_ui.L(big, 52, true, c)); t.Add(_ui.L(small, 20, false, GameUI.CDim));
                    tiles.Add(t);
                }
                Tile(st.day.ToString(), TT("days on Mars", "วันบนดาวอังคาร"), GameUI.CCyan);
                Tile(Mathf.RoundToInt(_restore.Percent) + "%", TT("base restored", "ฟื้นฐานสำเร็จ"), GameUI.CAmber);
                Tile(living + "%", TT("living conditions", "ความเป็นอยู่"), living >= 70 ? GameUI.CGreen : living >= 45 ? GameUI.CAmber : GameUI.CRed);
                Tile(st.guests.Count.ToString(), TT("friends saved", "เพื่อนที่ช่วยไว้"), GameUI.CGreen);
                box.Add(tiles);
                if (!won) { var tip = _ui.L(LoseTip(), 24, true, GameUI.CCyan); tip.style.marginTop = 18; tip.style.unityTextAlign = TextAnchor.MiddleCenter; box.Add(tip); }
                var row2 = new VisualElement(); row2.style.flexDirection = FlexDirection.Row; row2.style.marginTop = 24;
                if (!won && HasDaySave)
                    row2.Add(_ui.B(TT($"Retry from day {DaySaveDay}", $"เล่นใหม่จากวันที่ {DaySaveDay}"), () => { _lbContinueDay = true; StartLifeboat(_lbStowed, _lbFixedPet); }, 28, true));
                row2.Add(_ui.B(TT("Play again", "เล่นใหม่ทั้งหมด"), () => StartLifeboat(_lbStowed, _lbFixedPet), 28, won));
                row2.Add(_ui.B(_loc.T("menu.sources"), ShowSources, 24));
                row2.Add(_ui.B(_loc.T("end.title"), RestartToTitle, 24));
                box.Add(row2);
                root.Add(box);
            });
        }

        string LoseReason()
        {
            switch (_life.State.outcome)
            {
                case LifeOutcome.FailedAir: return TT("The base ran out of oxygen.", "ออกซิเจนในฐานหมด");
                case LifeOutcome.FailedMonster: return TT("Something that only looked like a visitor got inside.", "สิ่งที่แค่ดูเหมือนผู้มาเยือนเข้ามาในฐานได้");
                default: return _restore != null && _restore.Water < 5 ? TT("Too little water for too long: the body gave up.", "ขาดน้ำนานเกินไป ร่างกายไม่ไหว")
                    : _restore != null && _restore.Food < 5 ? TT("Too little food for too long: the body gave up.", "ขาดอาหารนานเกินไป ร่างกายไม่ไหว")
                    : TT("Health ran out.", "สุขภาพหมด");
            }
        }

        string LoseTip()
        {
            switch (_life.State.outcome)
            {
                case LifeOutcome.FailedAir: return TT("Tip: fix the air room early and run the oxygen maker when ORBI warns you.", "เคล็ดลับ: ฟื้นห้องผลิตอากาศให้เร็ว และเปิดเครื่องผลิตออกซิเจนเมื่อ ORBI เตือน");
                case LifeOutcome.FailedMonster: return TT("Tip: scan visitors. Living things are warm and breathe.", "เคล็ดลับ: สแกนผู้มาเยือนก่อน สิ่งมีชีวิตจริงตัวอุ่นและหายใจ");
                default: return TT("Tip: drink at the water shelf, eat at dinner, and restore the storage and greenhouse for food and water.", "เคล็ดลับ: ดื่มน้ำที่ชั้นน้ำ กินมื้อเย็น และฟื้นห้องเก็บของกับเรือนกระจกเพื่อได้น้ำและอาหาร");
            }
        }
    }
}
