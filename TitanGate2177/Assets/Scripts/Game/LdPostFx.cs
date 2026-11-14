// PROTOTYPE - NOT FOR PRODUCTION
// Full-screen grade on the top-down camera (built-in pipeline, OnRenderImage). GameFlow sets the values each frame.
using UnityEngine;

namespace TitanGate.Game
{
    [RequireComponent(typeof(Camera))]
    public class LdPostFx : MonoBehaviour
    {
        public float vignette = 1.1f, danger, dark, storm, glow = 1f;
        public Color tint = Color.white;
        Material _mat;

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (_mat == null)
            {
                var sh = Resources.Load<Shader>("Shaders/LastDockGrade");
                if (sh == null || !sh.isSupported) { Graphics.Blit(src, dst); return; }
                _mat = new Material(sh) { hideFlags = HideFlags.DontSave };
            }
            _mat.SetFloat("_Vignette", vignette); _mat.SetFloat("_Danger", danger); _mat.SetFloat("_Dark", dark);
            _mat.SetFloat("_Storm", storm); _mat.SetFloat("_Glow", glow); _mat.SetColor("_Tint", tint);
            Graphics.Blit(src, dst, _mat);
        }

        void OnDestroy() { if (_mat != null) Destroy(_mat); }
    }
}
