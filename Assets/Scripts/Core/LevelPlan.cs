namespace DrownedBell.Core
{
    public enum Beat { Teach, Develop, Combine, Rest, Climax }

    public static class LevelPlan
    {
        // Room IDs follow the room generator. The downward shaft and arena drops are one-way.
        public static readonly (Beat Beat, float Intensity)[] Beats =
        {
            (Beat.Teach, 0.1f), (Beat.Teach, 0.4f), (Beat.Teach, 0.3f),
            (Beat.Develop, 0.6f), (Beat.Combine, 0.8f), (Beat.Rest, 0.2f), (Beat.Climax, 1f),
        };

        public const int WinchRoom = 5;
        public static readonly (int From, int To, bool NeedsWinch)[] Exits =
        {
            (0, 1, false), (1, 0, false), (1, 2, false), (2, 1, false), (2, 3, false),
            (3, 4, false), (4, 3, false), (4, 5, false), (5, 4, false), (5, 6, false),
            (0, 5, true), (5, 0, true),
        };
    }
}
