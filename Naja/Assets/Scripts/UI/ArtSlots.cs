// LAST 72 HOURS — art slots.
// Friends drop PNG files into Assets/Resources/Art/ with the names listed in docs/ART_SLOTS.md.
// If a file exists the game uses it; if not, the graybox shape stays. No code change needed.
using System.Collections.Generic;
using UnityEngine;

namespace Last72
{
    public static class ArtSlots
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// Sprite for a slot name (e.g. "room_quarters", "obj_locker", "item_fuse"), or null.
        public static Sprite Get(string key)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            s = Resources.Load<Sprite>("Art/" + key);
            if (s == null)
            {
                // texture imported as "Default" instead of "Sprite": wrap it
                var tex = Resources.Load<Texture2D>("Art/" + key);
                if (tex != null) s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            cache[key] = s;
            return s;
        }

        /// Readable copy of a texture (cursor needs CPU-readable pixels, imported art usually isn't).
        public static Texture2D GetReadable(string key)
        {
            var s = Get(key);
            if (s == null) return null;
            var src = s.texture;
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        public static string RoomKey(Room r)
        {
            switch (r)
            {
                case Room.Quarters: return "room_quarters";
                case Room.Command: return "room_command";
                case Room.LifeSupport: return "room_lifesupport";
                case Room.Cabin: return "room_cabin";
                case Room.CommsRoom: return "room_commsroom";
                case Room.Crawlspace: return "room_crawlspace";
                case Room.PartsStorage: return "room_partsstorage";
                default: return "room_engineering";
            }
        }

        public static string ItemKey(Item i) { return "item_" + i.ToString().ToLowerInvariant(); }

        public static string HotspotKey(Hotspot h)
        {
            if (h.DoorTo.HasValue)
            {
                if (GameModel.IsInner(h.DoorTo.Value)) return "door_hatch";   // back-wall hatch
                if (GameModel.IsInner(h.Room)) return "door_back";            // way out of an inner room
                return h.X < 0.5f ? "door_left" : "door_right";
            }
            return "obj_" + h.Id;
        }
    }
}
