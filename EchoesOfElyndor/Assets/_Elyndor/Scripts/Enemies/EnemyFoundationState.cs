namespace Elyndor.Enemies
{
    /// <summary>
    /// Lesbare Zustaende der Gegnergrundlage. Bewusst generisch gehalten:
    /// der spaetere Wurzelstreifer und weitere Gegnerfamilien bauen darauf auf,
    /// ohne dass hier bereits ein konkreter Gegner festgelegt wird.
    /// </summary>
    public enum EnemyFoundationState
    {
        /// <summary>Kein Ziel wahrgenommen.</summary>
        Idle = 0,

        /// <summary>Ziel bemerkt, aber noch nicht verfolgt.</summary>
        Alert = 1,

        /// <summary>Bewegt sich auf das Ziel zu.</summary>
        Chase = 2,

        /// <summary>In Reichweite, fuehrt Angriffe aus.</summary>
        Attack = 3,

        /// <summary>Kurze Trefferreaktion; unterbricht Verfolgung und Angriff.</summary>
        Hurt = 4,

        /// <summary>Endzustand. Wird nie wieder verlassen.</summary>
        Dead = 5
    }
}
