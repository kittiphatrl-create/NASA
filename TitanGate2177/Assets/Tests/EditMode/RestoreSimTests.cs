// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does restoring an abandoned base room by room, with action-timed days, make a better 20-minute game
//           than surviving seven fixed days? (docs/superpowers/specs/2026-11-14-base-restoration-design.md)
// Date: 2026-11-14
using System.Collections.Generic;
using NUnit.Framework;
using TitanGate.Core;

namespace TitanGate.Tests
{
    public class RestoreSimTests
    {
        static readonly string[] NoBag = new string[0];

        /// <summary>A small base: lounge (lit from the start), power (first), four free rooms, comms (last).</summary>
        static RestoreConfig Cfg()
        {
            RestoreTaskDef T(string id, string room, float h, string item = null) => new RestoreTaskDef { id = id, room = room, hours = h, needItem = item, kind = "repair", puzzle = "wires" };
            return new RestoreConfig
            {
                rooms = new List<RestoreRoomDef>
                {
                    new RestoreRoomDef { id = "lounge", start = true },
                    new RestoreRoomDef { id = "power", first = true },
                    new RestoreRoomDef { id = "greenhouse" }, new RestoreRoomDef { id = "storage" },
                    new RestoreRoomDef { id = "air" }, new RestoreRoomDef { id = "quarters" },
                    new RestoreRoomDef { id = "comms", last = true },
                },
                tasks = new List<RestoreTaskDef>
                {
                    T("chip", "lounge", 0.5f), T("power_wire", "power", 1f),
                    T("gh_sweep", "greenhouse", 0.5f, "broom"), T("gh_lamps", "greenhouse", 1.5f),
                    T("st_pry", "storage", 0.5f, "crowbar"), T("st_pipes", "storage", 1.5f),
                    T("air_filter", "air", 1f), T("q_heater", "quarters", 1f),
                    T("c_dish", "comms", 1.5f),
                },
                roomsForComms = 3, startHour = 6, endHour = 20,
                hungerPerHour = 4, thirstPerHour = 5, fatiguePerWorkHour = 6, hotThirstMult = 2,
                weatherPool = new List<string> { "normal" },
            };
        }

        static RestoreSim PowerOn()
        {
            var s = new RestoreSim(Cfg(), 1);
            Assert.IsTrue(s.Do("chip")); Assert.IsTrue(s.Do("power_wire"));
            return s;
        }

        [Test]
        public void Start_OnlyPowerRoomOpen()
        {
            var s = new RestoreSim(Cfg(), 1);
            Assert.AreEqual(RoomState.Restored, s.StateOf("lounge"), "the lounge runs on emergency power");
            Assert.AreEqual(RoomState.Dark, s.StateOf("power"));
            Assert.AreEqual(RoomState.Locked, s.StateOf("greenhouse"));
            Assert.AreEqual(RoomState.Locked, s.StateOf("comms"));
            Assert.IsFalse(s.CanDo("gh_lamps", NoBag, out var why));
            Assert.AreEqual("restore.need.room", why);
        }

        [Test]
        public void PowerRestored_OpensOtherRoomsNotComms()
        {
            var s = PowerOn();
            Assert.AreEqual(RoomState.Restored, s.StateOf("power"));
            Assert.AreEqual(RoomState.Dark, s.StateOf("greenhouse"));
            Assert.AreEqual(RoomState.Dark, s.StateOf("quarters"));
            Assert.AreEqual(RoomState.Locked, s.StateOf("comms"));
        }

        [Test]
        public void CommsOpensAfterThreeRooms_AnyOrder()
        {
            var s = PowerOn();
            s.Do("q_heater"); s.Do("air_filter");
            Assert.AreEqual(RoomState.Locked, s.StateOf("comms"));
            s.Do("st_pry"); // not enough alone: storage needs both tasks
            Assert.IsFalse(s.CanDo("st_pry", NoBag, out var why), "already done or missing the crowbar");
            Assert.IsTrue(s.Do("st_pry", new[] { "crowbar" })); s.Do("st_pipes");
            Assert.AreEqual(RoomState.Restored, s.StateOf("storage"));
            Assert.AreEqual(RoomState.Dark, s.StateOf("comms"), "three of the four rooms open the radio room");
        }

        [Test]
        public void Do_NeedsItsItem()
        {
            var s = PowerOn();
            Assert.IsFalse(s.CanDo("gh_sweep", NoBag, out var why));
            Assert.AreEqual("restore.need.item", why);
            Assert.IsTrue(s.CanDo("gh_sweep", new[] { "broom" }, out _));
        }

