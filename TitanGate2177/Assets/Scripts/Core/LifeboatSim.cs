// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is Chapter 2 more fun as a read-and-choose survival diary (visitors, a pet, shared rations)
//           than as a walk-and-click outpost?
// Date: 2026-11-11
using System;
using System.Collections.Generic;

namespace TitanGate.Core
{
    /// <summary>The four things the lifeboat crew lives on. One unit is enough for one person for one day.</summary>
    [Serializable]
    public class Stock
    {
        public float o2, water, food, power;

        public void Add(Stock d)
        {
            if (d == null) return;
            o2 += d.o2; water += d.water; food += d.food; power += d.power;
        }

        public bool IsZero => o2 == 0 && water == 0 && food == 0 && power == 0;
    }

    /// <summary>A pet the cadet can take along. Its trait decides which hints and options it gives.</summary>
    [Serializable]
    public class PetDef
    {
        public string id;
        /// <summary>brave, smart or careful.</summary>
        public string trait;
        public float foodPerDay = 1;
    }

    /// <summary>A Martian visitor who can be let into the lifeboat. Fiction: no life has been found on Mars.</summary>
    [Serializable]
    public class VisitorDef
    {
        public string id;
        /// <summary>What the visitor uses each day inside. Oxygen is used whether it is fed or not.</summary>
        public Stock perDay = new Stock();
        /// <summary>What a fed visitor gives back each day.</summary>
        public Stock giftPerDay = new Stock();
        /// <summary>Steals at night, then leaves. A fed brave pet or a locked locker stops it.</summary>
        public bool steals;
        public float stealFood;
        /// <summary>Not a living visitor at all (fiction). Letting it in ends the run at once.</summary>
        public bool monster;
        /// <summary>Scanners that read something wrong on this visitor. A monster fails at least two.</summary>
        public List<string> failsScans = new List<string>();
    }

    /// <summary>One answer on a card.</summary>
    [Serializable]
    public class LifeOption
    {
        public string id;
        public Stock delta = new Stock();
        /// <summary>Supply flag from the 60-second scramble that this answer needs (never the only answer).</summary>
        public string needItem;
        public bool consumesItem;
        /// <summary>Pet trait this answer needs.</summary>
        public string needTrait;
        /// <summary>Visitor let in by this answer.</summary>
        public string admits;
        public string setsFlag;
        /// <summary>Story flag this answer needs (something done on an earlier card).</summary>
        public string needFlag;
        /// <summary>Guest who must be aboard for this answer.</summary>
        public string needGuest;
        /// <summary>Change to the cadet's health (negative = this answer costs health).</summary>
        public int health;
    }

    /// <summary>A situation to read and decide on. Cards of a day are shown in list order.</summary>
    [Serializable]
    public class LifeCard
    {
        public string id;
        public int day;
        /// <summary>Visitor at the airlock, or empty.</summary>
        public string visitor;
        /// <summary>How many clue lines the card shows (life.card.ID.clue1..N).</summary>
        public int clues;
        /// <summary>The card only comes up when this flag is set / is not set / this guest is aboard.</summary>
        public string ifFlag, unlessFlag, ifGuest;
        public List<LifeOption> options = new List<LifeOption>();
    }

    /// <summary>Something that happens during one night (the solar storm). A flag set beforehand makes it harmless.</summary>
    [Serializable]
    public class LifeNightDef
    {
        public string id;
        public int day;
        public string safeFlag;
        public int healthLoss;
        public Stock delta = new Stock();
    }

    /// <summary>What one stowed supply from Chapter 1 adds to the lifeboat stock.</summary>
    [Serializable]
    public class LifeSupplyDef
    {
        public string id;
        public Stock stock = new Stock();
    }

