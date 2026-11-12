// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// The 60-second scramble (60 Seconds style): the impact threw supplies all over the ship. Before
    /// the landing, the cadet has one minute to grab what fits in the landing lockers. Every supply
    /// carries into Chapter 2 as a start bonus or as a flag that unlocks event answers.
    /// </summary>
    public partial class GameFlow
    {
        readonly List<string> _stowed = new List<string>();
        bool _scramble;
        float _scrambleLeft;

        /// <summary>Where each loose supply lands: room and x offset (kept clear of the doors).</summary>
        static readonly (string id, int room, float x)[] LooseSpots =
        {
            ("sup_o2", RCockpit, 1.0f), ("sup_photo", RCockpit, -1.3f),
            ("sup_food", RQuarters, -0.5f), ("sup_seeds", RQuarters, -2.8f),
            ("sup_water", RLife, 0.5f), ("sup_filter", RLife, -2.6f),
            ("sup_battery", RFuel, 0.4f), ("sup_shield", RFuel, -2.4f),
            ("sup_medkit", RCargo, -1.3f), ("sup_cell", RCargo, 0.9f),
        };

        int StowSlots => Mathf.Max(1, _cfg.chapter1.stowSlots);

        IEnumerator BeginScramble()
        {
            _sfx.Play("alarm");
            _rig.Shake(0.4f, 0.08f);
            if (UseLifeboat) yield return LbPickBeforeScramble();
            yield return Say("orbi", "c1_scramble_start", Mathf.RoundToInt(_cfg.chapter1.scrambleSeconds), StowSlots);
            foreach (var (id, room, x) in LooseSpots)
            {
                if (_cfg.chapter1.supplies.Find(s => s.id == id) == null || _stowed.Contains(id)) continue;
                _world.Spot(_world.rooms[room], "loose_" + id, "sup." + id + ".name", Art.LooseSupply(id), x, 0f, 0.1f, null, WorldBuilder.OrderPropFront);
            }
            _scrambleLeft = _cfg.chapter1.scrambleSeconds;
            _scramble = true;
            RefreshAll();
        }

        /// <summary>Per frame while the scramble runs: the clock only moves while the player can act.</summary>
        void TickScramble()
        {
            if (!_scramble) return;
            if (!_ui.DialogOpen && !_ui.ModalOpen && !_ui.ScreenOpen) _scrambleLeft -= Time.deltaTime;
            _ui.SetIncident(_loc.T("c1_scramble_banner", Mathf.CeilToInt(Mathf.Max(0, _scrambleLeft)), _stowed.Count, StowSlots),
                Mathf.Clamp01(_scrambleLeft / _cfg.chapter1.scrambleSeconds));
            _sfx.Loop("siren", _scrambleLeft < 15f);
            if (_scrambleLeft <= 0 && !_busy) EndScramble();
        }

        IEnumerator GrabSupply(Hotspot h)
        {
            string id = h.id.Substring("loose_".Length);
            if (!_scramble || _stowed.Contains(id)) yield break;
            yield return _player.Act(Pose.Kneel0, Pose.Kneel1, 0.5f, "step");
            _stowed.Add(id);
            h.SetVisible(false);
            h.SetInteractable(false);
            _sfx.Play("good");
            _ui.Toast(_loc.T("c1_stowed", _loc.T($"sup.{id}.name"), _loc.T(UseLifeboat ? $"life.sup.{id}" : $"sup.{id}.desc"), _stowed.Count, StowSlots), GameUI.CGreen);
            if (_stowed.Count >= StowSlots) _scrambleLeft = 0f;
        }

        void EndScramble()
        {
            _scramble = false;
            _ui.SetIncident(null, 0);
            _sfx.Loop("siren", false);
            foreach (var (id, room, x) in LooseSpots)
            {
                var h = _world.Find("loose_" + id);
                if (h != null) { h.SetVisible(false); h.SetInteractable(false); }
            }
            StartCoroutine(FinishScramble());
        }

        IEnumerator FinishScramble()
        {
            _busy = true;
            yield return Say("orbi", _stowed.Count >= StowSlots ? "c1_scramble_full" : "c1_scramble_time", _stowed.Count, StowSlots);
            yield return LandingSequence();
        }
    }
}
