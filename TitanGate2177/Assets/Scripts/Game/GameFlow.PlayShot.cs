// PROTOTYPE - NOT FOR PRODUCTION
// Question: What does a full run feel like with the real clock and random emergencies on?
// Date: 2026-11-13
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TitanGate.Core;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Playtest pass (-autoshot -play): a simple player brain plays the whole game with the day clock and random
    /// emergencies switched on, like a person would: intro, morning jobs, scramble, landing, then the week in the
    /// base (events, jobs, salvage, emergencies, dinner). It logs timings and stock per day and takes regular
    /// screenshots, so a human can review pacing and problems. It does not judge fun; it records what happened.
    /// </summary>
    public partial class GameFlow
    {
        bool _playtest, _playDumb, _playFast;
        float _playClock;
        readonly StringBuilder _playNotes = new StringBuilder();

        void PNote(string s)
        {
            string line = $"[{_playClock,6:0.0}s] {s}";
            _playNotes.AppendLine(line);
            Log(line);
        }

        IEnumerator AutoPlay()
        {
            _playtest = true;
            _autoClock = true;
            var args = System.Environment.GetCommandLineArgs();
            _playDumb = System.Array.IndexOf(args, "-dumb") >= 0;
            if (System.Array.IndexOf(args, "-classic") >= 0) ForceClassicBase = true;   // the old 7-day base
            _playFast = System.Array.IndexOf(args, "-fast") >= 0;
            if (_playFast) Time.timeScale = 3f;
            PNote(_playDumb ? "player brain: careless (ignores repairs, lets everyone in)" : "player brain: careful");
            _loc.Current = Lang.TH;
            _autoAdvance = true;
            StartCoroutine(PlayTimer());
            yield return new WaitForSeconds(2f);
            PNote("start mission from the menu");
            StartCoroutine(StartChapter1());
            // watch the intro once, photograph a few moments, skip nothing
            float t = 0; int n = 0;
            while (!_lbPickForScramble && t < 120)
            {
                t += Time.deltaTime;
                if (InCutscene && t > 4 + n * 9 && n < 5) { n++; yield return Shot($"p0{n}_intro"); }
                yield return null;
            }
            PNote($"intro took {t:0}s");
            yield return Shot("p06_pet_pick");
            _lbScramblePet = "comet"; _lbPickForScramble = false; _ui.HideScreen();
            t = 0; while ((_bRoot == null || _bMode != 1 || _busy) && t < 30) { t += Time.deltaTime; yield return null; }
            float shipStart = _playClock;
            int guard = 0;
            while (_sTarget != null && guard++ < 12)
            {
                string target = _sTarget;
                yield return AutoWalkUse(target);
                if (target == "sofa") break;
            }
            PNote($"morning jobs took {_playClock - shipStart:0}s");
            yield return Shot("p07_ship_done");
            t = 0; while (!_sScramble && t < 60) { t += Time.deltaTime; yield return null; }
            t = 0; while (_busy && t < 30) { t += Time.deltaTime; yield return null; }
            float scr = _playClock;
            // grab whatever is closest, three at a time, then run to the pod
            while (!_sDone && _playClock - scr < 70)
            {
                BSpot best = null; float bd = float.MaxValue;
                foreach (var s in _bSpots)
                    if (s.id.StartsWith("sup_") && s.sr.enabled) { float d = Vector2.Distance(s.stand, _bPos); if (d < bd) { bd = d; best = s; } }
                if (best == null || _sHands.Count >= HandSlots) yield return AutoWalkUse("lifeboat");
                else yield return AutoWalkUse(best.id);
                if (_sTiltT > 3f) yield return Shot("p08_tilt");
            }
            PNote($"scramble: stowed {_stowed.Count}/{StowSlots} in {_playClock - scr:0}s, fire phases {_sPhaseFire}, tilted {_sTilted}");
            t = 0; int ln = 0;
            while ((_life == null || _bMode != 0) && t < 120)
            {
                t += Time.deltaTime;
                if (InCutscene && t > 3 + ln * 8 && ln < 4) { ln++; yield return Shot($"p09_land{ln}"); }
                yield return null;
            }
            PNote($"landing + docking cutscenes took {t:0}s");
            yield return new WaitForSeconds(1f);
            yield return PlayWeek();
            Log(_playNotes.ToString());
            Log($"done errors={_errors} checksFailed={_checksFailed}");
            _autoLog?.Close(); _autoLog = null;
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        IEnumerator PlayTimer() { while (true) { _playClock += Time.unscaledDeltaTime; yield return null; } }

        IEnumerator PlayWeek()
        {
            int lastDay = -1; float dayStart = 0, lastShot = 0; int shotN = 0;
            int guard = 0;
            while (_life.State.outcome == LifeOutcome.Ongoing && guard++ < 8000 && _playClock < 3000)
            {
                if (_playFast && Time.timeScale != 3f) Time.timeScale = 3f;   // StartLifeboat resets the time scale
                var st = _life.State;
                if (st.day != lastDay)
                {
                    if (lastDay > 0) PNote($"day {lastDay} lasted {_playClock - dayStart:0}s real time");
                    lastDay = st.day; dayStart = _playClock;
                    PNote($"DAY {st.day} 06:00 · O2 {st.stock.o2} water {st.stock.water} food {st.stock.food} power {st.stock.power} · health {st.health} morale {st.morale} · guests [{string.Join(",", st.guests)}] · seeds {_bSeeds}");
                }
                if (_playClock - lastShot > 25) { lastShot = _playClock; shotN++; yield return Shot($"q{st.day}_{shotN:00}"); }

                // emergencies first
                if (_playDumb && _emType != null) { if (_emType == "flare" && _emTimer > 20) PNote($"emergency {_emType}: ignored"); }
                else if (_emType == "flare") { PNote("solar storm alert: running to the storage room"); yield return AutoWalkUse("shelf_o2"); float w = 0; while (_emType != null && w < 30) { w += Time.deltaTime; yield return null; } continue; }
                else if ((_emType == "power" || _emType == "leak") && st.schedule.Hour < DinnerHour) { PNote($"emergency {_emType}: going to fix it at {st.schedule.Hour:0.0}h"); yield return PlayDoJob(Jobs["em_" + _emType]); float w = 0; while (_emType != null && w < 10) { w += Time.deltaTime; yield return null; } if (_emType != null) PNote("emergency still open"); continue; }

                if (_ui.ScreenOpen && _lbView == LbView.Result) { yield return new WaitForSeconds(0.6f); LbNextCard(); continue; }
                if (_ui.ScreenOpen && _lbView == LbView.Night) { yield return Shot($"q{st.day}_night"); yield return new WaitForSeconds(0.6f); LbNextCard(); continue; }
                if (_lbView == LbView.End) break;
                if (_lbView == LbView.Ration && (_bOpen || _bGather))
                {
                    float w = 0; while (!_ui.ScreenOpen && w < 6) { w += Time.deltaTime; yield return null; }
                    foreach (var who in _life.Mouths()) if (!_lbFed.Contains(who) && MealsLeft() > 0) MealFeed(who, true);
                    _lbMaker = !_playDumb && st.stock.o2 < 7 && _life.CanRunMaker && _life.PowerTonightWith(true);
                    yield return Shot($"q{st.day}_meal");
                    PNote($"dinner at {st.schedule.Hour:0.0}h · fed {_lbFed.Count}/{_life.Mouths().Count} · air maker {_lbMaker} · O2 tonight {_life.O2TonightWith(_lbMaker):0.#}");
                    LbSleep();
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
                if (_ui.Blocking) { yield return null; continue; }

                // the abandoned-base chapter: restore tasks, needs, storm, radio (GameFlow.PlayRestore.cs)
                if (RestoreMode && _restore != null && !_bOpen && !_bGather && (_lbView == LbView.Ration || _lbView == LbView.Card && BActiveSpot == null))
                {
                    bool did = false;
                    yield return PlayRestoreStep(x => did = x);
                    if (did) continue;
                    float h0 = _restore.Hour;
                    if (!_playDumb) { yield return PlayJobs(); RestoreSync(); if (_restore.Hour > h0 + 0.01f) continue; }
                    if (_lbView == LbView.Ration)
                    {
                        PNote($"day {_restore.Day}: nothing more fits at {_restore.Hour:0.0}h, dinner · restored {_restore.Percent:0}% · water {_restore.Water:0} food {_restore.Food:0} rest {_restore.Rest:0}");
                        yield return AutoWalkUse("seats");
                        float w = 0; while (!_bGather && w < 4) { w += Time.deltaTime; yield return null; }
                        continue;
                    }
                    PNote($"waiting for the visitor at {_restore.Hour:0.0}h");
                    if (!_restore.Nap(Mathf.Max(0.25f, VisitorHour - _restore.Hour))) _restore.Spend(Mathf.Max(0.25f, VisitorHour - _restore.Hour));
                    RestoreSync();
                    continue;
                }

                string active = BActiveSpot;
                if (_lbView == LbView.Card && active != null && _lbCard != null && CardJob != null)
                {
                    var job = CardJob; string cid = _lbCard.id; float hr = st.schedule.Hour;
                    if (_playDumb)
                    {
                        var last = _lbCard.options[_lbCard.options.Count - 1];
                        PNote($"{hr:00.0}h job {cid}: left broken");
                        _bOpen = true; JobGiveUp(last);
                    }
                    else yield return PlayDoJob(job);
                    yield return new WaitForSeconds(0.6f);
                    if (_lbCard != null && _lbCard.id == cid && CardJob == job) { PNote($"job {cid} still open, giving up"); _ui.HideScreen(); _bOpen = true; JobGiveUp(_lbCard.options[_lbCard.options.Count - 1]); }
                    continue;
                }
                if (_lbView == LbView.Card && active != null && _lbCard != null)
                {
                    string card = _lbCard.id; float hour = st.schedule.Hour;
                    yield return AutoWalkUse(active);
                    if (!_bOpen) BOpenEvent();
                    yield return new WaitForSeconds(0.5f);
                    if (!string.IsNullOrEmpty(_lbCard.visitor))
                    {
                        _peepTalk = 1;
                        if (_life.CanScan(_lbCard, "thermal")) _life.Scan(_lbCard.id, "thermal");
                        if (_life.CanScan(_lbCard, "breath")) _life.Scan(_lbCard.id, "breath");
                        BShowPeep(); yield return new WaitForSeconds(0.4f);
                        yield return Shot($"q{st.day}_peep_{_lbCard.visitor}");
                        if (IsTrader(_lbCard.visitor)) { BShowTrade(); yield return new WaitForSeconds(0.4f); yield return Shot($"q{st.day}_trade"); BShowPeep(); }
                    }
                    var pick = PlayPick(_lbCard);
                    PNote($"{hour:00.0}h event {card} ({active}) → {pick?.id}");
                    if (pick != null) LbChoose(pick);
                    yield return new WaitForSeconds(0.6f);
                    continue;
                }
                if (_lbView == LbView.Ration && !_bOpen)
                {
                    // free time before dinner: jobs, then salvage outside, then go to the pod
                    if (!_playDumb) yield return PlayJobs();
                    if (st.schedule.Hour >= DinnerHour || st.schedule.Remaining < 3)
                    {
                        PNote($"going to dinner at {st.schedule.Hour:0.0}h");
                        if (!_bGather) yield return AutoWalkUse("seats");
                        float w = 0; while (!_bGather && w < 4) { w += Time.deltaTime; yield return null; }
                    }
                    else yield return new WaitForSeconds(1f);   // waiting for the evening
                    continue;
                }
                yield return null;
            }
            PNote($"END outcome {_life.State.outcome} on day {_life.State.day} · health {_life.State.health} · O2 {_life.State.stock.o2} water {_life.State.stock.water} food {_life.State.stock.food} power {_life.State.stock.power}");
            yield return new WaitForSeconds(1f);
            yield return Shot("q_end");
        }

        readonly HashSet<string> _playJobDone = new HashSet<string>();

        IEnumerator PlayJobs()
        {
            var st = _life.State;
            string key = st.day + ":";
            if (st.stock.water < 4 && st.stock.power > 1.5f && _playJobDone.Add(key + "drill")) { PNote($"{st.schedule.Hour:0.0}h drill ice (water low)"); yield return AutoWalkUse("drill"); yield break; }
            // run the oxygen maker by day when air is getting low and the battery can spare it
            if (st.stock.o2 < 9 && _life.CanRunMaker && st.stock.power - LC.o2Maker.power + LC.solarPerDay >= LC.lifeSupportPower + 1 && !_bDone.Contains("moxie") && _playJobDone.Add(key + "moxie"))
            {
                PNote($"{st.schedule.Hour:0.0}h run the oxygen maker (O2 {st.stock.o2}, power {st.stock.power})");
                yield return AutoWalkUse("air");
                yield return new WaitForSeconds(0.4f);
                if (_shotN++ % 2 == 0) yield return Shot($"q{st.day}_moxie");
                if (_ui.ScreenOpen) { _ui.HideScreen(); yield return Act("repair", 1.2f, () => { if (_bDone.Add("moxie")) _life.ApplyTask(new Stock { power = -LC.o2Maker.power, o2 = LC.o2Maker.o2 }, 1f); }); }
                yield break;
            }
            // one project a day from the board, cheapest useful first
            if (st.stock.water >= 5 && st.stock.power >= 3 && !_playJobDone.Contains(key + "proj"))
            foreach (var p in Projects)
            {
                if (ProjectBlock(p) != null) continue;
                if (p.cost.water > 0 && st.stock.water < p.cost.water + 4) continue;
                if (p.cost.power > 0 && st.stock.power < p.cost.power + 2.5f) continue;
                _playJobDone.Add(key + "proj");
                PNote($"{st.schedule.Hour:0.0}h project {p.id}");
                yield return AutoWalkUse("board");
                yield return new WaitForSeconds(0.4f);
                yield return Shot($"q{st.day}_projects");
                ProjectStart(p);
                float t = 0; while (_pzRoot == null && t < 3) { t += Time.deltaTime; yield return null; }
                t = 0; while (_pzRoot != null && t < 10) { t += Time.unscaledDeltaTime; yield return null; }
                yield return new WaitForSecondsRealtime(1f);
                yield break;
            }
            for (int i = 0; i < 3; i++)
            {
                if (_bPlot[i] == 3 && _playJobDone.Add(key + "harvest" + i)) { PNote($"{st.schedule.Hour:0.0}h harvest plot {i}"); yield return AutoWalkUse("plot" + i); yield break; }
                if (_bPlot[i] == 0 && _bSeeds > 0 && st.stock.water > 3 && _playJobDone.Add(key + "plant" + i)) { PNote($"{st.schedule.Hour:0.0}h plant plot {i}"); yield return AutoWalkUse("plot" + i); yield break; }
            }
            if (_bPanelsDusty && st.stock.o2 > 3 && _playJobDone.Add(key + "solar")) { PNote($"{st.schedule.Hour:0.0}h clean solar panels"); yield return AutoWalkUse("solar"); yield break; }
            if (st.stock.water < 6 && st.stock.power > 1.5f && _playJobDone.Add(key + "drill")) { PNote($"{st.schedule.Hour:0.0}h drill ice"); yield return AutoWalkUse("drill"); yield break; }
            for (int i = 0; i < SalvageSpots.Length; i++)
            {
                var s = BSpotById("salvage" + i);
                if (s != null && s.sr.enabled && _playJobDone.Add(key + "salvage" + i)) { PNote($"{st.schedule.Hour:0.0}h search salvage {i}"); yield return AutoWalkUse("salvage" + i); yield break; }
            }
            if (!_bWreckSearched && _playJobDone.Add(key + "wreck")) { PNote($"{st.schedule.Hour:0.0}h search the rover"); yield return AutoWalkUse("wreck"); yield break; }
        }

        /// <summary>
        /// A careful player doing a repair job: choose a way that can work, fetch its part (toolbox or water
        /// shelf), call a friend over if there is one, walk to the job, open it and solve the panel.
        /// </summary>
        IEnumerator PlayDoJob(JobDef d)
        {
            bool em = d.id.StartsWith("em_");
            JobWay pick = null;
            foreach (var w in d.ways)
            {
                if (w.flag != null && !_life.State.flags.Contains(w.flag)) continue;
                if (!em && w.option != null && _lbCard.options.Find(o => o.id == w.option) == null) continue;
                if (w.guest != null) { if (WayGuest(w) != null && _jobDelegate == null) { pick = w; break; } continue; }
                var opt = WayOption(d, w);
                if (opt != null && !_life.CanChoose(opt, out _)) continue;
                if (w.item == null || BagHas(w.item) || w.item == "water_bag" && _life.State.stock.water >= 1 || StoreCount(w.item) > 0) { pick = w; break; }
            }
            if (pick == null)
            {
                PNote($"job {d.id}: no way to fix it");
                if (!em) { _bOpen = true; JobGiveUp(_lbCard.options[_lbCard.options.Count - 1]); }
                yield break;
            }
            if (pick.item != null && !BagHas(pick.item))
            {
                if (_bag.Count >= BagSlots) BagReturn(_bag[0]);
                PNote($"{_life.State.schedule.Hour:0.0}h fetch {pick.item} for {d.id}");
                if (pick.item == "water_bag") yield return AutoWalkUse("shelf_water");
                else
                {
                    yield return AutoWalkUse("toolbox");
                    yield return new WaitForSeconds(0.3f);
                    if (_ui.ScreenOpen && _shotN++ % 3 == 0) yield return Shot($"q{_life.State.day}_toolbox");
                    BagTake(pick.item); _ui.HideScreen();
                }
            }
            var spot = BSpotById(d.spot);
            if (spot != null && _life.State.guests.Count > 0 && _crewCall.Count == 0 && pick.guest == null) CrewCall(_life.State.guests[0], spot.stand);
            if (spot != null && _crewCall.Count > 0) { float w = 0; while (JobHelpers(d.spot) == 0 && w < 8) { w += Time.deltaTime; yield return null; } }
            yield return AutoWalkUse(d.spot);
            yield return new WaitForSeconds(0.4f);
            if (!_ui.ScreenOpen) JobShow(d);
            yield return new WaitForSeconds(0.3f);
            yield return Shot($"q{_life.State.day}_job_{d.id}");
            PNote($"{_life.State.schedule.Hour:0.0}h job {d.id}: way {pick.option ?? pick.item ?? pick.guest}, helpers {JobHelpers(d.spot)}");
            JobStart(d, pick);
            if (pick.guest != null) { float w = 0; while (_jobDelegate != null && w < 40) { w += Time.deltaTime; yield return null; } yield break; }
            float t = 0; while (_pzRoot == null && t < 3) { t += Time.deltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.8f);
            if (_pzRoot != null) yield return Shot($"q{_life.State.day}_panel_{pick.puzzle}");
            t = 0; while (_pzRoot != null && t < 10) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(1.2f);
        }

        int _shotN;

        /// <summary>A sensible player: refuse visitors whose scans look wrong, admit friendly ones while food lasts, else the first choice that can be paid.</summary>
        LifeOption PlayPick(LifeCard card)
        {
            if (_playDumb) { foreach (var o in card.options) if (_life.CanChoose(o, out _)) return o; return null; }
            bool odd = false;
            foreach (var sc in _life.ScansDone(card)) odd |= _life.IsOdd(card.visitor, sc);
            foreach (var o in card.options)
            {
                if (!_life.CanChoose(o, out _)) continue;
                if (!string.IsNullOrEmpty(card.visitor))
                {
                    if (odd) { if (o.id == "refuse") return o; continue; }
                    if (o.id == "admit" && _life.State.stock.food >= 5 && _life.State.guests.Count < 2) return o;
                    if (o.id != "admit" && o.id != "refuse") return o;
                    continue;
                }
                return o;
            }
            foreach (var o in card.options) if (_life.CanChoose(o, out _)) return o;
            return null;
        }
    }
}
