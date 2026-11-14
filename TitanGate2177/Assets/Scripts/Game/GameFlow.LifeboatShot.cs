// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is Chapter 2 more fun as a read-and-choose survival diary (visitors, a pet, shared rations)
//           than as a walk-and-click outpost?
// Date: 2026-11-11
using System.Collections;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Verification mode for the survival diary: run the player with "-autoshot -lifeboat". It plays the whole
    /// week three times through the real buttons (rescued with three stars, rescued the hard way, stranded),
    /// saves the screens to Captures/lifeboat/, logs every check, then quits.
    /// </summary>
    public partial class GameFlow
    {
        IEnumerator AutoLifeboat()
        {
            ForceDiary = true;   // the screenshot pass checks the diary screens
            _loc.Current = Lang.TH;
            yield return new WaitForSeconds(2.2f);
            BuildMainMenu();
            yield return Shot("00_menu_th");
            Check(_ui.Press(_loc.T("menu.lifeboat")), "main menu opens the survival diary");
            yield return new WaitForSeconds(0.8f);
            yield return Shot("01_pick_th");
            ToggleLang(); LbShow();
            yield return Shot("01_pick_en");
            ToggleLang(); LbShow();

            // run A: the careful pet warns about Mu and is ignored; everything else is done well
            Check(_ui.Press(_loc.T("life.pick.choose", _loc.T("pet.luna.name"))), "picked the careful pet");
            var planA = new Dictionary<string, string>
            {
                ["filter"] = "swap", ["visitor_tik"] = "trade", ["crate"] = "walk", ["visitor_mu"] = "admit", ["locker"] = "open",
                ["recycler"] = "repair", ["storm_warning"] = "water_wall", ["visitor_zee"] = "buy_food", ["storm_power"] = "safe_mode",
                ["visitor_nok"] = "water", ["far_crate"] = "walk", ["beacon"] = "nok_part", ["coords"] = "send", ["log"] = "friends",
            };
            var scansA = new Dictionary<string, string[]>
            {
                ["visitor_tik"] = new[] { "thermal", "breath" }, ["visitor_mu"] = new[] { "thermal" },
                ["visitor_shade"] = new[] { "thermal", "breath" }, ["visitor_ree"] = new[] { "breath" }, ["visitor_mimic"] = new[] { "thermal", "breath" },
            };
            planA["visitor_shade"] = "refuse"; planA["visitor_ree"] = "trade"; planA["visitor_mimic"] = "refuse";
            yield return AutoLifeboatRun("a", planA, true, scansA);
            var a = _life.State;
            Check(a.outcome == LifeOutcome.Rescued && a.day == LC.daysTotal, "run A: rescued at the end of day 7");
            Check(a.log.Exists(e => e.kind == LifeLogKind.Theft && e.id == "mu" && e.detail != null), "run A: Mu stole food and an item from the open locker");
            Check(a.log.FindAll(e => e.kind == LifeLogKind.NightSafe).Count == 2, "run A: the storm found the shelter ready and the electronics in safe mode");
            var starsA = _life.Stars();
            Check(starsA[0] && starsA[1] && starsA[2], "run A: three stars");
            yield return Shot("a_end_th");
            ToggleLang(); LbShow();
            yield return Shot("a_end_en");
            ToggleLang(); LbShow();

            // run B: the brave pet does the outside jobs; nothing is prepared in time, the beacon waits for the last day
            Check(_ui.Press(_loc.T("life.end.again")), "play again restarts the diary");
            yield return new WaitForSeconds(0.8f);
            Check(_ui.Press(_loc.T("life.pick.choose", _loc.T("pet.comet.name"))), "picked the brave pet");
            var planB = new Dictionary<string, string>
            {
                ["filter"] = "wash", ["visitor_tik"] = "trade", ["crate"] = "send_pet", ["visitor_mu"] = "admit", ["locker"] = "open",
                ["recycler"] = "repair", ["storm_warning"] = "later", ["visitor_zee"] = "buy_o2", ["storm_last"] = "send_pet", ["storm_power"] = "leave_on",
                ["visitor_nok"] = "admit", ["far_crate"] = "send_pet", ["beacon"] = "wait", ["beacon_last"] = "climb", ["coords_last"] = "crank", ["log"] = "air",
            };
            planB["visitor_shade"] = "refuse"; planB["visitor_ree"] = "refuse"; planB["visitor_mimic"] = "refuse";
            yield return AutoLifeboatRun("b", planB, false);
            var b = _life.State;
            Check(b.outcome == LifeOutcome.Rescued, "run B: rescued on the last possible day");
            Check(b.log.Exists(e => e.kind == LifeLogKind.TheftStopped && e.detail == "pet"), "run B: the brave pet stopped the theft");
            Check(b.log.Exists(e => e.kind == LifeLogKind.NightHit && e.id == "storm_power"), "run B: the storm damaged the battery that was left on");
            Check(b.health < LC.health && !_life.Stars()[1], "run B: the hand crank cost health, so no health star");
            yield return Shot("b_end_th");

            // run C: nothing is fixed and nobody is helped
            Check(_ui.Press(_loc.T("life.end.again")), "play again restarts the diary");
            yield return new WaitForSeconds(0.8f);
            Check(_ui.Press(_loc.T("life.pick.choose", _loc.T("pet.pixel.name"))), "picked the smart pet");
            var planC = new Dictionary<string, string>
            {
                ["filter"] = "ignore", ["visitor_tik"] = "refuse", ["crate"] = "leave", ["visitor_mu"] = "gift", ["locker"] = "open",
                ["recycler"] = "skip", ["storm_warning"] = "later", ["visitor_zee"] = "refuse", ["storm_last"] = "hide", ["storm_power"] = "leave_on",
                ["visitor_nok"] = "refuse", ["mu_returns"] = "accept", ["far_crate"] = "leave", ["beacon"] = "wait", ["beacon_last"] = "give_up", ["log"] = "water",
            };
            planC["visitor_shade"] = "refuse"; planC["visitor_ree"] = "refuse"; planC["visitor_mimic"] = "refuse";
            yield return AutoLifeboatRun("c", planC, false);
            var c = _life.State;
            Check(c.outcome == LifeOutcome.Stranded && _life.StrandedReason == "no_beacon", "run C: alive but stranded, because the beacon was never repaired");
            Check(c.log.Exists(e => e.kind == LifeLogKind.NightHit && e.id == "storm_crew"), "run C: the storm hurt the unsheltered cadet");
            Check(c.log.Exists(e => e.kind == LifeLogKind.Choice && e.id == "mu_returns"), "run C: Mu came back because it was fed at the hatch");
            yield return Shot("c_end_th");
            ToggleLang(); LbShow();
            yield return Shot("c_end_en");
            ToggleLang(); LbShow();

            // run D: one scan that happens to read normal, and the stranger is let in
            Check(_ui.Press(_loc.T("life.end.again")), "play again restarts the diary");
            yield return new WaitForSeconds(0.8f);
            Check(_ui.Press(_loc.T("life.pick.choose", _loc.T("pet.pixel.name"))), "picked the smart pet");
            var planD = new Dictionary<string, string>
            {
                ["filter"] = "wash", ["visitor_tik"] = "refuse", ["crate"] = "walk", ["visitor_mu"] = "refuse", ["locker"] = "open",
                ["recycler"] = "repair", ["storm_warning"] = "water_wall", ["visitor_zee"] = "refuse", ["storm_power"] = "safe_mode", ["visitor_shade"] = "admit",
            };
            yield return AutoLifeboatRun("d", planD, false, new Dictionary<string, string[]> { ["visitor_shade"] = new[] { "thermal" } });
            var d = _life.State;
            Check(d.outcome == LifeOutcome.FailedMonster && d.monsterLetIn == "shade" && d.day == 4, "run D: letting the monster in ends the run at once");
            yield return Shot("d_end_th");
            ToggleLang(); LbShow();
            yield return Shot("d_end_en");

            Log($"done errors={_errors} checksFailed={_checksFailed}");
            _autoLog?.Close();
            _autoLog = null;
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        /// <summary>After the real landing: the diary starts with what the scramble packed and the pet picked before it.</summary>
        IEnumerator AutoCh1LifeboatTail()
        {
            ForceDiary = true;   // the screenshot pass checks the diary screens
            float t = 0;
            while ((_life == null || _lbView != LbView.Card) && t < 30) { t += Time.deltaTime; yield return null; }
            _autoAdvance = false;
            Check(_life != null && _lbView == LbView.Card, "the landing leads into the survival diary");
            if (_life != null)
            {
                var st = _life.State;
                Check(st.pet == "luna", "the pet picked before the scramble is aboard");
                Check(st.items.Contains("med_kit"), "the stowed first aid kit is in the locker");
                Check(st.stock.food == LC.start.food + 3 && st.stock.o2 == LC.start.o2 + 3, "stowed food and oxygen were added to the stock");
                Check(Settings.SaveRoute == "lifeboat" && Settings.SavePet == "luna", "checkpoint saved for Continue");
                yield return Shot("08_diary_day1_th");
            }
            Log($"done errors={_errors} checksFailed={_checksFailed}");
            _autoLog?.Close();
            _autoLog = null;
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        /// <summary>Card screens that a short run still photographs: one of each new kind of screen.</summary>
        static readonly HashSet<string> LbKeyShots = new HashSet<string> { "crate", "storm_last", "visitor_nok", "beacon_last", "coords_last", "mu_returns", "visitor_shade" };

        /// <summary>Play until the end screen, answering each card as planned. everyScreen = photograph every card and night.</summary>
        IEnumerator AutoLifeboatRun(string tag, Dictionary<string, string> plan, bool everyScreen, Dictionary<string, string[]> scans = null)
        {
            int guard = 0;
            while (_lbView != LbView.End && guard++ < 120)
            {
                int day = _life.State.day;
                switch (_lbView)
                {
                    case LbView.Card:
                    {
                        string id = _lbCard.id;
                        if (everyScreen || LbKeyShots.Contains(id)) yield return Shot($"{tag}_d{day}_{id}_th");
                        if (scans != null && scans.TryGetValue(id, out var want))
                        {
                            foreach (var sc in want) Check(_ui.Press(_loc.T($"life.scan.{sc}.name")), $"run {tag} day {day}: scanned {_lbCard.visitor} with {sc}");
                            yield return Shot($"{tag}_d{day}_{id}_scanned_th");
                        }
                        if (everyScreen && (id == "visitor_mu" || id == "storm_warning" || id == "beacon"))
                        {
                            ToggleLang(); LbShow();
                            yield return Shot($"{tag}_d{day}_{id}_en");
                            ToggleLang(); LbShow();
                        }
                        Check(plan.ContainsKey(id) && _ui.Press(_loc.T($"life.card.{id}.{plan[id]}")), $"run {tag} day {day}: {id} -> {(plan.ContainsKey(id) ? plan[id] : "?")}");
                        break;
                    }
                    case LbView.Result:
                        if (everyScreen && (_lbCard.id == "visitor_tik" || _lbCard.id == "beacon" || _lbCard.id == "log")) yield return Shot($"{tag}_d{day}_{_lbCard.id}_result_th");
                        _ui.Press(_loc.T("life.continue"));
                        break;
                    case LbView.Ration:
                    {
                        // a careful evening: run the oxygen maker only when the air will not last, and keep power for the beacon
                        int left = LC.LastDay - day + 1;
                        float keep = _life.State.flags.Contains(LC.rescueFlag) ? 0 : 1;
                        bool run = _life.CanRunMaker && _life.State.stock.o2 < _life.O2Tonight * left && _life.State.stock.power - LC.o2Maker.power >= keep;
                        if (run != _lbMaker) { _lbMaker = run; LbShow(); }
                        if (everyScreen || run) yield return Shot($"{tag}_d{day}_ration_th");
                        if (everyScreen && day == 1)
                        {
                            // show what skipping looks like, then put the share back
                            _lbFed.Remove(LifeboatSim.Me); LbShow();
                            yield return Shot($"{tag}_d{day}_ration_skip_th");
                            _lbFed.Add(LifeboatSim.Me); LbShow();
                        }
                        Check(_ui.Press(_loc.T("life.ration.confirm")), $"run {tag} day {day}: went to sleep" + (run ? " with the oxygen maker on" : ""));
                        break;
                    }
                    case LbView.Night:
                        if (everyScreen || _lbNightDay == 2 || _lbNightDay == 4) yield return Shot($"{tag}_d{_lbNightDay}_night_th");
                        if (everyScreen && _lbNightDay == 4)
                        {
                            ToggleLang(); LbShow();
                            yield return Shot($"{tag}_d{_lbNightDay}_night_en");
                            ToggleLang(); LbShow();
                        }
                        _ui.Press(_loc.T("life.continue"));
                        break;
                }
                yield return new WaitForSeconds(0.12f);
            }
            Check(_lbView == LbView.End, $"run {tag} reached the end screen");
        }
    }
}
