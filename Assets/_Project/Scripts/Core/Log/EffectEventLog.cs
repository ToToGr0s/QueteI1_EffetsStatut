using System;
using System.Collections.Generic;

namespace QueteI1.StatusEffects
{
    public enum EffectEventType
    {
        Applied,
        Stacked,
        Refreshed,
        Rejected,
        Expired,
        Removed,
        Damage,
        Heal,
        Stats,
        Blocked,
        Death,
        Info
    }

    public readonly struct EffectEvent
    {
        public readonly float Time;
        public readonly string Entity;
        public readonly EffectEventType Type;
        public readonly string Message;

        public EffectEvent(float time, string entity, EffectEventType type, string message)
        {
            Time = time;
            Entity = entity;
            Type = type;
            Message = message;
        }
    }

    /// <summary>Journal de tout ce que fait le système, horodaté en temps de simulation.</summary>
    public sealed class EffectEventLog
    {
        readonly List<EffectEvent> entries = new List<EffectEvent>();
        readonly Func<float> clock;
        readonly int capacity;

        public EffectEventLog(Func<float> clock, int capacity = 300)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.capacity = Math.Max(1, capacity);
        }

        public float Now => clock();
        public IReadOnlyList<EffectEvent> Entries => entries;

        public event Action<EffectEvent> Added;

        public void Add(string entity, EffectEventType type, string message)
        {
            var entry = new EffectEvent(Now, entity, type, message);
            if (entries.Count >= capacity)
                entries.RemoveAt(0);
            entries.Add(entry);
            Added?.Invoke(entry);
        }

        public void Clear() => entries.Clear();
    }
}