    /// <summary>The "lifeboat" section of config.json: every number of the card-based Chapter 2.</summary>
    [Serializable]
    public class LifeboatConfig
    {
        public int daysTotal = 7;
        /// <summary>The prototype stops after this many days (0 = play all of daysTotal).</summary>
        public int sliceDays = 2;
        public int health = 3;
        public Stock start = new Stock();
        /// <summary>What the cadet uses each day.</summary>
        public Stock crewPerDay = new Stock();
        public float lifeSupportPower = 1, solarPerDay = 1;
        /// <summary>Extra oxygen used per day when the scrubber has no power, and when its filter is clogged.</summary>
        public float o2PenaltyNoPower = 1, o2PenaltyClogged = 1;
        public List<PetDef> pets = new List<PetDef>();
        public List<VisitorDef> visitors = new List<VisitorDef>();
        public List<LifeCard> cards = new List<LifeCard>();
        public List<LifeSupplyDef> supplies = new List<LifeSupplyDef>();
        /// <summary>Supplies used when the prototype is started from the menu, without playing the scramble.</summary>
        public List<string> demoStowed = new List<string>();
        public List<LifeNightDef> nights = new List<LifeNightDef>();
        /// <summary>The oxygen maker, an evening choice: it uses this much power and makes this much O2.</summary>
        public Stock o2Maker = new Stock();
        /// <summary>Flags that decide the ending: the beacon works, and the coordinates were sent.</summary>
        public string beaconFlag = "beacon_fixed", rescueFlag = "coords_sent";
        /// <summary>Flag that earns the third star (helped someone who needed it).</summary>
        public string kindFlag = "nok_helped";
        /// <summary>Scanners at the hatch (thermal, breath, heartbeat). Each visitor can be checked with this many.</summary>
        public List<string> scans = new List<string>();
        public int scansPerVisitor = 1;
        /// <summary>Extra scans when the pet with this trait is fed (it notices what to check).</summary>
        public string extraScanTrait = "careful";
        /// <summary>The landing leads into this diary instead of the walk-and-click outpost.</summary>
        public bool replacesOutpost = true;
        /// <summary>Play the week by walking around a top-down base: each card waits at a place in the base.</summary>
        public bool walkBase = true;
        /// <summary>Real seconds a knocking visitor waits, and a storm alarm lasts, before the last option is taken.</summary>
        public float visitorWaitSeconds = 45f, alarmSeconds = 30f;
        /// <summary>Hands-on jobs in the base. Each can be done once a day.</summary>
        public Stock taskSolar = new Stock { o2 = -0.5f, power = 1 };
        public Stock taskDrill = new Stock { power = -1, water = 1 };
        public Stock taskPlant = new Stock { water = -1 };
        public Stock taskHarvest = new Stock { food = 2 };
        public int growNights = 2, seedsBase = 2, seedsWithPack = 3;

        public PetDef GetPet(string id) => pets.Find(p => p.id == id);
        public VisitorDef GetVisitor(string id) => visitors.Find(v => v.id == id);
        public LifeCard GetCard(string id) => cards.Find(c => c.id == id);
        public int LastDay => sliceDays > 0 && sliceDays < daysTotal ? sliceDays : daysTotal;
    }

    /// <summary>Survived = a shortened prototype run ended alive. Rescued / Stranded are the endings of the full week.</summary>
    public enum LifeOutcome { Ongoing, Survived, Rescued, Stranded, FailedAir, FailedHealth, FailedMonster }

    public enum LifeLogKind { Choice, GuestJoined, GuestLeftHungry, Gift, Theft, TheftStopped, Hungry, PetHungry, NoPower, NightSafe, NightHit, MakerRan, Scan }

    public class LifeLog
    {
        public int day;
        public LifeLogKind kind;
        /// <summary>Card, visitor or pet the entry is about.</summary>
        public string id;
        /// <summary>Option chosen, or item stolen.</summary>
        public string detail;
        public float value;
    }

    public class LifeboatState
    {
        public readonly Stock stock = new Stock();
        public int day = 1;
        public int health;
        public int morale = 75;
        public readonly DailySchedule schedule = new DailySchedule();
        public string pet;
        /// <summary>A pet that was not fed last night gives no hints and will not do brave things today.</summary>
        public bool petFed = true;
        public LifeOutcome outcome = LifeOutcome.Ongoing;
        public readonly List<string> guests = new List<string>();
        public readonly HashSet<string> items = new HashSet<string>();
        public readonly HashSet<string> flags = new HashSet<string>();
        public readonly HashSet<string> answered = new HashSet<string>();
        public readonly List<LifeLog> log = new List<LifeLog>();
        /// <summary>Scanners already used on each visitor card.</summary>
        public readonly Dictionary<string, List<string>> scanned = new Dictionary<string, List<string>>();
        /// <summary>The monster that was let in, when the run failed that way.</summary>
        public string monsterLetIn;
    }

