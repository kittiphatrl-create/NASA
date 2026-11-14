// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-11
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Core
{
    /// <summary>A change to the outpost values. Used by actions, events, supplies and dilemma answers.</summary>
    [Serializable]
    public class ResourceDelta
    {
        public float air, power, shelter, supplies, energy;
    }

    /// <summary>A powered outpost system the player switches on or off (the power trade-off).</summary>
    [Serializable]
    public class SystemDef
    {
        public string id;
        public float powerPerHour;
        public bool onAtStart;
    }

    /// <summary>Something the astronaut does that takes in-game hours.</summary>
    [Serializable]
    public class OutpostAction
    {
        public string id;
        public int hours = 1;
        public ResourceDelta delta = new ResourceDelta();
        public string requiresFlag;
        /// <summary>Flag set when done. Sol flags last until the next morning (06:00).</summary>
        public string setsFlag;
        public bool solFlag;
        /// <summary>The required flag is used up (one-shot items).</summary>
        public bool consumesFlag;
        public bool outside;
        public bool cleansPanels;
        /// <summary>The action needs this powered system switched on.</summary>
        public string needsSystem;
        public bool science;
        public int minDay;
        /// <summary>Hotspot whose menu offers this action.</summary>
        public string hotspot;
        /// <summary>Knowledge card unlocked when the action is done.</summary>
        public string card;
    }

    /// <summary>A minor night event (02:00). Prevented by a sol flag or by enough shelter readiness.</summary>
    [Serializable]
    public class NightEventDef
    {
        public string id;
        public string preventFlag;
        public float preventMinShelter;
        public ResourceDelta penalty = new ResourceDelta();
        public float efficiencyLoss;
        public float dose;
    }

    /// <summary>The solar particle event: the week's major crisis. Severity values are game simulation.</summary>
    [Serializable]
    public class SpeDef
    {
        public int hour = 84;
        public int durationHours = 6;
        public int warningHour = 28;
        public float dose = 80, energyLoss = 40, airLoss = 35, powerLoss = 40;
        /// <summary>Shelter readiness counts this much when the shelter system is not powered.</summary>
        public float unpoweredShelterFactor = 0.5f;
    }

    /// <summary>A real-time daytime emergency: after N actions on a given day, reach a hotspot in time.</summary>
    [Serializable]
    public class IncidentDef
    {
        public string id;
        public int day;
        public int afterActions;
        public float seconds = 30f;
        public string hotspot;
        public ResourceDelta penalty = new ResourceDelta();
    }

    /// <summary>One answer to a morning dilemma or random event.</summary>
    [Serializable]
    public class DilemmaOption
    {
        public string id;
        public int hours;
        public ResourceDelta delta = new ResourceDelta();
        public float dose;
        public string setsFlag;
        public bool solFlag;
        /// <summary>Needed to pick this answer (usually a supply stowed in Chapter 1).</summary>
        public string needFlag;
        public string card;
    }

    /// <summary>A morning decision. Fixed ones have a day; random ones are drawn from a deck (60 Seconds style).</summary>
    [Serializable]
    public class DilemmaDef
    {
        public string id;
        public int day;
        public bool random;
        public string requiresFlag;
        public List<DilemmaOption> options = new List<DilemmaOption>();
    }

    /// <summary>The 7-day (168 h) Mars outpost rules. Every number lives in config.json.</summary>
    [Serializable]
    public class OutpostConfig
    {
        public int hoursTotal = 168;
        /// <summary>Local time on Mars when the survival clock starts (06:00 = dawn).</summary>
        public int landingClock = 6;
        public float maxValue = 100;
        public ResourceDelta start = new ResourceDelta();
        public float airUsePerHour = 1.4f, lifeSupportAirPerHour = 1f;
        public List<SystemDef> systems = new List<SystemDef>();
        public float solarPerHour = 5.5f;
        public int daylightFrom = 7, daylightTo = 19;
        public float dustPerDay = 0.12f, minEfficiency = 0.4f;
        public float suppliesUsePerHour = 0.45f;
        public float energyAwakePerHour = -1.4f, energySleepPerHour = 7f;
        public float lowEnergy = 20; public int lowEnergyExtraHours = 1, collapseSleepHours = 6;
        public int suppliesZeroFailHours = 24;
        public float warnAir = 25, warnSupplies = 20, warnPower = 15;
        /// <summary>Second air alarm: a sleeping astronaut is woken again when the air falls to this level.</summary>
        public float critAir = 10;
        public float radPerHour = 0.029f, radPerOutsideHour = 0.05f, radSafe = 50, radLimit = 150;
        public SpeDef spe = new SpeDef();
        /// <summary>Local times when a relay orbiter links the outpost with Earth.</summary>
        public int[] relayClocks = { 10, 22 };
        public int rescuePassEveryHours = 6;
        /// <summary>One-way Earth-Mars light delay used in this mission (real range about 4-24 minutes).</summary>
        public int commDelayMinutes = 14;
        public int greenhouseHoursToReady = 24;
        public int nightEventClock = 2;
        public int[] randomEventDays = { 1, 2, 4, 5, 6 };
        public int encounterClock = 8;
        public int[] encounterDays = { 2, 3, 4, 5, 6 };
        public List<OutpostAction> actions = new List<OutpostAction>();
        public List<NightEventDef> nightEvents = new List<NightEventDef>();
        public List<IncidentDef> incidents = new List<IncidentDef>();
        public List<DilemmaDef> dilemmas = new List<DilemmaDef>();

        public OutpostAction GetAction(string id) => actions.Find(a => a.id == id);
        public NightEventDef GetNightEvent(string id) => nightEvents.Find(e => e.id == id);
        public SystemDef GetSystem(string id) => systems.Find(s => s.id == id);
    }

    /// <summary>The landing procedure: the right order plus wrong steps that teach.</summary>
    [Serializable]
    public class EdlDef
    {
        public List<string> order = new List<string>();
        public List<string> distractors = new List<string>();
    }

    /// <summary>A destination on the nav console (only Mars is reachable with the fuel left).</summary>
    [Serializable]
    public class RouteDef
    {
        public string id;
        public EdlDef edl = new EdlDef();
    }

    /// <summary>A loose supply the cadet can stow before landing (the 60-second scramble).</summary>
    [Serializable]
    public class SupplyDef
    {
        public string id;
        /// <summary>Added to the outpost start values.</summary>
        public ResourceDelta delta = new ResourceDelta();
        /// <summary>Flag set at the outpost (unlocks faster or cheaper answers, never the only way).</summary>
        public string flag;
    }

    /// <summary>Chapter 1 numbers that carry into the outpost.</summary>
    [Serializable]
    public class Chapter1Config
    {
        public float cabinO2Start = 100f;
        public float cabinDrainPerSec = 0.8f;
        public float cabinFloor = 10f;
        public float carryO2Pivot = 50f;
        public float carryO2Factor = 0.3f;
        /// <summary>Seconds to grab loose supplies before landing, and how many the lockers hold.</summary>
        public float scrambleSeconds = 60f;
        /// <summary>Chapter 1 in the top-down ship (morning quests, impact, scramble) instead of the side-view puzzles.</summary>
        public bool topDown = true;
        public int stowSlots = 5;
        public List<SupplyDef> supplies = new List<SupplyDef>();
    }

    [Serializable]
    public class NavOption
    {
        public string id;
        public int fuelNeeded;
        /// <summary>Route this choice leads to; empty = out of reach.</summary>
        public string route;
    }

    [Serializable]
    public class Recipe
    {
        public string a, b, result;
    }

    /// <summary>Answers and rules for the puzzles. Each answer is printed on a knowledge card.</summary>
    [Serializable]
    public class PuzzleConfig
    {
        public string lockerCode = "7";
        public string podCode = "39";
        public int fuelGaugePercent = 18;
        public List<NavOption> navOptions = new List<NavOption>();
        public List<string> edlOrder = new List<string>();
        public List<string> catalysts = new List<string>();
        public string catalystCorrect = "item_cat_ni";
        public List<Recipe> recipes = new List<Recipe>();
        public int shiftTasksBeforeImpact = 2;
    }

    [Serializable]
    public class CardDef
    {
        public string id;
        public int chapter;
    }

    [Serializable]
    public class PresentationConfig
    {
        public float walkSpeed = 3.4f;
        public float actionSeconds = 1.2f;
        public float roomSpacing = 16f;
        public float cameraMoveSeconds = 0.6f;
        public float textScale = 1f;
    }

    /// <summary>Root of config.json.</summary>
    [Serializable]
    public class GameConfig
    {
        public PuzzleConfig puzzles = new PuzzleConfig();
        public List<CardDef> cards = new List<CardDef>();
        public PresentationConfig presentation = new PresentationConfig();
        public List<RouteDef> routes = new List<RouteDef>();
        public Chapter1Config chapter1 = new Chapter1Config();
        public OutpostConfig outpost = new OutpostConfig();
        /// <summary>The card-based Chapter 2 prototype (survival diary in the lifeboat).</summary>
        public LifeboatConfig lifeboat = new LifeboatConfig();
        /// <summary>The abandoned-base restoration chapter (rooms, tasks, needs, weather, relay passes).</summary>
        public RestoreConfig restore = new RestoreConfig();

        public RouteDef GetRoute(string id) => routes.Find(r => r.id == id);

        public static GameConfig FromJson(string json) => JsonUtility.FromJson<GameConfig>(json);
    }
}
