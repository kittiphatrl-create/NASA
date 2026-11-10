# PROTOTYPE - NOT FOR PRODUCTION
# Quick balance check that mirrors Assets/Scripts/Core/SurvivalSim.cs (the C# tests are the authority).
# A simple "sensible player" policy plays every route over many event orders and Chapter 1 carry-overs.
# Run: python tools/balance_sim.py
import json, random, itertools, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CFG = json.load(open(os.path.join(ROOT, "Assets/Resources/Data/config.json"), encoding="utf-8"))
PREVENT = {"panels_stowed": "stow_panels", "seals_inspected": "inspect_seals", "shutters_closed": "close_shutters",
           "heaters_set": "set_heaters", "hab_insulated": "insulate", "rtg_checked": "check_rtg"}
O2A = {"mars": "moxie", "moon": "electrolyze", "titan": "electrolyze"}
PROG = {"mars": "sabatier", "moon": "transmit", "titan": "charge_gate"}
KEYS = ("o2", "water", "power", "food", "stamina", "fuel")


class Sim:
    def __init__(self, route, seed, cabin=50, supplies=False, incidents_ok=True, science=True, choice=0):
        self.r = next(x for x in CFG["routes"] if x["id"] == route)
        self.route, self.s = route, self.r["survival"]
        self.A = {a["id"]: a for a in self.r["actions"]}
        self.E = {e["id"]: e for e in self.r["events"]}
        s, c1 = self.s, CFG["chapter1"]
        self.st = dict(o2=s["startO2"], water=s["startWater"], power=s["startPower"], food=s["startFood"], stamina=s["startStamina"], fuel=0)
        self.st["o2"] += round((cabin - c1["carryO2Pivot"]) * c1["carryO2Factor"])
        if supplies:
            for k, v in c1["suppliesBonus"].items(): self.st[k] += v
        self.clamp()
        self.flags = {"catalyst_installed", "drill_ready", "has_hull_patch", "line_repaired", "antenna_ready", "gate_found"}
        self.incidents_ok, self.science, self.choice = incidents_ok, science, choice
        self.dose, self.collapses, self.hits = 0.0, 0, 0
        n = s["solsTotal"] - 1
        self.nights = [None] * n
        for e in self.r["events"]:
            if e.get("major"): self.nights[e["night"] - 1] = e["id"]
        deck = [e["id"] for e in self.r["events"] if not e.get("major")]
        rng = random.Random(seed); rng.shuffle(deck); d = 0; last = None
        for night in s["randomEventNights"]:
            if night > n or self.nights[night - 1]: continue
            if d >= len(deck):
                d = 0; rng.shuffle(deck)
                if len(deck) > 1 and deck[0] == last: deck[0], deck[-1] = deck[-1], deck[0]
            self.nights[night - 1] = last = deck[d]; d += 1
        self.eff, self.mf = 1.0, 1.0
        self.deck = [d["id"] for d in self.r.get("dilemmas", []) if d.get("random")]
        random.Random(seed * 31 + 7).shuffle(self.deck)
        self.used, self.picks = set(), []

    def clamp(self):
        for k in KEYS: self.st[k] = max(0, min(100 if k != "fuel" else self.s["fuelTarget"], self.st[k]))

    def tonight(self, sol):
        return self.E.get(self.nights[sol - 1]) if sol <= len(self.nights) else None

    def shield(self):
        f = self.s.get("shieldFlag")
        return self.s.get("shieldFactor", 1) if f and f in self.flags else 1

    def cost(self, a):
        return a.get("ap", 1) + (self.s["lowStaminaExtraAp"] if self.st["stamina"] < self.s["lowStaminaThreshold"] else 0)

    def can(self, aid, sol, ap, solf):
        a = self.A.get(aid)
        if not a: return False
        st, s = self.st, self.s
        if self.cost(a) > ap or a.get("minSol", 0) > sol: return False
        if a.get("requiresFlag") and a["requiresFlag"] not in self.flags: return False
        f = a.get("setsFlag")
        if f and (f in self.flags or f in solf): return False
        d = a.get("delta", {})
        if d.get("stamina", 0) <= 0 and st["stamina"] <= 0: return False
        if any(d.get(k, 0) < 0 and st[k] < -d[k] for k in ("power", "water", "food")): return False
        if d.get("fuel", 0) > 0 and st["fuel"] >= s["fuelTarget"]: return False
        if d.get("stamina", 0) > 0 and st["stamina"] >= 100: return False
        return True

    def do(self, aid, solf):
        a = self.A[aid]; st = self.st
        c = self.cost(a)
        for k, v in a.get("delta", {}).items(): st[k] += v
        self.clamp()
        if a.get("cleansPanels"): self.eff = 1.0
        if a.get("outside"): self.dose += self.s.get("radPerEva", 0)
        if a.get("setsFlag"): (solf if a.get("solFlag") else self.flags).add(a["setsFlag"])
        return c

    def pick(self, sol, ap, solf):
        st, s, ev = self.st, self.s, self.tonight(sol)
        need_pw = max(s["habPowerLoad"], ev.get("preventMinPower", 0) if ev else 0)
        nxt = self.tonight(sol + 1)
        if nxt and nxt.get("major"): need_pw += 15  # save up the day before the big night
        need_w = ev.get("preventMinWater", 0) if ev else 0
        def ok(aid):
            if not self.can(aid, sol, ap, solf): return False
            d = self.A[aid].get("delta", {})
            if d.get("power", 0) < 0 and st["power"] + d["power"] < need_pw: return False
            if d.get("water", 0) < 0 and st["water"] + d["water"] < need_w: return False
            return True
        wants = []
        if ev and ev.get("preventFlag") and ev["preventFlag"] not in solf: wants.append(PREVENT[ev["preventFlag"]])
        if st["o2"] < 50: wants.append(O2A[self.route])
        if st["water"] < 40 or (need_w and st["water"] < need_w + 5): wants += ["drill_ice", "recycle"]
        if st["stamina"] < 35: wants.append("eat")
        if st["food"] < 30: wants.append("grow_greens")
        if "clean_panels" in self.A and self.eff < 0.75: wants.append("clean_panels")
        if self.route == "moon" and sol >= 2: wants.append("build_berm")
        if self.science: wants += [a["id"] for a in self.r["actions"] if a.get("science")]
        wants.append(PROG[self.route])
        wants += [O2A[self.route], "drill_ice", "grow_greens"]
        for w in wants:
            if ok(w): return w
        return None

    def play(self, verbose=False):
        s, st = self.s, self.st
        for sol in range(1, s["solsTotal"] + 1):
            prod = round(s["solarBase"] * self.eff * self.mf); self.mf = 1.0
            st["power"] = min(100, st["power"] + prod)
            ap, solf, acted = s["actionsPerSol"], set(), 0
            today = [d for d in self.r.get("dilemmas", []) if not d.get("random") and d["sol"] == sol]
            if not today and sol in s.get("encounterSols", []):
                for eid in self.deck:
                    d = next(x for x in self.r["dilemmas"] if x["id"] == eid)
                    if eid in self.used or (d.get("requiresFlag") and d["requiresFlag"] not in self.flags): continue
                    self.used.add(eid); today = [d]; break
            for d in today:
                def okopt(o):
                    if o.get("needFlag") and o["needFlag"] not in self.flags: return False
                    return all(st[k] >= o.get("need" + k.capitalize(), 0) for k in ("water", "power", "food", "stamina")) and o.get("ap", 0) <= ap
                opts = [x for x in d["options"] if okopt(x)]
                if d.get("random"):
                    # encounters: a sensible player takes the better deal
                    val = lambda x: sum(x.get("delta", {}).get(k, 0) for k in ("o2", "water", "power", "food", "fuel"))                         + 0.5 * x.get("delta", {}).get("stamina", 0) - 8 * x.get("ap", 0) - 5 * x.get("dose", 0)
                    o = max(opts, key=val)
                else:
                    o = d["options"][min(self.choice, len(d["options"]) - 1)]
                    if o not in opts: o = opts[0]
                self.picks.append((sol, d["id"], o["id"]))
                for k, v in o.get("delta", {}).items(): st[k] += v
                self.clamp()
                ap = max(0, ap - o.get("ap", 0)); self.dose += o.get("dose", 0)
                if o.get("setsFlag"): (solf if o.get("solFlag") else self.flags).add(o["setsFlag"])
            while ap > 0:
                aid = self.pick(sol, ap, solf)
                if not aid: break
                ap -= self.do(aid, solf); acted += 1
                for inc in self.r.get("incidents", []):
                    if inc["sol"] == sol and acted == inc["afterActions"] and not self.incidents_ok:
                        for k, v in inc["penalty"].items(): st[k] += v
                        self.clamp()
                if min(st["o2"], st["water"], st["food"]) <= 0: return False, "died day %d" % sol
            if verbose: print(f"  sol {sol} day-end {st} eff {self.eff:.2f} dose {self.dose:.1f} tonight {self.nights[sol-1] if sol <= len(self.nights) else '-'}")
            if sol == s["solsTotal"]:
                return st["fuel"] >= s["fuelTarget"], "end fuel %d" % st["fuel"]
            ev = self.tonight(sol)
            prevented = bool(ev) and (not ev.get("preventFlag") or ev["preventFlag"] in solf) \
                and st["water"] >= ev.get("preventMinWater", 0) and st["power"] >= ev.get("preventMinPower", 0)
            collapsed = st["stamina"] <= 0
            if collapsed: self.collapses += 1
            sh = self.shield()
            self.dose += s.get("radPerSol", 0) * sh
            st["power"] -= s["habPowerLoad"]; st["o2"] -= s["nightO2"]; st["water"] -= s["nightWater"]; st["food"] -= s["nightFood"]
            if ev:
                if prevented: self.mf = ev.get("preventedNextMorningSolarFactor", 1)
                else:
                    self.hits += 1
                    for k, v in ev.get("penalty", {}).items(): st[k] += v
                    self.dose += ev.get("dose", 0) * sh
                    self.eff -= ev.get("efficiencyLoss", 0); self.mf = ev.get("nextMorningSolarFactor", 1)
            if st["power"] < 0: st["o2"] -= s["powerOutO2Penalty"]; st["power"] = 0
            self.eff = max(s["minEfficiency"], self.eff - s["dustPerSol"])
            st["stamina"] += s["collapseStamina"] if collapsed else s["sleepStamina"]
            self.clamp()
            if verbose: print(f"     night {sol} {ev['id'] if ev else 'calm'} {'ok' if prevented else ('HIT' if ev else '')} {st}")
            if min(st["o2"], st["water"], st["food"]) <= 0: return False, "died night %d" % sol
            if self.dose >= s.get("radLimit", 1e9): return False, "radiation"