    /// <summary>
    /// Rules of the card-based survival week. Each day: answer the day's cards, decide who eats and drinks,
    /// then the night settles everything. Pure C#, no Unity types, so it runs in EditMode tests.
    /// </summary>
    public class LifeboatSim
    {
        public const string Me = "me", Pet = "pet";

        readonly LifeboatConfig _c;
        public LifeboatState State { get; } = new LifeboatState();
        public LifeboatConfig Config => _c;

        public LifeboatSim(LifeboatConfig c, string pet, Stock bonus = null, IEnumerable<string> items = null)
        {
            _c = c;
            State.stock.Add(c.start);
            State.stock.Add(bonus);
            State.health = c.health;
            State.pet = c.GetPet(pet) != null ? pet : null;
            if (items != null) foreach (var i in items) State.items.Add(i);
        }

        /// <summary>Stock bonus from the supplies stowed in the 60-second scramble.</summary>
        public static Stock Bonus(LifeboatConfig c, IEnumerable<string> stowed)
        {
            var s = new Stock();
            if (stowed == null) return s;
            foreach (var id in stowed) s.Add(c.supplies.Find(x => x.id == id)?.stock);
            return s;
        }

        public string PetTrait => State.pet == null ? null : _c.GetPet(State.pet).trait;

        // ------------------------------------------------------------------ cards

        /// <summary>The next card of today that has not been answered, or null when it is time to share rations.</summary>
        public LifeCard NextCard()
        {
            if (State.outcome != LifeOutcome.Ongoing) return null;
            foreach (var c in _c.cards)
                if (c.day == State.day && !State.answered.Contains(c.id) && Applies(c)) return c;
            return null;
        }

        /// <summary>Does the card's condition hold right now? A card later in the day can be opened by an earlier answer.</summary>
        public bool Applies(LifeCard c) =>
            (string.IsNullOrEmpty(c.ifFlag) || State.flags.Contains(c.ifFlag))
            && (string.IsNullOrEmpty(c.unlessFlag) || !State.flags.Contains(c.unlessFlag))
            && (string.IsNullOrEmpty(c.ifGuest) || State.guests.Contains(c.ifGuest));

        /// <summary>Cards of today: how many are answered, and how many there are as things stand now.</summary>
        public void CardProgress(out int done, out int total)
        {
            done = total = 0;
            foreach (var c in _c.cards)
            {
                if (c.day != State.day) continue;
                if (State.answered.Contains(c.id)) { done++; total++; }
                else if (Applies(c)) total++;
            }
        }

        /// <summary>What the pet says about a card (a localisation key), or null: no pet, or it went hungry.</summary>
        public string HintKey(LifeCard card) =>
            card == null || State.pet == null || !State.petFed ? null : $"life.card.{card.id}.hint.{PetTrait}";

        /// <summary>Can this answer be picked? reason is a localisation key when false.</summary>
        public bool CanChoose(LifeOption o, out string reason)
        {
            reason = null;
            var s = State.stock;
            if (!string.IsNullOrEmpty(o.needItem) && !State.items.Contains(o.needItem)) { reason = "life.need.item." + o.needItem; return false; }
            if (!string.IsNullOrEmpty(o.needTrait))
            {
                if (PetTrait != o.needTrait) { reason = "life.need.trait." + o.needTrait; return false; }
                if (!State.petFed) { reason = "life.need.pet_fed"; return false; }
            }
            if (!string.IsNullOrEmpty(o.needFlag) && !State.flags.Contains(o.needFlag)) { reason = "life.need.flag." + o.needFlag; return false; }
            if (!string.IsNullOrEmpty(o.needGuest) && !State.guests.Contains(o.needGuest)) { reason = "life.need.guest." + o.needGuest; return false; }
            if (o.health < 0 && State.health + o.health <= 0) { reason = "life.need.health"; return false; }
            if (s.o2 + o.delta.o2 < 0 || s.water + o.delta.water < 0 || s.food + o.delta.food < 0 || s.power + o.delta.power < 0)
            {
                reason = "life.need.stock";
                return false;
            }
            // spending air, or letting one more breather in, must still leave enough air for tonight
            bool joins = !string.IsNullOrEmpty(o.admits) && _c.GetVisitor(o.admits) != null && !State.guests.Contains(o.admits);
            if (joins || o.delta.o2 < 0)
            {
                float need = O2Tonight + (joins ? _c.GetVisitor(o.admits).perDay.o2 : 0);
                if (need > s.o2 + o.delta.o2) { reason = "life.need.air"; return false; }
            }
            return true;
        }

