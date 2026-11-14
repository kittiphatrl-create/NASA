// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do surprise emergencies (a locked room, a storm, a leak) add pressure that players enjoy solving?
// Date: 2026-11-13
using System.Collections;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Random daily emergencies in the walkable base (from day 2): a power fault that locks the power room until
    /// the breaker is reset, a solar storm alert (reach the storm shelter in time), or an air leak in the hub.
    /// ORBI announces each with a red alert. Engineers staying as guests (Rex, Jules) fix faults for free.
    /// Also: trading with travelling merchants, and guests who help or cause trouble.
    /// </summary>
    public partial class GameFlow
    {
        string _emType;               // null, "power", "flare", "leak"
        float _emDayClock, _emAt = -1, _emTimer, _emDrainT;
        int _emDay = -1;
        readonly List<SpriteRenderer> _emFx = new List<SpriteRenderer>();
        SpriteRenderer _emMarker, _emRoomTint;
        VisualElement _emAlert;

        static readonly Vector2Int[] PowerDoors = { new Vector2Int(25, 6), new Vector2Int(26, 6), new Vector2Int(29, 2), new Vector2Int(29, 3) };
        static readonly RectInt PowerRoom = new RectInt(23, 1, 6, 5);

        bool GuestFixer(out string who)
        {
            who = null;
            if (_life == null) return false;
            foreach (var g in _life.State.guests) if (g == "rex" || g == "jules") { who = g; return true; }
            return false;
        }

        void EmReset()
        {
            _emType = null; _emAt = -1; _emDay = -1; _emDayClock = 0;
            foreach (var f in _emFx) if (f != null) Destroy(f.gameObject);
            _emFx.Clear();
            _emMarker = null; _emRoomTint = null;
            _emAlert?.RemoveFromHierarchy(); _emAlert = null;
        }

        /// <summary>Called every frame in the base. Plans one possible emergency per day and runs the active one.</summary>
        void EmUpdate(float dt)
        {
            if (_bMode != 0 || _life == null || _life.State.outcome != LifeOutcome.Ongoing) return;
            if (_emDay != _life.State.day)
            {
                _emDay = _life.State.day;
                _emDayClock = 0;
                var rng = new System.Random((SeedOverride >= 0 ? SeedOverride : 1000) * 31 + _emDay * 7);
                // an emergency strikes at a random hour between 09:00 and 16:00 (game time, so it fits any pace)
                _emAt = _emDay >= 2 && (_autoLog == null || _playtest) && rng.NextDouble() < 0.55 ? 9f + (float)rng.NextDouble() * 7f : -1;   // the screenshot pass forces its own
                _emPlanned = new[] { "power", "flare", "leak" }[rng.Next(3)];
                // project "seal the hub seams": most leaks never happen
                if (_emPlanned == "leak" && _life.State.flags.Contains("hub_sealed") && rng.NextDouble() < 0.7) _emAt = -1;
            }
            // daytime = free to walk before dinner (events still open or not): the emergency clock keeps running in free time
            bool daytime = (_lbView == LbView.Card || _lbView == LbView.Ration) && !_bOpen && !_ui.Blocking && !_bGather && _life.State.schedule.Hour < DinnerHour;
            if (daytime) _emDayClock += dt;
            if (_emType == null && _emAt > 0 && _life.State.schedule.Hour >= _emAt && daytime) { _emAt = -1; EmStart(_emPlanned); }
            if (_emType == null || _ui.Blocking) return;

            // the marker over the fix point
            if (_emMarker != null)
            {
                _emMarker.transform.position = _emFixPos + Vector3.up * (1.4f + Mathf.Abs(Mathf.Sin(_bAnim * 5)) * 0.25f);
                _emMarker.sprite = Art.Marker(true);
            }
            if (_emRoomTint != null) _emRoomTint.color = new Color(0.9f, 0.05f, 0.05f, 0.18f + Mathf.Abs(Mathf.Sin(_bAnim * 3)) * 0.12f);
            foreach (var f in _emFx) if (f != null) f.sprite = Art.Spark((int)(_bAnim * 10) % 2);

            if (_emType == "flare")
            {
                _emTimer -= dt;
                if (_emTimer <= 0) EmFlareHits();
                return;
            }
            // power fault and leak keep costing until fixed
            _emDrainT -= dt;
            if (_emDrainT <= 0)
            {
                _emDrainT = 22f;
                _life.ApplyTask(_emType == "power" ? new Stock { power = -Mathf.Min(0.5f, _life.State.stock.power) } : new Stock { o2 = -Mathf.Min(0.5f, _life.State.stock.o2) });
                _ui.Toast(_loc.T("base.em." + _emType + ".drain"), GameUI.CRed);
            }
        }

        string _emPlanned;
        Vector3 _emFixPos;

        void EmStart(string type)
        {
            _emType = type;
            _emDrainT = 18f;
            _sfx.Play("alarm"); _sShake = 0.6f;
            EmAlert(_loc.T($"base.em.{type}.title"), _loc.T($"base.em.{type}.body"));
            DayNote(type == "power" ? TT("A fuse blew and the power room locked.", "ฟิวส์ขาด ห้องไฟฟ้าล็อก") : type == "leak" ? TT("An air leak in the hub.", "อากาศรั่วที่โถงกลาง") : TT("ORBI saw a solar flare.", "ORBI ตรวจพบเปลวสุริยะ"));
            if (type == "flare") _ui.Toast(TT("ORBI: X-rays and gamma rays from the flare arrive first, at the speed of light. The fast protons follow within minutes. Get to the storage shelter!",
                "ORBI: รังสีเอกซ์และรังสีแกมมาจากเปลวสุริยะมาถึงก่อนด้วยความเร็วแสง อนุภาคโปรตอนพลังงานสูงจะตามมาในไม่กี่นาที รีบไปที่หลบพายุในคลัง!"), GameUI.CAmber);
            if (type == "power")
            {
                foreach (var d in PowerDoors) _bSolid[d.x, d.y] = true;
                _emRoomTint = BSprite("em-tint", WorldBuilder.WhitePixel(), new Vector2(PowerRoom.x, PowerRoom.y), 29980);
                _emRoomTint.transform.localScale = new Vector3(PowerRoom.width * 16, PowerRoom.height * 16, 1);
                _emRoomTint.GetComponent<SpriteRenderer>().sprite = WorldBuilder.WhitePixel();
                // a pivot-centred white pixel: shift to cover the room rectangle
                _emRoomTint.transform.position = new Vector3(PowerRoom.x + PowerRoom.width / 2f, PowerRoom.y + PowerRoom.height / 2f, 0);
                _emFixPos = new Vector3(25.5f + 0.5f, 7.2f, 0);
                _bSpots.Add(new BSpot { id = "breaker", nameKey = "base.em.breaker", pos = new Vector2(26f, 7.1f), stand = new Vector2(26f, 7.6f), sr = BSprite("breaker", Art.Console("brk", 18, 18, Pal.Red, true), new Vector2(26.8f, 7.05f), SortY(7.05f)) });
                _emFx.Add(_bSpots[_bSpots.Count - 1].sr);
            }
            else if (type == "leak")
            {
                _emFixPos = new Vector3(19.5f, 9.0f, 0);
                var hiss = BSprite("leak", Art.Spark(0), new Vector2(19.5f, 9.4f), SortY(9.4f) + 2);
                hiss.transform.localScale = Vector3.one * 2.5f;
                _emFx.Add(hiss);
                _bSpots.Add(new BSpot { id = "leak", nameKey = "base.em.leak.spot", pos = new Vector2(19.5f, 9.4f), stand = new Vector2(19.5f, 8.6f), sr = hiss });
            }
            else
            {
                _emTimer = 25f;
                _emFixPos = new Vector3(18.5f, 4.0f, 0);
            }
            _emMarker = BSprite("em-marker", Art.Marker(true), _emFixPos, 31010);
            _emFx.Add(_emMarker);
        }

        /// <summary>Big red alert across the screen for a few seconds (ORBI's warning).</summary>
        void EmAlert(string title, string body)
        {
            _emAlert?.RemoveFromHierarchy();
            var a = new VisualElement { pickingMode = PickingMode.Ignore };
            a.style.position = Position.Absolute; a.style.left = 0; a.style.right = 0; a.style.top = 0; a.style.bottom = 0;
            a.style.borderTopWidth = a.style.borderBottomWidth = a.style.borderLeftWidth = a.style.borderRightWidth = 18;
            a.style.borderTopColor = a.style.borderBottomColor = a.style.borderLeftColor = a.style.borderRightColor = new Color(0.9f, 0.05f, 0.08f, 0.8f);
            a.style.backgroundColor = new Color(0.5f, 0f, 0.02f, 0.25f);
            a.style.alignItems = Align.Center; a.style.justifyContent = Justify.Center;
            var panel = new VisualElement { pickingMode = PickingMode.Ignore };
            panel.style.backgroundColor = new Color(0.12f, 0.01f, 0.03f, 0.94f); GameUI.Border(panel, GameUI.CRed, 4, 14);
            panel.style.paddingLeft = panel.style.paddingRight = 40; panel.style.paddingTop = 18; panel.style.paddingBottom = 18; panel.style.maxWidth = 1200;
            panel.Add(_ui.L("!! " + title + " !!", 46, true, GameUI.CRed));
            panel.Add(_ui.L(_loc.T("speaker.orbi") + ": " + body, 26));
            a.Add(panel);
            _ui.Root.Add(a);
            _emAlert = a;
            float t0 = Time.realtimeSinceStartup;
            a.schedule.Execute(() => a.style.opacity = 0.75f + Mathf.Sin((Time.realtimeSinceStartup - t0) * 9f) * 0.25f).Every(30).Until(() => Time.realtimeSinceStartup - t0 > 3.2f);
            a.schedule.Execute(() => { a.RemoveFromHierarchy(); if (_emAlert == a) _emAlert = null; }).StartingIn(3400);
        }

        /// <summary>The player used a spot while an emergency runs: the breaker or the leak opens its repair job.</summary>
        bool EmTryFix(BSpot s)
        {
            if (_emType == null) return false;
            if (!(_emType == "power" && s.id == "breaker" || _emType == "leak" && s.id == "leak")) return false;
            JobShow(Jobs["em_" + _emType]);
            return true;
        }

        void EmFlareHits()
        {
            string room = BRoomAt(_bPos);
            bool inStorage = room == "base.room.storage";
            bool walled = _life.State.flags.Contains("shelter_ready");
            if (inStorage)
            {
                _sfx.Play("good"); _ui.Toast(_loc.T("base.em.flare.safe"), GameUI.CGreen);
                RadAdd(walled ? 0.5f : 3f, walled ? null : TT("the shelter has no water wall yet", "ที่หลบพายุยังไม่มีกำแพงน้ำ"));
                DayNote(TT("Solar storm: we hid in the shelter.", "พายุสุริยะ: หลบอยู่ในที่หลบพายุทัน"));
            }
            else
            {
                _sfx.Play("bad"); _sShake = 0.5f;
                _life.State.morale = Mathf.Max(0, _life.State.morale - 10);
                _ui.Toast(_loc.T("base.em.flare.hit"), GameUI.CRed);
                RadAdd(BIsOutside(_bPos) ? 16f : 13f, TT("caught by the solar storm", "โดนพายุสุริยะ"));
                DayNote(TT("Solar storm: it caught me outside the shelter.", "พายุสุริยะ: ฉันหลบไม่ทัน โดนรังสีเต็ม ๆ"));
            }
            EmEnd();
        }

        void EmEnd()
        {
            if (_emType == "power") foreach (var d in PowerDoors) _bSolid[d.x, d.y] = false;
            _bSpots.RemoveAll(s => s.id == "breaker" || s.id == "leak");
            foreach (var f in _emFx) if (f != null) Destroy(f.gameObject);
            _emFx.Clear();
            if (_emRoomTint != null) Destroy(_emRoomTint.gameObject);
            _emRoomTint = null; _emMarker = null;
            _emType = null;
        }

        // ------------------------------------------------------------------ trading

        int _tradesLeft;
        string _tradeWith;

        static readonly (Stock give, Stock get)[] ZeeOffers =
        {
            (new Stock { water = 1 }, new Stock { food = 1 }),
            (new Stock { power = 2 }, new Stock { o2 = 1 }),
            (new Stock { food = 1 }, new Stock { power = 1 }),
        };
        static readonly (Stock give, Stock get)[] VelaOffers =
        {
            (new Stock { food = 1 }, new Stock { o2 = 1 }),
            (new Stock { water = 1 }, new Stock { power = 2 }),
            (new Stock { power = 1 }, new Stock { water = 1 }),
        };

        static bool IsTrader(string visitor) => visitor == "zee" || visitor == "vela";

        /// <summary>Trade screen with a travelling merchant: up to three swaps per visit, one unit each way.</summary>
        void BShowTrade()
        {
            string v = _lbCard.visitor;
            if (_tradeWith != _lbCard.id) { _tradeWith = _lbCard.id; _tradesLeft = 3; }
            var offers = v == "vela" ? VelaOffers : ZeeOffers;
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.width = 1200;
                box.style.backgroundColor = new Color(0.05f, 0.06f, 0.12f, 0.97f); GameUI.Border(box, GameUI.CAmber, 3, 14);
                box.style.paddingLeft = box.style.paddingRight = 30; box.style.paddingTop = 18; box.style.paddingBottom = 18;
                var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
                var sp = Art.Crew(v, 0, -1, false, false, true);
                var pic = new VisualElement(); float k = 220f / sp.rect.height; pic.style.width = sp.rect.width * k; pic.style.height = 220; pic.style.backgroundImage = new StyleBackground(sp);
                head.Add(pic);
                var col = new VisualElement(); col.style.marginLeft = 20; col.style.flexShrink = 1;
                col.Add(_ui.L(_loc.T("base.trade.title", LbName(v)), 32, true, GameUI.CAmber));
                col.Add(_ui.L(_loc.T($"base.trade.{v}.hello"), 22));
                col.Add(_ui.L(_loc.T("base.trade.left", _tradesLeft), 20, true, GameUI.CCyan));
                head.Add(col); box.Add(head);
                box.Add(BGaugeRow(0.6f));
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.justifyContent = Justify.Center; row.style.marginTop = 10;
                var group = new List<VisualElement>();
                foreach (var (give, get) in offers)
                {
                    var g = give; var r = get;
                    var cost = new Stock { o2 = -g.o2, water = -g.water, food = -g.food, power = -g.power };
                    var both = new Stock { o2 = r.o2 - g.o2, water = r.water - g.water, food = r.food - g.food, power = r.power - g.power };
                    bool can = _tradesLeft > 0 && _life.State.stock.o2 >= g.o2 && _life.State.stock.water >= g.water && _life.State.stock.food >= g.food && _life.State.stock.power >= g.power;
                    var hint = new VisualElement(); hint.style.flexDirection = FlexDirection.Row; hint.style.alignItems = Align.Center;
                    hint.Add(BDeltaIcons(cost)); hint.Add(_ui.L("→", 22, true, GameUI.CText)); hint.Add(BDeltaIcons(r));
                    row.Add(BCard(_loc.T("base.trade.offer"), hint, () =>
                    {
                        if (_life.ApplyTask(both)) { _tradesLeft--; _sfx.Play("good"); _ui.Toast(_loc.T("base.trade.done"), GameUI.CGreen); }
                        BShowTrade();
                    }, can, can ? null : "base.trade.cant", group, 340, GameUI.CAmber));
                }
                box.Add(row);
                var back = _ui.B(_loc.T("base.trade.back"), () => BShowPeep(), 24, true);
                back.style.alignSelf = Align.FlexStart; back.style.marginLeft = 0; back.style.marginTop = 14;
                box.Add(back);
                root.Add(box);
            });
        }

        /// <summary>After the night: guests who went hungry leave, and some break something on the way out.</summary>
        void BAfterNight(List<LifeLog> night)
        {
            if (night == null) return;
            if (_emType == "power" || _emType == "leak")
            {
                // nobody fixed it before bed: the crew patches it in the dark, which costs more than a proper repair
                bool pw = _emType == "power";
                _life.ApplyTask(pw ? new Stock { power = -Mathf.Min(1f, _life.State.stock.power) } : new Stock { o2 = -Mathf.Min(1f, _life.State.stock.o2) });
                _life.State.morale = Mathf.Max(0, _life.State.morale - 5);
                _ui.Toast(pw ? TT("Nobody fixed the short circuit. The crew patched it in the dark and lost power doing it.", "ไม่มีใครซ่อมไฟลัดวงจร ลูกเรือต้องซ่อมกันในความมืด เสียไฟไปเยอะ")
                             : TT("Nobody fixed the leak. Air hissed out all night before the crew patched it.", "ไม่มีใครซ่อมรอยรั่ว อากาศรั่วออกไปทั้งคืนกว่าจะอุดได้"), GameUI.CRed);
                EmEnd();
            }
            if (_life.State.flags.Contains("battery_plus")) _life.ApplyTask(new Stock { power = 0.5f });   // project: second battery string
            if (_life.State.flags.Contains("recycler_broken") && _life.State.stock.water > 0)
            {
                _life.ApplyTask(new Stock { water = -Mathf.Min(1f, _life.State.stock.water) });
                _ui.Toast(TT("The broken water recycler leaked again in the night.", "เครื่องรีไซเคิลน้ำที่เสียรั่วอีกแล้วตอนกลางคืน"), GameUI.CRed);
            }
            foreach (var e in night)
                if (e.kind == LifeLogKind.GuestLeftHungry)
                {
                    _life.ApplyTask(new Stock { power = -Mathf.Min(1f, _life.State.stock.power) });
                    _ui.Toast(_loc.T("base.guest.sabotage", LbName(e.id)), GameUI.CRed);
                }
        }
    }
}
