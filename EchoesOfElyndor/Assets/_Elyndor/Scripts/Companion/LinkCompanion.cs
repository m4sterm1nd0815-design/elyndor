using System;
using Elyndor.UIFoundation;
using UnityEngine;

namespace Elyndor.Companion
{
    /// <summary>Was Link gerade tut.</summary>
    public enum LinkState
    {
        /// <summary>Sitzt und beobachtet.</summary>
        Perched = 0,

        /// <summary>Kurzer Flug zum naechsten Sitzpunkt.</summary>
        Flying = 1,

        /// <summary>Kein Sitzpunkt erreichbar; haelt sich in Arens Naehe.</summary>
        Fallback = 2
    }

    /// <summary>
    /// Link, die Eule. Arens Begleiter im Finsterwald.
    ///
    /// Link spricht nicht, zeigt nicht und führt nicht. Er sitzt, sieht hin,
    /// wechselt gelegentlich den Platz und ruft manchmal. Mehr ist es nicht —
    /// und mehr soll es nicht sein: ein Begleiter, der den Blick lenkt, ist
    /// Atmosphäre; einer, der die Lösung zeigt, ersetzt das Rätsel.
    ///
    /// <b>Die entscheidende Eigenschaft dieser Klasse ist, was sie nicht
    /// kennt.</b> Sie hat keinen Verweis auf Rätsel, Anker oder deren Zustand.
    /// Seine Platzwahl kann deshalb gar nicht von der Lösung abhängen — nicht
    /// weil eine Regel es verbietet, sondern weil die Information hier nicht
    /// vorliegt. Ein Test hält fest, dass dieselbe Lage dieselbe Wahl ergibt,
    /// gleich wie die Anker stehen.
    ///
    /// Link hat keinen Collider. Er kann Aren nicht im Weg stehen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LinkCompanion : MonoBehaviour
    {
        [Header("Sitzpunkte")]
        [Tooltip("Leer lassen: dann werden die Sitzpunkte der Szene gesucht.")]
        [SerializeField] private LinkPerch[] perches = Array.Empty<LinkPerch>();

        [Tooltip("Leer lassen: dann wird Aren an seinen PlayerVitals gesucht.")]
        [SerializeField] private Transform player;

        [Header("Verhalten")]
        [Tooltip("Wie lange Link mindestens sitzen bleibt.")]
        [Min(0.5f)] [SerializeField] private float minPerchTime = 6f;

        [Tooltip("Ab diesem Abstand zu Aren sucht Link einen naeheren Platz.")]
        [Min(1f)] [SerializeField] private float followDistance = 18f;

        [Tooltip("Dauer eines Ortswechsels.")]
        [Min(0.2f)] [SerializeField] private float flightDuration = 1.4f;

        [Tooltip("Scheitelhoehe des Fluges ueber der geraden Verbindung.")]
        [Min(0f)] [SerializeField] private float flightArcHeight = 1.6f;

        [Tooltip("Wie schnell Link den Kopf dreht.")]
        [Min(10f)] [SerializeField] private float turnSpeedDegrees = 160f;

        [Tooltip("Bis zu diesem Abstand sieht Link Aren an; darueber hinaus " +
                 "schaut er in die Ruherichtung seines Sitzpunkts.")]
        [Min(1f)] [SerializeField] private float lookAtPlayerRange = 14f;

        [Header("Ruf")]
        [Tooltip("Kuerzester Abstand zwischen zwei Rufen. 0 schaltet ihn ab.")]
        [Min(0f)] [SerializeField] private float callInterval = 22f;

        [Header("Rueckfall")]
        [Tooltip("Versatz zu Aren, wenn kein Sitzpunkt taugt.")]
        [SerializeField] private Vector3 fallbackOffset = new Vector3(1.6f, 3.2f, -1.2f);

        private LinkPerch currentPerch;
        private LinkPerch targetPerch;
        private Vector3 flightFrom;
        private float flightProgress;
        private float perchTimer;
        private float callTimer;
        private float playerSearchCooldown;

        /// <summary>Wird bei jedem Ruf ausgeloest. Ohne Untertitel — Link spricht nicht.</summary>
        public static event Action AnyCalled;

        public LinkState State { get; private set; } = LinkState.Perched;
        public LinkPerch CurrentPerch => currentPerch;
        public int PerchChanges { get; private set; }
        public int Calls { get; private set; }

        private void Awake()
        {
            ResolvePerches();
            SettleOnNearestPerch();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Ein Schritt; oeffentlich, damit Tests ohne Bildrate takten koennen.</summary>
        public void Tick(float deltaTime)
        {
            float step = Mathf.Max(0f, deltaTime);

            ResolvePlayer(step);
            TickCall(step);

            switch (State)
            {
                case LinkState.Flying:
                    TickFlight(step);
                    break;

                default:
                    TickPerched(step);
                    break;
            }

            TickLook(step);
        }

        // ------------------------------------------------------------------

        private void TickPerched(float step)
        {
            perchTimer += step;

            if (currentPerch != null)
            {
                transform.position = currentPerch.transform.position;
                State = LinkState.Perched;
            }
            else if (player != null)
            {
                // Kein Sitzpunkt: in Arens Naehe halten, aber nie im Weg.
                transform.position = player.position + fallbackOffset;
                State = LinkState.Fallback;
            }

            if (perchTimer < minPerchTime)
            {
                return;
            }

            LinkPerch next = ChoosePerch();

            if (next == null || next == currentPerch)
            {
                return;
            }

            BeginFlight(next);
        }

        private void BeginFlight(LinkPerch next)
        {
            targetPerch = next;
            flightFrom = transform.position;
            flightProgress = 0f;
            perchTimer = 0f;
            State = LinkState.Flying;
        }

        private void TickFlight(float step)
        {
            if (targetPerch == null)
            {
                State = LinkState.Perched;
                return;
            }

            flightProgress += step / Mathf.Max(0.2f, flightDuration);
            float t = Mathf.Clamp01(flightProgress);

            Vector3 straight =
                Vector3.Lerp(flightFrom, targetPerch.transform.position, t);

            // Ein flacher Bogen statt einer Geraden: eine Eule gleitet, sie
            // fuehrt keine Schiene entlang.
            straight.y += Mathf.Sin(t * Mathf.PI) * flightArcHeight;
            transform.position = straight;

            if (t < 1f)
            {
                return;
            }

            currentPerch = targetPerch;
            targetPerch = null;
            perchTimer = 0f;
            PerchChanges++;
            State = LinkState.Perched;
        }

        /// <summary>
        /// Waehlt den naechsten Sitzpunkt — ausschliesslich nach Abstand zu
        /// Aren. Es gibt hier bewusst keinen Begriff von „richtig" oder
        /// „interessant fuer das Raetsel".
        /// </summary>
        public LinkPerch ChoosePerch()
        {
            if (perches == null || perches.Length == 0 || player == null)
            {
                return null;
            }

            LinkPerch best = null;
            float bestDistance = float.PositiveInfinity;

            foreach (LinkPerch perch in perches)
            {
                if (perch == null || perch == currentPerch)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    perch.transform.position, player.position);

                // Zu nah waere aufdringlich, zu fern waere kein Begleiter mehr.
                if (distance < perch.MinPlayerDistance ||
                    distance > followDistance)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = perch;
                }
            }

            return best;
        }

