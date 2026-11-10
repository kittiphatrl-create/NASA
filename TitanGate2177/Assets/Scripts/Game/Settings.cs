// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>Player settings, saved in PlayerPrefs. Values are 0..1 except textScale and textSpeed.</summary>
    public static class Settings
    {
        public static float Master { get => PlayerPrefs.GetFloat("vol_master", 0.8f); set { PlayerPrefs.SetFloat("vol_master", Mathf.Clamp01(value)); Changed?.Invoke(); } }
        public static float Effects { get => PlayerPrefs.GetFloat("vol_sfx", 0.7f); set { PlayerPrefs.SetFloat("vol_sfx", Mathf.Clamp01(value)); Changed?.Invoke(); } }
        public static float Ambience { get => PlayerPrefs.GetFloat("vol_amb", 0.5f); set { PlayerPrefs.SetFloat("vol_amb", Mathf.Clamp01(value)); Changed?.Invoke(); } }
        /// <summary>1.0, 1.15 or 1.3: multiplies every UI font size.</summary>
        public static float TextScale { get => PlayerPrefs.GetFloat("text_scale", 1f); set { PlayerPrefs.SetFloat("text_scale", value); Changed?.Invoke(); } }
        /// <summary>Characters per second for dialogue; 0 = show at once.</summary>
        public static int TextSpeed { get => PlayerPrefs.GetInt("text_speed", 70); set { PlayerPrefs.SetInt("text_speed", value); Changed?.Invoke(); } }

        public static event System.Action Changed;

        // ---- progress kept between runs

        /// <summary>Knowledge cards found in any run (comma list).</summary>
        public static string CardsEver { get => PlayerPrefs.GetString("cards_ever", ""); set => PlayerPrefs.SetString("cards_ever", value); }

        /// <summary>Most debrief stars earned on a route (0-3).</summary>
        public static int BestStars(string route) => PlayerPrefs.GetInt("stars_" + route, 0);

        /// <summary>Keep the higher score. Returns true when this is a new best.</summary>
        public static bool RecordStars(string route, int stars)
        {
            if (stars <= BestStars(route)) return false;
            PlayerPrefs.SetInt("stars_" + route, stars);
            PlayerPrefs.Save();
            return true;
        }

        public static bool HasSave => !string.IsNullOrEmpty(PlayerPrefs.GetString("save_route", ""));
        public static string SaveRoute => PlayerPrefs.GetString("save_route", "");
        public static float SaveCabin => PlayerPrefs.GetFloat("save_cabin", 50f);
        /// <summary>Supplies stowed in the Chapter 1 scramble (ids).</summary>
        public static System.Collections.Generic.List<string> SaveItems =>
            new System.Collections.Generic.List<string>(PlayerPrefs.GetString("save_items", "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));

        /// <summary>Checkpoint at the start of Chapter 2: the route and what Chapter 1 carried over.</summary>
        public static void SaveCheckpoint(string route, float cabin, System.Collections.Generic.IEnumerable<string> items)
        {
            PlayerPrefs.SetString("save_route", route);
            PlayerPrefs.SetFloat("save_cabin", cabin);
            PlayerPrefs.SetString("save_items", string.Join(",", items));
            PlayerPrefs.Save();
        }

        public static void ClearCheckpoint()
        {
            PlayerPrefs.DeleteKey("save_route");
            PlayerPrefs.Save();
        }
    }
}
