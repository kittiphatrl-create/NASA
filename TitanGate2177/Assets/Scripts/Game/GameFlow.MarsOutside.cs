// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does walking out onto Mars (hot spots you can only see with a meter, a storm warned a day ahead, a rover
//           that costs a lot of power) add tension without unfair surprises?
// Date: 2026-11-15
using System.Collections.Generic;
using System.Linq;
using TitanGate.Core.LastDock;
using UnityEngine;
using UnityEngine.UIElements;
using DockRoom = TitanGate.Core.LastDock.Room;
using Fault = TitanGate.Core.LastDock.Fault;
using Outcome = TitanGate.Core.LastDock.Outcome;

namespace TitanGate.Game
{
    /// <summary>
    /// LAST DOCK phase 3: the base's airlock opens onto Mars. Three sites to search once a day (one is a radiation hot
    /// spot each day, every one in a solar storm), the rover pad, the storm shelter, mystery food, raiders and dark
    /// nights. Rules: Core/LastDock/StationSimulation; this file draws and wires them.
    /// </summary>
    public partial class GameFlow
    {
        readonly SpriteRenderer[] _ldHot = new SpriteRenderer[StationSimulation.Sites];
        SpriteRenderer _ldRover, _ldDark, _ldStormTint;

        static readonly Vector2[] LdSitePos = { new Vector2(45f, 22.6f), new Vector2(51.5f, 12.2f), new Vector2(46f, 4.6f) };

        string LdSiteName(int i) => i switch
        {
            0 => TT("Crashed supply drop", "ซากกล่องเสบียงที่ตก"),
            1 => TT("Old rover wreck", "ซากรถสำรวจเก่า"),
            _ => TT("Ridge cache", "จุดเก็บของบนเนินหิน"),
        };

        string LdSiteFinds(int i) => i switch
        {
            0 => TT("parts, battery or oxygen", "อะไหล่ แบตเตอรี่ หรือออกซิเจน"),
            1 => TT("rover parts (or spare parts)", "ชิ้นส่วนรถ (หรืออะไหล่)"),
            _ => TT("2 boxes of unlabelled food", "อาหารไม่มีฉลาก 2 กล่อง"),
        };

        void LdLayoutOutside()
        {
            LRoom(27, 18, 31, 22, 6, "ld.airlock");
            foreach (int x in new[] { 27, 28 }) LDoor(x, 17, 6);
            foreach (int y in new[] { 20, 21 }) LDoor(32, y, 6);
            LRoom(33, 17, 55, 25, KOutside, null);
            LRoom(38, 1, 55, 16, KOutside, null);
        }

        void LdBuildOutside()
        {
            // airlock: suit lockers by the outer door
            BDeco(Art.Locker(false), 28.0f, 22.12f, 1);
            BDeco(Art.Locker(true), 30.6f, 22.12f, 1);
            BLamp(new Vector2(29.5f, 20f), new Color(1f, 0.7f, 0.3f), 4.5f, 0.45f);

            // outside: solar panels that feed the base, the three sites and the rover pad
            BDeco(Art.SolarArray(false, false), 41.5f, 17.6f, 4);
            BDeco(Art.SolarArray(false, true), 41.5f, 13.6f, 4);
            BProp("site_0", LdSiteName(0), Art.CargoPod(true), LdSitePos[0].x, LdSitePos[0].y, LdSitePos[0] + new Vector2(0, -1f), 2, 1);
            BProp("site_1", LdSiteName(1), Art.Station("rover_wreck", false) ?? Art.CargoPod(false), LdSitePos[1].x, LdSitePos[1].y, LdSitePos[1] + new Vector2(0, -1f), 2, 1);
            BProp("site_2", LdSiteName(2), Art.Crates(), LdSitePos[2].x, LdSitePos[2].y, LdSitePos[2] + new Vector2(0, -0.9f), 2, 1);
            for (int i = 0; i < StationSimulation.Sites; i++)
            {
                _ldHot[i] = BSprite("hot", Art.SoftLight(), LdSitePos[i] + new Vector2(0, 0.4f), 30001);
                _ldHot[i].transform.localScale = Vector3.one * 3.2f;
                _ldHot[i].color = Color.clear;
            }
            _ldRover = BProp("rover_pad", TT("Rover pad", "ลานจอดรถสำรวจ"), null, 35.5f, 23.0f, new Vector2(35.5f, 21.8f)).sr;
            BLamp(new Vector2(35.5f, 23.5f), new Color(1f, 0.8f, 0.5f), 3.5f, 0.35f);

            // full-map tints: a dark base when the battery is empty, an orange sky over the outside in a storm
            _ldDark = BSprite("ld_dark", LdPixel(), new Vector2(-20, -20), 29999);
            _ldDark.transform.localScale = new Vector3(_bW + 40, _bH + 40, 1); _ldDark.color = Color.clear;
            _ldStormTint = BSprite("ld_storm", LdPixel(), new Vector2(33, 1), 29998);
            _ldStormTint.transform.localScale = new Vector3(_bW - 33 + 20, _bH + 10, 1); _ldStormTint.color = Color.clear;
        }

