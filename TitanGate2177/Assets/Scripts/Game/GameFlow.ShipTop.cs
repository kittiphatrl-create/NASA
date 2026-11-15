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

        const string CaptainCard = "captain_keycard", CaptainDoor = "captain_door";
        static readonly Vector2Int CaptainDoorTile = new Vector2Int(36, 11);
        static SStep FindCaptainCard => new SStep { target = CaptainCard, textKey = "ship.q.captain_card" };
        static SStep UnlockCaptainDoor => new SStep { target = CaptainDoor, textKey = "ship.q.captain_door" };

        struct SStep { public string target, carry, textKey, factKey; }

        static readonly SStep[] ShipSteps =
        {
            new SStep { target = "food", carry = "sup_food", textKey = "ship.q.food" },
            new SStep { target = "bowl", textKey = "ship.q.bowl", factKey = "ship.fact.food" },
            // trimmed for a ~20-minute run (spec 2026-11-14 §6.1): the filter, water and plant jobs now live in the base restoration
            new SStep { target = "radio", textKey = "ship.q.radio", factKey = "ship.fact.radio" },
            FindCaptainCard, UnlockCaptainDoor,
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
        bool _sCaptainHasCard, _sCaptainUnlocked;
        Vector2 _sSeatPosition;
        bool SCanStand => _bMode == 1 && _sSitting && _sTarget != null;
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
            yield return LastDockMode ? LdCinIntro() : PlayCutscene("intro");   // LAST DOCK: Blender cinematic (GameFlow.Cinema.cs)
            LdScore(StageMusicPlayer.Cue.Ship);
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
            if (SUseCaptainAccess(s)) return;
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
            if (s.id == "sofa")
            {
                if (_sTarget == s.id) SCompleteStep(s);
                else SSeat(s);
                return;
            }
            if (s.id != _sTarget)
            {
                _ui.Toast(_loc.T("ship.look." + (_loc.Has("ship.look." + s.id) ? s.id : "other"), s.nameKey != null ? _loc.T(s.nameKey) : ""), GameUI.CDim);
                return;
            }
            StartCoroutine(Act(TaskPose(s.id), .85f, () => SCompleteStep(s)));
        }

        bool SUseCaptainAccess(BSpot spot)
        {
            if (spot.id == CaptainCard)
            {
                if (!spot.on) return true;
                if (_sTarget != CaptainCard)
                {
                    _ui.Toast(_loc.T("ship.spot.captain_card"), GameUI.CDim);
                    return true;
                }
                _sCaptainHasCard = true;
                BItemEffect(spot, CaptainCard);
                spot.on = false;
                spot.sr.gameObject.SetActive(false);
                SCompleteStep(spot);
                return true;
            }
            if (spot.id != CaptainDoor || _sCaptainUnlocked) return spot.id == CaptainDoor;
            if (!_sCaptainHasCard)
            {
                _sfx.Play("bad");
                _ui.Toast(_loc.T("ship.q.captain_card"), GameUI.CRed);
                return true;
            }

            _sCaptainHasCard = false;
            _sCaptainUnlocked = true;
            spot.nameKey = null;
            foreach (var door in _bDoorGroups)
                if (door.rect.Contains(CaptainDoorTile))
                    BSetDoorBlocked(door, false);
            BItemEffect(spot, CaptainDoor);
            SCompleteStep(spot);
            return true;
        }

        void SSeat(BSpot sofa)
        {
            _sSeatPosition = sofa.pos;
            _sSitting = true;
            _bMoving = false;
            _bPath = null;
            _bGoal = null;
            _bRight = true;
            if (BFree(sofa.stand)) _bPos = sofa.stand;
        }

        void SCompleteStep(BSpot s)
        {
            if (_sDone || _sScramble) return;
            var step = SSteps[_sStep];
            _sfx.Play(step.carry != null ? "collected" : "complete");
            _sCarry = step.carry;
            if (step.carry != null) BItemEffect(s, step.carry);
            if (step.target == "bowl") BSpotById("bowl").sr.sprite = Art.PetBowl(true);
            if (step.target == "can") BSpotById("can").sr.enabled = false;
            if (step.factKey != null) _ui.Toast(_loc.T("speaker.mentor") + ": " + _loc.T(step.factKey), GameUI.CGreen);
            if (step.target == "sofa") { StartCoroutine(SImpact(s)); return; }
            _sStep++;
            _sTarget = SSteps[_sStep].target;
            _ui.Toast(_loc.T(SSteps[_sStep].textKey, _loc.T($"pet.{_sPet}.name")), GameUI.CCyan);
        }

        void SGrab(BSpot s)
        {
            if (_sHands.Count >= HandSlots || LastDockMode && !LdPick(s.id)) { _sfx.Play("bad"); _ui.Toast(_loc.T("ship.scramble.hands_full", HandSlots), GameUI.CRed); return; }
            _sHands.Add(s.id);
            BItemEffect(s, s.id);
            StartCoroutine(Act("lift", .35f, null));
            s.sr.enabled = false;
            s.on = false;
            _sfx.Play("collected");
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
            _sfx.Play("complete");
            _ui.Toast(_loc.T("ship.scramble.stowed", put, _stowed.Count, StowSlots), GameUI.CGreen);
            if (_stowed.Count >= StowSlots) StartCoroutine(SEndScramble(true));
        }

        // ------------------------------------------------------------------ impact and scramble

        IEnumerator SImpact(BSpot sofa)
        {
            // Preserve the reachable floor position; the seated pose is inside the sofa collider.
            Vector2 standingPosition = _bPos;
            SSeat(sofa);
            _sTarget = null;
            yield return Say("orbi", "ship.rest");
            yield return new WaitForSeconds(1.4f);
            _sfx.Loop("ship_alarm", true);
            _sfx.Play("thud");
            _sShake = 0.7f;
            _sPowerFaultAt = _bAnim;
            if (!Settings.ReducedMotion) _ui.Flash(new Color(1f, .15f, .2f, .43f));
            yield return new WaitForSeconds(0.6f);
            yield return Fade(1f, 0.3f);
            yield return LastDockMode ? LdCinImpact() : BCutscene("impact");
            _bPos = standingPosition;
            _sPowerFaultAt = _bAnim;
            yield return Fade(0f, 0.3f);
            _sSitting = false; _petStay = false;
            _sScramble = true;
            _sLeft = _cfg.chapter1.scrambleSeconds;
            _sRumbleT = 2f;
            _sfx.Play("alarm");
            _sfx.Loop("rumble", true);
            _sShake = 1.2f;
            _sHatchSr.sprite = Art.LifeboatPod(true, 0, StowSlots);
            if (LastDockMode) LdNewInventory();
            SSpawnAlarmLights();
            foreach (var (id, x, y) in LastDockMode ? (IEnumerable<(string id, float x, float y)>)LdScrambleItems() : ShipSupplies)
            {
                var sp = BProp(id, LastDockMode ? LdSupName(id) : $"sup.{id}.name", Art.LooseSupply(id), x, y, new Vector2(x, y - 0.5f));
                sp.sr.sortingOrder = SortY(y) + 1;
                SAddPickupGlow(sp);
                LdAddPickRing(sp);   // a bright outline so it does not melt into the floor (GameFlow.LdHands.cs)
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
            LdHighlightsUpdate();

            if (!_sScramble || _sDone) return;
            if (!_ui.Blocking) _sLeft -= dt;

            // the ship shakes, and things get worse as time runs out
            _sRumbleT -= dt;
            if (_sRumbleT <= 0) { _sRumbleT = Random.Range(5f, 8f); _sShake = 0.5f; _sfx.Play("thud", 0.7f); }
            float elapsed = _cfg.chapter1.scrambleSeconds - _sLeft;
            if (_sPhaseFire == 0 && elapsed > 18f) { _sPhaseFire = 1; SFire(22, 10, 23, 12); }
            if (_sPhaseFire == 1 && elapsed > 34f) { _sPhaseFire = 2; if (_bPos.x < 32f && !LastDockMode) SFire(33, 10, 33, 12); }   // LAST DOCK: never wall off the cockpit and its vaults
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
            _sfx.Loop("ship_alarm", false);
            _sfx.Loop("rumble", false);
            int lost = _sHands.Count;
            _sHands.Clear();
            _busy = true;
            yield return Say("orbi", full ? "ship.scramble.full" : "ship.scramble.time", _stowed.Count, StowSlots);
            if (lost > 0) yield return Say("orbi", "ship.scramble.lost", lost);
            if (!LastDockMode) Settings.SaveCheckpoint("lifeboat", _cfg.chapter1.cabinO2Start, _stowed, _sPet);   // LAST DOCK saves itself when the station week starts
            yield return Fade(1f, 0.8f);
            yield return LastDockMode ? LdCinLaunch() : BCutscene("evacuate");
            if (LastDockMode) { _busy = false; StartCoroutine(LdAfterScramble()); yield break; }   // LAST DOCK: dock at the station
            yield return BCutscene("land_mars");
            yield return BCutscene("dock");
            _busy = false;
            StartLifeboat(new List<string>(_stowed), _sPet);
        }

        // ------------------------------------------------------------------ light and HUD

        /// <summary>Red warning beacons on the ship's equipment, one per prop, each blinking on its own beat.</summary>
        readonly List<(SpriteRenderer core, SpriteRenderer glow, float phase, float speed)> _sAlarms = new List<(SpriteRenderer, SpriteRenderer, float, float)>();

        void SSpawnAlarmLights()
        {
            _sAlarms.Clear();
            int n = 0;
            // Backup beacons are mounted near passageways and the launch bay.
            foreach (var p in new[] { new Vector2(7.6f, 11.6f), new Vector2(13.4f, 12.7f),
                new Vector2(22.4f, 12.7f), new Vector2(31.4f, 9.05f), new Vector2(36.7f, 12.6f),
                new Vector2(28.7f, 6.6f), new Vector2(33.1f, 6.6f) })
            {
                var core = BSprite("alarm-bulb", WorldBuilder.WhitePixel(), p, 30002);
                core.transform.localScale = new Vector3(2.2f, 1.15f, 1f);
                core.color = new Color(1f, 0.12f, 0.12f, 0f);
                var glow = BSprite("alarm-glow", Art.SoftLight(), p, 30001);
                if (_bGlowMaterial != null) glow.sharedMaterial = _bGlowMaterial;
                glow.transform.localScale = Vector3.one * 1.6f;
                glow.color = new Color(1f, 0.08f, 0.08f, 0f);
                // stagger the beat so the ship doesn't blink in unison
                _sAlarms.Add((core, glow, (n * .37f) % 1f, .72f + (n % 3) * .13f));
                n++;
            }
        }

        void SAlarmLights()
        {
            foreach (var (core, glow, phase, speed) in _sAlarms)
            {
                if (core == null || glow == null) continue;
                float t = Mathf.Repeat(_bAnim * speed + phase, 1f);
                float on = SPowerFault ? (Settings.ReducedMotion ? .6f : .22f + .78f * Mathf.Pow(Mathf.Sin(Mathf.PI * t), 2f)) : 0f;
                core.color = new Color(1f, 0.15f + on * 0.15f, 0.12f, on);
                glow.color = new Color(1f, .13f, .09f, on * .44f);
            }
        }

        readonly List<(BSpot spot, SpriteRenderer outline, float phase)> _sPickGlows =
            new List<(BSpot, SpriteRenderer, float)>();

        void SAddPickupGlow(BSpot sp, Color? tint = null)
        {
            if (sp?.sr?.sprite == null) return;
            var outline = BSprite("pickup-outline", Art.KestrelPickupOutline(sp.sr.sprite),
                sp.sr.transform.position, sp.sr.sortingOrder - 1);
            outline.transform.SetParent(sp.sr.transform, false);
            outline.transform.localPosition = Vector3.zero;
            outline.flipX = sp.sr.flipX;
            var color = tint ?? new Color(1f, .82f, .20f);
            color.a = .65f;
            outline.color = color;
            _sPickGlows.Add((sp, outline, _sPickGlows.Count * 2.7f));
        }

        void SPickupGlows()
        {
            foreach (var (spot, outline, phase) in _sPickGlows)
            {
                if (outline == null) continue;
                outline.enabled = spot.on && spot.sr != null && spot.sr.enabled;
                if (!outline.enabled) continue;
                float pulse = Settings.ReducedMotion ? .65f : .5f + .5f * Mathf.Sin(_bAnim * 3.6f + phase);
                var color = outline.color;
                color.a = .25f + pulse * .75f;
                outline.color = color;
            }
        }

        void SLight(float dt)
        {
            float dark = SPowerFault ? .25f : .065f;
            var col = new Color(.04f, .054f, .085f, dark);
            if (SPowerFault)
            {
                // keep only a faint red cast; the warning light now comes from the blinking beacons on equipment
                float pulse = Settings.ReducedMotion ? 0f : Mathf.Abs(Mathf.Sin(_bAnim * 2.5f)) * .025f;
                col = new Color(.15f, .042f, .059f, dark + pulse);
            }
            _bDark.color = Color.Lerp(_bDark.color, col, dt * 6f);
            SAlarmLights();
            SScreenGlows(dt);
            SUpdateHullLights();
            SPickupGlows();
            foreach (var (sr, strength) in _bLamps)
            {
                var c = sr.color;
                float intensity = SPowerFault ? (Settings.ReducedMotion ? .115f : Mathf.Lerp(.07f, .115f, Mathf.Clamp01(SPowerFaultAge))) : .17f;
                c.a = Mathf.Lerp(c.a, Mathf.Clamp01(intensity * strength), dt * 4f);
                sr.color = c;
            }
            if (_sHatchSr != null && _sScramble) _sHatchSr.sprite = Art.LifeboatPod(true, _stowed.Count, StowSlots);
            float arrowA = _sScramble ? 0.45f + Mathf.Abs(Mathf.Sin(_bAnim * 5)) * 0.5f : 0f;
            foreach (var ar in _sArrows) ar.color = new Color(1, 1, 1, Mathf.Lerp(ar.color.a, arrowA, dt * 6f));
        }

        void SHud()
        {
            RefreshPlayOverlay(_sScramble && !_sDone ? EscapeSummary() + "  ·  " +
                BookWord("ship.storage", _sHands.Count, HandSlots, _stowed.Count, StowSlots) : null,
                FieldBook.Red);
        }
    }
}
