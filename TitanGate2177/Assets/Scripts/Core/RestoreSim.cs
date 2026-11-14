// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does restoring an abandoned base room by room, with action-timed days, make a better 20-minute game
//           than surviving seven fixed days? (docs/superpowers/specs/2026-11-14-base-restoration-design.md)
// Date: 2026-11-14
using System;
using System.Collections.Generic;

namespace TitanGate.Core
{
    /// <summary>One thing to do in a room (sweep, repair, wire...). hours is shown to the player before it is done.</summary>
    [Serializable]
    public class RestoreTaskDef
    {
        public string id, room, needItem, puzzle, kind;
        /// <summary>The base spot (BSpot id) where the task is done, and items it hands over (comma list) when done.</summary>
        public string spot, gives;
        /// <summary>true = the needed item is used up (parts); false = a tool you keep (broom, crowbar).</summary>
        public bool consume;
        public float hours = 1f;
    }

    /// <summary>A room of the base. start = lit from the beginning; first = must be restored before any other; last = opens after roomsForComms rooms.</summary>
    [Serializable]
    public class RestoreRoomDef
    {
        public string id;
        public bool start, first, last;
    }

    [Serializable]
    public class RestoreConfig
    {
        public List<RestoreRoomDef> rooms = new List<RestoreRoomDef>();
        public List<RestoreTaskDef> tasks = new List<RestoreTaskDef>();
        public int roomsForComms = 3;
        public float startHour = 6, endHour = 20;
        /// <summary>Personal meters (0–100, 100 = fine): loss per game hour, per hour of work.</summary>
        public float hungerPerHour = 4, thirstPerHour = 5, fatiguePerWorkHour = 6, hotThirstMult = 1.8f;
        public List<string> weatherPool = new List<string>();
        /// <summary>Relay orbiter passes (game hours) and how long each lasts.</summary>
        public float relayHour1 = 9.5f, relayHour2 = 16.5f, relayLengthHours = 0.75f;
        /// <summary>Hours to re-power one room after a solar-storm blackout, and to fix the cooling loop on a hot day.</summary>
        public float resetHours = 0.5f, coolingHours = 1f, sendHours = 0.25f;
    }

    /// <summary>Everything RestoreSim needs to continue a run from a nightly save.</summary>
    [Serializable]
    public class RestoreSnapshot
    {
        public int day;
        public float hour, food, water, rest;
        public string[] done, unpowered;
        public bool signalled, coolingBroken;
    }

    public enum RoomState { Locked, Dark, Restored, Unpowered }

    /// <summary>
    /// Rules of restoring the abandoned base: rooms open in a fixed gate order (power first, comms last, the rest
    /// in any order), every task costs a shown number of game hours, and the clock only moves when the player
    /// acts. The player's own needs (food, water, rest) drop with time and work. Pure C# for EditMode tests.
    /// </summary>
    public class RestoreSim
    {
        readonly RestoreConfig _c;
        readonly HashSet<string> _done = new HashSet<string>();
        readonly HashSet<string> _unpowered = new HashSet<string>();
        readonly int _seed;
        const string ResetPrefix = "reset_";

        /// <summary>Today's weather: normal, cold, dust, hot_inside (the base cooling loop failed) or flare.</summary>
        public string Weather { get; private set; } = "normal";
        public bool CoolingBroken { get; private set; }
        public bool Signalled { get; private set; }

        public int Day { get; private set; } = 1;
        public float Hour { get; private set; }
        public float HoursLeft => Math.Max(0, _c.endHour - Hour);
        public float Food { get; private set; } = 100;
        public float Water { get; private set; } = 100;
        public float Rest { get; private set; } = 100;
        public RestoreConfig Config => _c;

        public RestoreSim(RestoreConfig c, int seed)
        {
            _c = c;
            _seed = seed;
            Hour = c.startHour;
            NewDayWeather();
        }

        /// <summary>Weather depends only on seed and day, so a loaded save gets the same days.</summary>
        string WeatherFor(int day)
        {
            if (_c.weatherPool == null || _c.weatherPool.Count == 0) return "normal";
            if (day == 1) return "normal";   // the first day is for learning the base
            // a mixed hash, not new Random(seed + day): nearby seeds give nearly the same first roll
            uint h = unchecked((uint)_seed * 0x9E3779B1u + (uint)day * 0x85EBCA6Bu);
            h ^= h >> 16; h = unchecked(h * 0x85EBCA6Bu); h ^= h >> 13; h = unchecked(h * 0xC2B2AE35u); h ^= h >> 16;   // murmur3 finaliser
            int n = _c.weatherPool.Count, i = (int)(h % (uint)n);
            // never the same weather two days running: jump half the pool instead
            if (n > 1 && _c.weatherPool[i] == WeatherFor(day - 1)) i = (i + Math.Max(1, n / 2)) % n;
            return _c.weatherPool[i];
        }

