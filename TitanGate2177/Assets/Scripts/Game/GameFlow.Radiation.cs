// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a visible radiation dose make sheltering and short trips outside feel like real choices?
// Date: 2026-11-13
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Radiation dose in game units (not real millisieverts): a little every night (cosmic rays reach the
    /// surface because Mars has thin air and no global magnetic field), more for every minute outside, and a lot
    /// when a solar storm hits someone who is not in the shelter. Every <see cref="DosePerSick"/> units the
    /// cadet loses one health (radiation sickness). The facts shown come from Curiosity's RAD instrument.
    /// </summary>
    public partial class GameFlow
    {
        const float DosePerSick = 12f, DoseMax = DosePerSick * 3f;
        float _dose, _radOutT;
        int _doseHits;

        void RadReset() { _dose = 0; _radOutT = 0; _doseHits = 0; }

        void RadAdd(float v, string why)
        {
            if (_life == null || v <= 0) return;
            _dose += v;
            if (why != null && v >= 1f) _ui.Toast(TT("Radiation +", "รังสีสะสมเพิ่ม · ") + why, GameUI.CAmber);
            while (_dose >= (_doseHits + 1) * DosePerSick)
            {
                _doseHits++;
                _life.ApplyHealth(-1);
                _sfx.Play("bad"); _sShake = 0.3f;
                _ui.Toast(TT("Too much radiation: you feel sick and weak (health −1).", "ได้รับรังสีมากเกินไป รู้สึกคลื่นไส้และอ่อนแรง (สุขภาพ −1)"), GameUI.CRed);
            }
        }

        /// <summary>Every frame in the base: walking outside adds dose (about 1 unit per 15 seconds).</summary>
        void RadUpdate(float dt)
        {
            if (_bMode != 0 || _life == null || _ui.Blocking || _bGather) return;
            if (!BIsOutside(_bPos)) { _radOutT = 0; return; }
            _radOutT += dt;
            if (_radOutT >= 15f) { _radOutT = 0; RadAdd(1f, null); }
        }

        /// <summary>One night: cosmic rays through the roof, more with the shelter unbuilt on a storm night.</summary>
        void RadNight(int day)
        {
            RadAdd(day == 4 && !_life.State.flags.Contains("shelter_ready") ? 4f : 0.6f, null);
        }

        float RadFrac => Mathf.Clamp01(_dose / DoseMax);
        Color RadColor() => _dose < DosePerSick ? GameUI.CGreen : _dose < DosePerSick * 2 ? GameUI.CAmber : GameUI.CRed;

        string RadLevelText()
        {
            float toNext = (_doseHits + 1) * DosePerSick - _dose;
            string lvl = _dose < DosePerSick * 0.5f ? TT("Low", "ต่ำ") : _dose < DosePerSick ? TT("Medium", "ปานกลาง") : _dose < DosePerSick * 2 ? TT("High", "สูง") : TT("Dangerous", "อันตราย");
            return lvl + TT($" · next sickness in {toNext:0.#} units", $" · อีก {toNext:0.#} หน่วยจะป่วย");
        }

        VisualElement RadBar(float width)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 6;
            row.tooltip = TT("Radiation dose", "รังสีสะสม");
            var ic = new Image { sprite = Art.RadIcon(), scaleMode = ScaleMode.ScaleToFit }; ic.style.width = 24; ic.style.height = 24; ic.style.marginRight = 6; row.Add(ic);
            var bar = new VisualElement(); bar.style.width = width; bar.style.height = 10; bar.style.backgroundColor = GameUI.CPanel2; Round(bar, 3);
            var fill = new VisualElement(); fill.style.width = Length.Percent(RadFrac * 100); fill.style.height = 10; fill.style.backgroundColor = RadColor(); Round(fill, 3); bar.Add(fill);
            for (int i = 1; i < 3; i++) { var tick = new VisualElement(); tick.style.position = Position.Absolute; tick.style.left = Length.Percent(i * 33.3f); tick.style.width = 2; tick.style.top = 0; tick.style.bottom = 0; tick.style.backgroundColor = new Color(0, 0, 0, 0.6f); bar.Add(tick); }
            row.Add(bar);
            return row;
        }
    }
}
