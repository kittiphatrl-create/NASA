// Video capture for the presentation clip. Off unless the player is started with:
//   -recordvideo <out.mp4> -ffmpeg <path to ffmpeg.exe> [-recordfps 30]
// The game then runs on a fixed clock (Time.captureFramerate), keeps running when its window is in the
// background, and streams every rendered frame straight into FFmpeg. The result is a smooth video at the
// requested frame rate even when encoding is slower than real time. Audio is not recorded.
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace TitanGate.Game
{
    public class VideoRecorder : MonoBehaviour
    {
        Process _ff;
        Stream _pipe;
        int _w, _h;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string outPath = Arg("-recordvideo"), ffmpeg = Arg("-ffmpeg");
            if (string.IsNullOrEmpty(outPath) || string.IsNullOrEmpty(ffmpeg)) return;
            var go = new GameObject("VideoRecorder");
            DontDestroyOnLoad(go);
            go.AddComponent<VideoRecorder>().Begin(outPath, ffmpeg, int.TryParse(Arg("-recordfps"), out int f) ? f : 30);
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }

        void Begin(string outPath, string ffmpeg, int fps)
        {
            Application.runInBackground = true;
            Time.captureFramerate = fps;
            _w = Screen.width & ~1; _h = Screen.height & ~1;
            var psi = new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = $"-y -f rawvideo -pix_fmt rgba -s {_w}x{_h} -r {fps} -i - -vf vflip " +
                            $"-c:v h264_nvenc -preset p5 -cq 19 -pix_fmt yuv420p \"{outPath}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
            _ff = Process.Start(psi);
            _pipe = _ff.StandardInput.BaseStream;
            StartCoroutine(Capture());
        }

        IEnumerator Capture()
        {
            var rt = new RenderTexture(_w, _h, 0, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(_w, _h, TextureFormat.RGBA32, false);
            while (_pipe != null)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, _w, _h), 0, 0, false);
                RenderTexture.active = prev;
                var data = tex.GetRawTextureData();
                try { _pipe.Write(data, 0, data.Length); }
                catch (IOException) { _pipe = null; }
            }
        }

        void OnApplicationQuit()
        {
            try { _pipe?.Flush(); _pipe?.Close(); } catch (IOException) { }
            _pipe = null;
            _ff?.WaitForExit(20000);
        }
    }
}