        private void TickLook(float step)
        {
            Vector3 direction;

            bool watchingPlayer =
                player != null &&
                Vector3.Distance(transform.position, player.position) <=
                lookAtPlayerRange;

            if (watchingPlayer)
            {
                direction = player.position - transform.position;
            }
            else if (currentPerch != null)
            {
                direction = currentPerch.RestingLookDirection;
            }
            else
            {
                return;
            }

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction.normalized, Vector3.up),
                turnSpeedDegrees * step);
        }

        private void TickCall(float step)
        {
            if (callInterval <= 0f)
            {
                return;
            }

            callTimer += step;

            if (callTimer < callInterval)
            {
                return;
            }

            callTimer = 0f;

            // An ruhigen Plaetzen schweigt Link.
            if (currentPerch != null && currentPerch.Quiet)
            {
                return;
            }

            Calls++;
            AnyCalled?.Invoke();
        }

        // ------------------------------------------------------------------

        /// <summary>Setzt die Sitzpunkte, etwa aus einem Aufbauwerkzeug.</summary>
        public void SetPerches(LinkPerch[] newPerches)
        {
            perches = newPerches ?? Array.Empty<LinkPerch>();
            SettleOnNearestPerch();
        }

        private void ResolvePerches()
        {
            if (perches != null && perches.Length > 0)
            {
                return;
            }

            perches = FindObjectsByType<LinkPerch>(FindObjectsInactive.Exclude);
        }

        private void ResolvePlayer(float step)
        {
            if (player != null)
            {
                return;
            }

            playerSearchCooldown -= step;

            if (playerSearchCooldown > 0f)
            {
                return;
            }

            playerSearchCooldown = 0.5f;

            PlayerVitals vitals = FindAnyObjectByType<PlayerVitals>();

            if (vitals != null)
            {
                player = vitals.transform;
            }
        }

        private void SettleOnNearestPerch()
        {
            if (perches == null || perches.Length == 0)
            {
                return;
            }

            foreach (LinkPerch perch in perches)
            {
                if (perch != null)
                {
                    currentPerch = perch;
                    transform.position = perch.transform.position;
                    break;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            AnyCalled = null;
        }
    }
}