        [Test]
        public void Do_SpendsShownHours()
        {
            var s = PowerOn();
            float before = s.Hour;
            Assert.AreEqual(0.5f, s.HoursFor("gh_sweep"), 1e-4f);
            Assert.IsTrue(s.Do("gh_sweep", new[] { "broom" }));
            Assert.AreEqual(before + 0.5f, s.Hour, 1e-4f);
            Assert.IsTrue(s.IsDone("gh_sweep"));
        }

        [Test]
        public void Do_RefusesWhenDayTooShort()
        {
            var s = PowerOn();
            s.Spend(19.8f - s.Hour);
            Assert.IsFalse(s.CanDo("q_heater", NoBag, out var why));
            Assert.AreEqual("restore.need.time", why);
            Assert.IsFalse(s.Do("q_heater"));
            Assert.IsFalse(s.IsDone("q_heater"));
            Assert.AreEqual(19.8f, s.Hour, 1e-4f, "a refused action spends nothing");
        }

        [Test]
        public void Mistakes_AddHalfHourEach_CappedAtOneHour()
        {
            var s = PowerOn();
            Assert.AreEqual(2.0f, s.HoursFor("gh_lamps", 1), 1e-4f);
            Assert.AreEqual(2.5f, s.HoursFor("gh_lamps", 3), 1e-4f);
        }

        [Test]
        public void Needs_DropWithTime_AndWorkTires()
        {
            var s = new RestoreSim(Cfg(), 1);
            s.Spend(2f);
            Assert.AreEqual(92f, s.Food, 1e-3f);
            Assert.AreEqual(90f, s.Water, 1e-3f);
            Assert.AreEqual(100f, s.Rest, 1e-3f, "waiting is not work");
            s.Spend(1f, 1f);
            Assert.AreEqual(94f, s.Rest, 1e-3f);
        }

        [Test]
        public void Tire_DrainsRestOnly_ClockUnchanged()
        {
            var s = new RestoreSim(Cfg(), 1);
            s.Tire(2f);
            Assert.AreEqual(88f, s.Rest, 1e-3f);
            Assert.AreEqual(6f, s.Hour);
            Assert.AreEqual(100f, s.Food);
        }

        [Test]
        public void Nap_SpendsTime_GivesBackRest()
        {
            var s = new RestoreSim(Cfg(), 1);
            s.Tire(8f);                       // rest 52
            Assert.IsTrue(s.Nap(1f));
            Assert.AreEqual(7f, s.Hour, 1e-4f);
            Assert.AreEqual(77f, s.Rest, 1e-3f, "+25 per hour of nap");
            s.Spend(19.5f - s.Hour);
            Assert.IsFalse(s.Nap(1f), "no nap that runs past bedtime");
        }

        [Test]
        public void EatDrinkSleep_Restore()
        {
            var s = new RestoreSim(Cfg(), 1);
            s.Spend(5f, 5f);
            s.Eat(); s.Drink();
            Assert.AreEqual(100f, s.Food); Assert.AreEqual(100f, s.Water);
            s.Sleep();
            Assert.AreEqual(2, s.Day); Assert.AreEqual(6f, s.Hour); Assert.AreEqual(100f, s.Rest);
        }

        [Test]
        public void Percent_CountsDoneTasks()
        {
            var s = new RestoreSim(Cfg(), 1);
            Assert.AreEqual(0f, s.Percent, 1e-3f);
            s.Do("chip");
            Assert.AreEqual(100f / 9f, s.Percent, 1e-3f);
        }

        static RestoreSim AllRooms(RestoreConfig c = null)
        {
            var s = new RestoreSim(c ?? Cfg(), 1);
            var bag = new[] { "broom", "crowbar" };
            foreach (var t in new[] { "chip", "power_wire", "gh_sweep", "gh_lamps", "st_pry", "st_pipes", "air_filter", "c_dish" }) Assert.IsTrue(s.Do(t, bag), t);
            return s;
        }

        [Test]
        public void Weather_DeterministicPerSeed()
        {
            var c = Cfg(); c.weatherPool = new List<string> { "normal", "cold", "dust", "hot_inside", "flare" };
            var a = new RestoreSim(c, 42); var b = new RestoreSim(c, 42);
            for (int d = 0; d < 6; d++) { Assert.AreEqual(a.Weather, b.Weather); a.Sleep(); b.Sleep(); }
        }

