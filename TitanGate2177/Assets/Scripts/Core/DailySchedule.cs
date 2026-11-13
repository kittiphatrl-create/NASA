using System;

namespace TitanGate.Core
{
    /// <summary>Action-based day, 06:00–22:00. Exploration and reading never spend time.</summary>
    public sealed class DailySchedule
    {
        public const float StartHour = 6, EndHour = 22, AvailableHours = EndHour - StartHour;
        public float UsedHours { get; private set; }
        public float Hour => StartHour + UsedHours;
        public float Remaining => AvailableHours - UsedHours;
        public float Progress => UsedHours / AvailableHours;
        public void Reset() => UsedHours = 0;
        public bool CanSpend(float hours, float reserved = 0) => !float.IsNaN(hours) && !float.IsNaN(reserved)
            && hours >= 0 && reserved >= 0 && hours + reserved <= Remaining + .0001f;
        public bool TrySpend(float hours, float reserved = 0)
        {
            if (!CanSpend(hours, reserved)) return false;
            UsedHours = Math.Min(AvailableHours, UsedHours + hours);
            return true;
        }
    }
}