        void NewDayWeather()
        {
            Weather = WeatherFor(Day);
            CoolingBroken = Weather == "hot_inside";
        }

        RestoreTaskDef Task(string id)
        {
            if (id != null && id.StartsWith(ResetPrefix))
            {
                string room = id.Substring(ResetPrefix.Length);
                return _unpowered.Contains(room) ? new RestoreTaskDef { id = id, room = room, hours = _c.resetHours, kind = "wire", puzzle = "wires" } : null;
            }
            return _c.tasks.Find(t => t.id == id);
        }
        RestoreRoomDef Room(string id) => _c.rooms.Find(r => r.id == id);

        bool AllDone(string room)
        {
            bool any = false;
            foreach (var t in _c.tasks) if (t.room == room) { any = true; if (!_done.Contains(t.id)) return false; }
            return any;
        }

        int FreeRoomsRestored()
        {
            int n = 0;
            foreach (var r in _c.rooms) if (!r.start && !r.first && !r.last && AllDone(r.id)) n++;
            return n;
        }

        bool PowerOn()
        {
            foreach (var r in _c.rooms) if (r.first) return AllDone(r.id);
            return true;
        }

        public RoomState StateOf(string room)
        {
            var r = Room(room);
            if (r == null) return RoomState.Locked;
            if (r.start) return RoomState.Restored;
            bool open = r.first || (r.last ? PowerOn() && FreeRoomsRestored() >= _c.roomsForComms : PowerOn());
            if (!open) return RoomState.Locked;
            if (_unpowered.Contains(room)) return RoomState.Unpowered;
            return AllDone(room) ? RoomState.Restored : RoomState.Dark;
        }

        public bool IsDone(string task) => _done.Contains(task);

        /// <summary>Share of the base's tasks done, 0–100.</summary>
        public float Percent
        {
            get
            {
                if (_c.tasks.Count == 0) return 0;
                int n = 0; foreach (var t in _c.tasks) if (_done.Contains(t.id)) n++;
                return 100f * n / _c.tasks.Count;
            }
        }

        /// <summary>Hours a task takes: its own time plus half an hour per mistake, at most one extra hour.</summary>
        public float HoursFor(string task, int mistakes = 0)
        {
            var t = Task(task);
            return t == null ? 0 : t.hours + Math.Min(1f, 0.5f * Math.Max(0, mistakes));
        }

        /// <summary>Can this task be done now? reasonKey: restore.need.done / room / item / time.</summary>
        public bool CanDo(string task, IReadOnlyCollection<string> bag, out string reasonKey)
        {
            reasonKey = null;
            var t = Task(task);
            if (t == null || _done.Contains(task)) { reasonKey = "restore.need.done"; return false; }
            if (StateOf(t.room) == RoomState.Locked) { reasonKey = "restore.need.room"; return false; }
            if (!string.IsNullOrEmpty(t.needItem) && (bag == null || !Contains(bag, t.needItem))) { reasonKey = "restore.need.item"; return false; }
            if (Hour + t.hours > _c.endHour + 1e-4f) { reasonKey = "restore.need.time"; return false; }
            return true;
        }

        static bool Contains(IReadOnlyCollection<string> bag, string id) { foreach (var b in bag) if (b == id) return true; return false; }

        /// <summary>Do a task: spends its hours (as work) and marks it done. Returns false and changes nothing when it cannot be done.</summary>
        public bool Do(string task, IReadOnlyCollection<string> bag = null, int mistakes = 0)
        {
            if (!CanDo(task, bag, out _)) return false;
            float h = Math.Min(HoursFor(task, mistakes), HoursLeft);
            Spend(h, h);
            if (task.StartsWith(ResetPrefix)) _unpowered.Remove(task.Substring(ResetPrefix.Length));
            else _done.Add(task);
            return true;
        }

        /// <summary>Move the clock: needs drop with time; work hours also tire. Never past the end of the day.</summary>
        public bool Spend(float hours, float work = 0)
        {
            if (hours < 0 || float.IsNaN(hours)) return false;
            hours = Math.Min(hours, HoursLeft);
            Hour += hours;
            Food = Clamp(Food - _c.hungerPerHour * hours);
            Water = Clamp(Water - _c.thirstPerHour * ThirstMult * hours);
            Rest = Clamp(Rest - _c.fatiguePerWorkHour * Math.Min(work, hours));
            return true;
        }

        public float ThirstMult => CoolingBroken ? _c.hotThirstMult : 1f;

        /// <summary>Fix the cooling loop on a hot day (a repair panel in the game). False when not broken or no time left.</summary>
        public bool FixCooling()
        {
            if (!CoolingBroken || Hour + _c.coolingHours > _c.endHour + 1e-4f) return false;
            Spend(_c.coolingHours, _c.coolingHours);
            CoolingBroken = false;
            return true;
        }

