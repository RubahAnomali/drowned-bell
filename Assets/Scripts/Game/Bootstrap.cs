using UnityEngine;

namespace DrownedBell.Game
{
    /// <summary>
    /// The main scene contains a "Drowned Bell" object with the GameController (so tuning can be saved in the
    /// Inspector). If you press Play in any other scene, this builds one from code so the game still runs.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureGame()
        {
            if (Object.FindAnyObjectByType<GameController>() != null) return;
            new GameObject("Drowned Bell").AddComponent<GameController>();
        }
    }
}