        [Test]
        public void Weather_DayOneAlwaysNormal()
        {
            var c = Cfg(); c.weatherPool = new List<string> { "cold", "dust", "hot_inside" };
            for (int seed = 0; seed < 20; seed++) Assert.AreEqual("normal", new RestoreSim(c, seed).Weather);
        }

        [Test]
        public void Weather_VariesFromDayToDay()
        {
            var c = Cfg(); c.weatherPool = new List<string> { "normal", "normal", "cold", "dust", "hot_inside", "flare" };
            int flares = 0, days = 0;
            for (int seed = 1; seed <= 30; seed++)
            {
                var s = new RestoreSim(c, seed);
                string prev = null; int run = 0, worstRun = 0;
                for (int d = 1; d < 8; d++)
                {
                    s.Sleep(); days++;
                    if (s.Weather == "flare") flares++;
                    run = s.Weather == prev ? run + 1 : 1; prev = s.Weather; worstRun = System.Math.Max(worstRun, run);
                }
                Assert.LessOrEqual(worstRun, 3, "seed " + seed + ": the same weather should not stick for 4+ days");
            }
            Assert.That(flares, Is.InRange(days / 12, days / 3), "about one day in six is a storm day");
        }

        [Test]
        public void Weather_NoFlareOnDayOne()
        {
            var c = Cfg(); c.weatherPool = new List<string> { "flare" };
            var s = new RestoreSim(c, 3);
            Assert.AreEqual("normal", s.Weather);
            s.Sleep();
            Assert.AreEqual("flare", s.Weather);
        }

        [Test]
        public void Send_InsideWindow_Succeeds()
        {
            var s = AllRooms();
            Assert.AreEqual(RoomState.Restored, s.StateOf("comms"));
            s.Spend(s.Config.relayHour2 + 0.1f - s.Hour);
            Assert.IsTrue(s.RelayOpen);
            Assert.IsTrue(s.SendSignal());
            Assert.IsTrue(s.Signalled);
        }

        [Test]
        public void Send_OutsideWindow_Fails_NextWindowLater()
        {
            var s = AllRooms();
            s.Sleep();   // a fresh morning, 06:00
            Assert.IsFalse(s.RelayOpen);
            Assert.AreEqual(s.Config.relayHour1, s.NextRelayHour, 1e-4f);
            Assert.IsFalse(s.SendSignal());
            s.Spend(s.Config.relayHour1 + s.Config.relayLengthHours + 0.1f - s.Hour);
            Assert.AreEqual(s.Config.relayHour2, s.NextRelayHour, 1e-4f);
            Assert.IsFalse(s.Signalled);
        }

        [Test]
        public void Send_BeforeCommsRestored_Fails()
        {
            var s = PowerOn();
            s.Spend(s.Config.relayHour1 + 0.1f - s.Hour);
            Assert.IsTrue(s.RelayOpen);
            Assert.IsFalse(s.SendSignal());
        }

        [Test]
        public void HotInside_ThirstFaster_UntilCoolingFixed()
        {
            var c = Cfg(); c.weatherPool = new List<string> { "hot_inside" };
            var s = new RestoreSim(c, 5);
            Assert.IsFalse(s.CoolingBroken, "day 1 is always a normal day (the player is still learning)");
            s.Sleep();
            Assert.IsTrue(s.CoolingBroken);
            s.Spend(1f);
            Assert.AreEqual(90f, s.Water, 1e-3f, "thirst x2 while the cooling loop is broken");
            Assert.IsTrue(s.FixCooling());
            Assert.IsFalse(s.CoolingBroken);
            float w = s.Water; s.Spend(1f);
            Assert.AreEqual(w - 5f, s.Water, 1e-3f);
        }

        [Test]
        public void Blackout_UnpowersRestoredRoomsAndAddsReset()
        {
            var s = PowerOn(); s.Do("q_heater");
            s.Blackout();
            Assert.AreEqual(RoomState.Unpowered, s.StateOf("quarters"));
            Assert.AreEqual(RoomState.Restored, s.StateOf("power"), "the generator room keeps the breakers");
            Assert.AreEqual(RoomState.Restored, s.StateOf("lounge"), "the lounge has its own emergency power");
            CollectionAssert.Contains(new List<string>(s.OpenTasks()), "reset_quarters");
            Assert.AreEqual(0.5f, s.HoursFor("reset_quarters"), 1e-4f);
        }