        void LdOutsideUpdate(float dt)
        {
            if (_ld == null || _ldRover == null) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(_bAnim * (Settings.ReducedMotion ? 0 : 3));
            for (int i = 0; i < _ldHot.Length; i++)
            {
                bool show = _ld.StormToday || _ld.HasRadMeter && _ld.IsHot(i);
                _ldHot[i].color = Color.Lerp(_ldHot[i].color, show ? new Color(1f, 0.15f, 0.1f, 0.25f + 0.2f * pulse) : Color.clear, dt * 4);
            }
            _ldRover.sprite = !_ld.RoverBuilt ? null : Art.Station(_ld.RoverSolar ? "rover_solar" : "rover", false);
            _ldRover.color = _ld.RoverBroken ? new Color(1f, 0.6f, 0.6f) : Color.white;
            float dark = _ld.Battery <= 0 ? 0.45f : _ld.Battery == 1 ? 0.22f : 0f;
            _ldDark.color = Color.Lerp(_ldDark.color, new Color(0.02f, 0.03f, 0.1f, dark), dt * 2);
            _ldStormTint.color = Color.Lerp(_ldStormTint.color, _ld.StormToday ? new Color(1f, 0.35f, 0.1f, 0.12f + 0.06f * pulse) : Color.clear, dt * 2);
            _sfx.Loop("heart", _ld.Health == 1 && _ld.Outcome == Outcome.Ongoing);
        }

        /// <summary>The top warning line: a fault first, then the storm, then the dark.</summary>
        (string text, Color color)? LdAlert()
        {
            var f = _ld.ActiveFault;
            if (f != Fault.None) { var inc = LdIncidentOf(f); return ("! " + inc.title + "  ·  " + TT($"Fix at {LdRoomName(inc.room)} or the station board", $"แก้ที่{LdRoomName(inc.room)} หรือแผงสถานะสถานี"), GameUI.CRed); }
            if (_ld.StormToday) return (TT("SOLAR STORM today: outside is dangerous. Power the shelter tonight (station board).", "พายุสุริยะวันนี้: ข้างนอกอันตราย เปิดห้องหลบรังสีคืนนี้ (แผงสถานะสถานี)"), GameUI.CAmber);
            if (_ld.StormTomorrow) return (TT("ORBI: a solar storm arrives TOMORROW. Keep 1 battery for the shelter.", "ORBI: พายุสุริยะจะมาพรุ่งนี้ เก็บแบต 1 ไว้เปิดห้องหลบรังสี"), GameUI.CAmber);
            if (_ld.DarkNights > 0) return (TT("The base went dark last night. Something scratched at the door... (story)", "เมื่อคืนฐานมืด มีเสียงขูดที่ประตู... (เรื่องแต่ง)"), GameUI.CAmber);
            if (_ld.Sick) return (TT("Upset stomach from bad food: one less work unit tomorrow.", "ท้องเสียจากอาหารเสีย พรุ่งนี้เวลาทำงานลดลง 1"), GameUI.CAmber);
            return null;
        }

        IEnumerable<string> LdOutsideActions(string spot)
        {
            if (spot.StartsWith("site_")) return new[] { "explore_" + spot.Substring(5) };
            if (spot == "rover_pad") return new[] { "assemble_rover", "rover_solar", "send_rover", "repair_rover" }.Where(a => _ld.Can(a, out var why) || why != "not_needed");
            return null;
        }

