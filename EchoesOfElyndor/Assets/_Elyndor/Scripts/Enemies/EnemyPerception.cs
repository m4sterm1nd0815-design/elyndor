using Elyndor.UIFoundation;
using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>
    /// Zielerkennung der Gegnergrundlage: Radius, optionaler Sichtwinkel und
    /// Sichtlinie. Ein einmal erkanntes Ziel gilt noch eine konfigurierbare
    /// Zeit als bekannt, damit der Gegner nicht bei jedem Baum abreisst.
    ///
    /// Die Pruefung arbeitet ohne Allokationen je Frame: die Sichtlinie nutzt
    /// einen vorab angelegten Trefferpuffer, und der Spieler wird nur gesucht,
    /// solange kein Ziel bekannt ist — und auch dann nur im eingestellten
    /// Intervall.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyPerception : MonoBehaviour
    {
        private const int LineOfSightBufferSize = 12;

        [Header("Ziel")]
        [Tooltip("Leer lassen: dann wird der Spieler anhand seiner PlayerVitals gesucht.")]
        [SerializeField] private Transform target;

        [Min(0f)]
        [Tooltip("Sekunden zwischen zwei Suchversuchen, solange kein Ziel bekannt ist.")]
        [SerializeField] private float targetSearchInterval = 0.5f;

        [Header("Wahrnehmung")]
        [Min(0f)] [SerializeField] private float detectionRadius = 12f;
        [SerializeField] private bool useViewAngle = true;
        [Range(1f, 360f)] [SerializeField] private float viewAngle = 150f;

        [Min(0f)]
        [Tooltip("Geraeuschradius. Innerhalb davon wird das Ziel ohne " +
                 "Sichtwinkel und ohne Sichtlinie bemerkt — man hoert um die " +
                 "Ecke. 0 schaltet das Gehoer ab.")]
        [SerializeField] private float hearingRadius;

        [Min(0f)]
        [Tooltip("So lange bleibt ein verlorenes Ziel noch bekannt.")]
        [SerializeField] private float loseTargetDelay = 2.5f;

        [Header("Sichtlinie")]
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private float eyeHeight = 1.5f;
        [SerializeField] private float targetHeight = 1f;
        [SerializeField] private LayerMask lineOfSightBlockers = ~0;

        private readonly RaycastHit[] lineOfSightHits =
            new RaycastHit[LineOfSightBufferSize];

        private float lostTimer;
        private float searchCooldown;

        /// <summary>Das aktuell verwendete Ziel; kann null sein.</summary>
        public Transform Target => target;

        /// <summary>Ist das Ziel in diesem Tick tatsaechlich zu sehen?</summary>
        public bool IsTargetVisible { get; private set; }

        /// <summary>
        /// Ist das Ziel in diesem Tick zu hoeren? Unabhaengig von Sichtwinkel
        /// und Sichtlinie — sonst waere jeder Busch ein Versteck.
        /// </summary>
        public bool IsTargetHeard { get; private set; }

        /// <summary>
        /// Gilt das Ziel als bekannt? Bleibt nach dem Sichtverlust noch
        /// <c>loseTargetDelay</c> Sekunden wahr.
        /// </summary>
        public bool HasTarget { get; private set; }

        /// <summary>Abstand zum Ziel in der Ebene; ohne Ziel unendlich.</summary>
        public float DistanceToTarget { get; private set; } = float.PositiveInfinity;

        public float DetectionRadius => detectionRadius;
        public float HearingRadius => hearingRadius;
        public float LoseTargetDelay => loseTargetDelay;

        /// <summary>Setzt die Wahrnehmungswerte, etwa aus einem Gegnerprofil.</summary>
        public void Configure(
            float newDetectionRadius,
            float newViewAngle,
            float newHearingRadius,
            float newLoseTargetDelay)
        {
            detectionRadius = Mathf.Max(0f, newDetectionRadius);
            viewAngle = Mathf.Clamp(newViewAngle, 1f, 360f);
            hearingRadius = Mathf.Max(0f, newHearingRadius);
            loseTargetDelay = Mathf.Max(0f, newLoseTargetDelay);
        }

        /// <summary>Setzt das Ziel ausdruecklich, etwa aus einem Spawner oder Test.</summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            ResetPerception();
        }

        /// <summary>Vergisst den aktuellen Wahrnehmungszustand, behaelt aber das Ziel.</summary>
        public void ResetPerception()
        {
            IsTargetVisible = false;
            IsTargetHeard = false;
            HasTarget = false;
            lostTimer = 0f;
            DistanceToTarget = float.PositiveInfinity;
        }

        /// <summary>
        /// Aktualisiert die Wahrnehmung. Wird vom <see cref="EnemyController"/>
        /// getaktet, damit Tests ohne feste Framezahl arbeiten koennen.
        /// </summary>
        public void Tick(float deltaTime)
        {
            ResolveTarget(deltaTime);

            if (target == null)
            {
                ResetPerception();
                return;
            }

            DistanceToTarget = PlanarDistanceTo(target.position);
            IsTargetVisible = EvaluateVisibility();
            IsTargetHeard =
                hearingRadius > 0f && DistanceToTarget <= hearingRadius;

            if (IsTargetVisible || IsTargetHeard)
            {
                HasTarget = true;
                lostTimer = 0f;
                return;
            }

            if (!HasTarget)
            {
                return;
            }

            lostTimer += Mathf.Max(0f, deltaTime);

            if (lostTimer >= loseTargetDelay)
            {
                HasTarget = false;
                lostTimer = 0f;
            }
        }

        private void ResolveTarget(float deltaTime)
        {
            if (target != null)
            {
                return;
            }

            // Ohne bekanntes Ziel nur im Intervall suchen — FindAnyObjectByType
            // ist zu teuer fuer jeden Frame.
            searchCooldown -= Mathf.Max(0f, deltaTime);

            if (searchCooldown > 0f)
            {
                return;
            }

            searchCooldown = Mathf.Max(0.05f, targetSearchInterval);

            PlayerVitals player = FindAnyObjectByType<PlayerVitals>();

            if (player != null)
            {
                target = player.transform;
            }
        }

        private bool EvaluateVisibility()
        {
            if (DistanceToTarget > detectionRadius)
            {
                return false;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (useViewAngle && toTarget.sqrMagnitude > 0.0001f)
            {
                Vector3 forward = transform.forward;
                forward.y = 0f;

                if (forward.sqrMagnitude > 0.0001f &&
                    Vector3.Angle(forward.normalized, toTarget.normalized) >
                    viewAngle * 0.5f)
                {
                    return false;
                }
            }

            return !requireLineOfSight || HasLineOfSight();
        }

        private bool HasLineOfSight()
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 aim = target.position + Vector3.up * targetHeight;
            Vector3 direction = aim - eye;
            float distance = direction.magnitude;

            if (distance <= 0.0001f)
            {
                return true;
            }

            direction /= distance;

            int hitCount = Physics.RaycastNonAlloc(
                eye,
                direction,
                lineOfSightHits,
                distance,
                lineOfSightBlockers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Transform hit = lineOfSightHits[i].transform;

                if (hit == null)
                {
                    continue;
                }

                // Eigene Kollider und das Ziel selbst blockieren nicht.
                if (hit == transform || hit.IsChildOf(transform) ||
                    hit == target || hit.IsChildOf(target))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private float PlanarDistanceTo(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            offset.y = 0f;

            return offset.magnitude;
        }
    }
}
