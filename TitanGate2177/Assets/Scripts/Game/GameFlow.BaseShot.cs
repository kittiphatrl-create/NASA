// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is the survival week more fun when the cadet walks around a top-down base and does each job?
// Date: 2026-11-12
using System.Collections;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>Screenshot pass for the walkable base (-autoshot -base): walks to every event through the real walking code.</summary>
    public partial class GameFlow
    {
        IEnumerator AutoBase()
        {
            _loc.Current = Lang.TH;
            yield return new WaitForSeconds(2f);
            StartLifeboat(null, "comet");
            yield return new WaitForSeconds(1.2f);
            Check(_bRoot != null && _bCam != null, "base built with its own camera");
            Check(!_busy, "walkable base releases the previous diary input lock");
            Check(_life != null && _lbView == LbView.Card && !_ui.ScreenOpen, "first card waits in the base (no diary screen)");
            yield return Shot("b01_start_th");
            float h0 = _life.State.schedule.Hour; _autoClock = true;
            yield return new WaitForSeconds(2.2f);
            _autoClock = false;
            Check(_life.State.schedule.Hour > h0 + 0.06f, "the day clock runs while you walk around");   // 2.2 s at 26 s per hour ≈ 0.085 h

            // hands-on jobs on day 1: plant, drill, walk outside
            yield return AutoWalkUse("plot0");
            Check(_bPlot[0] == 1, "planted plot 0");
            yield return AutoWalkUse("drill");
            Check(_bDone.Contains("drill"), "drilled ice");
            yield return Shot("b02_outside_drill");
            yield return AutoWalkUse("pet");
            Check(_ui.ModalOpen, "pet interaction opens command menu"); _ui.CloseModal();
            // walk straight away from the camera, then straight back: the back and front views while moving
            _bPos = new Vector2(22.5f, 8.2f); _bPetPos = _bPos + Vector2.left;
            _bPath = new List<Vector2> { new Vector2(22.5f, 10.8f) };
            yield return new WaitForSeconds(0.35f);
            yield return Shot("b02b_walk_away_back");
            _bPath = new List<Vector2> { new Vector2(24.5f, 8.2f) };
            yield return new WaitForSeconds(0.25f);
            yield return Shot("b02c_walk_toward_front");
            BSaveCadetSheet(System.IO.Path.Combine(_shotDir, "b00_cadet_frames.png"));

            var shots = new HashSet<string>();
            var visitorAdmit = new HashSet<string> { "tik", "nok", "ree", "aya" };
            int guard = 0;
            while (_life.State.outcome == LifeOutcome.Ongoing && guard++ < 90)
            {
                if (_lbView == LbView.Card && !_bOpen)
                {
                    string spot = BActiveSpot;
                    string card = _lbCard.id;
                    if (!shots.Contains("knock") && spot == "hatch") { shots.Add("knock"); yield return Shot("b03_knock"); }
                    if (!shots.Contains("alarm") && _bTimerAlarm) { shots.Add("alarm"); yield return new WaitForSeconds(0.6f); yield return Shot("b05_alarm"); }
                    yield return AutoWalkUse(spot);
                    Check(_bOpen && _ui.ScreenOpen && _lbView == LbView.Card, $"day {_life.State.day}: walked to {spot} and opened {card}");
                    if (!_bOpen) { BOpenEvent(); yield return null; }
                    if (!shots.Contains("peep") && spot == "hatch")
                    {
                        shots.Add("peep");
                        yield return new WaitForSeconds(0.5f);
                        yield return Shot("b04_peephole");
                        _peepTalk = 2; BShowPeep();
                        if (_life.CanScan(_lbCard, "thermal")) BRunScan("thermal", null, null, null, null, 0);
                        yield return new WaitForSeconds(0.75f);
                        yield return Shot("b04b_infrared_sweep");
                        yield return new WaitForSeconds(1.2f);
                        yield return Shot("b04c_evidence");
                        _peepTalk = 0; BPeepTalkVN(_lbCard); _peepTalk = 1;
                        yield return new WaitForSeconds(0.4f);
                        yield return Shot("b04e_talk_vn");
                        BShowPeep();
                        if (IsTrader(_lbCard.visitor))
                        {
                            BShowTrade();
                            yield return new WaitForSeconds(0.4f);
                            yield return Shot("b04d_trade");
                            float before = _life.State.stock.food;
                            _life.ApplyTask(new Stock { water = -1, food = 1 });
                            Check(_life.State.stock.food > before, "a trade moves stock in the simulation");
                            BShowPeep();
                        }
                    }
                    // pick: admit friendly visitors, otherwise the first option that can be taken
                    LifeOption pick = null;
                    foreach (var o in _lbCard.options)
                    {
                        if (!_life.CanChoose(o, out _)) continue;
                        if (!string.IsNullOrEmpty(_lbCard.visitor))
                        {
                            bool friend = visitorAdmit.Contains(_lbCard.visitor);
                            if (friend ? o.id == "admit" : o.id == "refuse") { pick = o; break; }
                            continue;
                        }
                        pick = o; break;
                    }
                    if (pick == null) foreach (var o in _lbCard.options) if (_life.CanChoose(o, out _)) { pick = o; break; }
                    LbChoose(pick);
                    yield return new WaitForSeconds(0.3f);
                    LbNextCard();
                    yield return new WaitForSeconds(0.3f);
                    if (!shots.Contains("crew") && _life.State.guests.Count > 0)
                    {
                        shots.Add("crew"); yield return AutoWalkUse("guest0");
                        Check(_ui.ScreenOpen, "admitted visitor can be approached and talked to (visual-novel view)");
                        yield return Shot("b05b_crew_conversation"); _ui.HideScreen();
                    }
                }
                else if (_lbView == LbView.Ration && !_bOpen)
                {
                    if (!shots.Contains("evening")) { shots.Add("evening"); yield return Shot("b06_evening"); }
                    yield return AutoWalkUse("seats");
                    if (!shots.Contains("pod")) { shots.Add("pod"); yield return new WaitForSeconds(1.2f); yield return Shot("b06a_crew_seated"); }
                    float gt = 0;
                    while (!_ui.ScreenOpen && gt < 6f) { gt += Time.deltaTime; yield return null; }
                    if (!shots.Contains("gather")) { shots.Add("gather"); yield return Shot("b06b_gather_pod"); }
                    Check(_ui.ScreenOpen && _lbView == LbView.Ration, $"day {_life.State.day}: ration opened at the bed");
                    if (!shots.Contains("ration"))
                    {
                        shots.Add("ration");
                        foreach (var who in _life.Mouths()) if (!_lbFed.Contains(who) && MealsLeft() > 0) MealFeed(who, true);
                        yield return new WaitForSeconds(0.3f);
                        yield return Shot("b07_meal");
                        Check(_mealCards.Count == _life.Mouths().Count, "meal scene shows everyone at the table");
                    }
                    foreach (var who in _life.Mouths()) if (!_lbFed.Contains(who) && MealsLeft() > 0) MealFeed(who, true);
                    LbSleep();
                    yield return new WaitForSeconds(0.3f);
                    if (!shots.Contains("night")) { shots.Add("night"); yield return Shot("b08_night"); }
                    if (_life.State.outcome != LifeOutcome.Ongoing) break;
                    LbNextCard();
                    yield return new WaitForSeconds(0.5f);
                    if (_life.State.day == 3 && !shots.Contains("day3"))
                    {
                        shots.Add("day3");
                        yield return AutoWalkUse("plot0");
                        Check(_bPlot[0] == 0, "harvested plot 0 on day 3");
                        yield return AutoWalkUse("solar");
                        yield return Shot("b09_day3_jobs");
                        yield return AutoWalkUse("shelf_food");
                        yield return Shot("b10_storage");
                        // emergencies: a power fault locks the power room until the breaker is reset
                        EmStart("power");
                        yield return new WaitForSeconds(0.6f);
                        yield return Shot("b12_red_alert");
                        Check(_bSolid[25, 6] && _bSolid[29, 2], "power fault locks the power room doors");
                        yield return new WaitForSeconds(3.2f);
                        yield return Shot("b13_power_room_locked");
                        yield return AutoWalkUse("toolbox");
                        Check(_ui.ScreenOpen, "the toolbox in storage opens");
                        yield return Shot("b13b_toolbox");
                        Check(BagTake("fuse") && BagHas("fuse"), "a fuse goes into the bag");
                        _ui.HideScreen();
                        JobShowBag(); yield return new WaitForSeconds(0.3f); yield return Shot("b13c_bag_tab"); _ui.HideScreen();
                        yield return AutoWalkUse("breaker");
                        Check(_ui.ScreenOpen, "the breaker opens its repair job");
                        yield return Shot("b13d_job_panel");
                        JobStart(Jobs["em_power"], Jobs["em_power"].ways[0]);
                        float et = 0; while (_pzRoot == null && et < 3f) { et += Time.deltaTime; yield return null; }
                        Check(_pzRoot != null, "the wires panel opens");
                        yield return new WaitForSecondsRealtime(0.6f);
                        yield return Shot("b13e_wires_panel");
                        et = 0; while (_emType != null && et < 8f) { et += Time.deltaTime; yield return null; }
                        Check(!BagHas("fuse"), "the fuse was used up");
                        Check(_emType == null && !_bSolid[25, 6], "breaker reset unlocks the power room");
                        // solar storm: reach the storm shelter in the storage room
                        int hp = _life.State.health;
                        EmStart("flare");
                        yield return AutoWalkUse("shelf_o2");
                        et = 0; while (_emType != null && et < 30f) { et += Time.deltaTime; yield return null; }
                        Check(_life.State.health == hp, "sheltering in storage avoids the solar storm");
                        BShowGuide();
                        yield return new WaitForSeconds(0.3f);
                        yield return Shot("b14_guide");
                        _ui.CloseModal();
                        // outside: search a pile and the old rover
                        float w0 = _life.State.stock.water + _life.State.stock.power + _life.State.stock.food + _bSeeds;
                        yield return AutoWalkUse("salvage1");
                        yield return AutoWalkUse("wreck");
                        Check(_bWreckSearched, "the old rover can be searched once");
                        yield return Shot("b15_outside_salvage");
                    }
                }
                else if (_lbView == LbView.End) break;
                else { LbNextCard(); yield return null; }
            }
            if (_lbView != LbView.End) { LbNextCard(); }
            yield return new WaitForSeconds(0.5f);
            Check(_lbView == LbView.End, "reached the end screen: " + _life.State.outcome);
            yield return Shot("b11_end");

            Log($"done errors={_errors} checksFailed={_checksFailed}");
            _autoLog?.Close();
            _autoLog = null;
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        /// <summary>Screenshot pass for the top-down Chapter 1 (-autoshot -ship): quests, impact, scramble, landing.</summary>
        IEnumerator AutoShip()
        {
            _loc.Current = Lang.TH;
            _autoAdvance = true;
            yield return new WaitForSeconds(2f);
            StartCoroutine(StartChapter1());
            float t = 0;
            while (!_lbPickForScramble && t < 40) { if (InCutscene) SkipCutscene(); t += Time.deltaTime; yield return null; }
            Check(_lbPickForScramble, "pet pick shown before the ship");
            yield return Shot("s00_pet_pick");
            _lbScramblePet = "pixel"; _lbPickForScramble = false; _ui.HideScreen();
            t = 0;
            while ((_bRoot == null || _bMode != 1 || _busy) && t < 20) { t += Time.deltaTime; yield return null; }
            Check(_bRoot != null && _bMode == 1, "top-down ship built");
            yield return new WaitForSeconds(0.6f);
            yield return Shot("s01_ship_start");
            int guard = 0;
            while (_sTarget != null && guard++ < 12)
            {
                string target = _sTarget;
                int step = _sStep;
                yield return AutoWalkUse(target);
                Check(_sStep != step || _sTarget != target || _sSitting, "quest step done: " + target);
                if (target == "cabinet") yield return Shot("s02_carry_filter");
                if (target == "plants") yield return Shot("s03_lab");
                if (target == "sofa") yield return Shot("s03b_seated_on_sofa");
                if (target == "sofa") break;
            }
            t = 0;
            while (!_sScramble && t < 40) { if (InCutscene) SkipCutscene(); t += Time.deltaTime; yield return null; }
            Check(_sScramble, "impact started the scramble");
            while (_busy && t < 60) { t += Time.deltaTime; yield return null; }
            Check(BFree(_bPos), "impact returns cadet to walkable floor outside sofa collider");
            Vector2 beforeEscape = _bPos;
            BStep(Vector2.down * .15f);
            Check(Vector2.Distance(beforeEscape, _bPos) > .1f, "cadet can move immediately after impact without pathfinding recovery");
            yield return Shot("s04_scramble");
            StartCoroutine(AutoTiltShot());
            // grab the nearest supplies three at a time and run them to the lifeboat
            string[] order = { "sup_food", "sup_filter", "sup_o2", "sup_battery", "sup_water", "sup_medkit" };
            int trips = 0;
            foreach (var id in order)
            {
                if (_sDone) break;
                var sp = BSpotById(id);
                if (sp == null || !sp.sr.enabled) continue;
                yield return AutoWalkUse(id);
                if (_sHands.Count >= HandSlots || id == order[order.Length - 1])
                {
                    if (trips == 0) yield return Shot("s05_hands_full");
                    yield return AutoWalkUse("lifeboat");
                    trips++;
                }
            }
            if (_sPhaseFire > 0) yield return Shot("s06_fire");
            t = 0;
            while (!_sDone && t < 70) { t += Time.deltaTime; yield return null; }
            Check(_sDone, "scramble ended, stowed " + _stowed.Count);
            t = 0;
            // let the landing play long enough to photograph the escape pod coming down, then skip the rest
            float lt = 0; bool podShot = false;
            while ((_life == null || _bMode != 0) && t < 80)
            {
                if (InCutscene) { lt += Time.deltaTime; if (!podShot && lt > 15.5f) { podShot = true; yield return Shot("s06b_pod_landing"); SkipCutscene(); } }
                t += Time.deltaTime; yield return null;
            }
            Check(_life != null && _bMode == 0 && _bRoot != null, "landed in the Mars base with " + _stowed.Count + " supplies");
            Check(!_busy, "landing releases controls in the playable Mars base");
            yield return new WaitForSeconds(1.2f);
            yield return Shot("s07_base_after_landing");
            Log($"done errors={_errors} checksFailed={_checksFailed}");
            _autoLog?.Close();
            _autoLog = null;
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        /// <summary>All cadet frames in one image: rows front / back / side, walk then run, idle at the end.</summary>
        static void BSaveCadetSheet(string path) => SaveActorAtlas(path, "cadet");

        IEnumerator AutoTiltShot()
        {
            float t = 0;
            while (_sTiltT < 2.5f && t < 60) { t += Time.deltaTime; yield return null; }
            Check(_sTilted, "the ship tilts once during the scramble");
            yield return Shot("s05b_ship_tilt");
        }

        /// <summary>Click a place in the base (through BGoTo, like the mouse) and wait until the cadet arrives.</summary>
        IEnumerator AutoWalkUse(string id)
        {
            var s = BSpotById(id);
            if (s == null) { Check(false, "no spot " + id); yield break; }
            if (_bMode == 0 && BIsOutside(s.stand) != BIsOutside(_bPos)) { BSetDoor(true); _bDoorT = 30f; }   // the test opens the airlock like a player would
            BGoTo(s.stand, s);
            float t = 0;
            while ((_bPath != null || _bGoal != null) && t < 25f) { t += Time.deltaTime; yield return null; }
            Check(t < 25f, "walked to " + id);
            while (ActionLocked) yield return null;
            yield return new WaitForSeconds(0.25f);
        }
    }
}