        // ------------------------------------------------------------------ relay orbiter and the distress signal

        /// <summary>Is the relay orbiter above the base now (a short window, twice a day)?</summary>
        public bool RelayOpen => InWindow(_c.relayHour1) || InWindow(_c.relayHour2);
        bool InWindow(float start) => Hour >= start - 1e-4f && Hour < start + _c.relayLengthHours;

        /// <summary>Start of the current or next pass today; tomorrow's first pass (+24) when today's are over.</summary>
        public float NextRelayHour => Hour < _c.relayHour1 + _c.relayLengthHours ? _c.relayHour1 : Hour < _c.relayHour2 + _c.relayLengthHours ? _c.relayHour2 : _c.relayHour1 + 24f;

        bool CommsReady
        {
            get
            {
                foreach (var r in _c.rooms) if (r.last) return StateOf(r.id) == RoomState.Restored;
                return false;
            }
        }

        /// <summary>Send the distress call: needs the radio room restored and the relay orbiter overhead.</summary>
        public bool SendSignal()
        {
            if (Signalled || !CommsReady || !RelayOpen) return false;
            Spend(Math.Min(_c.sendHours, HoursLeft));
            Signalled = true;
            return true;
        }

        // ------------------------------------------------------------------ solar storm

        /// <summary>An unprepared solar storm: every restored room except the lounge and the generator loses power.</summary>
        public void Blackout()
        {
            foreach (var r in _c.rooms)
                if (!r.start && !r.first && AllDone(r.id) && StateOf(r.id) == RoomState.Restored) _unpowered.Add(r.id);
        }

        // ------------------------------------------------------------------ ORBI's danger warnings

        /// <summary>
        /// What ORBI must warn about now (localisation keys), given the base stock, the O₂ everyone breathes
        /// tonight, the power life support needs tonight and tonight's ration. Only real dangers, nothing else.
        /// </summary>
        public List<string> Warnings(Stock stock, float o2Tonight, float powerTonight, Stock ration, bool flareComing)
        {
            var w = new List<string>();
            if (stock.o2 < o2Tonight) w.Add("restore.warn.o2");
            if (stock.power < powerTonight) w.Add("restore.warn.power");
            if (ration != null && stock.water < ration.water) w.Add("restore.warn.water");
            if (ration != null && stock.food < ration.food) w.Add("restore.warn.food");
            if (flareComing) w.Add("restore.warn.flare");
            if (Food < 25) w.Add("restore.warn.food_me");
            if (Water < 25) w.Add("restore.warn.water_me");
            if (Rest < 25) w.Add("restore.warn.rest_me");
            return w;
        }

        // ------------------------------------------------------------------ save

        public RestoreSnapshot Snapshot() => new RestoreSnapshot
        {
            day = Day, hour = Hour, food = Food, water = Water, rest = Rest,
            done = new List<string>(_done).ToArray(), unpowered = new List<string>(_unpowered).ToArray(),
            signalled = Signalled, coolingBroken = CoolingBroken,
        };

        public void Load(RestoreSnapshot s)
        {
            if (s == null) return;
            Day = s.day; Hour = s.hour; Food = s.food; Water = s.water; Rest = s.rest;
            _done.Clear(); if (s.done != null) foreach (var d in s.done) _done.Add(d);
            _unpowered.Clear(); if (s.unpowered != null) foreach (var u in s.unpowered) _unpowered.Add(u);
            Weather = WeatherFor(Day);
            CoolingBroken = s.coolingBroken; Signalled = s.signalled;
        }

        static float Clamp(float v) => v < 0 ? 0 : v > 100 ? 100 : v;

        /// <summary>Work that already moved the clock elsewhere (old base jobs): only tires the cadet.</summary>
        public void Tire(float workHours) => Rest = Clamp(Rest - _c.fatiguePerWorkHour * Math.Max(0, workHours));

        /// <summary>A nap on the sofa or a bunk: time passes, rest comes back (+25 per hour). Not past bedtime.</summary>
        public bool Nap(float hours)
        {
            if (hours <= 0 || Hour + hours > _c.endHour + 1e-4f) return false;
            Spend(hours);
            Rest = Clamp(Rest + 25f * hours);
            return true;
        }

        public void Eat() => Food = 100;
        public void Drink() => Water = 100;
        public void Sleep() { Day++; Hour = _c.startHour; Rest = 100; NewDayWeather(); }

        /// <summary>Tasks not done yet in rooms that are open.</summary>
        public IEnumerable<string> OpenTasks()
        {
            foreach (var t in _c.tasks)
                if (!_done.Contains(t.id) && StateOf(t.room) != RoomState.Locked) yield return t.id;
            foreach (var r in _c.rooms) if (_unpowered.Contains(r.id)) yield return ResetPrefix + r.id;
        }
    }
}
