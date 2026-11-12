# -*- coding: utf-8 -*-
# PROTOTYPE - NOT FOR PRODUCTION
# Quick balance check for the card-based Chapter 2 (the lifeboat diary).
# Mirrors Assets/Scripts/Core/LifeboatSim.cs and reads the shipped config.json.
# The C# EditMode tests are the authority; this is only for tuning numbers fast.
# Run:  python tools/lifeboat_sim.py [playouts]
import json, os, random, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CFG = json.load(open(os.path.join(ROOT, "Assets/Resources/Data/config.json"), encoding="utf-8"))
C = CFG["lifeboat"]
RES = ("o2", "water", "food", "power")


class Sim:
    def __init__(self, pet, stowed):
        self.stock = dict(C["start"])
        for sid in stowed:
            for sup in C["supplies"]:
                if sup["id"] == sid:
                    for r in RES: self.stock[r] += sup["stock"].get(r, 0)
        self.items = {s["flag"] for s in CFG["chapter1"]["supplies"] if s["id"] in stowed and s.get("flag")}
        self.pet = pet
        self.trait = next((p["trait"] for p in C["pets"] if p["id"] == pet), None)
        self.pet_fed = True
        self.health = C["health"]
        self.day = 1
        self.guests, self.flags, self.answered = [], set(), set()
        self.outcome = None
        self.path = []

    def visitor(self, vid): return next(v for v in C["visitors"] if v["id"] == vid)
    def last_day(self): return C["sliceDays"] if 0 < C["sliceDays"] < C["daysTotal"] else C["daysTotal"]

    def applies(self, c):
        return ((not c.get("ifFlag") or c["ifFlag"] in self.flags)
                and (not c.get("unlessFlag") or c["unlessFlag"] not in self.flags)
                and (not c.get("ifGuest") or c["ifGuest"] in self.guests))

    def next_card(self):
        if self.outcome: return None
        for c in C["cards"]:
            if c["day"] == self.day and c["id"] not in self.answered and self.applies(c): return c
        return None

    def power_tonight(self): return self.stock["power"] + C["solarPerDay"] >= C["lifeSupportPower"]

    def o2_tonight(self):
        o2 = C["crewPerDay"]["o2"] + sum(self.visitor(g)["perDay"]["o2"] for g in self.guests)
        if "filter_clogged" in self.flags: o2 += C["o2PenaltyClogged"]
        if not self.power_tonight(): o2 += C["o2PenaltyNoPower"]
        return o2

    def can(self, o):
        d = o["delta"]
        if o.get("needItem") and o["needItem"] not in self.items: return False
        if o.get("needTrait") and (self.trait != o["needTrait"] or not self.pet_fed): return False
        if o.get("needFlag") and o["needFlag"] not in self.flags: return False
        if o.get("needGuest") and o["needGuest"] not in self.guests: return False
        if o.get("health", 0) < 0 and self.health + o["health"] <= 0: return False
        if any(self.stock[r] + d.get(r, 0) < 0 for r in RES): return False
        joins = bool(o.get("admits")) and o["admits"] not in self.guests
        if joins or d.get("o2", 0) < 0:
            need = self.o2_tonight() + (self.visitor(o["admits"])["perDay"]["o2"] if joins else 0)
            if need > self.stock["o2"] + d.get("o2", 0): return False
        return True

    def choose(self, card, o):
        self.answered.add(card["id"])
        for r in RES: self.stock[r] += o["delta"].get(r, 0)
        self.health = min(C["health"], self.health + o.get("health", 0))
        if o.get("consumesItem") and o.get("needItem"): self.items.discard(o["needItem"])
        if o.get("setsFlag"): self.flags.add(o["setsFlag"])
        if o.get("admits") and o["admits"] not in self.guests:
            if self.visitor(o["admits"]).get("monster"):
                self.outcome = "failed_monster"; self.path.append(card["id"] + ":" + o["id"]); return
            self.guests.append(o["admits"])
        self.path.append(card["id"] + ":" + o["id"])

    def mouths(self): return ["me"] + (["pet"] if self.pet else []) + list(self.guests)

    def ration(self, who):
        if who == "me": return C["crewPerDay"]["water"], C["crewPerDay"]["food"]
        if who == "pet": return 0, next(p["foodPerDay"] for p in C["pets"] if p["id"] == self.pet)
        v = self.visitor(who)["perDay"]
        return v["water"], v["food"]

    def can_feed(self, fed):
        w = sum(self.ration(x)[0] for x in fed); f = sum(self.ration(x)[1] for x in fed)
        return w <= self.stock["water"] and f <= self.stock["food"]

    def can_run_maker(self):
        m = C.get("o2Maker")
        return bool(m) and self.stock["power"] >= m["power"]

    def end_day(self, fed, run_maker=False):
        s = self.stock
        if run_maker and self.can_run_maker():
            s["power"] -= C["o2Maker"]["power"]; s["o2"] += C["o2Maker"]["o2"]
        o2 = self.o2_tonight(); powered = self.power_tonight()
        s["water"] -= sum(self.ration(x)[0] for x in fed); s["food"] -= sum(self.ration(x)[1] for x in fed)
        if "me" not in fed: self.health -= 1
        if self.pet: self.pet_fed = "pet" in fed
        s["power"] += C["solarPerDay"]
        if powered: s["power"] -= C["lifeSupportPower"]
        for g in self.guests:
            if g in fed:
                for r in RES: s[r] += self.visitor(g)["giftPerDay"].get(r, 0)
        air_short = o2 > s["o2"]
        s["o2"] = max(0, s["o2"] - o2)
        guarded = self.trait == "brave" and self.pet_fed
        locked = "locker_locked" in self.flags
        for g in list(self.guests):
            v = self.visitor(g)
            if not v["steals"]: continue
            self.guests.remove(g)
            if guarded or locked: continue
            s["food"] -= min(v["stealFood"], s["food"])
            if self.items: self.items.discard(sorted(self.items)[0])
        self.flags.discard("locker_locked")
        for g in list(self.guests):
            if g not in fed: self.guests.remove(g)
        for n in C.get("nights", []):
            if n["day"] != self.day or (n.get("safeFlag") and n["safeFlag"] in self.flags): continue
            self.health -= n.get("healthLoss", 0)
            for r in RES: s[r] = max(0, s[r] + n["delta"].get(r, 0))
        if air_short: self.outcome = "failed_air"
        elif self.health <= 0: self.outcome = "failed_health"
        elif self.day < self.last_day(): self.day += 1
        elif self.last_day() < C["daysTotal"]: self.outcome = "survived"
        else: self.outcome = "rescued" if C["rescueFlag"] in self.flags else "stranded"

    def stars(self):
        won = self.outcome == "rescued"
        return sum([won, won and self.health >= C["health"], won and (C["kindFlag"] in self.flags or len(self.guests) > 0)])


