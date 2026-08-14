using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>
    /// Bewegung der Gegnergrundlage. Bewusst ohne NavMesh: im Projekt gibt es
    /// noch keine gebackene Navigation, und dieses Paket soll ohne
    /// Szenenaenderung testbar bleiben. Liegt ein
    /// <see cref="CharacterController"/> vor, wird ueber ihn bewegt,
    /// sonst direkt ueber den Transform.
    ///
    /// In Hurt und Dead findet keine Ortsveraenderung statt, und gedreht wird
    /// dort ebenfalls nicht. In Attack wird nur seitlich ausgewichen, und auch
    /// das nur, solange kein Angriff laeuft — ein Gegner, der sich waehrend
    /// seines eigenen Ansetzens noch loesen kann, macht den Konter wertlos.
    /// In Retreat wird die Richtung umgekehrt.
    ///
    /// Seitwaertsschritt und Rueckzugstempo sind mit 0 vorbelegt: ein Gegner
    /// ohne eigene Werte verhaelt sich unveraendert.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyMovement : MonoBehaviour
    {
        [Header("Bewegung")]
        [Min(0f)] [SerializeField] private float moveSpeed = 2.6f;

        [Min(0f)]
        [Tooltip("Abstand, ab dem vor dem Ziel angehalten wird.")]
        [SerializeField] private float stopDistance = 1.6f;

        [Min(0f)] [SerializeField] private float turnSpeedDegrees = 540f;

        [Header("Seitlicher Schritt")]
        [Tooltip("Tempo des Umkreisens in Angriffsreichweite. 0 schaltet es ab.")]
        [Min(0f)] [SerializeField] private float strafeSpeed;

        [Tooltip("Sekunden bis zum Richtungswechsel des Umkreisens.")]
        [Min(0.05f)] [SerializeField] private float strafeInterval = 1.6f;

        [Header("Rueckzug")]
        [Tooltip("Tempo beim Loesen vom Ziel. 0 nutzt das normale Tempo.")]
        [Min(0f)] [SerializeField] private float retreatSpeed;

        [Header("Schwerkraft")]
        [Tooltip("Wirkt nur, wenn ein CharacterController vorhanden ist.")]
        [SerializeField] private float gravity = -18f;

        private CharacterController characterController;
        private float verticalVelocity;
        private float strafeTimer;
        private float strafeSign = 1f;

        /// <summary>Hat sich der Gegner im letzten Tick bewegt?</summary>
        public bool IsMoving { get; private set; }

        /// <summary>Weicht der Gegner in diesem Tick seitlich aus?</summary>
        public bool IsStrafing { get; private set; }

        public float StopDistance => stopDistance;
        public float StrafeSpeed => strafeSpeed;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        /// <summary>Setzt Geschwindigkeit und Halteabstand.</summary>
        public void Configure(float newMoveSpeed, float newStopDistance)
        {
            moveSpeed = Mathf.Max(0f, newMoveSpeed);
            stopDistance = Mathf.Max(0f, newStopDistance);
        }

        /// <summary>Setzt zusaetzlich Seitwaertsschritt und Rueckzugstempo.</summary>
        public void Configure(
            float newMoveSpeed,
            float newStopDistance,
            float newStrafeSpeed,
            float newStrafeInterval,
            float newRetreatSpeed)
        {
            Configure(newMoveSpeed, newStopDistance);

            strafeSpeed = Mathf.Max(0f, newStrafeSpeed);
            strafeInterval = Mathf.Max(0.05f, newStrafeInterval);
            retreatSpeed = Mathf.Max(0f, newRetreatSpeed);
        }

        /// <summary>
        /// Bewegt den Gegner. Wird vom <see cref="EnemyController"/> getaktet.
        /// <paramref name="committed"/> meldet einen laufenden Angriff: dann
        /// wird die Position nicht mehr veraendert.
        /// </summary>
        public void Tick(
            EnemyFoundationState state,
            Transform target,
            float deltaTime,
            bool committed = false)
        {
            IsMoving = false;
            IsStrafing = false;

            bool mayTurn =
                state != EnemyFoundationState.Hurt &&
                state != EnemyFoundationState.Dead;

            if (target == null || deltaTime <= 0f)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            // Ein laufender Angriff bindet den Gegner an Ort und Blickrichtung.
            // Wuerde er waehrend des Ansetzens noch nachdrehen oder
            // nachruecken, waere der Telegraph keine Zusage mehr, sondern nur
            // eine Verzoegerung — und Ausweichen bliebe wirkungslos.
            if (committed)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance <= 0.0001f)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            Vector3 toTargetDirection = toTarget / distance;

            if (state == EnemyFoundationState.Retreat)
            {
                MoveAwayFromTarget(toTargetDirection, deltaTime);
                return;
            }

            if (mayTurn)
            {
                RotateTowards(toTargetDirection, deltaTime);
            }

            if (state == EnemyFoundationState.Attack)
            {
                TickStrafe(toTargetDirection, deltaTime, committed);
                return;
            }

            // Angriff und Rueckzug sind oben abgehandelt; hier bleiben Idle,
            // Alert, Chase und Hurt. In Hurt wird nicht gelaufen.
            if (!mayTurn || distance <= stopDistance)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            Vector3 step = toTargetDirection * moveSpeed * deltaTime;

            // Nicht ueber den Halteabstand hinausschiessen.
            float maxStep = distance - stopDistance;

            if (step.magnitude > maxStep)
            {
                step = toTargetDirection * maxStep;
            }

            IsMoving = step.sqrMagnitude > 0f;
            Move(step, deltaTime);
        }

        /// <summary>
        /// Umkreist das Ziel in Angriffsreichweite und wechselt dabei
        /// regelmaessig die Seite. Das ist der lesbare Teil des Musters:
        /// Beobachten, seitlich versetzen, dann erst ansetzen.
        /// </summary>
        private void TickStrafe(
            Vector3 toTargetDirection, float deltaTime, bool committed)
        {
            if (committed || strafeSpeed <= 0f)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            strafeTimer += deltaTime;

            if (strafeTimer >= strafeInterval)
            {
                strafeTimer = 0f;
                strafeSign = -strafeSign;
            }

            Vector3 side =
                Vector3.Cross(Vector3.up, toTargetDirection) * strafeSign;

            Vector3 step = side * strafeSpeed * deltaTime;

            IsMoving = step.sqrMagnitude > 0f;
            IsStrafing = IsMoving;

            Move(step, deltaTime);
        }

        /// <summary>
        /// Loest sich vom Ziel. Gedreht wird in die Fluchtrichtung: ein Tier,
        /// das rueckwaerts vom Spieler weggeht, laese sich als Angriff
        /// missverstehen.
        /// </summary>
        private void MoveAwayFromTarget(
            Vector3 toTargetDirection, float deltaTime)
        {
            Vector3 away = -toTargetDirection;

            RotateTowards(away, deltaTime);

            float speed = retreatSpeed > 0f ? retreatSpeed : moveSpeed;

            if (speed <= 0f)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            Vector3 step = away * speed * deltaTime;

            IsMoving = step.sqrMagnitude > 0f;
            Move(step, deltaTime);
        }

        /// <summary>
        /// Laeuft auf einen festen Punkt zu statt auf ein Ziel. Fuer die
        /// Rueckkehr in den Begegnungsbereich: dort gibt es kein Ziel mehr,
        /// dem gefolgt werden koennte, nur noch einen Ort.
        /// </summary>
        public void MoveTowardsPosition(Vector3 position, float deltaTime)
        {
            IsMoving = false;
            IsStrafing = false;

            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 toHome = position - transform.position;
            toHome.y = 0f;
            float distance = toHome.magnitude;

            if (distance <= 0.15f)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            Vector3 direction = toHome / distance;

            RotateTowards(direction, deltaTime);

            Vector3 step = direction * moveSpeed * deltaTime;

            if (step.magnitude > distance)
            {
                step = toHome;
            }

            IsMoving = step.sqrMagnitude > 0f;
            Move(step, deltaTime);
        }

        private void RotateTowards(Vector3 direction, float deltaTime)
        {
            Quaternion desired = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, desired, turnSpeedDegrees * deltaTime);
        }

        private void ApplyGravityOnly(float deltaTime)
        {
            if (characterController == null || deltaTime <= 0f)
            {
                return;
            }

            Move(Vector3.zero, deltaTime);
        }

        private void Move(Vector3 horizontalStep, float deltaTime)
        {
            if (characterController == null)
            {
                transform.position += horizontalStep;
                return;
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }
            else
            {
                verticalVelocity += gravity * deltaTime;
            }

            characterController.Move(
                horizontalStep + Vector3.up * verticalVelocity * deltaTime);
        }
    }
}
