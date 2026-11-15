// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does planning dinner for three people (who eats a meal, who risks an unlabelled box, who goes without)
//           make food a daily decision again, as in the old base week?
// Date: 2026-11-15
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TitanGate.Core.LastDock;
using UnityEngine;
using UnityEngine.UIElements;
using Outcome = TitanGate.Core.LastDock.Outcome;
using Res = TitanGate.Core.LastDock.Res;

namespace TitanGate.Game
{
    /// <summary>
    /// LAST DOCK dinner (the old shared-meal system, people only) and the save that makes Continue work.
    /// You are alone in the base (the user removed the extra crew). Rules live in StationSimulation (Hunger, MealPlan, "dinner").
    /// </summary>
    public partial class GameFlow
    {
        static readonly string[] LdCrewIds = { "cadet", "aya", "jules" };
        string LdCrewName(int i) => i == 0 ? TT("You", "คุณ") : i == 1 ? TT("Aya · engineer", "อายะ · วิศวกร") : TT("Jules · botanist", "จูลส์ · นักพฤกษศาสตร์");
        Sprite LdCrewSprite(int i) => i == 0 ? (Art.Cadet8(0, "idle") ?? Art.Cadet(0, -1, false, false)) : Art.Crew(LdCrewIds[i], 0, -1);
        readonly List<SpriteRenderer> _ldCrewSr = new List<SpriteRenderer>();

        /// <summary>Aya and Jules live in the base too: they stand by the table.</summary>
        void LdPlaceCrew()
        {
            _ldCrewSr.Clear();
            var spots = new[] { new Vector2(18.6f, 9.4f), new Vector2(25.4f, 9.4f) };
            for (int i = 1; i < 3 && i - 1 < spots.Length; i++)
            {
                var sr = BSprite("crew_" + LdCrewIds[i], Art.Crew(LdCrewIds[i], 0, -1), spots[i - 1], SortY(spots[i - 1].y));
                BShadowFor(sr, 1f);
                _ldCrewSr.Add(sr);
            }
        }

        // ------------------------------------------------------------------ the dinner screen