        [Test]
        public void ResetTask_RepowersRoom()
        {
            var s = PowerOn(); s.Do("q_heater"); s.Blackout();
            Assert.IsTrue(s.Do("reset_quarters"));
            Assert.AreEqual(RoomState.Restored, s.StateOf("quarters"));
            CollectionAssert.DoesNotContain(new List<string>(s.OpenTasks()), "reset_quarters");
        }

        [Test]
        public void Snapshot_RoundTrips()
        {
            var s = PowerOn(); s.Do("q_heater"); s.Spend(1f); s.Blackout(); s.Sleep();
            var snap = s.Snapshot();
            var t = new RestoreSim(Cfg(), 1);
            t.Load(snap);
            Assert.AreEqual(s.Day, t.Day); Assert.AreEqual(s.Hour, t.Hour); Assert.AreEqual(s.Food, t.Food); Assert.AreEqual(s.Water, t.Water);
            Assert.AreEqual(RoomState.Unpowered, t.StateOf("quarters"));
            Assert.IsTrue(t.IsDone("power_wire"));
            Assert.AreEqual(s.Weather, t.Weather);
        }

        static RestoreConfig Shipped() => GameConfig.FromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Resources", "Data", "config.json"))).restore;

        [Test]
        public void ShippedConfig_IsPlayable()
        {
            var c = Shipped();
            Assert.IsNotNull(c);
            Assert.IsTrue(c.rooms.Exists(r => r.start) && c.rooms.Exists(r => r.first) && c.rooms.Exists(r => r.last));
            var given = new List<string>();
            foreach (var t in c.tasks) if (!string.IsNullOrEmpty(t.gives)) given.AddRange(t.gives.Split(','));
            float hours = 0;
            foreach (var t in c.tasks)
            {
                Assert.IsTrue(c.rooms.Exists(r => r.id == t.room), t.id + " room");
                Assert.IsFalse(string.IsNullOrEmpty(t.spot), t.id + " needs a spot in the base");
                if (!string.IsNullOrEmpty(t.needItem)) CollectionAssert.Contains(given, t.needItem, t.id + " needs an item nobody can find");
                hours += t.hours;
            }
            Assert.GreaterOrEqual(hours, 30f, "restoring should take 3-4 days, not one");
            Assert.LessOrEqual(hours, 56f);
            Assert.Greater(c.relayHour2, c.relayHour1);
        }

        [Test]
        public void ShippedConfig_CanBeFinished()
        {
            // a greedy player who always has every item still restores everything and can send
            var s = new RestoreSim(Shipped(), 7);
            var bag = new List<string>();
            foreach (var t in s.Config.tasks) if (!string.IsNullOrEmpty(t.gives)) bag.AddRange(t.gives.Split(','));
            for (int guard = 0; guard < 400 && s.StateOf(s.Config.rooms.Find(r => r.last).id) != RoomState.Restored; guard++)
            {
                string next = null;
                foreach (var id in s.OpenTasks()) if (s.CanDo(id, bag, out _)) { next = id; break; }
                if (next == null) { s.Eat(); s.Drink(); s.Sleep(); continue; }
                s.Do(next, bag);
            }
            Assert.AreEqual(RoomState.Restored, s.StateOf("comms"));
            Assert.LessOrEqual(s.Day, 4, "all restoration fits in about four days of work");
            Assert.GreaterOrEqual(s.Day, 3, "and needs at least three");
        }

        [Test]
        public void Warnings_ListDangerBeforeLoss()
        {
            var s = new RestoreSim(Cfg(), 1);
            var ok = new Stock { o2 = 6, water = 6, food = 6, power = 4 };
            var ration = new Stock { water = 2, food = 2 };
            CollectionAssert.IsEmpty(s.Warnings(ok, 2, 1, ration, false));
            var low = new Stock { o2 = 1.5f, water = 1, food = 6, power = 0.5f };
            var w = s.Warnings(low, 2, 1, ration, true);
            CollectionAssert.Contains(w, "restore.warn.o2");
            CollectionAssert.Contains(w, "restore.warn.power");
            CollectionAssert.Contains(w, "restore.warn.water");
            CollectionAssert.DoesNotContain(w, "restore.warn.food");
            CollectionAssert.Contains(w, "restore.warn.flare");
            s.Spend(14f, 14f);
            CollectionAssert.Contains(s.Warnings(ok, 2, 1, ration, false), "restore.warn.rest_me");
        }

        [Test]
        public void OpenTasks_ListsOnlyReachableUndone()
        {
            var s = new RestoreSim(Cfg(), 1);
            CollectionAssert.AreEquivalent(new[] { "chip", "power_wire" }, new List<string>(s.OpenTasks()));
        }
    }
}
