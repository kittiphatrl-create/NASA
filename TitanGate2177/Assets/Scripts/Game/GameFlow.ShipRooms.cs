// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does one key card that opens only one of two locked rooms turn the 60 seconds into a real decision?
// Date: 2026-11-15
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// LAST DOCK's ship: five more rooms, each with a reason to exist. Medbay (crew health on a long flight), EVA prep
    /// (suits and tools for spacewalks), cold pantry (frozen food for the trip), and two locked vaults: the power vault
    /// (a portable solar panel) and the comms vault (a radio repair kit). One key card lies somewhere on the ship, at a
    /// different spot every run; it opens one vault, then it is spent. Smaller items appear at random each run.
    /// </summary>
    public partial class GameFlow
    {
        int _ldShipSeed;
        bool _ldHasCard;
        string _ldVaultOpen;   // null, "power" or "comms"

        // door tiles of the two vaults (in the cockpit's top and bottom walls)
        static readonly Vector2Int VaultPowerDoor = new Vector2Int(38, 16), VaultCommsDoor = new Vector2Int(39, 6);
        static readonly RectInt VaultPowerRect = new RectInt(37, 17, 5, 5), VaultCommsRect = new RectInt(37, 1, 5, 5);

        void LdShipRoomsLayout()
        {
            Art.UseStationTiles = true;   // the ship gets the same weathered Blender walls and floors as the Mars base
            LRoom(9, 21, 16, 26, 1, "ship.room.medbay");
            LRoom(18, 21, 25, 26, 4, "ship.room.eva");
            LRoom(27, 21, 34, 26, 7, "ship.room.pantry");
            LRoom(37, 17, 41, 21, 5, "ship.room.vault_power");
            LRoom(37, 1, 41, 5, 5, "ship.room.vault_comms");
            foreach (int x in new[] { 12, 13 }) LDoor(x, 20, 1);
            foreach (int x in new[] { 21, 22 }) LDoor(x, 20, 4);
            foreach (int x in new[] { 30, 31 }) LDoor(x, 20, 7);
            foreach (int x in new[] { 38, 39 }) LDoor(x, 16, 5);
            foreach (int x in new[] { 39, 40 }) LDoor(x, 6, 5);
        }

        string VaultName(string v) => v == "power" ? TT("power vault", "ห้องเก็บอุปกรณ์ไฟฟ้า") : TT("comms vault", "ห้องอะไหล่สื่อสาร");
        string VaultPrize(string v) => v == "power" ? TT("a portable solar panel (+1 power every day)", "แผงโซลาร์พกพา (ไฟ +1 ทุกวัน)")
                                                   : TT("a radio repair kit (fix the radio in 1 day instead of 3)", "ชุดซ่อมวิทยุ (ซ่อมวิทยุเสร็จใน 1 วันแทน 3 วัน)");

        void LdShipRoomsProps()
        {
            _ldHasCard = false; _ldVaultOpen = null;
            _ldShipSeed = SeedOverride >= 0 ? SeedOverride : System.Environment.TickCount;

            // ---- Medbay (x 9-16, y 21-26): a bed for check-ups, the medicine cabinet, a body scanner
            BDeco(Art.Bed(), 11.2f, 24.6f, 3);
            BDeco(Art.Locker(true), 15.6f, 26.12f, 1);
            BDeco(Art.Console("medscan", 22, 26, Pal.Green), 13.2f, 26.12f, 1);
            BLamp(new Vector2(12.5f, 23.5f), new Color(0.75f, 1f, 0.85f), 5.5f, 0.45f);

            // ---- EVA prep (x 18-25, y 21-26): suit lockers and the tool rack used before spacewalks
            BDeco(Art.Locker(false), 19.0f, 26.12f, 1);
            BDeco(Art.Locker(false), 20.6f, 26.12f, 1);
            BDeco(Art.Shelf(false), 24.0f, 26.12f, 2);
            BDeco(Art.Table(), 22.0f, 22.4f, 3);
            BLamp(new Vector2(21.5f, 23.5f), new Color(1f, 0.85f, 0.6f), 5.5f, 0.45f);

            // ---- Cold pantry (x 27-34, y 21-26): freezers keep months of meals for the trip
            BDeco(Art.FoodLocker(), 28.4f, 26.12f, 1);
            BDeco(Art.FoodLocker(), 29.8f, 26.12f, 1);
            BDeco(Art.Shelf(true), 33.2f, 26.12f, 2);
            BDeco(Art.Crates(), 33.4f, 21.6f, 2);
            BLamp(new Vector2(30.5f, 23.5f), new Color(0.6f, 0.85f, 1f), 5.5f, 0.45f);

            // ---- Power vault (x 37-41, y 17-21): spare batteries and the folded solar panel, locked for safety
            BDeco(Art.BatteryBank(3), 38.4f, 21.12f, 2);
            BDeco(Art.FuelTank(0.8f, false), 40.9f, 17.4f, 1);
            BLamp(new Vector2(39.5f, 19.5f), new Color(1f, 0.85f, 0.4f), 4f, 0.45f);

            // ---- Comms vault (x 37-41, y 1-5): expensive radio spares, locked
            BDeco(Art.Console("vaultradio", 26, 30, Pal.Cyan), 40.4f, 5.12f, 1);
            BDeco(Art.Shelf(true), 38.0f, 5.12f, 1);
            BLamp(new Vector2(39.5f, 3f), new Color(0.4f, 0.9f, 1f), 4f, 0.45f);

            // the locked doors: walk up and press E
            BProp("vault_power", TT("Locked: power vault — inside: ", "ล็อก: ห้องเก็บอุปกรณ์ไฟฟ้า — ข้างในมี ") + VaultPrize("power"), null, 39f, 16.6f, new Vector2(39f, 14.8f));
            BProp("vault_comms", TT("Locked: comms vault — inside: ", "ล็อก: ห้องอะไหล่สื่อสาร — ข้างในมี ") + VaultPrize("comms"), null, 40f, 6.4f, new Vector2(40f, 7.7f));

            // one key card, somewhere different every run
            var spots = new[] { new Vector2(14.6f, 16.8f), new Vector2(24.6f, 17.6f), new Vector2(11.0f, 4.2f), new Vector2(24.2f, 4.2f),
                                new Vector2(5.0f, 9.2f), new Vector2(33.6f, 17.8f), new Vector2(12.6f, 22.6f), new Vector2(29.0f, 22.8f) };
            var rng = new System.Random(_ldShipSeed * 13 + 7);
            var p = spots[rng.Next(spots.Length)];
            var card = BProp("keycard", TT("Key card", "คีย์การ์ด"), Art.LooseSupply("keycard"), p.x, p.y, p + new Vector2(0, -0.6f));
            card.sr.sortingOrder = SortY(p.y) + 1;
            BLamp(p + new Vector2(0, 0.3f), new Color(1f, 0.9f, 0.5f), 1.4f, 0.55f);
        }

        /// <summary>LAST DOCK scramble items: the four basics spread over the ship, the vault prizes, and a random set of extras.</summary>
        List<(string id, float x, float y)> LdScrambleItems()
        {
            var list = new List<(string, float, float)>(LdShipSupplies);
            list.Add(("sup_solar", 39.5f, 19.2f));
            list.Add(("sup_radiokit", 39.5f, 2.8f));
            var rng = new System.Random(_ldShipSeed * 31 + 3);
            void Maybe(string id, double chance, params Vector2[] at) { if (rng.NextDouble() < chance) { var v = at[rng.Next(at.Length)]; list.Add((id, v.x, v.y)); } }
            Maybe("sup_medkit", 0.8, new Vector2(10.6f, 22.4f), new Vector2(15.0f, 22.6f));
            Maybe("sup_radmeter", 0.7, new Vector2(23.6f, 22.6f), new Vector2(19.4f, 24.2f));
            Maybe("sup_flashlight", 0.7, new Vector2(24.6f, 24.4f), new Vector2(20.0f, 16.2f));
            Maybe("sup_frozen", 0.75, new Vector2(31.6f, 22.4f), new Vector2(28.8f, 23.6f));
            return list;
        }

        /// <summary>A scramble item inside a vault that is still locked (the bot skips these).</summary>
        bool LdShipItemLocked(BSpot s)
        {
            var c = new Vector2Int(Mathf.FloorToInt(s.pos.x), Mathf.FloorToInt(s.pos.y));
            return VaultPowerRect.Contains(c) && _ldVaultOpen != "power" || VaultCommsRect.Contains(c) && _ldVaultOpen != "comms";
        }

        /// <summary>Is this ship door one of the vaults and still shut?</summary>
        bool ShipVaultDoorLocked(RectInt doorRect)
        {
            if (_bMode != 1 || !LastDockMode) return false;
            if (doorRect.Contains(VaultPowerDoor)) return _ldVaultOpen != "power";
            if (doorRect.Contains(VaultCommsDoor)) return _ldVaultOpen != "comms";
            return false;
        }

        /// <summary>Ship spots for LAST DOCK: the key card and the vault doors. True when handled.</summary>
        bool LdShipUse(BSpot s)
        {
            if (!LastDockMode) return false;
            if (s.id == "keycard")
            {
                _ldHasCard = true;
                s.sr.enabled = false; s.on = false;
                _sfx.Play("card");
                _ui.Toast(TT("Key card! It opens ONE locked room, then it is used up: the power vault or the comms vault.",
                             "ได้คีย์การ์ด! เปิดห้องล็อกได้ห้องเดียว แล้วการ์ดจะใช้ไม่ได้อีก: ห้องเก็บอุปกรณ์ไฟฟ้า หรือ ห้องอะไหล่สื่อสาร"), GameUI.CAmber);
                return true;
            }
            if (s.id != "vault_power" && s.id != "vault_comms") return false;
            string v = s.id == "vault_power" ? "power" : "comms";
            if (_ldVaultOpen == v) { _ui.Toast(TT("This room is open.", "ห้องนี้เปิดแล้ว"), GameUI.CDim); return true; }
            if (_ldVaultOpen != null) { _sfx.Play("bad"); _ui.Toast(TT("The key card is used up. This room stays locked.", "คีย์การ์ดใช้ไปแล้ว ห้องนี้เปิดไม่ได้อีก"), GameUI.CRed); return true; }
            if (!_ldHasCard) { _sfx.Play("bad"); _ui.Toast(TT("Locked. A key card is somewhere on the ship.", "ล็อกอยู่ มีคีย์การ์ดซ่อนอยู่สักที่บนยาน"), GameUI.CRed); return true; }
            _ui.ShowModal(p =>
            {
                LdHeader(p, TT("Use the key card here?", "ใช้คีย์การ์ดเปิดห้องนี้?"));
                p.Add(LdPara(TT($"The {VaultName(v)} holds {VaultPrize(v)}.", $"{VaultName(v)} มี{VaultPrize(v)}")));
                string other = v == "power" ? "comms" : "power";
                p.Add(LdPara(TT($"The card opens only ONE room. The {VaultName(other)} ({VaultPrize(other)}) will stay locked.",
                                $"การ์ดเปิดได้ห้องเดียว {VaultName(other)} ({VaultPrize(other)}) จะเปิดไม่ได้อีก"), GameUI.CAmber));
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 12;
                row.Add(_ui.B(TT("Open it", "เปิดห้องนี้"), () => { _ui.CloseModal(); LdOpenVault(v); }, 26, true));
                row.Add(_ui.B(TT("Not yet", "ยังก่อน"), _ui.CloseModal, 24));
                p.Add(row);
            });
            return true;
        }

        void LdOpenVault(string v)
        {
            _ldVaultOpen = v; _ldHasCard = false;
            _sfx.Play("good");
            _ui.Toast(TT($"The {VaultName(v)} is open. Grab {VaultPrize(v)}!", $"เปิด{VaultName(v)}แล้ว รีบหยิบ{VaultPrize(v)}!"), GameUI.CGreen);
        }

        /// <summary>Playtest bot: pick up the key card and open a vault before grabbing supplies.</summary>
        IEnumerator LdBotKeycard(string vault)
        {
            var card = BSpotById("keycard");
            if (card == null || !card.sr.enabled) yield break;
            yield return AutoWalkUse("keycard");
            if (!_ldHasCard) yield break;
            yield return AutoWalkUse("vault_" + vault);
            _ui.CloseModal();
            LdOpenVault(vault);
            PNote($"bot: key card used on the {vault} vault");
        }
    }
}