def idle(route, seed):
    """Doing nothing at all must lose."""
    sim = Sim(route, seed)
    sim.pick = lambda *a: None
    return sim.play()[0]


if __name__ == "__main__":
    for route in ("mars", "moon", "titan"):
        print("==", route)
        Sim(route, 1).play(verbose=True)
        for cabin, sup, inc, ch in itertools.product([10, 50, 100], [False, True], [True, False], [0, 1]):
            res = []
            for seed in range(40):
                sim = Sim(route, seed, cabin, sup, inc, choice=ch)
                win, why = sim.play()
                res.append((win, why, sim))
            wins = sum(1 for w, _, _ in res if w)
            worst = min(res, key=lambda x: min(x[2].st["o2"], x[2].st["water"], x[2].st["food"]))
            fails = {why for w, why, _ in res if not w}
            stars = sum(1 for w, _, sm in res if w and sm.dose <= sm.s["radSafe"] and sm.collapses == 0)
            print(f"  cabin={cabin:3} sup={sup!s:5} incOK={inc!s:5} pick={ch} wins {wins}/40 healthy {stars} fails {fails or '-'} worst-end {worst[2].st} maxdose {max(r[2].dose for r in res):.0f}")
        print("  idle wins:", sum(idle(route, s) for s in range(10)), "/10")
