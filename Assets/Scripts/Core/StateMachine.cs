using System;
using System.Collections.Generic;

namespace DrownedBell.Core
{
    /// <summary>
    /// Explicit finite state machine. Legal transitions live in an adjacency table, so an illegal request is
    /// rejected and counted instead of silently corrupting an enemy.
    /// </summary>
    public sealed class StateMachine<T> where T : struct, Enum
    {
        readonly Dictionary<T, T[]> allowed;
        readonly T initial;
        public T State { get; private set; }
        public float Time { get; private set; }
        public int Rejected { get; private set; }

        public StateMachine(T initial, Dictionary<T, T[]> allowed)
        {
            this.initial = initial;
            State = initial;
            this.allowed = allowed;
        }

        public bool Go(T to)
        {
            if (EqualityComparer<T>.Default.Equals(to, State)) return true;
            if (Array.IndexOf(allowed[State], to) < 0)
            {
                Rejected++;
                return false;
            }
            State = to;
            Time = 0f;
            return true;
        }

        /// <summary>Back to the initial state without counting it as a transition (encounter reset).</summary>
        public void Reset()
        {
            State = initial;
            Time = 0f;
        }

        public void Tick(float dt) { Time += dt; }
    }
}