        /// <summary>Answer a card. Returns the option, or null when it cannot be picked.</summary>
        // ------------------------------------------------------------------ checking a visitor

        /// <summary>How many scans this visitor card allows in total (one more with a fed careful pet).</summary>
        public int ScanBudget(LifeCard card)
        {
            if (card == null || string.IsNullOrEmpty(card.visitor) || _c.scans.Count == 0) return 0;
            bool extra = PetTrait == _c.extraScanTrait && State.petFed;
            return Math.Min(_c.scans.Count, _c.scansPerVisitor + (extra ? 1 : 0));
        }

        public IReadOnlyList<string> ScansDone(LifeCard card) =>
            card != null && State.scanned.TryGetValue(card.id, out var l) ? l : (IReadOnlyList<string>)Array.Empty<string>();

        public int ScansLeft(LifeCard card) => ScanBudget(card) - ScansDone(card).Count;

        public bool CanScan(LifeCard card, string scan) =>
            State.outcome == LifeOutcome.Ongoing && card != null && card == NextCard() && _c.scans.Contains(scan)
            && ScansLeft(card) > 0 && !(State.scanned.TryGetValue(card.id, out var done) && done.Contains(scan));

        /// <summary>Point a scanner at the visitor. Returns true when the reading is wrong for a living being.</summary>
        public bool? Scan(string cardId, string scan)
        {
            var card = _c.GetCard(cardId);
            if (!CanScan(card, scan)) return null;
            if (!State.scanned.TryGetValue(card.id, out var l)) State.scanned[card.id] = l = new List<string>();
            l.Add(scan);
            bool odd = IsOdd(card.visitor, scan);
            State.log.Add(new LifeLog { day = State.day, kind = LifeLogKind.Scan, id = card.visitor, detail = scan, value = odd ? 1 : 0 });
            return odd;
        }

        public bool IsOdd(string visitor, string scan) => _c.GetVisitor(visitor)?.failsScans.Contains(scan) ?? false;

        /// <summary>
        /// A small hands-on job done while walking around the base (clean panels, drill ice, plant, harvest).
        /// Every job trades one stock for another. Returns false if a cost cannot be paid. Stocks never go below zero.
        /// </summary>
        public bool ApplyTask(Stock delta, float hours = 0, float reservedHours = 0)
        {
            if (delta == null || State.outcome != LifeOutcome.Ongoing) return false;
            var s = State.stock;
            if (s.o2 + delta.o2 < 0 || s.water + delta.water < 0 || s.food + delta.food < 0 || s.power + delta.power < 0) return false;
            if (!State.schedule.TrySpend(hours, reservedHours)) return false;
            s.Add(delta);
            return true;
        }

        public LifeOption Choose(string cardId, string optionId)
        {
            var card = _c.GetCard(cardId);
            var o = card?.options.Find(x => x.id == optionId);
            if (o == null || State.outcome != LifeOutcome.Ongoing || card.day != State.day || State.answered.Contains(cardId)) return null;
            if (!CanChoose(o, out _)) return null;
            State.schedule.TrySpend(Math.Min(1, State.schedule.Remaining));
            State.answered.Add(cardId);
            State.stock.Add(o.delta);
            State.health = Math.Min(_c.health, State.health + o.health);
            if (o.consumesItem && !string.IsNullOrEmpty(o.needItem)) State.items.Remove(o.needItem);
            if (!string.IsNullOrEmpty(o.setsFlag)) State.flags.Add(o.setsFlag);
            State.log.Add(new LifeLog { day = State.day, kind = LifeLogKind.Choice, id = cardId, detail = optionId });
            if (!string.IsNullOrEmpty(o.admits) && _c.GetVisitor(o.admits) != null && !State.guests.Contains(o.admits))
            {
                if (_c.GetVisitor(o.admits).monster)
                {
                    State.monsterLetIn = o.admits;
                    State.outcome = LifeOutcome.FailedMonster;
                    return o;
                }
                State.guests.Add(o.admits);
                State.log.Add(new LifeLog { day = State.day, kind = LifeLogKind.GuestJoined, id = o.admits });
            }
            return o;
        }