        IEnumerator LdDinnerScreen()
        {
            var plan = _ld.MealPlan ?? _ld.AutoMeals();
            bool done = false;
            void Build()
            {
                _ui.ShowScreen(root =>
                {
                    root.Add(GameUI.Backdrop(new Color(0.02f, 0.03f, 0.06f, 0.92f)));
                    var box = new VisualElement(); box.style.alignItems = Align.Center;
                    var title = _ui.L(TT("DINNER", "อาหารเย็น"), 64, true, LdGold); LdFont(title); box.Add(title);
                    // the tray: what is left after this plan
                    int food = _ld.Food - plan.Count(m => m == Meal.Food), boxes = _ld.MysteryFood - plan.Count(m => m == Meal.Mystery);
                    var tray = LdTileRow();
                    tray.Add(LdTile("food", food.ToString(), TT("meals left", "มื้อที่เหลือ"), food > 0 ? GameUI.CGreen : GameUI.CAmber));
                    tray.Add(LdTile("food", boxes + "?", TT("unlabelled boxes", "กล่องไม่มีฉลาก"), GameUI.CAmber));
                    box.Add(tray);
                    var people = new VisualElement(); people.style.flexDirection = FlexDirection.Row; people.style.marginTop = 16;
                    for (int i = 0; i < _ld.Hunger.Length; i++)
                    {
                        int who = i;
                        var card = new VisualElement(); card.style.width = 300; card.style.marginLeft = 10; card.style.marginRight = 10; card.style.alignItems = Align.Center;
                        card.style.paddingTop = 12; card.style.paddingBottom = 12; card.style.backgroundColor = new Color(0.07f, 0.08f, 0.12f, 0.96f);
                        int h = _ld.Hunger[i], after = plan[i] == Meal.None ? h + 1 : 0;
                        GameUI.Border(card, after >= _ld.C.hungerStarve ? GameUI.CRed : after >= _ld.C.hungerWeak ? GameUI.CAmber : LdGoldDim, 2, 14);
                        var face = new Image { sprite = LdCrewSprite(i), scaleMode = ScaleMode.ScaleToFit }; face.style.width = 120; face.style.height = 140; card.Add(face);
                        var nm = _ui.L(LdCrewName(i), 22, true, GameUI.CText); LdFont(nm); card.Add(nm);
                        // hunger as three pips; red ones are hunger already, the next one shows what skipping does
                        var pips = new VisualElement(); pips.style.flexDirection = FlexDirection.Row; pips.style.marginTop = 6; pips.style.marginBottom = 8;
                        for (int k = 0; k < _ld.C.hungerStarve; k++)
                        {
                            var pip = new VisualElement(); pip.style.width = 22; pip.style.height = 22; pip.style.marginLeft = 3; pip.style.marginRight = 3; GameUI.Border(pip, GameUI.CText, 2, 11);
                            pip.style.backgroundColor = k < h ? GameUI.CRed : k < after ? new Color(1f, 0.6f, 0.2f, 0.6f) : Color.clear;
                            pips.Add(pip);
                        }
                        card.Add(pips);
                        var state = after >= _ld.C.hungerStarve ? TT("starving: crew −1 ♥", "หิวจัด: ลูกเรือ −1 ♥") : after >= _ld.C.hungerWeak ? TT("weak: −1 turn tomorrow", "อ่อนแรง: พรุ่งนี้ −1 ช่วง") : after > 0 ? TT("hungry", "หิว") : TT("full", "อิ่ม");
                        card.Add(_ui.L(state, 18, true, after >= _ld.C.hungerWeak ? GameUI.CRed : after > 0 ? GameUI.CAmber : GameUI.CGreen));
                        var choices = new VisualElement(); choices.style.flexDirection = FlexDirection.Row; choices.style.marginTop = 10;
                        void Choice(Meal m, string icon, string label)
                        {
                            var test = (Meal[])plan.Clone(); test[who] = m;
                            bool ok = _ld.PlanFits(test), on = plan[who] == m;
                            var c = new VisualElement(); c.style.width = 82; c.style.height = 66; c.style.marginLeft = 4; c.style.marginRight = 4; c.style.alignItems = Align.Center; c.style.justifyContent = Justify.Center;
                            c.style.backgroundColor = on ? new Color(0.95f, 0.62f, 0.18f, 0.95f) : new Color(1, 1, 1, 0.05f); GameUI.Border(c, on ? LdGold : LdGoldDim, 2, 10);
                            c.Add(LdIconImage(icon, 26, on ? GameUI.Ink : ok ? GameUI.CText : GameUI.CDim));
                            c.Add(_ui.L(label, 15, true, on ? GameUI.Ink : ok ? GameUI.CText : GameUI.CDim));
                            c.SetEnabled(ok || on);
                            c.RegisterCallback<ClickEvent>(_ => { if (!ok) return; plan[who] = m; _sfx.Play(m == Meal.None ? "click" : "pickup"); Build(); });
                            choices.Add(c);
                        }
                        Choice(Meal.Food, "food", TT("meal", "อาหาร"));
                        Choice(Meal.Mystery, "help", TT("box ?", "กล่อง ?"));
                        Choice(Meal.None, "time", TT("skip", "งด"));
                        card.Add(choices);
                        people.Add(card);
                    }
                    box.Add(people);
                    box.Add(LdPara(TT("Unlabelled boxes are a gamble: some are spoiled (−1 turn tomorrow).", "กล่องไม่มีฉลากเสี่ยงดวง: บางกล่องเสีย (พรุ่งนี้ −1 ช่วง)"), GameUI.CDim, 18));
                    var eat = _ui.B(TT("Eat", "กินข้าว"), () => done = true, 30, true); eat.style.marginTop = 14; box.Add(eat);
                    root.Add(box);
                });
            }
            Build();
            float t = 0;
            while (!done && !(_playtest && (t += Time.deltaTime) > 1f)) yield return null;
            if (_playtest && !_ldShotDinner) { _ldShotDinner = true; yield return Shot("ld08_dinner"); }
            _ld.MealPlan = plan;
            _ui.HideScreen();
        }

