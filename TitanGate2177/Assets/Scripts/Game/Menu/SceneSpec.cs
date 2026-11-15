// Describes a layered menu background: the images (back to front, each with a depth for
// parallax), optional sprite-sheet loops, and points of interest. Written by the Blender
// pipeline as menu_scene_<name>.json; see Art/Blender/menu_pipeline.
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game.Menu
{
    public class SceneLayer
    {
        public string Name, File, Role;
        public string Video;                      // optional looping clip (Resources path) played over the still, e.g. "Cine/menu_space"
        public float Depth;                       // 0 = farthest, 1 = nearest
        public Rect Rect = new Rect(0, 0, 1, 1);  // normalised, top-left origin
        public int Cols = 1, Rows = 1, Frames = 1;
        public float Fps;
        public Texture2D Tex;
        public bool IsSheet => Frames > 1 && Cols * Rows >= Frames;
    }

    public class ScenePoint
    {
        public Vector2 P;
        public string Layer;
        public Color Color = new Color(1f, 0.55f, 0.25f);
        public float Radius, Glow;
    }

    /// <summary>Points of the first single-layer key art (menu_points.json); used only by the fallback scene.</summary>
    [System.Serializable]
    public class MenuPoints
    {
        public Vector2 sun = new Vector2(0.955f, 0.215f);
        public float sunRadius = 0.025f, sunGlow = 0.12f;
        public Vector2 airlock = new Vector2(0.66f, 0.47f);
        public List<Vector2> beacons = new List<Vector2>();
        public List<Color> beaconColors = new List<Color>();
        public List<Vector2> horizon = new List<Vector2>();
        public Rect cadet = new Rect(0.6f, 0.43f, 0.12f, 0.5f);
        public float roverWidth = 0.026f, roverGround = 0.92f;
    }

    public class SceneSpec
    {
        public string Name = "legacy";
        public float Aspect = 16f / 9f;
        public readonly List<SceneLayer> Layers = new List<SceneLayer>();
        public ScenePoint Sun, Earth;
        public readonly List<ScenePoint> Beacons = new List<ScenePoint>();
        public Vector2 Airlock = new Vector2(0.6f, 0.5f);
        public ScenePoint DoorLight;              // where the airlock's light spills out when the door opens (Mars)
        public Vector3? Window;                   // x, y, radius (ship scene)

        public SceneLayer Find(string name) => Layers.Find(l => l.Name == name);
    }
}
