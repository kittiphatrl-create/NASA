// PROTOTYPE - NOT FOR PRODUCTION
// Question: Can a careful player restore the base and send the call in about 20 minutes, while a careless one loses?
// Date: 2026-11-14
using System;
using System.Collections;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Player brains for the abandoned-base chapter (-autoshot -ship -play [-dumb]). Careful: keeps itself fed and
    /// watered, prepares for the storm, fixes the air and water rooms first, finds parts before it needs them, and
    /// sends the call at a relay pass. Careless: does random tasks, never drinks or naps, ignores warnings.
    /// </summary>
    public partial class GameFlow
    {
        static readonly string[] RoomPriority = { "power", "air", "storage", "greenhouse", "quarters", "comms" };
        readonly HashSet<string> _playSkip = new HashSet<string>();   // tasks whose spot the bot could not reach (today)
        int _playSkipDay = -1;

        /// <summary>One step of the restore brain. done(true) when it did something that took game time or a walk.</summary>
        IEnumerator PlayRestoreStep(Action<bool> done)
        {
            var st = _life.State; var r = _restore;
            float hr = r.Hour;
            if (_playSkipDay != r.Day) { _playSkipDay = r.Day; _playSkip.Clear(); }
            // the ship lands the morning after the signal
            if (_rPickup)
            {
                PNote($"day {r.Day}: the rescue ship landed, saying goodbye");
                yield return AutoWalkUse("hatch_out");
                if (!_ui.ScreenOpen) yield return AutoWalkUse("hatch");
                yield return new WaitForSeconds(0.4f);
                yield return Shot("r_goodbye");
                _ui.HideScreen();
                bool fin = false;
                RestoreRescueScene(() => { st.outcome = LifeOutcome.Rescued; _lbView = LbView.End; LbShow(); fin = true; });
                yield return new WaitForSecondsRealtime(4f); yield return Shot("r_rescue_scene");
                float wt = 0; while (!fin && wt < 15) { wt += Time.unscaledDeltaTime; yield return null; }
                yield return new WaitForSecondsRealtime(0.6f); yield return Shot("r_summary");
                done(true); yield break;
            }
            if (!_playDumb)
            {
                // body first
                if (r.Water < 30 && st.stock.water >= 0.25f && r.HoursLeft >= 0.25f) { PNote($"{hr:0.0}h drink (water {r.Water:0})"); yield return AutoWalkUse("shelf_water"); done(true); yield break; }
                if (r.Food < 30 && st.stock.food >= 1f && r.HoursLeft >= 0.25f) { PNote($"{hr:0.0}h snack (food {r.Food:0})"); yield return AutoWalkUse("shelf_food"); done(true); yield break; }
                if (r.Rest < 25 && r.HoursLeft >= 1.2f) { PNote($"{hr:0.0}h nap (rest {r.Rest:0})"); yield return AutoWalkUse("seats"); yield return WaitIdle(); done(true); yield break; }
                // storm day: safe mode, then shelter in the lounge until it passes
                if (r.Weather == "flare" && !_rFlareDone && r.StateOf("power") == RoomState.Restored)
                {
                    if (!_rSafeMode && hr < FlareHitHour - 0.6f) { PNote($"{hr:0.0}h storm day: safe mode at the breaker box"); yield return AutoWalkUse("breaker_box"); yield return WaitPuzzle(); done(true); yield break; }
                    if (hr >= FlareCallHour - 0.5f)
                    {
                        PNote($"{hr:0.0}h storm coming: sheltering in the lounge");
                        yield return AutoWalkUse("orbi");   // the lounge, without sitting down to dinner
                        _ui.HideScreen();
                        if (r.Hour < FlareHitHour) { r.Spend(FlareHitHour - r.Hour + 0.05f); RestoreSync(); }
                        yield return new WaitForSeconds(0.5f);
                        done(true); yield break;
                    }
                }
                if (r.CoolingBroken && r.HoursLeft >= 1f && !_playSkip.Contains("cooling"))
                {
                    PNote($"{hr:0.0}h fix the cooling loop");
                    yield return AutoWalkUse("panel"); yield return WaitPuzzle();
                    if (r.CoolingBroken) { PNote("cooling still broken after the attempt"); _playSkip.Add("cooling"); Check(false, "cooling loop fixed at the panel"); }
                    done(true); yield break;
                }
                // air: the oxygen maker once a day when it runs and power allows
                if (r.StateOf("air") == RoomState.Restored && st.stock.o2 < 9 && _life.CanRunMaker && !_bDone.Contains("moxie") && st.stock.power - LC.o2Maker.power + LC.solarPerDay >= LC.lifeSupportPower + 0.5f && r.HoursLeft >= 1f)
                {
                    PNote($"{hr:0.0}h oxygen maker (O2 {st.stock.o2})");
                    yield return AutoWalkUse("air");
                    if (_ui.ScreenOpen) { _ui.HideScreen(); if (_bDone.Add("moxie")) { _life.ApplyTask(new Stock { power = -LC.o2Maker.power, o2 = LC.o2Maker.o2 }); r.Spend(1f); RestoreSync(); } }
                    done(true); yield break;
                }
                // food: harvest ripe beds, plant empty ones once the greenhouse works
                if (r.StateOf("greenhouse") == RoomState.Restored)
                    for (int i = 0; i < 3; i++)
                    {
                        bool ripe = _bPlot[i] == 3, empty = _bPlot[i] == 0 && _bSeeds > 0 && st.stock.water > 2;
                        if ((ripe && r.HoursLeft >= 1.2f || empty && r.HoursLeft >= 2.2f) && _playJobDone.Add($"{r.Day}:bed{i}"))
                        {
                            PNote($"{hr:0.0}h {(ripe ? "harvest" : "plant")} bed {i}");
                            yield return AutoWalkUse("plot" + i); yield return WaitIdle();
                            done(true); yield break;
                        }
                    }
                // send the call when the radio works
                if (r.StateOf("comms") == RoomState.Restored && !r.Signalled)
                {
                    float next = r.NextRelayHour;
                    if (r.RelayOpen || next < 24 && next - hr <= r.HoursLeft - 0.3f)
                    {
                        if (!r.RelayOpen) { PNote($"{hr:0.0}h waiting by the radio for the relay pass at {next:0.0}"); r.Spend(next - hr); RestoreSync(); }
                        PNote($"{r.Hour:0.0}h sending the distress call");
                        yield return AutoWalkUse("radio");
                        yield return new WaitForSeconds(0.3f);
                        yield return Shot($"r{r.Day}_radio");
                        _ui.HideScreen();
                        RestoreSend();
                        done(true); yield break;
                    }
                }
            }
            // restoration: the highest-priority task that can be done now (find its part first if needed)
            string task = PickRestoreTask();
            if (task != null)
            {
                var t = TaskDef(task);
                string spot = t != null ? t.spot : ResetSpot(task.Substring(6));
                PNote($"{hr:0.0}h task {task} ({_restore.HoursFor(task):0.#} h) at {spot}");
                yield return AutoWalkUse(spot);
                var sp = BSpotById(spot);
                if (sp != null && Vector2.Distance(_bPos, sp.stand) > 1.6f)
                {
                    PNote($"UNREACHABLE spot {spot} for {task} (stood at {_bPos})");
                    Check(false, "restore spot reachable: " + spot);
                    _playSkip.Add(task);
                    done(true); yield break;
                }
                yield return new WaitForSeconds(0.2f);
                if (_ui.ScreenOpen && _shotN++ % 4 == 0) yield return Shot($"r{r.Day}_task_{task}");
                if (_ui.ScreenOpen) { RestoreStart(task); yield return WaitPuzzle(); }
                done(true); yield break;
            }
            done(false);
        }

        string PickRestoreTask()
        {
            var r = _restore; var bag = RestoreBag();
            var open = new List<string>(r.OpenTasks());
            open.RemoveAll(id => _playSkip.Contains(id));
            if (_playDumb)
            {
                // careless: whatever is doable, in random order, and only now and then
                if (UnityEngine.Random.value < 0.35f) return null;
                var ok = open.FindAll(id => r.CanDo(id, bag, out _));
                return ok.Count == 0 ? null : ok[UnityEngine.Random.Range(0, ok.Count)];
            }
            if (open.Contains("chip")) return "chip";
            foreach (var id in open) if (id.StartsWith("reset_") && r.CanDo(id, bag, out _)) return id;
            // the room order above; inside a room, any doable task; a missing part sends us to the junk that has it
            foreach (var room in RoomPriority)
                foreach (var id in open)
                {
                    var t = TaskDef(id);
                    if (t == null || t.room != room) continue;
                    if (r.CanDo(id, bag, out var why)) return id;
                    if (why == "restore.need.item")
                    {
                        var finder = open.Find(f => { var ft = TaskDef(f); return ft != null && !string.IsNullOrEmpty(ft.gives) && Array.IndexOf(ft.gives.Split(','), t.needItem) >= 0; });
                        if (finder != null && r.CanDo(finder, bag, out _)) return finder;
                    }
                }
            foreach (var id in open) if (r.CanDo(id, bag, out _)) return id;
            return null;
        }

        IEnumerator WaitPuzzle()
        {
            float t = 0; while (_pzRoot == null && t < 3) { t += Time.unscaledDeltaTime; yield return null; }
            t = 0; while (_pzRoot != null && t < 12) { t += Time.unscaledDeltaTime; yield return null; }
            yield return WaitIdle();
        }

        IEnumerator WaitIdle()
        {
            float t = 0; while ((ActionLocked || _busy) && t < 6) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.4f);
            if (_ui.ScreenOpen && _pzRoot == null && _lbView != LbView.Result && _lbView != LbView.Night) _ui.HideScreen();
        }
    }
}
