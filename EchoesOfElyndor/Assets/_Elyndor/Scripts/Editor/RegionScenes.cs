namespace Elyndor.EditorTools
{
    /// <summary>
    /// Die derzeit produktiv verwendeten Szenen an einer Stelle.
    ///
    /// Migrator, Integritaetspruefung und Tests liefen bisher jeweils mit einem
    /// eigenen, fest verdrahteten Pfad. Sobald eine Region dazukommt oder
    /// umzieht, laufen solche Kopien auseinander und eine Szene faellt still aus
    /// der Pruefung. Deshalb gibt es genau eine Liste.
    /// </summary>
    public static class RegionScenes
    {
        public const string Finsterwald =
            "Assets/_Elyndor/Scenes/Finsterwald.unity";

        public const string Sonnenfelder =
            "Assets/_Elyndor/Scenes/Sonnenfelder.unity";

        public const string Nebelmoor =
            "Assets/_Elyndor/Scenes/Nebelmoor.unity";

        /// <summary>
        /// Sandbox ohne Erlebnisschicht. Bewusst keine Region: Bootstrap traegt
        /// weder HUD noch Audio und wird deshalb gegen ein eigenes,
        /// minimales Profil geprueft.
        /// </summary>
        public const string Bootstrap =
            "Assets/_Elyndor/Scenes/Bootstrap.unity";

        /// <summary>
        /// Alle Regionsszenen. Reihenfolge ist die Referenzregion zuerst,
        /// damit ein Sammelreport mit dem bekannten Zustand beginnt.
        /// </summary>
        public static readonly string[] All =
        {
            Finsterwald,
            Sonnenfelder,
            Nebelmoor
        };
    }
}
