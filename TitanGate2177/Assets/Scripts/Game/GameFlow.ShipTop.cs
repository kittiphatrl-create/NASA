// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a busy morning of small quests, then an impact and a 60-second scramble, pull players in?
// Date: 2026-11-12
using System.Collections;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Chapter 1 in the top-down view: the ship Kestrel. A morning of short quests that teach walking, carrying and
    /// using things (and where everything is), a rest on the sofa, the debris impact, then 60 real seconds to carry
    /// supplies to the lifeboat. The lifeboat lands on Mars and the walkable base week begins.
    /// </summary>
    public partial class GameFlow
    {
        bool TopDownCh1 => _cfg.chapter1.topDown && !ForceDiary && !ForceOutpost;

        struct SStep { public string target, carry, textKey, factKey; }

        static readonly SStep[] ShipSteps =
        {
            new SStep { target = "food", carry = "sup_food", textKey = "ship.q.food" },
            new SStep { target = "bowl", textKey = "ship.q.bowl", factKey = "ship.fact.food" },
            // trimmed for a ~20-minute run (spec 2026-11-14 §6.1): the filter, water and plant jobs now live in the base restoration
            new SStep { target = "radio", textKey = "ship.q.radio", factKey = "ship.fact.radio" },
            new SStep { target = "sofa", textKey = "ship.q.sofa" },
        };

        /// <summary>Where each supply lies on the ship when the scramble starts (the rooms the quests showed).</summary>
        static readonly (string id, float x, float y)[] ShipSupplies =
        {
            ("sup_water", 11.5f, 4.5f), ("sup_shield", 13.5f, 6.6f),
            ("sup_o2", 21.5f, 4.6f), ("sup_filter", 24.5f, 6.6f),
            ("sup_medkit", 23.0f, 17.4f), ("sup_seeds", 10.6f, 18.0f),
            ("sup_photo", 14.8f, 14.6f), ("sup_food", 28.4f, 16.8f),
            ("sup_battery", 5.5f, 6.0f), ("sup_cell", 40.0f, 8.0f),
        };

        const int HandSlots = 3;

        int _sStep;
        string _sTarget, _sCarry;
        bool _sScramble, _sDone, _sSitting;
        float _sLeft, _sRumbleT, _sShake;
        int _sPhaseFire;
        bool _sTilted;
        float _sTiltT;
        readonly List<string> _sHands = new List<string>();
        readonly List<SpriteRenderer> _sFx = new List<SpriteRenderer>();
        SpriteRenderer _sCarrySr, _sHatchSr;
        readonly List<SpriteRenderer> _sHandSr = new List<SpriteRenderer>();
        string _sPet;

        IEnumerator StartShipTop()
        {
            _chapter = 3;   // GameFlow's side-view input stays off; the top-down mode drives input
            _route = "mars";
            _stowed.Clear();
            _ui.HideScreen();
            ClearWorld();
            _busy = true;
            yield return Fade(1f, 0f);
            yield return PlayCutscene("intro");
            _player.gameObject.SetActive(false);
            _orbi.gameObject.SetActive(false);
            _ui.SetHudVisible(false);

            // pick the crew pet first: it lives on the ship and comes along
            _sPet = null;
            if (!NoPets)
            {
                _ui.FadeTo(0.6f, 0.3f);
                yield return LbPickBeforeScramble();
                _sPet = _lbScramblePet ?? LC.pets[0].id;
            }

            _sStep = 0; _sTarget = SSteps[0].target; _sCarry = null;
            _sScramble = false; _sDone = false; _sSitting = false; _sPhaseFire = 0; _sShake = 0; _sTilted = false; _sTiltT = 0;
            _sHands.Clear(); _sHandSr.Clear();
            BBegin(1, new Vector2(22.5f, 15.5f));
            if (_sPet != null)
            {
                _bPet.sprite = Art.Pet(_sPet);
                _bPet.gameObject.SetActive(true);
                _bSpots.Add(new BSpot { id = "pet", sr = _bPet, nameKey = _loc.T($"pet.{_sPet}.name") });
            }
            _sCarrySr = BSprite("carry", null, Vector2.zero, 31003);
            _sfxLoopShip(true);
            yield return Fade(0f, 0.8f);
            _mentor = BSprite("senior-pilot", Art.Crew("mentor", 0, -1), new Vector2(39.5f, 12.5f), SortY(12.5f));
            BShadowFor(_mentor, 1f);
            _bSpots.Add(new BSpot { id = "mentor", sr = _mentor, stand = new Vector2(39.5f, 11.5f), nameKey = "speaker.mentor" });
            // LAST DOCK: no pet, bound for Mars, one morning job (the old lines talked about feeding the pet)
            yield return Say("mentor", LastDockMode ? "ld.mentor.welcome" : "story.mentor.welcome");
            yield return Say("orbi", LastDockMode ? "ld.orbi.intro1" : "ship.intro.1");
            if (_sPet != null) yield return Say("orbi", "ship.intro.2", _loc.T($"pet.{_sPet}.name"));
            _busy = false;
            _ui.Toast(_loc.T(SSteps[0].textKey, _loc.T($"pet.{_sPet}.name")), GameUI.CCyan);
        }

        void _sfxLoopShip(bool on) => _sfx.Loop("hum", on);

        // ------------------------------------------------------------------ use

        void SUse(BSpot s)
        {
            if (_sDone || _sSitting || ActionLocked) return;
            if (LdShipUse(s)) return;   // LAST DOCK: key card and locked vaults (GameFlow.ShipRooms.cs)
            if (s.id == "mentor") { StartCoroutine(SMentorAdvice()); return; }
            if (s.id == "pet") { _bHeartT = 1.2f; _sfx.Play("good", 0.6f); _ui.Toast(_loc.T("base.pet", s.nameKey), GameUI.CGreen); return; }
            if (_sScramble)
            {
                if (s.id.StartsWith("sup_")) SGrab(s);
                else if (s.id == "lifeboat") SStow();
                else _ui.Toast(_loc.T("ship.scramble.notime"), GameUI.CRed);
                return;
            }
            if (s.id != _sTarget)
            {
                _ui.Toast(_loc.T("ship.look." + (_loc.Has("ship.look." + s.id) ? s.id : "other"), s.nameKey != null ? _loc.T(s.nameKey) : ""), GameUI.CDim);
                return;
            }
            if (s.id != "sofa") { StartCoroutine(Act(TaskPose(s.id), .85f, () => SCompleteStep(s))); return; }
            SCompleteStep(s);
        }

        void SCompleteStep(BSpot s)
        {
            if (_sDone || _sScramble) return;
            var step = SSteps[_sStep];
            _sfx.Play(step.carry != null ? "card" : "good");
            _sCarry = step.carry;
            if (step.target == "bowl") BSpotById("bowl").sr.sprite = Art.PetBowl(true);
            if (step.target == "can") BSpotById("can").sr.enabled = false;
            if (step.factKey != null) _ui.Toast(_loc.T("speaker.mentor") + ": " + _loc.T(step.factKey), GameUI.CGreen);
            if (step.target == "sofa") { StartCoroutine(SImpact()); return; }
            _sStep++;
            _sTarget = SSteps[_sStep].target;
            _ui.Toast(_loc.T(SSteps[_sStep].textKey, _loc.T($"pet.{_sPet}.name")), GameUI.CCyan);
        }

        void SGrab(BSpot s)
        {
            if (_sHands.Count >= HandSlots || LastDockMode && !LdPick(s.id)) { _sfx.Play("bad"); _ui.Toast(_loc.T("ship.scramble.hands_full", HandSlots), GameUI.CRed); return; }
            _sHands.Add(s.id);
            StartCoroutine(Act("lift", .35f, null));
            s.sr.enabled = false;
            s.on = false;
            _sfx.Play("card");
            _ui.Toast(_loc.T("ship.scramble.grab", LastDockMode ? LdSupName(s.id) : _loc.T($"sup.{s.id}.name"), _sHands.Count, HandSlots), GameUI.CAmber);
        }

        void SStow()
        {
            if (_sHands.Count == 0) { _ui.Toast(_loc.T("ship.scramble.empty"), GameUI.CDim); return; }
            int put = 0;
            if (LastDockMode) { put = LdDeposit(); for (int i = 0; i < put; i++) { _stowed.Add(_sHands[0]); _sHands.RemoveAt(0); } }
            else while (_sHands.Count > 0 && _stowed.Count < StowSlots) { _stowed.Add(_sHands[0]); _sHands.RemoveAt(0); put++; }
            _sfx.Play("thud");
            StartCoroutine(Act("lift", .35f, null));
            _sfx.Play("good");
            _ui.Toast(_loc.T("ship.scramble.stowed", put, _stowed.Count, StowSlots), GameUI.CGreen);
            if (_stowed.Count >= StowSlots) StartCoroutine(SEndScramble(true));
        }

        // ------------------------------------------------------------------ impact and scramble

        IEnumerator SImpact()
        {
            // Preserve the reachable floor position; the seated pose is inside the sofa collider.
            Vector2 standingPosition = _bPos;
            _sSitting = true;
            _sTarget = null;
            _bPath = null; _bGoal = null;
            _bPos = new Vector2(31.2f, 16.45f); _bRight = true;
            yield return Say("orbi", "ship.rest");
            yield return new WaitForSeconds(1.4f);
            _sfx.Play("thud");
            _sShake = 0.7f;
            _ui.Flash(new Color(1f, 0.15f, 0.2f, 0.6f));
            yield return new WaitForSeconds(0.6f);
            yield return Fade(1f, 0.3f);
            yield return BCutscene("impact");
            _bPos = standingPosition;
            yield return Fade(0f, 0.3f);
            _sSitting = false; _petStay = false;
            _sScramble = true;
            _sLeft = _cfg.chapter1.scrambleSeconds;
            _sRumbleT = 2f;
            _sfx.Play("alarm");
            _sfx.Loop("rumble", true);
            _sShake = 1.2f;
            _ui.Flash(new Color(1f, 0.15f, 0.2f, 0.5f));
            _sHatchSr.sprite = Art.LifeboatPod(true, 0, StowSlots);
            if (LastDockMode) LdNewInventory();
            foreach (var (id, x, y) in LastDockMode ? (IEnumerable<(string id, float x, float y)>)LdScrambleItems() : ShipSupplies)
            {
                var sp = BProp(id, LastDockMode ? LdSupName(id) : $"sup.{id}.name", Art.LooseSupply(id), x, y, new Vector2(x, y - 0.5f));
                sp.sr.sortingOrder = SortY(y) + 1;
                BLamp(new Vector2(x, y + 0.4f), new Color(1f, 0.85f, 0.4f), 1.6f, 0.5f);
            }
            _busy = true;
            yield return Say("mentor", LastDockMode ? "ld.mentor.emergency" : "story.mentor.emergency");
            yield return Say("sys", "ship.scramble.sys");
            if (LastDockMode) { LdScrambleRules(); while (_ui.ModalOpen) yield return null; }   // LAST DOCK: the rules before the clock
            else
            {
                yield return Say("orbi", "ship.scramble.1", Mathf.RoundToInt(_sLeft));
                yield return Say("orbi", "ship.scramble.2", HandSlots, StowSlots);
            }
            _busy = false;
        }

        void SUpdate(float dt)
        {
            // what the cadet carries floats over their head
            string carry = _sScramble ? null : _sCarry;
            _sCarrySr.gameObject.SetActive(carry != null && !ActionLocked);
            if (carry != null)
            {
                _sCarrySr.sprite = Art.LooseSupply(carry);
                _sCarrySr.transform.localScale = Vector3.one * .45f;
                _sCarrySr.transform.position = new Vector3(_bPos.x, _bPos.y + .7f + Mathf.Sin(_bAnim * 4) * 0.05f, 0);
            }
            for (int i = 0; i < Mathf.Max(_sHands.Count, _sHandSr.Count); i++)
            {
                if (i >= _sHandSr.Count) _sHandSr.Add(BSprite("hand" + i, null, Vector2.zero, 31004 + i));
                bool on = i < _sHands.Count;
                _sHandSr[i].gameObject.SetActive(on && !ActionLocked);
                if (on)
                {
                    _sHandSr[i].sprite = Art.LooseSupply(_sHands[i]);
                    _sHandSr[i].transform.localScale = Vector3.one * .35f;
                    _sHandSr[i].transform.position = new Vector3(_bPos.x + (i - (_sHands.Count - 1) / 2f) * 0.35f, _bPos.y + .72f, 0);
                }
            }
            if (_sSitting) _bMoving = false;

            if (!_sScramble || _sDone) return;
            if (!_ui.Blocking) _sLeft -= dt;

            // the ship shakes, and things get worse as time runs out
            _sRumbleT -= dt;
            if (_sRumbleT <= 0) { _sRumbleT = Random.Range(5f, 8f); _sShake = 0.5f; _sfx.Play("thud", 0.7f); }
            float elapsed = _cfg.chapter1.scrambleSeconds - _sLeft;
            if (_sPhaseFire == 0 && elapsed > 18f) { _sPhaseFire = 1; SFire(22, 10, 23, 12); }
            if (_sPhaseFire == 1 && elapsed > 34f) { _sPhaseFire = 2; if (_bPos.x < 32f) SFire(33, 10, 33, 12); }
            foreach (var f in _sFx)
            {
                f.sprite = Art.Fire((int)(_bAnim * 8 + f.transform.position.x) % 2);
            }
            // halfway through, the hull lurches: the whole ship tilts and everything slides
            if (!_sTilted && elapsed > 12f) { _sTilted = true; _sTiltT = 4.5f; _sfx.Play("thud"); _sfx.Play("alarm"); _sShake = 1.2f; _ui.Flash(new Color(1f, 0.2f, 0.1f, 0.5f)); _ui.Toast(_loc.T("ship.scramble.tilt"), GameUI.CRed); }
            if (_sTiltT > 0)
            {
                _sTiltT -= dt;
                float k = _sTiltT > 3.5f ? (4.5f - _sTiltT) : Mathf.Clamp01(_sTiltT / 3.5f);
                float ang = 11f * k * (1 + Mathf.Sin(_bAnim * 9) * 0.08f);
                _bCam.transform.rotation = Quaternion.Euler(0, 0, ang);
                BStep(new Vector2(-1.6f * k * dt, 0));   // the floor tilts: you slide toward the low side
                if (_sTiltT <= 0) _bCam.transform.rotation = Quaternion.identity;
            }
            if (_sLeft <= 0) StartCoroutine(SEndScramble(false));
        }

        /// <summary>Fire blocks a doorway (if the cadet is not standing in it).</summary>
        void SFire(int x0, int y0, int x1, int y1)
        {
            int px = Mathf.FloorToInt(_bPos.x), py = Mathf.FloorToInt(_bPos.y);
            if (px >= x0 - 1 && px <= x1 + 1 && py >= y0 - 1 && py <= y1 + 1) return;
            for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                _bSolid[x, y] = true;
                var f = BSprite("fire", Art.Fire(0), new Vector2(x + 0.5f, y + 0.1f), SortY(y) + 5);
                _sFx.Add(f);
                BLamp(new Vector2(x + 0.5f, y + 0.6f), new Color(1f, 0.5f, 0.15f), 3f, 0.9f);
            }
            _sShake = 0.8f;
            _sfx.Play("alarm");
            _ui.Toast(_loc.T("ship.scramble.fire"), GameUI.CRed);
        }

        IEnumerator SEndScramble(bool full)
        {
            if (_sDone) yield break;
            _sDone = true;
            _sScramble = false;
            _bPath = null; _bGoal = null;
            _sfx.Loop("rumble", false);
            int lost = _sHands.Count;
            _sHands.Clear();
            _busy = true;
            yield return Say("orbi", full ? "ship.scramble.full" : "ship.scramble.time", _stowed.Count, StowSlots);
            if (lost > 0) yield return Say("orbi", "ship.scramble.lost", lost);
            Settings.SaveCheckpoint("lifeboat", _cfg.chapter1.cabinO2Start, _stowed, _sPet);
            yield return Fade(1f, 0.8f);
            yield return BCutscene("evacuate");
            if (LastDockMode) { _busy = false; StartCoroutine(LdAfterScramble()); yield break; }   // LAST DOCK: dock at the station
            yield return BCutscene("land_mars");
            yield return BCutscene("dock");
            _busy = false;
            StartLifeboat(new List<string>(_stowed), _sPet);
        }

        // ------------------------------------------------------------------ light and HUD

        void SLight(float dt)
        {
            float dark = _sScramble ? 0.22f + (Mathf.PerlinNoise(_bAnim * 3f, 0) > 0.8f ? 0.3f : 0f) : 0.04f;
            var col = new Color(0.03f, 0.04f, 0.14f, dark);
            if (_sScramble)
            {
                float pulse = 0.06f + Mathf.Abs(Mathf.Sin(_bAnim * 4)) * 0.16f;
                col = new Color(0.75f, 0.03f, 0.08f, pulse + dark * 0.35f);
            }
            _bDark.color = Color.Lerp(_bDark.color, col, dt * 6f);
            foreach (var (sr, strength) in _bLamps)
            {
                var c = sr.color;
                c.a = Mathf.Lerp(c.a, Mathf.Clamp01(0.1f + dark * 1.1f) * strength, dt * 4f);
                sr.color = c;
            }
            if (_sHatchSr != null && _sScramble) _sHatchSr.sprite = Art.LifeboatPod(true, _stowed.Count, StowSlots);
            float arrowA = _sScramble ? 0.45f + Mathf.Abs(Mathf.Sin(_bAnim * 5)) * 0.5f : 0f;
            foreach (var ar in _sArrows) ar.color = new Color(1, 1, 1, Mathf.Lerp(ar.color.a, arrowA, dt * 6f));
        }

        void SHud()
        {
            bool show = !_ui.ScreenOpen && !InCutscene;
            _bHud.style.display = _bHudPortrait.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (_bHudSpeed != null) _bHudSpeed.style.display = show && !_ui.Blocking ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            // Chapter 1 is a scripted morning, not the Mars action-time simulation.
            _bHudDay.text = GameUI.Thai(TT("DAY 0", "วันที่ 0"));
            _bHudDay.style.color = GameUI.Ink;
            _bHudStock.text = "06:00";
            _bHudStock.tooltip = TT("Kestrel · scripted morning briefing", "ยาน KESTREL · ช่วงเช้าของบทเปิดเรื่อง");
            _bClockMarker.style.left = Length.Percent(25);
            _bHudTimer.style.display = _bHudBar.style.display = _sScramble ? DisplayStyle.Flex : DisplayStyle.None;
            if (_sScramble)
            {
                _bHudTimer.text = GameUI.Thai(_loc.T("ship.hud.timer", Mathf.CeilToInt(Mathf.Max(0, _sLeft))));
                _bHudBar.style.width = Length.Percent(Mathf.Clamp01(_sLeft / _cfg.chapter1.scrambleSeconds) * 100f);
            }
            _bHudT -= Time.unscaledDeltaTime;
            if (_bHudT > 0) return; _bHudT = .2f;
            _bHudPortrait.Clear();
            _bHudPortrait.Add(BPortraitPanel());
            _bHudPortrait.Add(ShipHandsStrip());
            _bHudPortrait.Add(ShipMissionPanel());
        }

        VisualElement ShipHandsStrip()
        {
            var strip = new VisualElement { pickingMode = PickingMode.Ignore };
            strip.style.position = Position.Absolute; strip.style.left = 508; strip.style.bottom = 24;
            strip.style.flexDirection = FlexDirection.Row; strip.style.alignItems = Align.Center;
            strip.style.paddingLeft = 20; strip.style.paddingRight = 24; strip.style.paddingTop = strip.style.paddingBottom = 6;
            strip.Add(GameUI.Plate(GameUI.Paper, GameUI.Ink, 15, true, 5));
            strip.Add(_ui.L(TT("Carrying", "ของที่ถือ"), 17, true, GameUI.Ink));
            for (int i = 0; i < HandSlots; i++)
            {
                var slot = new VisualElement { pickingMode = PickingMode.Ignore };
                slot.style.width = slot.style.height = 28; slot.style.marginLeft = 5;
                slot.style.backgroundColor = GameUI.CPanel2;
                string item = _sScramble ? (i < _sHands.Count ? _sHands[i] : null) : i == 0 ? _sCarry : null;
                if (item != null) slot.style.backgroundImage = new StyleBackground(Art.LooseSupply(item));
                strip.Add(slot);
            }
            return strip;
        }

        VisualElement ShipMissionPanel()
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.style.position = Position.Absolute; box.style.right = 24; box.style.top = 24; box.style.width = 400;
            box.style.paddingLeft = box.style.paddingRight = 18; box.style.paddingTop = 12; box.style.paddingBottom = 16;
            box.Add(GameUI.Plate(GameUI.CPanel, _sScramble ? GameUI.CRed : GameUI.CAmber));
            box.Add(_ui.L(_sScramble ? _loc.T("ship.hud.emergency") : TT("MISSION · KESTREL", "ภารกิจ · KESTREL"), 22, true, _sScramble ? GameUI.CRed : GameUI.CAmber));
            if (_sScramble || _sDone)
            {
                box.Add(_ui.L(_loc.T("ship.hud.lifeboat", _stowed.Count, StowSlots), 23, true));
                var names = new List<string>(); foreach (var h in _sHands) names.Add(LastDockMode ? LdSupName(h) : _loc.T($"sup.{h}.name"));
                box.Add(_ui.L(_loc.T("ship.hud.hands", names.Count == 0 ? "—" : string.Join(", ", names), _sHands.Count, HandSlots), 19));
                return box;
            }
            for (int i = 0; i < SSteps.Length; i++)
            {
                bool done = i < _sStep, current = i == _sStep;
                var line = new VisualElement { pickingMode = PickingMode.Ignore };
                line.style.flexDirection = FlexDirection.Row; line.style.marginTop = 7;
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.style.width = dot.style.height = 12; dot.style.flexShrink = 0; dot.style.marginRight = 10; dot.style.marginTop = 7;
                dot.style.backgroundColor = done ? GameUI.CGreen : current ? GameUI.CAmber : GameUI.CCyanD;
                var label = _ui.L(_loc.T(SSteps[i].textKey, _loc.T($"pet.{_sPet}.name")), current ? 21 : 18, current, done ? GameUI.CGreen : current ? GameUI.CText : GameUI.CDim);
                label.style.flexShrink = 1; line.Add(dot); line.Add(label); box.Add(line);
            }
            return box;
        }
    }
}