        bool _ldShotDinner;

        // ------------------------------------------------------------------ save and Continue

        readonly List<string> _ldActions = new List<string>();
        const string LdSaveKey = "lastdock_save";

        [System.Serializable]
        class LdSave { public int seed; public int[] kit; public string actions; }

        void LdRecord(string action) { if (!_ldReplaying) _ldActions.Add(action); }
        bool _ldReplaying;

        public static bool LdHasSave() => PlayerPrefs.HasKey(LdSaveKey);
        static void LdClearSave() { PlayerPrefs.DeleteKey(LdSaveKey); PlayerPrefs.DeleteKey(LdSaveKey + "_day"); PlayerPrefs.DeleteKey(LdSaveKey + "_at"); PlayerPrefs.Save(); }

        /// <summary>Day reached and when the run was saved, for the main menu's Continue card (day 0 = unknown, older saves).</summary>
        public static (int day, System.DateTime? at) LdSaveInfo()
        {
            int day = PlayerPrefs.GetInt(LdSaveKey + "_day", 0);
            System.DateTime? at = long.TryParse(PlayerPrefs.GetString(LdSaveKey + "_at", ""), out var bin) ? System.DateTime.FromBinary(bin) : (System.DateTime?)null;
            return (day, at);
        }

        /// <summary>The run is deterministic from its seed, so a save is the seed, the kit and every action taken.</summary>
        void LdWriteSave()
        {
            if (_ld == null || _ld.Outcome != Outcome.Ongoing) { LdClearSave(); return; }
            var kit = new int[System.Enum.GetValues(typeof(Res)).Length];
            foreach (var kv in _ldKit) kit[(int)kv.Key] = kv.Value;
            PlayerPrefs.SetString(LdSaveKey, JsonUtility.ToJson(new LdSave { seed = _ldSeed, kit = kit, actions = string.Join("|", _ldActions) }));
            PlayerPrefs.SetInt(LdSaveKey + "_day", _ld.Day);
            PlayerPrefs.SetString(LdSaveKey + "_at", System.DateTime.UtcNow.ToBinary().ToString());
            PlayerPrefs.Save();
        }

        /// <summary>Continue: rebuild the run by replaying every saved action on the same seed.</summary>
        bool LdContinue()
        {
            if (!LdHasSave()) return false;
            LdSave save;
            try { save = JsonUtility.FromJson<LdSave>(PlayerPrefs.GetString(LdSaveKey)); } catch { LdClearSave(); return false; }
            if (save == null || save.kit == null) { LdClearSave(); return false; }
            var kit = new Dictionary<Res, int>();
            for (int i = 0; i < save.kit.Length; i++) kit[(Res)i] = save.kit[i];
            StartLastDock(kit, save.seed, true);
            _ldReplaying = true;
            foreach (var a in (save.actions ?? "").Split('|'))
            {
                if (string.IsNullOrEmpty(a)) continue;
                if (a.StartsWith("dinner:"))
                {
                    _ld.MealPlan = a.Substring(7).Split(',').Select(x => (Meal)int.Parse(x)).ToArray();
                    _ld.Do("dinner");
                }
                else _ld.Do(a);
                _ldActions.Add(a);
            }
            _ldReplaying = false;
            _ldSeenFault = _ld.ActiveFault;
            _ui.Toast(TT($"Welcome back. Day {_ld.Day}.", $"ยินดีต้อนรับกลับ วันที่ {_ld.Day}"), GameUI.CAmber);
            if (_ld.DinnerPending) StartCoroutine(LdDinnerAndDay(_ld.Day, _ld.Log.Count));
            return true;
        }
    }
}