def feed_plan(sim, skip_pet_when_short=True):
    """Feed everyone the stock allows; guests give way first, then the pet, the cadet last."""
    plan = sim.mouths()
    while not sim.can_feed(plan): plan.pop()
    return plan


GOOD_FLAGS = {"shelter_ready": 3, "electronics_safe": 1.5, "beacon_fixed": 12, "coords_sent": 12, "nok_helped": 1.5, "mu_fed": 0.5}
BAD_FLAGS = {"filter_clogged": 3}


def worth(sim):
    """How good a position looks to a careful player: days the supplies cover, plus progress, minus harm."""
    left = sim.last_day() - sim.day + 1
    fw = sum(sim.ration(x)[0] for x in sim.mouths()); ff = sum(sim.ration(x)[1] for x in sim.mouths())
    cover = min(sim.stock["o2"] / sim.o2_tonight(), sim.stock["water"] / fw, sim.stock["food"] / ff)
    v = 4 * min(cover, left) + 0.25 * min(sim.stock["power"], 4) + 2 * sim.health
    v += sum(w for f, w in GOOD_FLAGS.items() if f in sim.flags) - sum(w for f, w in BAD_FLAGS.items() if f in sim.flags)
    return v


def clone(sim):
    c = Sim.__new__(Sim)
    c.__dict__ = dict(sim.__dict__)
    c.stock = dict(sim.stock); c.items = set(sim.items); c.flags = set(sim.flags)
    c.answered = set(sim.answered); c.guests = list(sim.guests); c.path = list(sim.path)
    return c


def careful_choice(sim, card, allowed):
    best, best_v = None, None
    if card.get("visitor") and sim.visitor(card["visitor"]).get("monster"):
        # a careful player scans first; every monster fails at least two of three scanners
        allowed = [o for o in allowed if not o.get("admits")]
    for o in allowed:
        c = clone(sim); c.choose(card, o)
        v = worth(c)
        if best_v is None or v > best_v + 1e-9: best, best_v = o, v
    return best


def careful_night(sim):
    plan = feed_plan(sim)
    left = sim.last_day() - sim.day + 1
    run = sim.can_run_maker() and sim.stock["o2"] < sim.o2_tonight() * left and sim.stock["power"] - C["o2Maker"]["power"] >= (1 if C["rescueFlag"] not in sim.flags else 0)
    sim.end_day(plan, run)


def playout(pet, stowed, rng):
    """rng = None plays the careful player; otherwise answers are random (and the O2 maker runs at random)."""
    sim = Sim(pet, stowed)
    while not sim.outcome:
        card = sim.next_card()
        if card is None:
            if rng is None: careful_night(sim)
            else: sim.end_day(feed_plan(sim), rng.random() < 0.5)
            continue
        allowed = [o for o in card["options"] if sim.can(o)]
        assert allowed, "no answer left on " + card["id"]
        sim.choose(card, careful_choice(sim, card, allowed) if rng is None else rng.choice(allowed))
    return sim


if __name__ == "__main__":
    n = int(sys.argv[1]) if len(sys.argv) > 1 and sys.argv[1].isdigit() else 4000
    loadouts = {"none": [], "demo": C["demoStowed"], "all10": [s["id"] for s in CFG["chapter1"]["supplies"]][:5]}
    print("cards", len(C["cards"]), "start", C["start"], "playouts", n)
    for pet in [p["id"] for p in C["pets"]] + [None]:
        for name, stowed in loadouts.items():
            rng = random.Random(7)
            outs, stars, best = {}, {0: 0, 1: 0, 2: 0, 3: 0}, None
            for _ in range(n):
                s = playout(pet, stowed, rng)
                outs[s.outcome] = outs.get(s.outcome, 0) + 1
                stars[s.stars()] += 1
                if best is None or s.stars() > best.stars(): best = s
            order = ["rescued", "stranded", "survived", "failed_air", "failed_health", "failed_monster"]
            line = "  ".join(f"{k}={100 * outs.get(k, 0) / n:4.1f}%" for k in order if outs.get(k))
            c = playout(pet, stowed, None)
            print(f"pet={str(pet):6} loot={name:5} random: {line}  3-star={100 * stars[3] / n:4.1f}%   careful: {c.outcome} stars={c.stars()} health={c.health} stock={ {k: int(v) for k, v in c.stock.items()} }")
            if "-v" in sys.argv: print("     ", " ".join(c.path))
