// Playtest route for the live 3D menu (-autoshot -menulive): opens the main menu, turns and zooms the ship through
// the same calls the pointer and wheel use, and captures each state. No desktop input is sent.
using System.Collections;
using TitanGate.Game.Menu;
using UnityEngine;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        MenuLive3D _mmLive;

        IEnumerator AutoMenuLive()
        {
            yield return new WaitForSeconds(1f);
            ShowMainMenu();
            yield return new WaitForSeconds(5f);   // past the intro
            Check(_mmLive != null, "live 3D menu scene built");
            if (_mmLive != null) Log("probe " + _mmLive.Probe());
            yield return Shot("m1_default");
            if (_mmLive != null)
            {
                for (int i = 0; i < 30; i++) { _mmLive.Drag(new Vector2(-14f, 4f)); yield return null; }
                _mmLive.EndDrag();
                yield return new WaitForSeconds(1.2f);
                yield return Shot("m2_dragged");
                for (int i = 0; i < 5; i++) { _mmLive.Zoom(-1f); yield return new WaitForSeconds(0.06f); }
                yield return new WaitForSeconds(1.2f);
                yield return Shot("m3_zoomed");
                for (int i = 0; i < 9; i++) { _mmLive.Zoom(1f); yield return new WaitForSeconds(0.06f); }
                for (int i = 0; i < 40; i++) { _mmLive.Drag(new Vector2(18f, -6f)); yield return null; }
                _mmLive.EndDrag();
                yield return new WaitForSeconds(1.2f);
                yield return Shot("m4_far_side");
                yield return new WaitForSeconds(9f);   // idle: the camera drifts home and turns on its own
                yield return Shot("m5_idle");
            }
            Log($"done errors={_errors} checksFailed={_checksFailed}");
            Application.Quit();
        }
    }
}
