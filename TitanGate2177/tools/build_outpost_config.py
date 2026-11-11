# PROTOTYPE - NOT FOR PRODUCTION
# One-off migration: rewrites Assets/Resources/Data/config.json for the 7-day (168 h) Mars outpost.
# Run once: python tools/build_outpost_config.py
import json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
P = os.path.join(ROOT, "Assets/Resources/Data/config.json")
c = json.load(open(P, encoding="utf-8"))

def act(id, hours, delta=None, **kw):
    a = {"id": id, "hours": hours, "delta": delta or {}}
    a.update(kw)
    return a

def opt(id, hours=0, delta=None, **kw):
    o = {"id": id, "hours": hours, "delta": delta or {}}
    o.update(kw)
    return o

outpost = {
    "hoursTotal": 168,
    "landingClock": 6,
    "maxValue": 100,
    "start": {"air": 70, "power": 50, "shelter": 0, "supplies": 55, "energy": 80},
    "airUsePerHour": 1.0,
    "lifeSupportAirPerHour": 1.3,
    "systems": [
        {"id": "life_support", "powerPerHour": 2.5, "onAtStart": True},
        {"id": "greenhouse", "powerPerHour": 2.0, "onAtStart": False},
        {"id": "beacon", "powerPerHour": 1.0, "onAtStart": False},
        {"id": "shelter", "powerPerHour": 2.0, "onAtStart": False},
    ],
    "solarPerHour": 7.0, "daylightFrom": 7, "daylightTo": 19,
    "dustPerDay": 0.12, "minEfficiency": 0.4,
    "suppliesUsePerHour": 0.35,
    "energyAwakePerHour": -1.4, "energySleepPerHour": 7.0,
    "lowEnergy": 20, "lowEnergyExtraHours": 1, "collapseSleepHours": 6,
    "suppliesZeroFailHours": 24,
    "warnAir": 25, "warnSupplies": 20, "warnPower": 15,
    "radPerHour": 0.029, "radPerOutsideHour": 0.05, "radSafe": 50, "radLimit": 150,
    "spe": {"hour": 84, "durationHours": 6, "dose": 60, "energyLoss": 30, "airLoss": 10,
            "unpoweredShelterFactor": 0.5, "warningHour": 28},
    "relayClocks": [10, 22], "rescuePassEveryHours": 6, "commDelayMinutes": 14,
    "greenhouseHoursToReady": 24,
    "nightEventClock": 2, "randomEventDays": [1, 2, 4, 5, 6],
    "encounterClock": 8, "encounterDays": [2, 3, 4, 5, 6],
    "actions": [
        act("moxie", 2, {"power": -6, "air": 14}, hotspot="s_o2gen", card="moxie"),
        act("recycle", 2, {"power": -4, "supplies": 6}, hotspot="s_recycler", card="water_recycle"),
        act("drill_ice", 3, {"energy": -12, "supplies": 12}, requiresFlag="drill_ready", outside=True, hotspot="mars_drill", card="ice"),
        act("sabatier", 2, {"power": -6, "supplies": 8}, requiresFlag="catalyst_installed", hotspot="s_reactor", card="sabatier"),
        act("eat", 1, {"supplies": -6, "energy": 25}, hotspot="s_food"),
        act("clean_panels", 2, {"energy": -8}, outside=True, cleansPanels=True, hotspot="mars_solar", card="solar"),
        act("stow_panels", 1, {"energy": -4}, setsFlag="panels_stowed", solFlag=True, outside=True, hotspot="mars_solar"),
        act("inspect_seals", 1, {"energy": -3}, setsFlag="seals_inspected", solFlag=True, hotspot="s_seals"),
        act("shelter_water", 2, {"supplies": -10, "shelter": 25}, hotspot="s_bed", card="radiation"),
        act("shelter_regolith", 4, {"energy": -20, "shelter": 35}, outside=True, hotspot="mars_scrap", card="regolith"),
        act("shelter_kit", 1, {"shelter": 40}, requiresFlag="shield_kit", consumesFlag=True, hotspot="s_bed"),
        act("repair_beacon", 3, {"energy": -8}, requiresFlag="has_hull_patch", setsFlag="beacon_repaired", hotspot="s_line"),
        act("repair_beacon_chip", 1, {"energy": -2}, requiresFlag="data_chip", setsFlag="beacon_repaired", hotspot="s_line"),
        act("send_coords", 1, {}, requiresFlag="beacon_repaired", needsSystem="beacon", hotspot="s_nav"),
        act("ask_earth", 1, {}, hotspot="s_nav"),
        act("setup_greenhouse", 3, {"energy": -6}, setsFlag="greenhouse_started", hotspot="s_food", card="veggie"),
        act("setup_greenhouse_seeds", 1, {}, requiresFlag="seed_pack", setsFlag="greenhouse_started", hotspot="s_food", card="veggie"),
        act("sci_sunset", 1, {"energy": -3}, science=True, minDay=1, setsFlag="sci_sunset", hotspot="s_scope", card="mars_sunset"),
        act("sci_core", 2, {"energy": -6}, science=True, minDay=3, setsFlag="sci_core", requiresFlag="drill_ready", outside=True, hotspot="mars_drill", card="mars_lake"),
        act("sci_weather", 1, {"energy": -3}, science=True, minDay=5, setsFlag="sci_weather", outside=True, hotspot="mars_solar", card="mars_pressure"),
    ],
    "nightEvents": [
        {"id": "dust_devil", "preventFlag": "panels_stowed", "penalty": {}, "efficiencyLoss": 0.25},
        {"id": "micro_leak", "preventFlag": "seals_inspected", "penalty": {"air": -12}},
        {"id": "minor_flare", "preventMinShelter": 30, "penalty": {"energy": -15}, "dose": 8},
    ],
    "incidents": [
        {"id": "recycler_fire", "day": 2, "afterActions": 2, "seconds": 30, "hotspot": "s_recycler", "penalty": {"supplies": -10, "power": -6}},
        {"id": "hatch_leak", "day": 3, "afterActions": 3, "seconds": 30, "hotspot": "s_airlock", "penalty": {"air": -12}},
        {"id": "scrubber_clog", "day": 5, "afterActions": 2, "seconds": 30, "hotspot": "s_o2gen", "penalty": {"air": -12}},
    ],
    "dilemmas": [
        {"id": "mars_rover", "day": 6, "options": [opt("fetch", 2, {"power": 12, "energy": -12}, dose=1.0), opt("stay")]},
        {"id": "orbi_rest", "random": True, "options": [opt("rest", 0, {"power": 8}, setsFlag="orbi_off", solFlag=True), opt("stay_on", 0, {"power": -4})]},
        {"id": "supply_pod", "random": True, "options": [opt("fetch", 2, {"energy": -15, "supplies": 15}, dose=0.5), opt("leave")]},
        {"id": "family_message", "random": True, "options": [opt("watch", 1, {"energy": 20}, card="isolation"), opt("later")]},
        {"id": "greenhouse_leak", "random": True, "options": [opt("replant", 0, {}, needFlag="seed_pack"), opt("patch", 1, {"energy": -8}), opt("let_freeze", 0, {"supplies": -8})]},
        {"id": "spare_parts", "random": True, "options": [opt("sort", 1, {"energy": -5}, setsFlag="spare_filter"), opt("leave_box")]},
        {"id": "filter_wear", "random": True, "options": [opt("swap", 0, {}, needFlag="spare_filter"), opt("clean_by_hand", 1, {"energy": -10, "air": -6})]},
        {"id": "cut_hand", "random": True, "options": [opt("medkit", 0, {}, needFlag="med_kit"), opt("bandage", 1, {}), opt("ignore", 0, {"energy": -20})]},
        {"id": "mars_frost", "random": True, "options": [opt("scrape", 1, {"energy": -10}, card="co2_frost"), opt("wait", 0, {"power": -10}, card="co2_frost")]},
    ],
}

