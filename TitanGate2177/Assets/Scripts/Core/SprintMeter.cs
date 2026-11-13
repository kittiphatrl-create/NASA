using System;

namespace TitanGate.Core
{
    public sealed class SprintMeter
    {
        public float Value { get; private set; } = 100;
        public bool Exhausted { get; private set; }
        public bool CanSprint => !Exhausted && Value > 0;
        float rest;
        public void Reset() { Value = 100; rest = 0; Exhausted = false; }
        public void Tick(float dt, bool running)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if (running && CanSprint)
            {
                Value = Math.Max(0, Value - dt * 23); rest = .7f;
                if (Value <= 0) Exhausted = true;
            }
            else
            {
                float recover = Math.Max(0, dt - rest); rest = Math.Max(0, rest - dt);
                Value = Math.Min(100, Value + recover * 21);
                if (Value >= 30) Exhausted = false;
            }
        }
    }
}
