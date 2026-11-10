// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// The ship Kestrel: the same five rooms in both chapters. After landing the rooms keep their layout,
    /// gain damage, and the cargo airlock opens onto the world the player chose.
    /// Layout rule: every prop sits between x = -3.3 and +3.3 so nothing overlaps a door (doors at +-4.1).
    /// </summary>
    public partial class GameFlow
    {
        const int RCockpit = 0, RQuarters = 1, RLife = 2, RFuel = 3, RCargo = 4, ROutside = 5;

        void BuildShip(bool landed)
        {
            var w = _world;
            var kind = landed ? RoomKind.Hab : RoomKind.Ship;
            string L = landed ? "L" : "";
            var cockpit = w.AddRoom("room.cockpit", kind, Art.BackWall("cockpit" + L, landed, new RectInt(36, 34, 88, 46)));
            var quarters = w.AddRoom("room.quarters", kind, Art.BackWall("quarters" + L, landed, new RectInt(88, 46, 36, 26)));
            var life = w.AddRoom("room.lifesupport", kind, Art.BackWall("life" + L, landed));
            var fuel = w.AddRoom("room.fuelbay", kind, Art.BackWall("fuel" + L, landed));
            var cargo = w.AddRoom("room.cargo", kind, Art.BackWall("cargo" + L, landed, new RectInt(64, 52, 26, 22)));

            // cockpit: telescope | pilot seat (front) | main console | navigation
            w.Spot(cockpit, "s_scope", "spot.s_scope", Art.Telescope(), -3.7f, 0, 1.3f, 0.9f);
            w.Spot(cockpit, "s_console", landed ? "spot.s_console_goal" : "spot.s_console", Art.Console("main", 50, 26, Pal.Cyan), -0.5f, 0, 1.45f);
            w.Spot(cockpit, "s_seat", "spot.s_seat", Art.PilotSeat(), -2.2f, 0, 0.6f, 0.9f, WorldBuilder.OrderPropFront);
            w.Spot(cockpit, "s_nav", landed ? "spot.s_nav_comms" : "spot.s_nav", Art.NavComputer(false), 1.9f, 0, 1.4f);
            w.DoorTo(cockpit, 1, RQuarters, "spot.door_quarters");

            // crew quarters: bunk (+ emergency bag on it in chapter 1) | food | toolbox, board above the bunk
            w.Spot(quarters, "s_bed", "spot.s_bed", Art.Bed(), -1.7f, 0, 1.2f, 1.7f);
            w.Spot(quarters, "s_food", "spot.s_food", Art.FoodLocker(), 0.55f, 0, 1.6f);
            w.Spot(quarters, "s_toolbox", "spot.s_toolbox", Art.Toolbox(landed && _route == "mars"), 2.0f, 0, 1.3f, -0.9f);
            w.Spot(quarters, "s_board", "spot.s_board", Art.MissionBoard(false), -1.6f, 2.7f, 1.88f, 0.2f);
            w.DoorTo(quarters, -1, RCockpit, "spot.door_cockpit");
            w.DoorTo(quarters, 1, RLife, "spot.door_lifesupport");

            // life support: oxygen maker | water recycler | supply cabinet, seal panel high on the wall
            w.Spot(life, "s_o2gen", landed && _route == "mars" ? "spot.s_moxie" : "spot.s_o2gen",
                landed && _route == "mars" ? Art.Moxie(false) : Art.Electrolyzer(false), -2.4f, 0, 1.5f, 1.2f);
            w.Spot(life, "s_recycler", "spot.s_recycler", Art.Recycler(), -0.5f, 0, 1.5f);
            w.Spot(life, "s_cabinet", "spot.s_cabinet", Art.Cabinet(landed && _route == "mars"), 1.5f, 0, 1.5f);
            w.Spot(life, "s_seals", "spot.s_seals", Art.SealPanel(), 2.7f, 2.6f, 1.88f, -0.6f);
            w.DoorTo(life, -1, RQuarters, "spot.door_quarters");
            w.DoorTo(life, 1, RFuel, "spot.door_fuelbay");

            // fuel bay: tank | reactor | pipe low on the wall, gauge and (chapter 1) breach high
            w.Spot(fuel, "s_tank", "spot.s_tank", Art.FuelTank(0.64f, false), -2.6f, 0, 1.5f, 1.1f);
            w.Spot(fuel, "s_reactor", _route == "mars" ? "spot.s_reactor_mars" : "spot.s_reactor", Art.Sabatier(false, false), -0.6f, 0, 1.4f, 1.3f);
            var line = w.Spot(fuel, "s_line", landed && _route == "mars" ? "spot.s_line_broken" : "spot.s_line", Art.FuelLine(false, landed && _route == "mars" ? 0 : -1), 1.6f, 0.75f, 1.9f, -0.4f);
            line.transform.localScale = new Vector3(0.7f, 1f, 1f);
            w.Spot(fuel, "s_gauge", "spot.s_gauge", Art.Gauge(64, false), 1.2f, 2.1f, 1.88f, -0.3f);
            if (!landed)
            {
                var breach = w.Spot(fuel, "s_breach", "spot.s_breach", Art.Breach(false, 0), 2.5f, 3.0f, 1.88f, -0.8f);
                breach.SetVisible(false);
            }
            else w.Deco(fuel, Art.Breach(true, 0), 2.5f, 3.0f, 1.89f);
            w.DoorTo(fuel, -1, RLife, "spot.door_lifesupport");
            w.DoorTo(fuel, 1, RCargo, "spot.door_cargo");

            // cargo: emergency locker | manual shelf | crates (front), airlock on the right
            w.Spot(cargo, "s_locker", "spot.s_locker", Art.Locker(false), -2.5f, 0, 1.5f);
            w.Spot(cargo, "s_shelf", "spot.s_shelf", Art.Shelf(true), -0.3f, 0, 1.6f);
            w.Spot(cargo, "s_crates", "spot.s_crates", Art.Crates(), 1.9f, 0, 1.0f, -1.0f, WorldBuilder.OrderPropFront);
            w.Spot(cargo, "s_airlock", landed ? "spot.s_airlock_out" : "spot.s_airlock", Art.Door(false, true), 4.1f, 0, WorldBuilder.ZBack - 0.1f, 0f);
            w.DoorTo(cargo, -1, RFuel, "spot.door_fuelbay");

            if (landed)
            {
                // landing damage: sparks and a dim emergency light everywhere
                for (int i = 0; i < w.rooms.Count; i++)
                {
                    var r = w.rooms[i];
                    var sp = w.Deco(r, Art.Spark(i % 2), (i % 2 == 0 ? -1 : 1) * 2.9f, 4.2f + (i % 3) * 0.3f, 1.85f, WorldBuilder.OrderFx);
                    _sparks.Add(sp);
                }
            }
        }

        readonly System.Collections.Generic.List<SpriteRenderer> _sparks = new System.Collections.Generic.List<SpriteRenderer>();

        /// <summary>The outside room (index 5) shares the floor line; the route fills it.</summary>
        Room AddOutsideRoom(string nameKey)
        {
            var outside = _world.AddRoom(nameKey, RoomKind.Outside, null);
            var hull = _world.Deco(outside, Art.ShipHull(), -5.2f, 0, 1.2f);
            var door = _world.Spot(outside, "door_out", "spot.door_airlock_in", Art.Door(false, true), -4.0f, 0, 0.9f, 0.6f);
            door.leadsTo = RCargo;
            return outside;
        }

        void AnimateSparks()
        {
            for (int i = 0; i < _sparks.Count; i++)
            {
                if (_sparks[i] == null) continue;
                bool on = Mathf.PerlinNoise(Time.time * 3f, i * 7.3f) > 0.62f;
                _sparks[i].enabled = on;
                if (on) _sparks[i].sprite = Art.Spark((int)(Time.time * 10) % 2);
            }
        }
    }
}
