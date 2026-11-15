// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is LAST DOCK more fun as a management game — a nightly power budget, machines that wear and can be
//           serviced, a water chain (O₂ maker, recycler, ice) and two rescue routes — than as a list
//           of repairs? Rules: Core/LastDock/StationSimulation (docs/LASTDOCK_MANAGEMENT.md). This file only draws.
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
    public partial class GameFlow
    {
        // ------------------------------------------------------------------ words

        string LdSysName(Sys s) => s switch
        {
            Sys.Scrubber => TT("CO₂ scrubber", "เครื่องกรอง CO₂"), Sys.O2Gen => TT("O₂ maker", "เครื่องผลิต O₂"), Sys.Recycler => TT("Water recycler", "เครื่องรีไซเคิลน้ำ"),
            Sys.Heater => TT("Heater", "ฮีตเตอร์"), _ => TT("Rescue beacon", "เสาสัญญาณขอความช่วยเหลือ"),
        };

        string LdSysOff(Sys s) => s switch
        {
            Sys.Scrubber => TT("off: CO₂ rises every turn", "ปิด: CO₂ เพิ่มทุกช่วง"),
            Sys.O2Gen => TT("off: −1 O₂ tank a night", "ปิด: ถัง O₂ −1 ต่อคืน"),
            Sys.Recycler => TT("off: no water back", "ปิด: ไม่ได้น้ำคืน"),

            Sys.Heater => TT("off: cold night, 1 fewer turn tomorrow", "ปิด: คืนหนาว พรุ่งนี้เวลาทำงาน −1"),
            _ => TT("off: no signal tonight", "ปิด: คืนนี้ไม่ส่งสัญญาณ"),
        };

        string LdSysOn(Sys s) => s switch
        {
            Sys.Scrubber => TT("removes the CO₂ we breathe out", "กรอง CO₂ ที่เราหายใจออก"),
            Sys.O2Gen => TT("water → O₂ (+1 tank, −1 water a night)", "น้ำ → O₂ (ถัง +1 น้ำ −1 ต่อคืน)"),
            Sys.Recycler => TT("+1 water a night", "น้ำ +1 ต่อคืน"),
            Sys.Heater => TT("keeps the crew warm at night", "ให้ลูกเรืออุ่นตอนกลางคืน"),
            _ => TT("+1 signal a night", "สัญญาณ +1 ต่อคืน"),
        };

        string LdMachineName(Machine m) => m switch
        {
            Machine.Scrubber => TT("CO₂ scrubber", "เครื่องกรอง CO₂"), Machine.O2Gen => TT("O₂ maker", "เครื่องผลิต O₂"), Machine.Fan => TT("Air fan", "พัดลม"),
            Machine.Thermal => TT("Coolant loop", "ท่อระบายความร้อน"), _ => TT("Water recycler", "เครื่องรีไซเคิลน้ำ"),
        };

        static string LdSysIcon(Sys s) => s switch { Sys.Scrubber => "co2", Sys.O2Gen => "o2", Sys.Recycler => "water", Sys.Heater => "heat", _ => "signal" };

        static Color LdCondColor(int c) => c >= 60 ? GameUI.CGreen : c >= 40 ? GameUI.CAmber : GameUI.CRed;

        Label LdSectionLabel(string text)
        {
            var l = _ui.L(text, 20, true, LdGold); LdFont(l); l.style.marginTop = 6; l.style.marginBottom = 2; l.style.flexShrink = 0;
            return l;
        }

        // ------------------------------------------------------------------ the power plan

        /// <summary>Forecast for the current plan: solar, what the station uses, the battery after tonight, and how long things last.</summary>
        VisualElement LdForecastBlock()
        {
            var f = _ld.Look();
            var box = new VisualElement(); box.style.marginBottom = 8; box.style.flexShrink = 0;
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap; row.style.alignItems = Align.Center;
            void Chip(string icon, string text, Color c)
            {
                var chip = new VisualElement(); chip.style.flexDirection = FlexDirection.Row; chip.style.alignItems = Align.Center; chip.style.marginRight = 8; chip.style.marginBottom = 6;
                chip.style.paddingLeft = 8; chip.style.paddingRight = 10; chip.style.paddingTop = 4; chip.style.paddingBottom = 4;
                chip.style.backgroundColor = new Color(0.05f, 0.07f, 0.11f, 0.95f); GameUI.Border(chip, c * new Color(1, 1, 1, 0.6f), 1, 8);
                chip.Add(LdIconImage(icon, 22, c)); var l = _ui.L(" " + text, 19, true, c); LdFont(l); chip.Add(l);
                row.Add(chip);
            }
            Chip("sun", TT($"Solar +{f.solar}", $"โซลาร์ +{f.solar}") + (_ld.Dust > 0 ? TT($" (dust {_ld.Dust}%)", $" (ฝุ่น {_ld.Dust}%)") : ""), _ld.Dust >= 30 ? GameUI.CAmber : GameUI.CText);
            Chip("battery", TT($"Uses −{f.draw}", $"ใช้ −{f.draw}"), GameUI.CText);
            Chip("battery", TT($"Battery tonight → {Mathf.Max(0, f.batteryTonight)}", $"แบตหลังคืนนี้ → {Mathf.Max(0, f.batteryTonight)}"), f.shortTonight ? GameUI.CRed : f.batteryTonight < _ld.C.dockPower ? GameUI.CAmber : GameUI.CGreen);
            string Days(int d) => d >= 99 ? "∞" : d.ToString();
            Chip("o2", TT($"O₂ lasts {Days(f.o2Days)} d", $"O₂ พอ {Days(f.o2Days)} วัน"), f.o2Days <= 1 ? GameUI.CRed : f.o2Days <= 3 ? GameUI.CAmber : GameUI.CGreen);
            Chip("water", TT($"Water lasts {Days(f.waterDays)} d", $"น้ำพอ {Days(f.waterDays)} วัน"), f.waterDays <= 1 ? GameUI.CRed : f.waterDays <= 3 ? GameUI.CAmber : GameUI.CGreen);
            Chip("food", TT($"Food lasts {Days(f.foodDays)} d", $"อาหารพอ {Days(f.foodDays)} วัน"), f.foodDays <= 1 ? GameUI.CRed : f.foodDays <= 3 ? GameUI.CAmber : GameUI.CGreen);
            box.Add(row);
            if (f.shortTonight) box.Add(LdPara("! " + TT("Not enough power tonight: the least important systems will switch themselves off. Choose yourself below.",
                "คืนนี้ไฟไม่พอ ระบบที่สำคัญน้อยจะถูกปิดเอง เลือกปิดเองด้านล่างดีกว่า"), GameUI.CRed, 19));
            return box;
        }

        /// <summary>On/off rows for these systems. Switching is free (planning, not work); the night pays for it.</summary>
        VisualElement LdPowerRows(IEnumerable<Sys> systems)
        {
            var box = new VisualElement(); box.style.flexShrink = 0; box.style.marginBottom = 6;
            foreach (var s in systems)
            {
                if (s == Sys.Beacon && !_ld.BeaconBuilt) continue;
                bool on = _ld.On[(int)s];
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 4;
                row.style.paddingLeft = 10; row.style.paddingRight = 10; row.style.paddingTop = 4; row.style.paddingBottom = 4;
                row.style.backgroundColor = on ? new Color(0.06f, 0.12f, 0.1f, 0.95f) : new Color(0.08f, 0.08f, 0.1f, 0.95f);
                GameUI.Border(row, on ? new Color(0.36f, 0.89f, 0.54f, 0.45f) : new Color(1, 1, 1, 0.1f), 1, 8);
                row.Add(LdIconImage(LdSysIcon(s), 26, on ? GameUI.CGreen : GameUI.CDim));
                var mid = new VisualElement(); mid.style.flexGrow = 1; mid.style.marginLeft = 10;
                var name = _ui.L(LdSysName(s), 21, true, on ? GameUI.CText : GameUI.CDim); LdFont(name); mid.Add(name);
                mid.Add(_ui.L(on ? LdSysOn(s) : LdSysOff(s), 16, false, on ? GameUI.CDim : GameUI.CAmber));
                row.Add(mid);
                var draw = _ui.L(TT($"−{_ld.DrawOf(s)} power", $"ไฟ −{_ld.DrawOf(s)}"), 18, true, on ? GameUI.CText : GameUI.CDim); draw.style.marginRight = 10; row.Add(draw);
                string act = (on ? "power_off_" : "power_on_") + StationSimulation.Key(s);
                var b = _ui.B(on ? TT("ON", "เปิดอยู่") : TT("OFF", "ปิดอยู่"), () => LdDo(act), 18, on);
                b.style.width = 140; b.style.whiteSpace = WhiteSpace.NoWrap; b.style.marginLeft = 0; b.style.marginRight = 0;
                row.Add(b);
                box.Add(row);
            }
            return box;
        }

        /// <summary>Condition bars for the machines in a room, with what each one risks.</summary>
        VisualElement LdConditionBlock(IEnumerable<Machine> machines, bool compact = false)
        {
            var box = new VisualElement(); box.style.flexShrink = 0; box.style.marginBottom = 6;
            foreach (var m in machines)
            {
                int c = _ld.Cond[(int)m]; bool ok = _ld.IsOk(m);
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 3;
                var name = _ui.L(LdMachineName(m), 19, true, GameUI.CText); name.style.width = compact ? 190 : 230; LdFont(name); row.Add(name);
                var bar = new VisualElement(); bar.style.width = compact ? 150 : 260; bar.style.height = 14; bar.style.backgroundColor = new Color(1, 1, 1, 0.08f); GameUI.Border(bar, new Color(1, 1, 1, 0.15f), 1, 4);
                var fill = new VisualElement(); fill.style.height = Length.Percent(100); fill.style.width = Length.Percent(ok ? c : 0); fill.style.backgroundColor = LdCondColor(ok ? c : 0);
                bar.Add(fill); row.Add(bar);
                string state = !ok ? TT("BROKEN", "เสีย") : $"{c}%";
                var l = _ui.L("  " + state, 18, true, ok ? LdCondColor(c) : GameUI.CRed); LdFont(l); row.Add(l);
                if (ok && !_ld.Running(m)) row.Add(_ui.L(TT("  off: no wear", "  ปิด: ไม่สึก"), 16, false, GameUI.CDim));
                else if (ok && IncidentDirector.BreakChance(c) > 0) row.Add(_ui.L(TT($"  may break {Mathf.RoundToInt((float)IncidentDirector.BreakChance(c) * 100)}%", $"  อาจเสีย {Mathf.RoundToInt((float)IncidentDirector.BreakChance(c) * 100)}%"), 16, false, LdCondColor(c)));
                box.Add(row);
            }
            if (!compact) box.Add(LdPara(TT($"Service = 1 turn, no parts, +{_ld.C.serviceGain}%. Repair after it breaks = 1 part. Running machines wear about {_ld.C.wearPerNight}% a night.",
                $"บำรุง = 1 ช่วง ไม่ใช้อะไหล่ +{_ld.C.serviceGain}% ซ่อมหลังเสีย = อะไหล่ 1 เครื่องที่ทำงานสึกราว {_ld.C.wearPerNight}% ต่อคืน"), GameUI.CDim, 16));
            return box;
        }

        static readonly Machine[] LdLifeMachines = { Machine.Scrubber, Machine.O2Gen, Machine.Fan, Machine.Recycler };

        /// <summary>The radio route as a checklist, and the beacon route next to it.</summary>
        VisualElement LdRescueBlock()
        {
            var box = new VisualElement(); box.style.flexShrink = 0; box.style.marginBottom = 8;
            var head = _ui.L(TT("Rescue: send our location 3 times, keep 2 battery for the docking port on day 7", "ภารกิจกู้ภัย: ส่งพิกัด 3 ครั้ง และเก็บแบต 2 ไว้เปิดช่องเทียบท่าวันที่ 7"), 19, true, LdGold);
            head.style.whiteSpace = WhiteSpace.Normal; box.Add(head);
            void Step(bool done, string text)
            {
                var l = _ui.L((done ? "✓  " : "○  ") + text, 18, done, done ? GameUI.CGreen : GameUI.CText); l.style.whiteSpace = WhiteSpace.Normal; box.Add(l);
            }
            Step(_ld.RadioRepair >= 1, TT("Radio: new board (1 part)", "วิทยุ: เปลี่ยนแผงวงจร (อะไหล่ 1)"));
            Step(_ld.RadioRepair >= 2, TT("Radio: power test (1 battery, the coolant loop must work)", "วิทยุ: ทดสอบไฟ (แบต 1 ท่อระบายความร้อนต้องใช้ได้)"));
            Step(_ld.RadioFixed, TT("Radio: fix the antenna outside (airlock)", "วิทยุ: ซ่อมเสาอากาศข้างนอก (ประตูออกข้างนอก)"));
            Step(_ld.Signal >= _ld.C.signalNeeded, TT($"Signals sent: {_ld.Signal}/{_ld.C.signalNeeded}", $"ส่งสัญญาณแล้ว: {_ld.Signal}/{_ld.C.signalNeeded}"));
            var or = _ui.L(_ld.BeaconBuilt ? TT("Beacon built: it sends one signal every night it has power.", "สร้างเสาสัญญาณแล้ว: ส่งสัญญาณ 1 ครั้งทุกคืนที่มีไฟ")
                : TT($"Or: build a beacon outside from {_ld.C.beaconParts} rover parts + 1 part (found {_ld.RoverParts}). It sends by itself each night, but costs power.",
                     $"หรือ: สร้างเสาสัญญาณข้างนอกจากชิ้นส่วนรถ {_ld.C.beaconParts} + อะไหล่ 1 (มี {_ld.RoverParts}) ส่งเองทุกคืน แต่กินไฟ"), 17, false, GameUI.CCyan);
            or.style.whiteSpace = WhiteSpace.Normal; or.style.marginTop = 4; box.Add(or);
            return box;
        }

        // ------------------------------------------------------------------ map objects

        void LdBuildManageProps()
        {
            // Life Support: the water recycler on the floor by the left wall (clear of the door at x 20-21)
            BProp("ld_recycler", TT("Water recycler", "เครื่องรีไซเคิลน้ำ"), Art.Station("recycler", false) ?? Art.Recycler(), 17.4f, 19.3f, new Vector2(18.9f, 19.6f), 2, 1);
            // outside: the beacon mast appears once it is built
            _ldBeaconSr = BSprite("ld_beacon", null, new Vector2(36.2f, 21.6f), SortY(21.6f));
        }

        SpriteRenderer _ldBeaconSr;

        void LdManageUpdate()
        {
            if (_ld == null) return;
            if (_ldBeaconSr != null) _ldBeaconSr.sprite = _ld.BeaconBuilt ? Art.Dish(_ld.On[(int)Sys.Beacon]) : null;
        }
    }
}
