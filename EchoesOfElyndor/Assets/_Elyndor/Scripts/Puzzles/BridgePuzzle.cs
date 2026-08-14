using System;
using Elyndor.Core;
using Elyndor.Memory;
using UnityEngine;

namespace Elyndor.Puzzles
{
    /// <summary>
    /// „Die geteilte Brücke" — das erste Memory-Watch-Rätsel.
    ///
    /// Die Watch zeigt Vergangenheit und Beziehungen. Sie bewegt nichts durch
    /// die Zeit und zeigt nie die Lösung. Der Spieler liest das Echo und
    /// verändert nur heutige Objekte: drei drehbare Anker, die den gefallenen
    /// Stamm halten.
    ///
    /// Dieser Controller hält den Zustand und entscheidet, was erlaubt ist. Die
    /// Bewertung der Ankerstellung liegt in <see cref="BridgePuzzleRules"/> und
    /// ist damit ohne Szene prüfbar; die Anker selbst wissen nicht, ob sie
    /// richtig stehen.
    ///
    /// Jeder Fehlversuch ist umkehrbar. Es gibt keinen Zustand, aus dem der
    /// Spieler nicht mehr herauskommt, und keinen, der ihn bestraft.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BridgePuzzle : MonoBehaviour
    {
        [Header("Identität")]
        [Tooltip("Stabile ID für Sitzungszustand und späteres Speichern.")]
        [SerializeField]
        private string puzzleId = "finsterwald_bridge_memory_puzzle_v1";

        [Tooltip("Die Memory Site, deren Echo dieses Rätsel erklärt.")]
        [SerializeField] private string memorySiteId = "finsterwald_bruecke_01";

        [Header("Teile")]
        [SerializeField] private BridgeAnchor[] anchors = Array.Empty<BridgeAnchor>();

        [Tooltip("Der gefallene Stamm, der zur Brücke wird.")]
        [SerializeField] private Transform fallenLog;

        [Tooltip("Wo der Stamm nach der Freigabe liegt.")]
        [SerializeField] private Transform deployedPose;

        [Tooltip("Begehbare Fläche; erst mit den Bohlen sicher.")]
        [SerializeField] private Collider walkway;

        [Tooltip("Die zwei Bohlen, die den Übergang sichern.")]
        [SerializeField] private GameObject[] plankVisuals = Array.Empty<GameObject>();

        [Header("Zeiten")]
        [Tooltip("Wie lange der Stamm zum Einrasten braucht.")]
        [Min(0.1f)] [SerializeField] private float deployDuration = 2.2f;

        [Tooltip("Sperre nach einem verkanteten Versuch.")]
        [Min(0f)] [SerializeField] private float recoverDuration = 1.2f;

        [Header("Rückmeldung")]
        [SerializeField, TextArea(2, 4)]
        private string tensionHoldsText =
            "Das Seil steht straff. Der Stamm könnte tragen.";

        [SerializeField, TextArea(2, 4)]
        private string tensionSlackText =
            "Irgendwo gibt das Seil nach. So trägt der Stamm nicht.";

        [SerializeField, TextArea(2, 4)]
        private string jammedText =
            "Der Stamm verkantet und rutscht zurück in die Seile.";

        [SerializeField, TextArea(2, 4)]
        private string solvedText =
            "Der Stamm liegt in den alten Aufnahmen. Die Brücke trägt wieder.";

        [Min(1f)] [SerializeField] private float messageDuration = 5f;

        private Vector3 stowedPosition;
        private Quaternion stowedRotation;
        private float deployProgress;
        private float recoverRemaining;
        private int planksPlaced;
        private bool playerInZone;
        private bool subscribed;

        /// <summary>Alter und neuer Zustand nach einem erfolgten Wechsel.</summary>
        public event Action<BridgePuzzleState, BridgePuzzleState> StateChanged;

        /// <summary>
        /// Wird ausgeloest, sobald die Ankerstellung bewertet wurde — beim
        /// Pruefen am Seilbock und beim Freigeben des Stamms. Der Parameter
        /// sagt <b>nur</b>, ob die Stellung traegt.
        ///
        /// Bewusst ein einzelnes bool und nicht etwa die Ankerstellungen oder
        /// die Zahl der richtigen Anker: an diesem Ereignis haengt der Ton,
        /// und ein Ton, der verraet <em>welcher</em> Anker falsch steht, waere
        /// die Loesung in Raten. Was hier nicht uebergeben wird, kann auch
        /// nicht versehentlich hoerbar werden.
        /// </summary>
        public static event Action<bool> AnyTensionEvaluated;

        public BridgePuzzleState State { get; private set; } =
            BridgePuzzleState.Dormant;

        public string PuzzleId => puzzleId;
        public int PlanksPlaced => planksPlaced;
        public bool PlayerInZone => playerInZone;

        /// <summary>Meldet die Watch hier ihre Bereitschaft?</summary>
        public bool WatchAvailable =>
            playerInZone && BridgePuzzleRules.AllowsWatch(State);

        public bool CanTurnAnchors =>
            BridgePuzzleRules.AllowsAnchorTurning(State);

        public bool CanRelease => BridgePuzzleRules.AllowsRelease(State);

        public bool CanPlacePlanks =>
            BridgePuzzleRules.AllowsPlanks(State) &&
            planksPlaced < RequiredPlanks;

        public bool IsSolved => State == BridgePuzzleState.Solved;

        /// <summary>Wie viele Bohlen der Übergang braucht.</summary>
        public int RequiredPlanks => Mathf.Max(1, plankVisuals.Length);

        private void Awake()
        {
            if (fallenLog != null)
            {
                stowedPosition = fallenLog.position;
                stowedRotation = fallenLog.rotation;
            }

            foreach (BridgeAnchor anchor in anchors)
            {
                if (anchor != null)
                {
                    anchor.Turned += HandleAnchorTurned;
                }
            }

            Subscribe();

            // Bewusst in Awake und nicht in Start: der Zustand des Rätsels
            // hängt an keiner anderen Komponente, und ein Aufbau, der erst
            // eine Bildwiederholung später gültig wird, wäre weder von Tests
            // noch von einem Spawner sauber zu benutzen.
            RestoreFromSession();
        }

        private void OnDestroy()
        {
            foreach (BridgeAnchor anchor in anchors)
            {
                if (anchor != null)
                {
                    anchor.Turned -= HandleAnchorTurned;
                }
            }

            Unsubscribe();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Ein Schritt der Rätsellogik; öffentlich für Tests.</summary>
        public void Tick(float deltaTime)
        {
            float step = Mathf.Max(0f, deltaTime);

            if (State == BridgePuzzleState.Recovering)
            {
                recoverRemaining -= step;

                if (recoverRemaining <= 0f)
                {
                    recoverRemaining = 0f;
                    TrySetState(BridgePuzzleState.Configuring);
                }

                return;
            }

            if (State == BridgePuzzleState.BridgeDeploying)
            {
                deployProgress += step / Mathf.Max(0.1f, deployDuration);
                ApplyDeployPose(Mathf.Clamp01(deployProgress));

                if (deployProgress >= 1f)
                {
                    deployProgress = 1f;
                    TrySetState(BridgePuzzleState.Securing);
                }
            }
        }

        // ------------------------------------------------------------------
        // Eingänge
        // ------------------------------------------------------------------

        /// <summary>Meldet, dass Aren die Resonanzzone betreten oder verlassen hat.</summary>
        public void SetPlayerInZone(bool inside)
        {
            playerInZone = inside;

            if (inside)
            {
                TrySetState(BridgePuzzleState.WatchAvailable);
                return;
            }

            // Nur aus der reinen Bereitschaft heraus zurückfallen. Was Aren
            // einmal gesehen oder gedreht hat, macht ein Schritt aus der Zone
            // nicht rückgängig.
            if (State == BridgePuzzleState.WatchAvailable)
            {
                TrySetState(BridgePuzzleState.Dormant);
            }
        }

        /// <summary>
        /// Das Echo der alten Brücke wurde gesehen.
        ///
        /// Wer die Erinnerung tatsächlich betrachtet, steht offensichtlich an
        /// der Brücke — auch wenn die Resonanzzone das aus irgendeinem Grund
        /// noch nicht gemeldet hat. Der Zwischenschritt wird deshalb
        /// nachgeholt statt das Rätsel daran scheitern zu lassen. Ein Rätsel,
        /// das sich an der Reihenfolge zweier Auslöser verschluckt, wäre für
        /// den Spieler ununterscheidbar von einem kaputten Rätsel.
        /// </summary>
        public void NotifyEchoObserved()
        {
            if (State == BridgePuzzleState.Dormant)
            {
                TrySetState(BridgePuzzleState.WatchAvailable);
            }

            TrySetState(BridgePuzzleState.EchoObserved);
        }

        /// <summary>
        /// Prüft die Seilspannung. Meldet nur, <em>dass</em> etwas nicht trägt,
        /// nie <em>welcher</em> Anker falsch steht — sonst wäre der Hinweis die
        /// Lösung in Raten.
        /// </summary>
        public bool CheckTension()
        {
            bool holds = IsConfigurationCorrect();

            NarrationEvents.RaiseMessage(
                holds ? tensionHoldsText : tensionSlackText, messageDuration);
            AnyTensionEvaluated?.Invoke(holds);

            return holds;
        }

        /// <summary>
        /// Gibt den Stamm frei. Das ist zugleich der Test: trägt die
        /// Ankerstellung, rollt er in die Aufnahmen; trägt sie nicht,
        /// verkantet er und alles setzt umkehrbar zurück.
        /// </summary>
        public bool TryRelease()
        {
            if (!CanRelease)
            {
                return false;
            }

            if (!IsConfigurationCorrect())
            {
                NarrationEvents.RaiseMessage(jammedText, messageDuration);
                AnyTensionEvaluated?.Invoke(false);
                recoverRemaining = recoverDuration;

                // Aus ReadyToRelease heraus erst wieder in die Konfiguration,
                // damit der Weg in die Sperre immer derselbe ist.
                if (State == BridgePuzzleState.ReadyToRelease)
                {
                    TrySetState(BridgePuzzleState.Configuring);
                }

                TrySetState(BridgePuzzleState.Recovering);

                return false;
            }

            AnyTensionEvaluated?.Invoke(true);

            if (State == BridgePuzzleState.Configuring)
            {
                TrySetState(BridgePuzzleState.ReadyToRelease);
            }

            deployProgress = 0f;
            TrySetState(BridgePuzzleState.BridgeDeploying);

            return true;
        }

        /// <summary>Legt eine Bohle. Gibt zurück, ob sie gelegt wurde.</summary>
        public bool TryPlacePlank()
        {
            if (!CanPlacePlanks)
            {
                return false;
            }

            if (planksPlaced < plankVisuals.Length &&
                plankVisuals[planksPlaced] != null)
            {
                plankVisuals[planksPlaced].SetActive(true);
            }

            planksPlaced++;

            if (planksPlaced >= RequiredPlanks)
            {
                TrySetState(BridgePuzzleState.Solved);
            }

            return true;
        }

        /// <summary>Trägt die aktuelle Ankerstellung?</summary>
        public bool IsConfigurationCorrect()
        {
            return BridgePuzzleRules.IsCorrect(
                NotchesOf(BridgeAnchorId.SouthDeep),
                NotchesOf(BridgeAnchorId.Side),
                NotchesOf(BridgeAnchorId.North));
        }

        /// <summary>Kerbenzahl eines Ankers; 0, wenn er fehlt.</summary>
        public int NotchesOf(BridgeAnchorId id)
        {
            foreach (BridgeAnchor anchor in anchors)
            {
                if (anchor != null && anchor.AnchorId == id)
                {
                    return anchor.Notches;
                }
            }

            return 0;
        }

        // ------------------------------------------------------------------

        private void HandleAnchorTurned(BridgeAnchor anchor)
        {
            // Die erste Drehung öffnet die Konfigurationsphase. Aus
            // ReadyToRelease heraus fällt jede weitere Drehung dorthin
            // zurück — die geprüfte Spannung gilt dann nicht mehr.
            if (State == BridgePuzzleState.EchoObserved ||
                State == BridgePuzzleState.ReadyToRelease)
            {
                TrySetState(BridgePuzzleState.Configuring);
            }
        }

        private bool TrySetState(BridgePuzzleState next)
        {
            if (!BridgePuzzleRules.CanTransition(State, next))
            {
                return false;
            }

            BridgePuzzleState previous = State;
            State = next;

            ApplyStateEffects(next);
            PuzzleSessionState.SetBridgeState(puzzleId, next);
            StateChanged?.Invoke(previous, next);

            return true;
        }

        private void ApplyStateEffects(BridgePuzzleState state)
        {
            switch (state)
            {
                case BridgePuzzleState.Recovering:
                    // Der Stamm rutscht sichtbar in die Ausgangslage zurück.
                    deployProgress = 0f;
                    ApplyDeployPose(0f);
                    break;

                case BridgePuzzleState.Securing:
                    ApplyDeployPose(1f);
                    break;

                case BridgePuzzleState.Solved:
                    ApplyDeployPose(1f);

                    if (walkway != null)
                    {
                        walkway.enabled = true;
                    }

                    NarrationEvents.RaiseMessage(solvedText, messageDuration);
                    break;
            }

            if (walkway != null && state != BridgePuzzleState.Solved)
            {
                // Vor der Sicherung trägt der Stamm nicht.
                walkway.enabled = false;
            }
        }

        private void ApplyDeployPose(float t)
        {
            if (fallenLog == null || deployedPose == null)
            {
                return;
            }

            fallenLog.position =
                Vector3.Lerp(stowedPosition, deployedPose.position, t);
            fallenLog.rotation =
                Quaternion.Slerp(stowedRotation, deployedPose.rotation, t);
        }

        /// <summary>
        /// Stellt den Stand der Sitzung wieder her. Übergangszustände sind
        /// dort bereits auf ihren letzten stabilen Stand zurückgeführt, es
        /// kann also keine Zwischengeometrie entstehen.
        /// </summary>
        private void RestoreFromSession()
        {
            BridgePuzzleState stored =
                PuzzleSessionState.GetBridgeState(puzzleId);

            if (stored == BridgePuzzleState.Dormant)
            {
                ApplyStateEffects(BridgePuzzleState.Dormant);
                return;
            }

            State = stored;

            if (stored == BridgePuzzleState.Solved)
            {
                planksPlaced = RequiredPlanks;

                foreach (GameObject plank in plankVisuals)
                {
                    if (plank != null)
                    {
                        plank.SetActive(true);
                    }
                }
            }

            deployProgress =
                stored == BridgePuzzleState.Securing ||
                stored == BridgePuzzleState.Solved
                    ? 1f
                    : 0f;

            ApplyStateEffects(stored);
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            MemorySite.AnyActivationCompleted += HandleMemorySiteActivated;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            MemorySite.AnyActivationCompleted -= HandleMemorySiteActivated;
            subscribed = false;
        }

        private void HandleMemorySiteActivated(MemorySite site)
        {
            if (site == null || site.SiteId != memorySiteId)
            {
                return;
            }

            NotifyEchoObserved();
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            AnyTensionEvaluated = null;
        }
    }
}