        // ------------------------------------------------------------------ rations

        /// <summary>Everyone who can be given food and water tonight: me, the pet, then each guest.</summary>
        public List<string> Mouths()
        {
            var m = new List<string> { Me };
            if (State.pet != null) m.Add(Pet);
            m.AddRange(State.guests);
            return m;
        }

        /// <summary>Food and water one mouth takes when fed.</summary>
        public Stock RationOf(string who)
        {
            if (who == Me) return new Stock { water = _c.crewPerDay.water, food = _c.crewPerDay.food };
            if (who == Pet) return new Stock { food = State.pet == null ? 0 : _c.GetPet(State.pet).foodPerDay };
            var v = _c.GetVisitor(who);
            return v == null ? new Stock() : new Stock { water = v.perDay.water, food = v.perDay.food };
        }

        /// <summary>Food and water needed to feed everyone in the plan.</summary>
        public Stock RationNeed(ICollection<string> fed)
        {
            var need = new Stock();
            foreach (var who in Mouths()) if (fed.Contains(who)) need.Add(RationOf(who));
            return need;
        }

        public bool CanFeed(ICollection<string> fed)
        {
            var need = RationNeed(fed);
            return need.water <= State.stock.water && need.food <= State.stock.food;
        }

        /// <summary>Can the oxygen maker run this evening?</summary>
        public bool CanRunMaker => _c.o2Maker.o2 > 0 && State.stock.power >= _c.o2Maker.power;

        /// <summary>Will the scrubber have power tonight (after today's sun), with or without running the oxygen maker first?</summary>
        public bool PowerTonightWith(bool runMaker) =>
            State.stock.power - (runMaker && CanRunMaker ? _c.o2Maker.power : 0) + _c.solarPerDay >= _c.lifeSupportPower;

        public bool PowerTonight => PowerTonightWith(false);

        /// <summary>Oxygen everyone inside breathes tonight. Air cannot be rationed: a guest inside always uses it.</summary>
        public float O2TonightWith(bool runMaker)
        {
            float o2 = _c.crewPerDay.o2;
            foreach (var g in State.guests) o2 += _c.GetVisitor(g).perDay.o2;
            if (State.flags.Contains("filter_clogged")) o2 += _c.o2PenaltyClogged;
            if (!PowerTonightWith(runMaker)) o2 += _c.o2PenaltyNoPower;
            return o2;
        }

        public float O2Tonight => O2TonightWith(false);

        /// <summary>Whole days the stock lasts for the crew as it is now, if everyone is fed every day.</summary>
        public int DaysOfSupplies()
        {
            var need = RationNeed(Mouths());
            float o2 = O2Tonight;
            float days = float.MaxValue;
            if (o2 > 0) days = Math.Min(days, State.stock.o2 / o2);
            if (need.water > 0) days = Math.Min(days, State.stock.water / need.water);
            if (need.food > 0) days = Math.Min(days, State.stock.food / need.food);
            return days == float.MaxValue ? 99 : (int)Math.Floor(days);
        }

        // ------------------------------------------------------------------ night

