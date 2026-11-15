// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a living title screen (parallax layers from Blender, drifting Mars dust, a title that builds itself,
//           icon tiles, a cadet you can poke) make the first ten seconds exciting?
// Date: 2026-11-15
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Lang = TitanGate.Core.Lang;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        static Texture2D _ldCadetLayer;

        static Texture2D LdCadetLayer()
        {
            if (_ldCadetLayer != null) return _ldCadetLayer;
            var ta = Resources.Load<TextAsset>("Sprites/KeyArt/title_cadet.png");
            if (ta == null) return null;
            _ldCadetLayer = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            _ldCadetLayer.LoadImage(ta.bytes);
            return _ldCadetLayer;
        }

        static VisualElement LdFill()
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute; e.style.left = 0; e.style.right = 0; e.style.top = 0; e.style.bottom = 0;
            return e;
        }

        /// <summary>LAST DOCK title screen. Returns false when the art is missing (the old menu is used then).</summary>
        bool LdBuildTitle() => BuildMainMenuV2();   // the redesigned menu (GameFlow.MainMenu.cs, Scripts/Game/Menu)
    }
}
