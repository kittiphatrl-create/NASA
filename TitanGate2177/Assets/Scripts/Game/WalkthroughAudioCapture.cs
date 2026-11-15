using System.Collections.Concurrent;
using System.IO;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>QA-only capture of the actual listener mix; never synthesizes or replaces audio.</summary>
    public sealed class WalkthroughAudioCapture : MonoBehaviour
    {
        readonly ConcurrentQueue<float[]> _blocks = new ConcurrentQueue<float[]>();
        BinaryWriter _writer;
        string _path;
        volatile bool _recording;
        volatile int _channels = 2;
        int _rate;
        public void Begin(string path)
        {
            _path = path; _rate = AudioSettings.outputSampleRate;
            _writer = new BinaryWriter(File.Create(path)); _recording = true;
        }
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_recording) return;
            _channels = channels; _blocks.Enqueue((float[])data.Clone());
        }
        void Update() => Drain();
        void Drain()
        {
            if (_writer == null) return;
            while (_blocks.TryDequeue(out var block)) foreach (var sample in block) _writer.Write(sample);
        }
        public void Finish()
        {
            if (_writer == null) return;
            _recording = false; Drain(); _writer.Close(); _writer = null;
            File.WriteAllText(_path + ".json", "{\"sampleRate\":" + _rate + ",\"channels\":" + _channels + "}");
        }
        void OnApplicationQuit() => Finish();
        void OnDestroy() => Finish();
    }
}
