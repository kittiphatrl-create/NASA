using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        StreamWriter _recordTimes;
        IEnumerator RecordWalkthrough()
        {
            string dir = Path.Combine(_shotDir, "frames"); Directory.CreateDirectory(dir);
            var listener = FindFirstObjectByType<AudioListener>();
            if (listener != null) listener.gameObject.AddComponent<WalkthroughAudioCapture>().Begin(Path.Combine(dir, "audio.f32"));
            _recordTimes = new StreamWriter(Path.Combine(dir, "times.csv"), false);
            float start = Time.unscaledTime, next = start; int frame = 0;
            while (frame < 6000)
            {
                yield return new WaitForEndOfFrame();
                float now = Time.unscaledTime;
                if (now < next) continue;
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"frame_{frame:00000}.png"));
                _recordTimes.WriteLine(frame + "," + (now - start).ToString("F5", CultureInfo.InvariantCulture)); _recordTimes.Flush();
                frame++; next = now + 1f / 12;
            }
            _recordTimes?.Close(); _recordTimes = null;
        }
        void OnApplicationQuit() { _recordTimes?.Close(); _recordTimes = null; }
    }
}