        /// <summary>
        /// Close the day: share rations with the mouths in the plan, then the night happens.
        /// Returns what happened, or null when today's cards are not all answered or the plan cannot be paid.
        /// runMaker = run the oxygen maker this evening first (ignored when there is not enough power).
        /// </summary>
        public List<LifeLog> EndDay(ICollection<string> fed, bool runMaker = false)
        {
            var s = State;
            if (s.outcome != LifeOutcome.Ongoing || NextCard() != null || !CanFeed(fed)) return null;
            var night = new List<LifeLog>();
            void Note(LifeLogKind kind, string id, string detail = null, float value = 0)
            {
                var e = new LifeLog { day = s.day, kind = kind, id = id, detail = detail, value = value };
                s.log.Add(e);
                night.Add(e);
            }

            if (runMaker && CanRunMaker)
            {
                s.stock.power -= _c.o2Maker.power;
                s.stock.o2 += _c.o2Maker.o2;
                Note(LifeLogKind.MakerRan, "o2_maker", null, _c.o2Maker.o2);
            }
            float o2 = O2Tonight;
            bool powered = PowerTonight;
            var need = RationNeed(fed);
            s.stock.water -= need.water;
            s.stock.food -= need.food;

            if (!fed.Contains(Me)) { s.health--; Note(LifeLogKind.Hungry, Me); }
            if (s.pet != null)
            {
                s.petFed = fed.Contains(Pet);
                if (!s.petFed) Note(LifeLogKind.PetHungry, s.pet);
            }

            // the sun charges the battery, then life support runs through the night
            s.stock.power += _c.solarPerDay;
            if (powered) s.stock.power -= _c.lifeSupportPower;
            else Note(LifeLogKind.NoPower, "life_support", null, _c.o2PenaltyNoPower);

            foreach (var g in s.guests)
            {
                var v = _c.GetVisitor(g);
                if (fed.Contains(g) && !v.giftPerDay.IsZero)
                {
                    s.stock.Add(v.giftPerDay);
                    Note(LifeLogKind.Gift, g);
                }
            }

            bool airShort = o2 > s.stock.o2;
            s.stock.o2 = Math.Max(0, s.stock.o2 - o2);

            // visitors who steal do it while everyone sleeps, then leave
            bool guarded = PetTrait == "brave" && s.petFed;
            bool locked = s.flags.Contains("locker_locked");
            foreach (var g in new List<string>(s.guests))
            {
                var v = _c.GetVisitor(g);
                if (!v.steals) continue;
                s.guests.Remove(g);
                if (guarded || locked) { Note(LifeLogKind.TheftStopped, g, guarded ? "pet" : "lock"); continue; }
                float took = Math.Min(v.stealFood, s.stock.food);
                s.stock.food -= took;
                string item = null;
                var sorted = new List<string>(s.items);
                sorted.Sort(string.CompareOrdinal);
                if (sorted.Count > 0) { item = sorted[0]; s.items.Remove(item); }
                Note(LifeLogKind.Theft, g, item, took);
            }
            s.flags.Remove("locker_locked");   // the lock holds for one night

            foreach (var g in new List<string>(s.guests))
            {
                if (fed.Contains(g)) continue;
                s.guests.Remove(g);
                Note(LifeLogKind.GuestLeftHungry, g);
            }

            foreach (var n in _c.nights)
            {
                if (n.day != s.day) continue;
                if (!string.IsNullOrEmpty(n.safeFlag) && s.flags.Contains(n.safeFlag)) { Note(LifeLogKind.NightSafe, n.id); continue; }
                s.health -= n.healthLoss;
                s.stock.Add(n.delta);
                s.stock.o2 = Math.Max(0, s.stock.o2); s.stock.water = Math.Max(0, s.stock.water);
                s.stock.food = Math.Max(0, s.stock.food); s.stock.power = Math.Max(0, s.stock.power);
                Note(LifeLogKind.NightHit, n.id);
            }

            s.morale = Math.Max(0, Math.Min(100, s.morale + (night.Exists(e => e.kind == LifeLogKind.Hungry || e.kind == LifeLogKind.PetHungry) ? -15 : 3)));
            if (airShort) s.outcome = LifeOutcome.FailedAir;
            else if (s.health <= 0) s.outcome = LifeOutcome.FailedHealth;
            if (s.outcome == LifeOutcome.Ongoing)
            {
                if (s.day < _c.LastDay) { s.day++; s.schedule.Reset(); }
                else if (_c.LastDay < _c.daysTotal) s.outcome = LifeOutcome.Survived;
                else s.outcome = s.flags.Contains(_c.rescueFlag) ? LifeOutcome.Rescued : LifeOutcome.Stranded;
            }
            return night;
        }

        // ------------------------------------------------------------------ results

        /// <summary>Why no rescue came, as a key suffix for "life.end.stranded.*": the first rescue step still missing.</summary>
        public string StrandedReason =>
            State.flags.Contains(_c.rescueFlag) ? null : !State.flags.Contains(_c.beaconFlag) ? "no_beacon" : "not_sent";

        /// <summary>Debrief stars: rescued; in full health; helped someone who needed it (or still hosts a guest).</summary>
        public bool[] Stars()
        {
            bool won = State.outcome == LifeOutcome.Rescued;
            return new[]
            {
                won,
                won && State.health >= _c.health,
                won && (State.flags.Contains(_c.kindFlag) || State.guests.Count > 0),
            };
        }
    }
}
