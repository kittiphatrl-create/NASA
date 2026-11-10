// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Core
{
    public enum Lang { EN = 0, TH = 1 }

    [Serializable]
    public class StringEntry
    {
        public string k, en, th;
    }

    [Serializable]
    public class StringTable
    {
        public List<StringEntry> entries = new List<StringEntry>();
    }

    /// <summary>Two-language string table. Missing keys show as #key so they are easy to spot.</summary>
    public class Localization
    {
        readonly Dictionary<string, StringEntry> _map = new Dictionary<string, StringEntry>();
        Lang _current;

        public event Action Changed;

        public Lang Current
        {
            get => _current;
            set { if (_current == value) return; _current = value; Changed?.Invoke(); }
        }

        public int Count => _map.Count;

        public static Localization FromJson(string json, Lang lang = Lang.EN)
        {
            var loc = new Localization { _current = lang };
            var table = JsonUtility.FromJson<StringTable>(json);
            foreach (var e in table.entries)
                if (!string.IsNullOrEmpty(e.k)) loc._map[e.k] = e;
            return loc;
        }

        public bool Has(string key) => key != null && _map.ContainsKey(key);

        public IEnumerable<StringEntry> All => _map.Values;

        /// <summary>Text for key in the current language, formatted with args.</summary>
        public string T(string key, params object[] args) => TIn(_current, key, args);

        public string TIn(Lang lang, string key, params object[] args)
        {
            if (key == null || !_map.TryGetValue(key, out var e)) return "#" + key;
            var s = lang == Lang.TH && !string.IsNullOrEmpty(e.th) ? e.th : e.en;
            if (args == null || args.Length == 0) return s;
            try { return string.Format(s, args); }
            catch (FormatException) { return s; }
        }
    }
}