        VisualElement LdOutsideStatus(string spot)
        {
            var box = new VisualElement(); box.style.marginBottom = 10;
            if (spot.StartsWith("site_"))
            {
                int i = spot[5] - '0';
                box.Add(LdPara(TT("You may find: ", "อาจเจอ: ") + LdSiteFinds(i), GameUI.CText, 22));
                string rad = _ld.StormToday ? TT("Radiation: HIGH everywhere (solar storm)", "รังสี: สูงทุกจุด (พายุสุริยะ)")
                    : _ld.HasRadMeter ? (_ld.IsHot(i) ? TT("Radiation meter: HIGH here today — go somewhere else", "เครื่องวัดรังสี: วันนี้จุดนี้รังสีสูง ไปจุดอื่น") : TT("Radiation meter: safe here today", "เครื่องวัดรังสี: วันนี้จุดนี้ปลอดภัย"))
                    : TT("Radiation: unknown. One of the three sites is a hot spot today (crew −1 there).", "รังสี: ไม่รู้ วันนี้มี 1 ใน 3 จุดที่รังสีสูง (ลูกเรือ −1)");
                bool danger = _ld.StormToday || _ld.HasRadMeter && _ld.IsHot(i);
                box.Add(LdPara(rad, danger ? GameUI.CRed : _ld.HasRadMeter ? GameUI.CGreen : GameUI.CAmber, 22));
                box.Add(LdPara(TT("One trip outside a day.", "ออกไปข้างนอกได้วันละครั้ง"), GameUI.CDim, 20));
            }
            else
            {
                box.Add(LdPara(_ld.RoverBuilt ? (_ld.RoverBroken ? TT("Rover: broken (1 part to fix)", "รถ: เสีย (ใช้อะไหล่ 1 ซ่อม)") : TT("Rover: ready", "รถ: พร้อม"))
                    : TT($"Rover parts found: {_ld.RoverParts}/{_ld.C.roverPartsNeeded} (search the old rover wreck)", $"ชิ้นส่วนรถ: {_ld.RoverParts}/{_ld.C.roverPartsNeeded} (หาได้ที่ซากรถสำรวจเก่า)"), _ld.RoverBuilt ? GameUI.CGreen : GameUI.CAmber, 22));
                if (_ld.RoverBuilt) box.Add(LdPara(_ld.RoverSolar ? TT("Solar panel fitted: trips cost no base power.", "ติดแผงโซลาร์แล้ว: ออกไปไม่กินไฟฐาน")
                    : TT($"Each trip needs a big charge: {_ld.C.roverCharge} battery.", $"ออกไปแต่ละครั้งต้องชาร์จไฟมาก: แบต {_ld.C.roverCharge}"), GameUI.CDim, 20));
            }
            return box;
        }

        void LdShowOutsidePanel(string spot)
        {
            _ui.ShowModal(p =>
            {
                LdHeader(p, spot == "rover_pad" ? TT("Rover pad", "ลานจอดรถสำรวจ") : LdSiteName(spot[5] - '0'));
                p.Add(LdPara(TT($"Day {_ld.Day} of {_ld.C.days} · work units left: {_ld.WorkUnits}", $"วันที่ {_ld.Day} จาก {_ld.C.days} · เวลาทำงานเหลือ: {_ld.WorkUnits}"), GameUI.CCyan, 22));
                p.Add(LdOutsideStatus(spot));
                foreach (var a in LdOutsideActions(spot)) p.Add(LdActionRow(a));
                if (spot.StartsWith("site_") && _ld.StormToday) p.Add(LdPara("NASA: " + LdStormFact, GameUI.CCyan, 20));
            });
        }

        string LdStormFact => TT("In May 2024 NASA's Curiosity rover measured a jump in radiation on the surface of Mars during a big solar storm.",
                                 "เดือนพฤษภาคม 2024 รถสำรวจ Curiosity ของ NASA วัดรังสีบนพื้นดาวอังคารได้สูงขึ้นมากระหว่างพายุสุริยะครั้งใหญ่");

        void LdShowRaid()
        {
            _ui.ShowModal(p =>
            {
                LdHeader(p, TT("Strangers followed the rover home!", "มีคนแปลกหน้าตามรถกลับมาถึงฐาน!"));
                p.Add(LdPara(TT("They want supplies. (This part is a made-up story, not a real NASA event.)", "พวกเขาต้องการเสบียง (ส่วนนี้เป็นเรื่องแต่ง ไม่ใช่เหตุการณ์จริงของ NASA)"), GameUI.CDim, 20));
                foreach (var a in new[] { "raid_food", "raid_battery", "raid_refuse" }) p.Add(LdActionRow(a));
            }, false);
        }
    }
}