c["outpost"] = outpost
c["routes"] = [{"id": "mars", "edl": next(r for r in c["routes"] if r["id"] == "mars")["edl"]}]
c["chapter1"]["supplies"] = [
    {"id": "sup_o2", "delta": {"air": 12}},
    {"id": "sup_water", "delta": {"supplies": 8}},
    {"id": "sup_food", "delta": {"supplies": 10}},
    {"id": "sup_battery", "delta": {"power": 20}},
    {"id": "sup_photo", "delta": {"energy": 15}},
    {"id": "sup_medkit", "delta": {}, "flag": "med_kit"},
    {"id": "sup_filter", "delta": {}, "flag": "spare_filter"},
    {"id": "sup_seeds", "delta": {}, "flag": "seed_pack"},
    {"id": "sup_cell", "delta": {}, "flag": "data_chip"},
    {"id": "sup_shield", "delta": {}, "flag": "shield_kit"},
]
c["chapter1"].pop("suppliesBonus", None)
c["chapter1"]["carryO2Factor"] = 0.3
p = c["puzzles"]
for k in ("moonPodCode", "titanGateCode"):
    p.pop(k, None)
p["recipes"] = [r for r in p["recipes"] if r["result"] == "item_hull_patch"]
# Mars is the only destination; the other two stay on the console, out of fuel range
p["navOptions"] = [{"id": "titan", "fuelNeeded": 60, "route": ""}, {"id": "earth", "fuelNeeded": 35, "route": ""},
                   {"id": "mars", "fuelNeeded": 15, "route": "mars"}]
moon_titan_cards = {"moon_landing", "apollo", "moon_ice", "moon_south", "moon_dust", "earth_sky", "electrolysis", "regolith",
                    "earthrise", "moon_resources", "moonquakes", "lro", "gravity_assist", "huygens", "titan_cold", "titan_air",
                    "rtg", "titan_lakes", "dragonfly", "titan_haze", "titan_ocean", "titan_dunes", "titan_pebbles"}
keep_regolith = True  # Mars shelters can use regolith too
c["cards"] = [x for x in c["cards"] if x["id"] not in moon_titan_cards or (keep_regolith and x["id"] == "regolith")]
for x in c["cards"]:
    if x["id"] == "regolith":
        x["chapter"] = 2
for x in ("comm_delay", "sep_2017", "eclss"):
    if not any(y["id"] == x for y in c["cards"]):
        c["cards"].append({"id": x, "chapter": 2})
json.dump(c, open(P, "w", encoding="utf-8"), ensure_ascii=False, indent=2)
print("cards", len(c["cards"]), "actions", len(outpost["actions"]))
