using System;

namespace DrownedBell.Core
{
    /// <summary>
    /// A tiny seeded random source for the bosses (xorshift32, the same numbers on every platform). A run has one seed:
    /// a new game picks a new one, a save keeps it, and every attempt at a boss mixes in the death count, so no two
    /// saves and no two attempts play the same sequence. The same seed always replays the same fight, which is what
    /// the tests and the QA bot rely on.
    /// </summary>
    public sealed class Dice
    {
        uint state;

        public Dice(int seed) { Reseed(seed); }

        public void Reseed(int seed)
        {
            state = unchecked((uint)seed * 2654435761u + 0x9E3779B9u);
            if (state == 0) state = 1;
            Next();
            Next();
        }

        uint Next()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        /// <summary>0 up to (but not including) `n`.</summary>
        public int Int(int n) => (int)(Next() % (uint)Math.Max(1, n));

        /// <summary>0 up to (but not including) 1.</summary>
        public float Value() => (Next() >> 8) / 16777216f;

        public float Range(float lo, float hi) => lo + (hi - lo) * Value();
        public bool Chance(float p) => Value() < p;

        /// <summary>Mixes the run seed with what is being rolled for (`salt`: which boss) and which attempt it is.</summary>
        public static int Mix(int seed, int salt, int attempt) =>
            unchecked(seed * 73856093 ^ salt * 19349663 ^ attempt * 83492791);
    }

    /// <summary>
    /// A shuffle bag: the choices are dealt in random order, and the bag is refilled and reshuffled when empty. It looks
    /// random (the order is different every time) but never runs dry of a choice or repeats one for long, so a fight
    /// cannot go a whole minute without, say, a delayed swing, and the player is never handed the same thing five times.
    /// </summary>
    public sealed class Bag
    {
        readonly int[] items, order;
        int index;

        public Bag(params int[] items)
        {
            this.items = items;
            order = (int[])items.Clone();
            index = order.Length;
        }

        public void Reset() { index = order.Length; }

        public int Next(Dice dice)
        {
            if (index >= order.Length)
            {
                Array.Copy(items, order, items.Length);
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = dice.Int(i + 1);
                    int t = order[i];
                    order[i] = order[j];
                    order[j] = t;
                }
                index = 0;
            }
            return order[index++];
        }
    }
}
