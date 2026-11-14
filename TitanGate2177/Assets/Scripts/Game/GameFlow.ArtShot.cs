using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using TitanGate.Core;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        string ArtGeometrySignature()
        {
            var b = new StringBuilder();
            for (int x = 0; x < _bW; x++) for (int y = 0; y < _bH; y++) b.Append(_bSolid[x, y] ? '1' : '0');
            foreach (var s in _bSpots.OrderBy(s => s.id)) b.Append(s.id).Append(s.stand.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).Append(s.stand.y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            return b.ToString();
        }

        IEnumerator AutoArt()
        {
            _loc.Current = Lang.TH; yield return new WaitForSeconds(2);
            string signature = null;
            for (int theme = 0; theme < 3; theme++)
            {
                Art.HabitatTheme = theme; StartLifeboat(null, "comet"); yield return new WaitForSeconds(.7f);
                // Presentation-only staging; the full play route independently verifies navigation.
                for (float y = 10.5f; y < 13; y++) { var at = new Vector2(22.5f, y); if (BFree(at)) { _bPos = at; break; } }
                BSnapCamera();
                var floor = Art.BaseFloor(0, 0).bounds.size;
                Check(Mathf.Abs(floor.x - 1) < .001f && Mathf.Abs(floor.y - 1) < .001f, "floor keeps its one-world-unit footprint: theme " + theme);
                var wall = Art.BaseWall(true, false);
                Check(Mathf.Abs(wall.bounds.size.x - 1) < .001f && Mathf.Abs(wall.bounds.size.y - 2) < .001f, "wall keeps its original world bounds: theme " + theme);
                if (signature == null) signature = ArtGeometrySignature();
                Check(ArtGeometrySignature() == signature, "collision and interaction coordinates unchanged across themes: " + theme);
                yield return Shot("theme_" + theme + "_arrival");
                var snap = _restore.Snapshot(); snap.done = _cfg.restore.tasks.Select(t => t.id).ToArray(); snap.unpowered = new string[0]; _restore.Load(snap);
                RestoreSync(); yield return new WaitForSeconds(9); BSnapCamera();
                yield return Shot("theme_" + theme + "_restored");
                Check(!_ui.Blocking && BFree(_bPos), "art staging leaves player on reachable floor: " + theme);
            }
            Art.HabitatTheme = 0;
            Log($"done errors={_errors} checksFailed={_checksFailed}"); _autoLog?.Close(); _autoLog = null;
            yield return new WaitForSeconds(.5f); Application.Quit();
        }
    }
}
