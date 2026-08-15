using System;
using Elyndor.Memory;
using Elyndor.Puzzles;
using UnityEngine;

namespace Elyndor.World
{
    /// <summary>
    /// Der Wald antwortet auf Arens Verstehen.
    ///
    /// Die Veränderung ist bewusst leise. Kein Lichtblitz, keine Welle, kein
    /// Moment. Ein paar graue Pflanzen bekommen Farbe, das Wasser wird klarer,
    /// das Licht etwas wärmer, und ein Weg, der vorher nicht ging, geht jetzt.
    /// Der Spieler soll zuerst denken „Moment — hier ist etwas anders" und erst
    /// danach „der Wald reagiert". Ein Effekt, der das sofort ausspricht,
    /// nimmt ihm beides weg.
    ///
    /// <b>Zwei Bedingungen, nicht eine.</b> Rätsel und Memory Site laufen im
    /// Code getrennt; verlangt werden deshalb beide. „Trigger betreten" genügt
    /// ausdrücklich nicht — sonst reagierte der Wald auf einen Schritt statt
    /// auf ein Verstehen.
    ///
    /// Der Ablauf ist idempotent: mehrfaches Auslösen, ein Szenenwechsel oder
    /// eine Wiederherstellung führen zu genau demselben Ergebnis, ohne
    /// doppelte Objekte und ohne doppelte Effekte.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FinsterwaldRegeneration : MonoBehaviour
    {
        [Header("Identität")]
        [SerializeField]
        private string regionStateId = "finsterwald_bruecke_regeneration";

        [Header("Bedingungen")]
        [Tooltip("Das Rätsel, das gelöst sein muss.")]
        [SerializeField] private BridgePuzzle puzzle;

        [Tooltip("Die Memory Site, die gesehen sein muss.")]
        [SerializeField] private string memorySiteId = "finsterwald_bruecke_01";

        [Header("Was zurückkehrt")]
        [Tooltip("Vorher abgeschaltete Objekte: gesunde Triebe, Halme.")]
        [SerializeField] private GameObject[] awakening = Array.Empty<GameObject>();

        [Tooltip("Pflanzen, die Farbe zurückbekommen.")]
        [SerializeField] private Renderer[] recolouring = Array.Empty<Renderer>();

        [Tooltip("Zielfarbe der wiederbelebten Pflanzen.")]
        [SerializeField] private Color revivedTint = new Color(0.42f, 0.62f, 0.28f);

        [Tooltip("Wasserflächen, die klarer werden.")]
        [SerializeField] private Renderer[] clearingWater = Array.Empty<Renderer>();

        [Tooltip("Zielfarbe des klareren Wassers.")]
        [SerializeField] private Color clearWaterTint =
            new Color(0.46f, 0.72f, 0.78f, 0.72f);

        [Tooltip("Licht, das etwas wärmer wird. Darf fehlen.")]
        [SerializeField] private Light warmingLight;

        [SerializeField] private Color warmLightColour =
            new Color(1f, 0.95f, 0.86f);

        [Header("Wegöffnung")]
        [Tooltip("Der einzige Collider, der verändert wird. Alles andere " +
                 "bleibt, wie es ist.")]
        [SerializeField] private Collider blockedPath;

        /// <summary>Wird genau einmal je Abschnitt ausgeloest.</summary>
        public static event Action AnyRegionRegenerated;

        /// <summary>Ist dieser Abschnitt regeneriert?</summary>
        public bool IsRegenerated { get; private set; }

        /// <summary>Wurde die Memory Site in dieser Sitzung gesehen?</summary>
        public bool MemorySeen => MemorySessionState.IsActivated(memorySiteId);

        /// <summary>Ist das Rätsel gelöst?</summary>
        public bool PuzzleSolved => puzzle != null && puzzle.IsSolved;

        /// <summary>Sind beide Bedingungen erfüllt?</summary>
        public bool ConditionsMet => PuzzleSolved && MemorySeen;

        public string RegionStateId => regionStateId;

        private void Awake()
        {
            // Ein bereits regenerierter Abschnitt wird sofort wiederhergestellt
            // — ohne Ton und ohne Ereignis, denn geschehen ist es schon.
            if (RegionRegenerationState.IsRegenerated(regionStateId))
            {
                Apply();
                IsRegenerated = true;
            }
        }

        private void Update()
        {
            Tick();
        }

        /// <summary>Ein Schritt; oeffentlich fuer Tests.</summary>
        public void Tick()
        {
            if (IsRegenerated || !ConditionsMet)
            {
                return;
            }

            Regenerate();
        }

        /// <summary>
        /// Führt die Veränderung aus. Mehrfache Aufrufe sind folgenlos.
        /// </summary>
        public void Regenerate()
        {
            if (IsRegenerated)
            {
                return;
            }

            IsRegenerated = true;
            RegionRegenerationState.MarkRegenerated(regionStateId);

            Apply();

            AnyRegionRegenerated?.Invoke();
        }

        /// <summary>
        /// Der eigentliche Zustand. Bewusst getrennt vom Auslösen: das
        /// Wiederherstellen nach einem Szenenwechsel braucht denselben
        /// Zustand, aber weder Ton noch Ereignis.
        /// </summary>
        private void Apply()
        {
            foreach (GameObject shoot in awakening)
            {
                if (shoot != null && !shoot.activeSelf)
                {
                    shoot.SetActive(true);
                }
            }

            Tint(recolouring, revivedTint);
            Tint(clearingWater, clearWaterTint);

            if (warmingLight != null)
            {
                warmingLight.color = warmLightColour;
            }

            // Der einzige Collider, der angefasst wird.
            if (blockedPath != null)
            {
                blockedPath.enabled = false;
            }
        }

        /// <summary>
        /// Färbt über einen <see cref="MaterialPropertyBlock"/>. Direkt auf das
        /// Material zu schreiben würde das geteilte Asset verändern — und damit
        /// jede andere Pflanze im Wald gleich mit.
        /// </summary>
        private static void Tint(Renderer[] renderers, Color colour)
        {
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            int baseColour = Shader.PropertyToID("_BaseColor");
            int legacyColour = Shader.PropertyToID("_Color");

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(block);
                block.SetColor(baseColour, colour);
                block.SetColor(legacyColour, colour);
                renderer.SetPropertyBlock(block);
            }
        }

        /// <summary>Setzt Verweise, etwa aus einem Aufbauwerkzeug.</summary>
        public void Configure(
            BridgePuzzle newPuzzle,
            GameObject[] newAwakening,
            Renderer[] newRecolouring,
            Renderer[] newWater,
            Light newLight,
            Collider newBlockedPath)
        {
            puzzle = newPuzzle;
            awakening = newAwakening ?? Array.Empty<GameObject>();
            recolouring = newRecolouring ?? Array.Empty<Renderer>();
            clearingWater = newWater ?? Array.Empty<Renderer>();
            warmingLight = newLight;
            blockedPath = newBlockedPath;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            AnyRegionRegenerated = null;
        }
    }
}
