// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a short mission list at the side tell new players what to do next, without walls of text?
// Date: 2026-11-14
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// The mission tracker (restore mode): five steps from "plug ORBI in" to "say goodbye", the current one in
    /// amber, done ones in green, later ones dim, plus one line about today (weather, storm time, next relay pass).
    /// Built on the right side under the portrait; refreshed with the HUD.
    /// </summary>
    public partial class GameFlow
    {
        VisualElement MissionPanel()
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            if (!RestoreMode || _restore == null) return box;
            box.style.position = Position.Absolute; box.style.right = 24; box.style.top = 350; box.style.width = 400;
            box.style.backgroundColor = new Color(0.03f, 0.06f, 0.1f, 0.9f); GameUI.Border(box, GameUI.CAmber, 2, 12);
            box.style.paddingLeft = box.style.paddingRight = 16; box.style.paddingTop = 10; box.style.paddingBottom = 12;
            box.Add(_ui.L(TT("MISSION", "ภารกิจ"), 22, true, GameUI.CAmber));
            var r = _restore;
            int rooms = 0; foreach (var id in new[] { "greenhouse", "storage", "air", "quarters" }) if (r.StateOf(id) == RoomState.Restored) rooms++;
            var steps = new (string text, bool done)[]
            {
                (TT("Plug ORBI's chip into the lounge console", "เสียบชิป ORBI ที่เครื่องในห้องนั่งเล่น"), r.IsDone("chip")),
                (TT("Get the generator running (power room)", "เปิดเครื่องปั่นไฟ (ห้องไฟฟ้า)"), r.StateOf("power") == RoomState.Restored),
                (TT($"Bring back 3 rooms, any order ({Mathf.Min(rooms, 3)}/3)", $"ฟื้นห้องให้ได้ 3 ห้อง ลำดับไหนก็ได้ ({Mathf.Min(rooms, 3)}/3)"), rooms >= 3),
                (TT("Fix the radio room", "ซ่อมห้องวิทยุ"), r.StateOf("comms") == RoomState.Restored),
                (TT("Send the call when the orbiter passes", "ส่งสัญญาณตอนยานโคจรผ่าน"), r.Signalled),
                (TT("Wait for the rescue ship", "รอยานกู้ภัยมารับ"), false),
            };
            bool current = false;
            foreach (var (text, done) in steps)
            {
                bool now = !done && !current;
                if (now) current = true;
                var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 6;
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.style.width = 14; dot.style.height = 14; dot.style.marginTop = 6; dot.style.marginRight = 10; dot.style.flexShrink = 0;
                dot.style.backgroundColor = done ? GameUI.CGreen : now ? GameUI.CAmber : new Color(0.25f, 0.27f, 0.33f);
                GameUI.Border(dot, new Color(0, 0, 0, 0.6f), 2, 7);
                row.Add(dot);
                var l = _ui.L(text, now ? 21 : 18, now, done ? GameUI.CGreen : now ? GameUI.CText : GameUI.CDim);
                l.style.flexShrink = 1; l.style.whiteSpace = WhiteSpace.Normal;
                row.Add(l);
                box.Add(row);
            }
            string today = r.Weather == "flare" && !_rFlareDone ? TT("Today: solar storm at 13:00. Safe mode + lounge!", "วันนี้: พายุสุริยะ 13:00 ตั้งโหมดปลอดภัย แล้วหลบห้องนั่งเล่น!")
                : r.CoolingBroken ? TT("Today: the cooling loop is broken (31°C inside).", "วันนี้: ระบบระบายความร้อนเสีย (ในฐาน 31°C)")
                : r.Weather == "cold" ? TT("Today: very cold, heaters use more power.", "วันนี้: หนาวจัด ฮีตเตอร์กินไฟมากขึ้น")
                : r.Weather == "dust" ? TT("Today: dust storm, less solar power.", "วันนี้: พายุฝุ่น แผงโซลาร์ได้ไฟน้อยลง")
                : r.StateOf("comms") == RoomState.Restored && !r.Signalled ? TT("Next orbiter pass: ", "ยานโคจรผ่านครั้งถัดไป: ") + HourText(r.NextRelayHour)
                : null;
            if (today != null) { var t = _ui.L(today, 17, true, GameUI.CCyan); t.style.marginTop = 10; t.style.whiteSpace = WhiteSpace.Normal; box.Add(t); }
            return box;
        }
    }
}
