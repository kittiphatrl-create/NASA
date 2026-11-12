# PROTOTYPE - NOT FOR PRODUCTION
# Balance model of the 7-day (168 h) Mars outpost. Mirrors Assets/Scripts/Core/OutpostSim.cs;
# the C# EditMode tests are the authority. Run: python tools/balance_sim.py
import json, os, random, itertools

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CFG = json.load(open(os.path.join(ROOT, "Assets/Resources/Data/config.json"), encoding="utf-8"))
O = CFG["outpost"]
KEYS = ("air", "power", "shelter", "supplies", "energy")


class Sim:
    def __init__(self, seed, cabin=50, items=(), pick=0, heed=True, incidents_ok=True):
        self.o = O
        s = O["start"]
        self.v = {k: float(s[k]) for k in KEYS}
        c1 = CFG["chapter1"]
        self.v["air"] += round((cabin - c1["carryO2Pivot"]) * c1["carryO2Factor"])
        self.flags, self.sol = set(), set()
        self.flags |= {"catalyst_installed", "drill_ready", "has_hull_patch"}  # puzzles solved
        for sid in items:
            sup = next(x for x in c1["supplies"] if x["id"] == sid)
            for k, d in sup["delta"].items(): self.v[k] += d
            if sup.get("flag"): self.flags.add(sup["flag"])
        self.clamp()
        self.systems = {x["id"]: x["onAtStart"] for x in O["systems"]}
        self.H, self.eff, self.dose = 0, 1.0, 0.0
        self.collapses, self.zero_supplies, self.gh_hours = 0, 0, 0
        self.outcome = None
        self.coords = 0  # 0 none, 1 pending, 2 acknowledged
        self.actions_today, self.done_incidents = 0, set()
        self.pick, self.heed, self.incidents_ok = pick, heed, incidents_ok
        self.A = {a["id"]: a for a in O["actions"]}
        rng = random.Random(seed)
        deck = [e["id"] for e in O["nightEvents"]]
        self.nights, last, d = {}, None, len(deck)
        for day in O["randomEventDays"]:
            if d >= len(deck):
                rng.shuffle(deck); d = 0
                if len(deck) > 1 and deck[0] == last: deck[0], deck[-1] = deck[-1], deck[0]
            self.nights[day] = last = deck[d]; d += 1
        self.enc = [x["id"] for x in O["dilemmas"] if x.get("random")]
        random.Random(seed * 31 + 7).shuffle(self.enc)
        self.enc_used, self.choices = set(), []
        self.spe_done = False

    # ---------------------------------------------------------------- clock
    def clock(self, h=None): return (O["landingClock"] + (self.H if h is None else h)) % 24
    def day(self, h=None): return 1 + (self.H if h is None else h) // 24
    def clamp(self):
        for k in KEYS: self.v[k] = max(0.0, min(O["maxValue"], self.v[k]))
    def has(self, f): return f in self.flags or f in self.sol

    def tick(self, sleeping):
        o, v, c = self.o, self.v, self.clock()
        if o["daylightFrom"] <= c < o["daylightTo"]: v["power"] += o["solarPerHour"] * self.eff
        for sysd in o["systems"]:
            if not self.systems[sysd["id"]]: continue
            if v["power"] >= sysd["powerPerHour"]: v["power"] -= sysd["powerPerHour"]
            else: self.systems[sysd["id"]] = False
        v["air"] -= o["airUsePerHour"]
        if self.systems["life_support"]: v["air"] += o["lifeSupportAirPerHour"]
        v["supplies"] -= o["suppliesUsePerHour"]
        v["energy"] += o["energySleepPerHour"] if sleeping else o["energyAwakePerHour"] - (1 if v["supplies"] <= 0 else 0)
        if "greenhouse_started" in self.flags and self.systems["greenhouse"]:
            self.gh_hours += 1
            if self.gh_hours >= o["greenhouseHoursToReady"]: self.flags.add("greenhouse_ready")
        self.dose += o["radPerHour"]
        self.clamp()
        self.zero_supplies = self.zero_supplies + 1 if v["supplies"] <= 0 else 0
        self.H += 1
        self.after_hour()

    def after_hour(self):
        o, v = self.o, self.v
        if v["air"] <= 0: self.outcome = self.outcome or "failed_air"
        if self.zero_supplies >= o["suppliesZeroFailHours"]: self.outcome = self.outcome or "failed_supplies"
        if self.outcome: return
        if self.clock() == o["nightEventClock"]:
            d = self.day()  # 02:00 still belongs to this day: days change at 06:00
            eid = self.nights.get(d)
            if eid:
                e = next(x for x in o["nightEvents"] if x["id"] == eid)
                ok = (not e.get("preventFlag") or self.has(e["preventFlag"])) and v["shelter"] >= e.get("preventMinShelter", 0)
                if not self.heed and e.get("preventFlag"): ok = False
                if not ok:
                    for k, dv in e.get("penalty", {}).items(): v[k] += dv
                    self.eff -= e.get("efficiencyLoss", 0); self.dose += e.get("dose", 0)
                    self.clamp()
        spe = o["spe"]
        if self.H == spe["hour"] and not self.spe_done:
            self.spe_done = True
            p = v["shelter"] / 100.0 * (1 if self.systems["shelter"] else spe["unpoweredShelterFactor"])
            self.dose += spe["dose"] * (1 - p); v["energy"] -= spe["energyLoss"] * (1 - p); v["air"] -= spe["airLoss"] * (1 - p); v["power"] -= spe.get("powerLoss", 0) * (1 - p)
            self.clamp()
        if self.H % o["rescuePassEveryHours"] == 0 and self.coords == 1 and self.systems["beacon"]: self.coords = 2
        if self.H % 24 == 0:
            self.sol.clear(); self.actions_today = 0
            self.eff = max(o["minEfficiency"], self.eff - o["dustPerDay"])
        if self.H >= o["hoursTotal"]:
            self.outcome = "rescued" if self.coords == 2 else "stranded"

    def advance(self, n, sleeping=False):
        """Returns False when it stopped early (low-air alarm wakes a sleeping astronaut)."""
        start_air = self.v["air"]
        for _ in range(n):
            if self.outcome: return False
            crit = self.o.get("critAir", 10)
            if sleeping and ((self.v["air"] <= crit and start_air > crit) or (self.v["air"] <= self.o["warnAir"] and start_air > self.o["warnAir"])):
                return False
            self.tick(sleeping)
            if not sleeping and self.v["energy"] <= 0 and not self.outcome:
                self.collapses += 1; self.advance(self.o["collapseSleepHours"], True); return False
        return True

    # ---------------------------------------------------------------- actions
    def hours_for(self, a):
        return a["hours"] + (self.o["lowEnergyExtraHours"] if self.v["energy"] < self.o["lowEnergy"] else 0)

    def can(self, aid):
        a = self.A[aid]; v = self.v
        if a.get("minDay", 0) > self.day(): return False
        if a.get("requiresFlag") and a["requiresFlag"] not in self.flags: return False
        f = a.get("setsFlag")
        if f and self.has(f): return False
        if a.get("needsSystem") and not self.systems[a["needsSystem"]]: return False
        d = a["delta"]
        for k in ("power", "supplies"):
            if d.get(k, 0) < 0 and v[k] < -d[k]: return False
        if d.get("energy", 0) <= 0 and v["energy"] <= 0: return False
        if d.get("shelter", 0) > 0 and v["shelter"] >= self.o["maxValue"]: return False
        if d.get("energy", 0) > 0 and v["energy"] >= self.o["maxValue"]: return False
        if aid == "send_coords" and self.coords: return False
        return self.H + self.hours_for(a) <= self.o["hoursTotal"]

    def do(self, aid):
        a = self.A[aid]; h = self.hours_for(a)
        for k, dv in a["delta"].items(): self.v[k] += dv
        self.clamp()
        if a.get("cleansPanels"): self.eff = 1.0
        if a.get("setsFlag"): (self.sol if a.get("solFlag") else self.flags).add(a["setsFlag"])
        if a.get("consumesFlag"): self.flags.discard(a["requiresFlag"])
        if a.get("outside"): self.dose += self.o["radPerOutsideHour"] * h
        if aid == "send_coords": self.coords = 1
        self.actions_today += 1
        for inc in self.o["incidents"]:
            if inc["day"] == self.day() and self.actions_today >= inc["afterActions"] and inc["id"] not in self.done_incidents:
                self.done_incidents.add(inc["id"])
                if not self.incidents_ok:
                    for k, dv in inc["penalty"].items(): self.v[k] += dv
                    self.clamp()
        self.advance(h)

    def dilemma(self):
        o = self.o
        fixed = [d for d in o["dilemmas"] if not d.get("random") and d.get("day") == self.day()]
        todays = fixed
        if not fixed and self.day() in o["encounterDays"]:
            for eid in self.enc:
                d = next(x for x in o["dilemmas"] if x["id"] == eid)
                if eid in self.enc_used or (d.get("requiresFlag") and d["requiresFlag"] not in self.flags): continue
                self.enc_used.add(eid); todays = [d]; break
        for d in todays:
            ok = [x for x in d["options"] if not x.get("needFlag") or x["needFlag"] in self.flags]
            if d.get("random"):
                val = lambda x: sum(x["delta"].get(k, 0) for k in ("air", "power", "supplies", "shelter")) + 0.5 * x["delta"].get("energy", 0) - 6 * x.get("hours", 0) - 5 * x.get("dose", 0)
                x = max(ok, key=val)
            else:
                x = d["options"][min(self.pick, len(d["options"]) - 1)]
            for k, dv in x["delta"].items(): self.v[k] += dv
            self.clamp(); self.dose += x.get("dose", 0)
            if x.get("setsFlag"): (self.sol if x.get("solFlag") else self.flags).add(x["setsFlag"])
            self.choices.append((self.day(), d["id"], x["id"]))
            if x.get("hours"): self.advance(x["hours"])

    def policy(self):
        v, o, c = self.v, self.o, self.clock()
        spe = o["spe"]
        self.systems["greenhouse"] = ("greenhouse_started" in self.flags and "greenhouse_ready" not in self.flags
                                      and o["daylightFrom"] <= c < o["daylightTo"] and v["power"] > 30)
        self.systems["beacon"] = self.coords == 1 or ("beacon_repaired" in self.flags and self.coords == 0)
        self.systems["shelter"] = spe["hour"] - 2 <= self.H <= spe["hour"] and v["power"] > 10
        self.systems["life_support"] = True
        tonight = self.nights.get(self.day())
        wants = []
        night_power = (24 + 6 - 19) * o["systems"][0]["powerPerHour"]  # keep life support running overnight
        if v["air"] < 40 or (c >= 18 and v["air"] < 55): wants.append("moxie")
        if c >= 17 and v["power"] < night_power and "clean_panels" not in wants and self.eff < 0.85: wants.append("clean_panels")
        if v["supplies"] < 35: wants += ["drill_ice", "sabatier", "recycle"]
        if v["energy"] < 35: wants.append("eat")
        if self.heed and tonight and c >= 14:
            e = next(x for x in o["nightEvents"] if x["id"] == tonight)
            if e.get("preventFlag") == "panels_stowed": wants.append("stow_panels")
            if e.get("preventFlag") == "seals_inspected": wants.append("inspect_seals")
            if e.get("preventMinShelter", 0) > v["shelter"]: wants += ["shelter_kit", "shelter_water", "shelter_regolith"]
        if self.eff < 0.7: wants.append("clean_panels")
        if self.heed and v["shelter"] < 75 and self.H < spe["hour"]: wants += ["shelter_kit", "shelter_water" if v["supplies"] > 45 else "shelter_regolith", "shelter_regolith"]
        wants += ["repair_beacon_chip", "repair_beacon", "send_coords", "setup_greenhouse_seeds", "setup_greenhouse"]
        wants += [a["id"] for a in o["actions"] if a.get("science")]
        if v["air"] < 75: wants.append("moxie")
        if v["supplies"] < 70: wants += ["drill_ice", "sabatier", "recycle"]
        for w in wants:
            if self.can(w):
                d = self.A[w]["delta"]
                if d.get("power", 0) < 0 and v["power"] + d["power"] < 15: continue
                return w
        return None

    def play(self):
        last_day = 0
        while not self.outcome:
            if self.day() != last_day and self.clock() >= 6:
                last_day = self.day(); self.dilemma()
                if self.outcome: break
            c = self.clock()
            if c >= 22 or c < 6:
                self.systems["greenhouse"] = False
                if v_low := (self.v["air"] <= self.o["warnAir"]):
                    pass
                if not v_low or not self.can("moxie"):
                    self.advance((24 + 6 - c) % 24 or 24, True); continue
            aid = self.policy()
            if aid: self.do(aid)
            else: self.advance(1)
        return self.outcome


if __name__ == "__main__":
    ITEMS_STRONG = ["sup_o2", "sup_food", "sup_battery", "sup_shield", "sup_cell"]
    for heed in (True, False):
        print("== heed forecasts" if heed else "== ignore forecasts")
        for cabin, items, inc, pick in itertools.product([10, 50, 100], [(), ITEMS_STRONG], [True, False], [0, 1]):
            res = [Sim(s, cabin, items, pick, heed, inc) for s in range(40)]
            outs = [r.play() for r in res]
            n = {k: outs.count(k) for k in set(outs)}
            minair = min(r.v["air"] for r in res); maxdose = max(r.dose for r in res)
            stars = sum(1 for r in res if r.outcome == "rescued" and r.dose <= O["radSafe"] and r.collapses == 0)
            print(f"  cabin={cabin:3} loot={'strong' if items else 'none':6} incOK={inc!s:5} pick={pick} {n} healthy={stars} endAir>={minair:.0f} dose<={maxdose:.0f}")
