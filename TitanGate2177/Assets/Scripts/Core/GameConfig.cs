// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Core
{
    /// <summary>A change to the life-support values. Used by actions and event penalties.</summary>
    [Serializable]
    public class ResourceDelta
    {
        public int o2, water, power, food, stamina, fuel;
    }

    /// <summary>One thing the astronaut can do on Mars that costs time (action points).</summary>
    [Serializable]
    public class ActionDef
    {
        public string id;
        public int ap = 1;
        public ResourceDelta delta = new ResourceDelta();
        /// <summary>Persistent flag required before the action is allowed (e.g. a repaired part).</summary>
        public string requiresFlag;
        /// <summary>Flag set when done. Sol flags are cleared every morning.</summary>
        public string setsFlag;
        public bool solFlag;
        /// <summary>Restores solar panel efficiency to 100%.</summary>
        public bool cleansPanels;
        /// <summary>Done outside the hab: adds the route's EVA radiation dose.</summary>
        public bool outside;
        /// <summary>Not offered before this sol (Mission Control asks for science tasks over the week).</summary>
        public int minSol;
        /// <summary>A Mission Control science task: counts toward the debrief's science star.</summary>
        public bool science;
        /// <summary>Hotspot whose action menu offers this task (science tasks only).</summary>
        public string hotspot;
        /// <summary>Knowledge card unlocked when the action is done.</summary>
        public string card;
    }

    /// <summary>A night event. Major events have a fixed night; minor ones are drawn from a deck.</summary>
    [Serializable]
    public class EventDef
    {
        public string id;
        public bool major;
        public int night;
        public string preventFlag;
        public int preventMinWater;
        public int preventMinPower;
        public ResourceDelta penalty = new ResourceDelta();
        public float efficiencyLoss;
        public float nextMorningSolarFactor = 1f;
        public float preventedNextMorningSolarFactor = 1f;
        /// <summary>Radiation dose (mSv) taken when the event is not prevented.</summary>
        public float dose;
    }

    /// <summary>Tuning for the Mars survival chapter. Every number lives in config.json.</summary>
    [Serializable]
    public class SurvivalConfig
    {
        public int startO2 = 60, startWater = 50, startPower = 40, startFood = 70, startStamina = 80;
        public int maxValue = 100;
        public int solsTotal = 4;
        public int launchSol = 4;
        public int actionsPerSol = 6;
        public int solarBase = 40;
        public int habPowerLoad = 14;
        public int nightO2 = 14, nightWater = 10, nightFood = 8;
        public float dustPerSol = 0.15f;
        public float minEfficiency = 0.3f;
        public int powerOutO2Penalty = 25;
        public int sleepStamina = 45;
        public int collapseStamina = 20;
        public int lowStaminaThreshold = 25;
        public int lowStaminaExtraAp = 1;
        public int fuelTarget = 100;
        public int warnThreshold = 25;
        public int[] randomEventNights = { 1, 2 };
        /// <summary>Persistent flag the goal console needs besides full progress (e.g. line_repaired).</summary>
        public string goalFlag = "line_repaired";
        /// <summary>Background radiation inside the hab per sol, in mSv (real surface values).</summary>
        public float radPerSol;
        /// <summary>Extra dose for each action done outside.</summary>
        public float radPerEva;
        /// <summary>Mission dose limit: reaching it ends the mission.</summary>
        public float radLimit = 150f;
        /// <summary>At or under this dose the debrief counts the crew as healthy.</summary>
        public float radSafe = 50f;
        /// <summary>Persistent flag for a shield (e.g. a regolith berm) and the dose multiplier it gives.</summary>
        public string shieldFlag;
        public float shieldFactor = 1f;
        /// <summary>Mornings that draw a random encounter when no fixed dilemma is set for that day.</summary>
        public int[] encounterSols = new int[0];
    }

    /// <summary>
    /// A real-time emergency during the day: after N actions on a given sol, the player has
    /// a few seconds to reach a hotspot or take the penalty.
    /// </summary>
    [Serializable]
    public class IncidentDef
    {
        public string id;
        public int sol;
        public int afterActions;
        public float seconds = 30f;
        public string hotspot;
        public ResourceDelta penalty = new ResourceDelta();
    }

    /// <summary>One answer to a morning dilemma: what it costs and gives.</summary>
    [Serializable]
    public class DilemmaOption
    {
        public string id;
        public int ap;
        public ResourceDelta delta = new ResourceDelta();
        public float dose;
        /// <summary>Optional flag; sol flags last until the next morning.</summary>
        public string setsFlag;
        public bool solFlag;
        /// <summary>What the option needs to be picked (prepared earlier = more choices).</summary>
        public int needWater, needPower, needFood, needStamina;
        public string needFlag;
        /// <summary>Knowledge card unlocked by this answer.</summary>
        public string card;
    }

    /// <summary>A morning decision with no single right answer (60 Parsecs style).</summary>
    [Serializable]
    public class DilemmaDef
    {
        public string id;
        /// <summary>Fixed morning (ignored for random encounters).</summary>
        public int sol;
        /// <summary>Random encounter: drawn from the deck on an encounter morning (60 Seconds style).</summary>
        public bool random;
        /// <summary>Only drawn once this flag is set (follow-up events).</summary>
        public string requiresFlag;
        public List<DilemmaOption> options = new List<DilemmaOption>();
    }

    /// <summary>The landing procedure for one world: the right order plus wrong steps that teach.</summary>
    [Serializable]
    public class EdlDef
    {
        public List<string> order = new List<string>();
        public List<string> distractors = new List<string>();
    }

    /// <summary>One destination after the debris impact: its own Chapter 2.</summary>
    [Serializable]
    public class RouteDef
    {
        public string id;
        public SurvivalConfig survival = new SurvivalConfig();
        public List<ActionDef> actions = new List<ActionDef>();
        public List<EventDef> events = new List<EventDef>();
        public List<IncidentDef> incidents = new List<IncidentDef>();
        public List<DilemmaDef> dilemmas = new List<DilemmaDef>();
        public EdlDef edl = new EdlDef();
    }

    /// <summary>A loose supply the cadet can stow before landing (the 60-second scramble).</summary>
    [Serializable]
    public class SupplyDef
    {
        public string id;
        /// <summary>Added to the Chapter 2 start values.</summary>
        public ResourceDelta delta = new ResourceDelta();
        /// <summary>Flag set in Chapter 2 (unlocks event answers, e.g. med_kit).</summary>
        public string flag;
    }

    /// <summary>Chapter 1 numbers that carry into Chapter 2.</summary>
    [Serializable]
    public class Chapter1Config
    {
        public float cabinO2Start = 100f;
        public float cabinDrainPerSec = 0.8f;
        public float cabinFloor = 10f;
        public float carryO2Pivot = 50f;
        public float carryO2Factor = 0.4f;
        public ResourceDelta suppliesBonus = new ResourceDelta();
        /// <summary>Seconds to grab loose supplies before landing, and how many the lockers hold.</summary>
        public float scrambleSeconds = 60f;
        public int stowSlots = 5;
        public List<SupplyDef> supplies = new List<SupplyDef>();
    }

    [Serializable]
    public class NavOption
    {
        public string id;
        public int fuelNeeded;
        /// <summary>Which Chapter 2 this choice leads to.</summary>
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
        public string moonPodCode = "1969";
        public string titanGateCode = "179";
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
        public SurvivalConfig survival = new SurvivalConfig();
        public List<ActionDef> actions = new List<ActionDef>();
        public List<EventDef> events = new List<EventDef>();
        public PuzzleConfig puzzles = new PuzzleConfig();
        public List<CardDef> cards = new List<CardDef>();
        public PresentationConfig presentation = new PresentationConfig();
        public List<RouteDef> routes = new List<RouteDef>();
        public List<IncidentDef> incidents = new List<IncidentDef>();
        public List<DilemmaDef> dilemmas = new List<DilemmaDef>();
        public Chapter1Config chapter1 = new Chapter1Config();

        public RouteDef GetRoute(string id) => routes.Find(r => r.id == id);

        /// <summary>A view of this config whose survival/actions/events/incidents are one route's.</summary>
        public GameConfig ForRoute(string id)
        {
            var r = GetRoute(id);
            if (r == null) return this;
            return new GameConfig
            {
                survival = r.survival, actions = r.actions, events = r.events, incidents = r.incidents, dilemmas = r.dilemmas,
                puzzles = puzzles, cards = cards, presentation = presentation, routes = routes, chapter1 = chapter1
            };
        }

        public static GameConfig FromJson(string json) => JsonUtility.FromJson<GameConfig>(json);

        public ActionDef GetAction(string id) => actions.Find(a => a.id == id);
        public EventDef GetEvent(string id) => events.Find(e => e.id == id);
    }
}
