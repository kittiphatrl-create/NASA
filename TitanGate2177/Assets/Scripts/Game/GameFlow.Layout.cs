// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do rooms with their own shape, floor and job make the ship and the base feel like real places?
// Date: 2026-11-12
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Room-by-room layouts for the two top-down maps (see docs/redesign-v2.md, "Layouts").
    /// Ship Kestrel: engine room at the back, a long corridor down the middle with three rooms on each side, the
    /// cockpit at the nose. Mars: the landed lifeboat pod, a short tube, then the hub with modules around it.
    /// A room is a rectangle of floor; walls are made around every room; doors are floor tiles cut through walls.
    /// </summary>
    public partial class GameFlow
    {
        // tile kinds: -1 void (space), -2 Mars ground (not walkable), 2 walkable Mars ground, others = room floors
        const int KVoid = -1, KGround = -2, KOutside = 2;
        int[,] _bKind;
        readonly List<(RectInt r, string key)> _bRooms = new List<(RectInt, string)>();
        Vector2 _bHatch;   // base: left tile of the airlock door in the wall row

        void LRoom(int x0, int y0, int x1, int y1, int kind, string key)
        {
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) _bKind[x, y] = kind;
            if (key != null) _bRooms.Add((new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1), key));
        }

        void LDoor(int x, int y, int kind = 3) => _bKind[x, y] = kind;

        /// <summary>Name key of the room the point is in (null in a doorway or outside).</summary>
        string BRoomAt(Vector2 p)
        {
            var c = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
            foreach (var (r, key) in _bRooms) if (r.Contains(c)) return key;
            return null;
        }

        bool BIsOutside(Vector2 p)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
            return x >= 0 && y >= 0 && x < _bW && y < _bH && _bKind[x, y] == KOutside;
        }

        void BLayout()
        {
            _bRooms.Clear();
            if (_bMode == 1)
            {
                _bW = 44; _bH = 22;
                _bKind = new int[_bW, _bH];
                for (int x = 0; x < _bW; x++) for (int y = 0; y < _bH; y++) _bKind[x, y] = KVoid;
                LRoom(2, 4, 7, 17, 5, "ship.room.engine");
                LRoom(9, 10, 35, 12, 3, "ship.room.corridor");
                LRoom(9, 14, 16, 19, 1, "ship.room.lab");
                LRoom(18, 14, 25, 19, 4, "ship.room.quarters");
                LRoom(27, 14, 34, 19, 7, "ship.room.galley");
                LRoom(9, 3, 16, 8, 0, "ship.room.storage");
                LRoom(18, 3, 25, 8, 0, "ship.room.life");
                LRoom(27, 3, 34, 8, 8, "ship.room.bay");
                LRoom(37, 7, 41, 15, 0, "ship.room.cockpit");
                for (int y = 10; y <= 12; y++) { LDoor(8, y); LDoor(36, y); }
                foreach (int x in new[] { 12, 13, 21, 22, 30, 31 }) { LDoor(x, 13); LDoor(x, 9); }
                foreach (int y in new[] { 16, 17 }) { LDoor(17, y, 4); LDoor(26, y, 4); LDoor(8, y, 5); }
                foreach (int y in new[] { 5, 6 }) { LDoor(17, y, 0); LDoor(26, y, 0); LDoor(8, y, 5); }
            }
            else
            {
                _bW = 46; _bH = 28;
                _bKind = new int[_bW, _bH];
                for (int x = 0; x < _bW; x++) for (int y = 0; y < _bH; y++) _bKind[x, y] = KGround;
                LRoom(2, 9, 8, 15, 6, "base.room.pod");
                LRoom(10, 11, 14, 12, 3, "base.room.tube");
                LRoom(16, 7, 28, 16, 0, "base.room.hub");
                LRoom(16, 18, 25, 23, 1, "base.room.greenhouse");
                LRoom(27, 18, 31, 21, 0, "base.room.airlock");
                LRoom(30, 9, 36, 15, 4, "base.room.quarters");
                LRoom(16, 1, 21, 5, 0, "base.room.storage");
                LRoom(23, 1, 28, 5, 5, "base.room.power");
                LRoom(30, 1, 36, 5, 0, "base.room.comms");
                LRoom(27, 23, 45, 27, KOutside, null);
                LRoom(38, 1, 45, 22, KOutside, null);
                LRoom(33, 17, 37, 22, KOutside, null);
                foreach (int y in new[] { 11, 12 }) { LDoor(9, y, 6); LDoor(15, y); LDoor(29, y, 4); }
                foreach (int x in new[] { 20, 21 }) LDoor(x, 17, 1);
                foreach (int x in new[] { 27, 28 }) LDoor(x, 17, 0);
                foreach (int x in new[] { 18, 19 }) LDoor(x, 6, 0);
                foreach (int x in new[] { 25, 26 }) LDoor(x, 6, 5);
                foreach (int y in new[] { 2, 3 }) LDoor(29, y, 0);
                LDoor(29, 22, 0); LDoor(30, 22, 0);
                _bHatch = new Vector2(29, 22);
            }
        }

        /// <summary>A wall is any non-floor tile touching a room floor (outside ground does not get walls).</summary>
        bool LMakesWall(int x, int y)
        {
            if (_bKind[x, y] >= 0) return false;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= _bW || ny >= _bH) continue;
                    int k = _bKind[nx, ny];
                    if (k >= 0 && k != KOutside) return true;
                }
            return false;
        }

        bool LMakesWallAt(int x, int y) => x >= 0 && y >= 0 && x < _bW && y < _bH && _bWall != null && _bWall[x, y];

        /// <summary>A floor tile cut through a horizontal wall: floor above and below, wall a short way left and right.</summary>
        bool LDoorwayRow(int x, int y)
        {
            if (y <= 0 || y >= _bH - 1 || _bKind[x, y] < 0 || _bKind[x, y] == KOutside) return false;
            if (_bKind[x, y + 1] < 0 || _bKind[x, y - 1] < 0) return false;
            if (LMakesWallAt(x, y + 1) || LMakesWallAt(x, y - 1)) return false;
            int l = x - 1, r = x + 1;
            while (l > x - 3 && l >= 0 && !LMakesWallAt(l, y) && _bKind[l, y] >= 0) l--;
            while (r < x + 3 && r < _bW && !LMakesWallAt(r, y) && _bKind[r, y] >= 0) r++;
            return LMakesWallAt(l, y) && LMakesWallAt(r, y);
        }

        bool LIsHull(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= _bW || ny >= _bH) return true;
                    if (_bKind[nx, ny] < 0 && !_bWall[nx, ny] || _bKind[nx, ny] == KOutside) return true;
                }
            return false;
        }

        // ------------------------------------------------------------------ ship props

        void BBuildShipProps()
        {
            // ---- Lab (x 9-16, y 14-19): lettuce under grow lamps
            BProp("deco", null, Art.Plot(3, false), 10.8f, 16.4f, Vector2.zero, 2, 1);
            BProp("plants", "ship.spot.plants", Art.Plot(3, false), 13.4f, 16.4f, new Vector2(13.4f, 15.6f), 2, 1);
            BProp("deco", null, Art.Plot(2, false), 16.0f, 16.4f, Vector2.zero, 2, 1);
            BDeco(Art.Shelf(false), 10.4f, 19.12f, 2);
            BProp("can", "ship.spot.can", Art.LooseSupply("sup_water"), 15.6f, 18.8f, new Vector2(15.4f, 18.2f));
            BSprite("porthole", Art.Porthole(false), new Vector2(12.6f, 20f), SortY(20) + 1);
            for (int i = 0; i < 3; i++) BLamp(new Vector2(10.8f + i * 2.6f, 17.2f), new Color(0.95f, 0.35f, 0.95f), 3.2f, 0.6f);

            // ---- Crew quarters (x 18-25, y 14-19): bunks, the pet's bowl
            BDeco(Art.Bed(), 19.8f, 18.6f, 3);
            BDeco(Art.Bed(), 19.8f, 16.4f, 3);
            BProp("bowl", "ship.spot.bowl", Art.PetBowl(false), 24.4f, 14.6f, new Vector2(23.6f, 15.0f));
            BDeco(Art.Locker(false), 25.1f, 19.12f, 1);
            BSprite("porthole", Art.Porthole(false), new Vector2(22.2f, 20f), SortY(20) + 1);
            BLamp(new Vector2(21.5f, 16.5f), new Color(1f, 0.85f, 0.6f), 5.5f, 0.5f);

            // ---- Galley and lounge (x 27-34, y 14-19): sofa, table, food locker, parts cabinet
            BProp("sofa", "ship.spot.sofa", Art.Sofa(), 31.2f, 16.6f, new Vector2(31.2f, 15.8f), 3, 1);
            BProp("food", "ship.spot.food", Art.FoodLocker(), 33.9f, 19.12f, new Vector2(33.6f, 18.4f), 1, 1);
            BProp("cabinet", "ship.spot.cabinet", Art.Cabinet(true), 27.8f, 19.12f, new Vector2(28.4f, 18.4f), 1, 1);
            BDeco(Art.Table(), 29.4f, 14.3f, 2);
            BLamp(new Vector2(31f, 16.5f), new Color(1f, 0.85f, 0.6f), 6f, 0.5f);

            // ---- Storage (x 9-16, y 3-8)
            BDeco(Art.Shelf(true), 10.6f, 8.12f, 2);
            BDeco(Art.Shelf(false), 13.0f, 8.12f, 2);
            BDeco(Art.Crates(), 15.4f, 3.4f, 2);
            BLamp(new Vector2(12.5f, 5.5f), new Color(0.75f, 0.85f, 1f), 5f, 0.4f);

            // ---- Life support (x 18-25, y 3-8)
            BProp("recycler", "ship.spot.recycler", Art.Recycler(), 19.6f, 8.12f, new Vector2(19.6f, 7.4f), 2, 1);
            BDeco(Art.Moxie(true), 23.4f, 8.12f, 2);
            BDeco(Art.FuelTank(0.7f, false), 25.2f, 3.3f, 1);
            BLamp(new Vector2(21.5f, 5.5f), new Color(0.6f, 1f, 0.7f), 5f, 0.45f);

            // ---- Lifeboat bay (x 27-34, y 3-8): the lifeboat waits on the floor, hatch facing down
            _sHatchSr = BSprite("lbpod", Art.LifeboatPod(false, 0, StowSlots), new Vector2(30.5f, 5.0f), SortY(5.0f));
            BProp("lifeboat", "ship.spot.lifeboat", null, 30.5f, 5.0f, new Vector2(30.5f, 4.2f), 4, 2);
            BLamp(new Vector2(30.5f, 6f), new Color(1f, 0.7f, 0.3f), 5f, 0.45f);

            // ---- Engine room (x 2-7, y 4-17): tanks and the fuel maker
            BDeco(Art.FuelTank(0.9f, false), 3.0f, 17.12f, 1);
            BDeco(Art.FuelTank(0.6f, false), 4.6f, 17.12f, 1);
            BDeco(Art.Sabatier(true, true), 4.5f, 11.4f, 2);
            BDeco(Art.Rtg(true), 6.6f, 4.4f, 1);
            BLamp(new Vector2(4.5f, 10f), new Color(1f, 0.55f, 0.3f), 6f, 0.45f);

            // ---- Cockpit (x 37-41, y 7-15): window, two seats, the radio
            BSprite("window", Art.ShipWindow(), new Vector2(37.5f, 16f), SortY(16) + 1);
            BDeco(Art.PilotSeat(), 39.0f, 12.6f, 1);
            BDeco(Art.PilotSeat(), 39.0f, 9.6f, 1);
            BProp("radio", "ship.spot.radio", Art.Console("sradio", 26, 30, Pal.Cyan), 40.6f, 15.12f, new Vector2(40.4f, 14.4f), 1, 1);
            BDeco(Art.NavComputer(false), 38.0f, 15.12f, 1);
            BLamp(new Vector2(39.5f, 11f), new Color(0.5f, 0.8f, 1f), 5.5f, 0.45f);

            // ---- Corridor: arrows on the floor that light up during the scramble, pointing to the bay
            _sArrows.Clear();   // no floor guide arrows: the "!" over the bay door is enough
            BLamp(new Vector2(16f, 11.5f), new Color(0.7f, 0.8f, 1f), 4f, 0.35f);
            BLamp(new Vector2(26f, 11.5f), new Color(0.7f, 0.8f, 1f), 4f, 0.35f);
        }

        readonly List<SpriteRenderer> _sArrows = new List<SpriteRenderer>();

        // ------------------------------------------------------------------ Mars base props

        /// <summary>Seats in the lifeboat pod, where the crew gathers each evening (60 Parsecs-style status check).</summary>
        static readonly Vector2[] PodSeats = { new Vector2(3.4f, 13.0f), new Vector2(7.6f, 13.0f), new Vector2(3.4f, 10.2f), new Vector2(7.6f, 10.2f) };

        void BBuildProps()
        {
            // Low-contrast living areas anchor furniture without adding invisible collision.
            BSprite("pod-rug", Art.HabitatMat(false), new Vector2(5.3f, 11.8f), -29995);
            BSprite("hub-rug", Art.HabitatMat(true), new Vector2(22f, 11.5f), -29995);
            // ---- Lifeboat pod (x 2-8, y 9-15): four seats round the console, supplies on the shelves, porthole
            BProp("shelf_water", "base.spot.shelf_water", Art.SupplyShelf("water", 0), 3.2f, 15.12f, new Vector2(3.4f, 14.4f), 2, 1);
            BProp("shelf_food", "base.spot.shelf_food", Art.SupplyShelf("food", 0), 7.3f, 15.12f, new Vector2(7.2f, 14.4f), 2, 1);
            BSprite("porthole", Art.Porthole(true), new Vector2(4.5f, 16f), SortY(16) + 1);
            foreach (var p in PodSeats) BDeco(Art.PilotSeat(), p.x, p.y, 1);
            BProp("seats", "base.place.seats", Art.Console("pod", 22, 20, Pal.Amber), 5.5f, 11.4f, new Vector2(5.5f, 10.6f), 1, 1);
            BLamp(new Vector2(5.5f, 12f), new Color(1f, 0.8f, 0.5f), 6f, 0.55f);

            // ---- Tube (x 10-14, y 11-12)
            BLamp(new Vector2(12.5f, 12f), new Color(0.7f, 0.85f, 1f), 3f, 0.35f);

            // ---- Hub (x 16-28, y 7-16): mission board, air maker, big table
            BProp("board", "base.place.board", Art.MissionBoard(false), 23.6f, 17.05f, new Vector2(23.6f, 16.3f), order: SortY(17) + 1);
            BProp("air", "base.place.air", Art.Moxie(true), 17.4f, 16.12f, new Vector2(17.8f, 15.4f), 2, 1);
            BDeco(Art.Table(), 22.0f, 11.2f, 3);
            BDeco(Art.Crates(), 27.4f, 7.6f, 2);
            BLamp(new Vector2(22f, 12f), new Color(1f, 0.85f, 0.6f), 8f, 0.5f);

            // ---- Greenhouse (x 16-25, y 18-23): three plots under grow lamps
            for (int i = 0; i < 3; i++)
            {
                float px = 17.6f + i * 2.9f;
                BProp("plot" + i, "base.spot.plot", Art.Plot(0, false), px, 20.4f, new Vector2(px, 19.6f), 2, 1);
                BLamp(new Vector2(px, 21.2f), new Color(0.95f, 0.35f, 0.95f), 3.4f, 0.6f);
            }
            BDeco(Art.Recycler(), 24.8f, 23.12f, 1);

            // ---- Airlock (x 27-31, y 18-21): the outer door with a peephole
            _bDoor = BSprite("door", Art.PeepDoor(false), _bHatch, SortY(_bHatch.y) + 2);
            BProp("hatch", "base.place.hatch", null, _bHatch.x + 1, _bHatch.y + 0.2f, new Vector2(_bHatch.x + 1, _bHatch.y - 0.6f));
            BProp("hatch_out", "base.place.hatch", null, _bHatch.x + 1, _bHatch.y + 1.2f, new Vector2(_bHatch.x + 1, _bHatch.y + 1.6f));
            BSetDoor(false);
            BDeco(Art.Locker(false), 27.5f, 21.12f, 1);
            BDeco(Art.Locker(true), 31.5f, 21.12f, 1);
            BLamp(new Vector2(29.5f, 20f), new Color(1f, 0.7f, 0.3f), 4.5f, 0.45f);

            // ---- Quarters (x 30-36, y 9-15): beds; guests stay here
            BDeco(Art.Bed(), 32.0f, 14.4f, 3);
            BDeco(Art.Bed(), 35.2f, 9.4f, 3);
            BSprite("porthole", Art.Porthole(true), new Vector2(34f, 16f), SortY(16) + 1);
            BLamp(new Vector2(33f, 12f), new Color(1f, 0.85f, 0.6f), 5.5f, 0.5f);

            // ---- Storage (x 16-21, y 1-5): O2 tanks, locker, water recycler, storm shelter
            BProp("shelf_o2", "base.spot.shelf_o2", Art.SupplyShelf("o2", 0), 17.2f, 5.12f, new Vector2(17.4f, 4.4f), 2, 1);
            BProp("locker", "base.place.locker", Art.Locker(false), 21.1f, 5.12f, new Vector2(20.4f, 4.4f), 1, 1);
            BProp("toolbox", "base.place.toolbox", Art.Cabinet(true), 21.5f, 3.12f, new Vector2(20.6f, 3.3f), 1, 1);   // right wall, clear of the door at x 18-19
            BProp("recycler", "base.place.recycler", Art.Recycler(), 17.2f, 1.6f, new Vector2(18.4f, 1.8f), 2, 1);
            BProp("shelter", "base.place.shelter", Art.WaterWall(false), 20.6f, 1.15f, new Vector2(19.6f, 2.6f), 2, 1);
            BLamp(new Vector2(19f, 3f), new Color(0.75f, 0.85f, 1f), 5f, 0.4f);

            // ---- Power (x 23-28, y 1-5)
            BProp("battery", "base.spot.battery", Art.BatteryBank(0), 24.0f, 5.12f, new Vector2(24.0f, 4.4f), 2, 1);
            BProp("panel", "base.place.panel", Art.Console("pwr", 26, 30, Pal.Green), 28.0f, 5.12f, new Vector2(27.8f, 4.4f), 2, 1);
            BDeco(Art.FuelTank(0.6f, false), 23.6f, 1.4f, 1);
            BLamp(new Vector2(25.5f, 3f), new Color(0.5f, 1f, 0.6f), 5f, 0.4f);

            // ---- Comms (x 30-36, y 1-5)
            BProp("radio", "base.place.radio", Art.Console("radio", 26, 30, Pal.Cyan), 35.4f, 5.12f, new Vector2(35.2f, 4.4f), 2, 1);
            BDeco(Art.NavComputer(true), 31.2f, 5.12f, 1);
            BLamp(new Vector2(33f, 3f), new Color(0.4f, 0.9f, 1f), 5f, 0.45f);

            // ---- Outside
            BProp("solar", "base.spot.solar", Art.SolarArray(false, false), 41.5f, 24.4f, new Vector2(41.5f, 23.6f), 4, 1);
            BDeco(Art.SolarArray(false, false), 41.5f, 19.6f, 4);
            BProp("drill", "base.spot.drill", Art.DrillRig(true, false), 41.0f, 13.4f, new Vector2(41.0f, 12.6f), 2, 1);
            BProp("deco", null, Art.IceSite(), 41.0f, 12.95f, Vector2.zero, order: -29990);
            BProp("beacon", "base.place.beacon", Art.Dish(false), 43.0f, 6.6f, new Vector2(43.0f, 5.8f), 2, 1);
            BProp("crate", "base.place.crate", Art.CargoPod(false), 35.0f, 25.6f, new Vector2(35.0f, 24.8f), 2, 1);
            BLamp(_bHatch + new Vector2(1, 1.2f), new Color(1f, 0.75f, 0.35f), 4f, 0.5f);
            BBuildSalvage();
        }
    }
}
