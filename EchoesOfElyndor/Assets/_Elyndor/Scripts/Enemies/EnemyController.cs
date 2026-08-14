using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>
    /// Verbindet Wahrnehmung, Zustandsmaschine, Bewegung, Angriff und
    /// Lebenspunkte. Der Controller entscheidet nur, welcher Zustand gelten
    /// soll, und taktet die Fachkomponenten — die eigentliche Arbeit bleibt
    /// dort. Er kennt keine Szene und laesst sich zur Laufzeit auf einem
    /// beliebigen GameObject zusammensetzen.
    ///
    /// <see cref="Tick"/> ist oeffentlich, damit Tests und spaetere
    /// Zeitsteuerungen den Gegner ohne feste Framezahl schrittweise laufen
    /// lassen koennen.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyStateMachine))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("Verdacht")]
        [Tooltip("Wie lange das Ziel nur beobachtet wird, bevor verfolgt " +
                 "oder angegriffen wird. 0 reagiert sofort.")]
        [Min(0f)] [SerializeField] private float suspicionDuration;

        [SerializeField] private EnemyStateMachine stateMachine;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private EnemyMovement movement;
        [SerializeField] private EnemyAttack attack;
        [SerializeField] private EnemyHitReaction hitReaction;
        [SerializeField] private EnemyRetreat retreat;
        [SerializeField] private EnemyLeash leash;

        private bool subscribed;
        private float suspicionTimer;

        public EnemyFoundationState State =>
            stateMachine == null
                ? EnemyFoundationState.Idle
                : stateMachine.Current;

        public EnemyStateMachine StateMachine => stateMachine;
        public EnemyHealth Health => health;
        public EnemyPerception Perception => perception;
        public EnemyMovement Movement => movement;
        public EnemyAttack Attack => attack;
        public EnemyHitReaction HitReaction => hitReaction;
        public EnemyRetreat Retreat => retreat;
        public EnemyLeash Leash => leash;

        public float SuspicionDuration => suspicionDuration;

        /// <summary>
        /// Fortschritt der Verdachtsphase von 0 bis 1. Fuer eine spaetere
        /// Anzeige und fuer Tests, die die Phase nicht ueber die Uhr messen
        /// wollen.
        /// </summary>
        public float SuspicionProgress01 =>
            suspicionDuration <= 0f
                ? 1f
                : Mathf.Clamp01(suspicionTimer / suspicionDuration);

        /// <summary>Setzt die Verdachtsdauer, etwa aus einem Gegnerprofil.</summary>
        public void ConfigureSuspicion(float newSuspicionDuration)
        {
            suspicionDuration = Mathf.Max(0f, newSuspicionDuration);
        }

        private void Awake()
        {
            EnsureSubscribed();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Bindet den Tod an. Bewusst in Awake und zusaetzlich beim Takten:
        /// der Tod muss auch dann greifen, wenn der Controller abgeschaltet
        /// ist und von aussen getaktet wird.
        /// </summary>
        private void EnsureSubscribed()
        {
            if (subscribed)
            {
                return;
            }

            ResolveReferences();

            if (health == null)
            {
                return;
            }

            health.Died += HandleDied;
            subscribed = true;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Ein Schritt der Gegnerlogik.</summary>
        public void Tick(float deltaTime)
        {
            EnsureSubscribed();

            if (stateMachine == null)
            {
                return;
            }

            if (stateMachine.IsDead)
            {
                return;
            }

            // Erst die Trefferreaktion: sie darf den Zustand freigeben,
            // bevor neu entschieden wird.
            hitReaction?.Tick(deltaTime);
            perception?.Tick(deltaTime);
            retreat?.Tick(deltaTime);
            leash?.Tick(deltaTime);

            bool recovering = hitReaction != null && hitReaction.IsRecovering;

            // Ausserhalb seines Begegnungsbereichs kehrt der Gegner zurueck
            // und nimmt bis dahin kein Ziel wahr. Ohne dieses Vergessen
            // wuerde er an der Grenze zwischen Rueckkehr und Verfolgung hin
            // und her kippen.
            if (leash != null && leash.IsReturning && !recovering)
            {
                perception?.ResetPerception();
                stateMachine.TrySetState(EnemyFoundationState.Idle);
                movement?.MoveTowardsPosition(leash.Home, deltaTime);
                attack?.Tick(stateMachine.Current, null, deltaTime);

                return;
            }

            if (!recovering)
            {
                UpdateDesiredState(deltaTime);
            }

            Transform target = perception == null ? null : perception.Target;
            bool targetKnown = perception != null && perception.HasTarget;

            // Waehrend Telegraph und Erholung steht der Gegner fest. Die
            // Bewegung muss das wissen, sonst weicht er waehrend seines
            // eigenen Ansetzens noch aus.
            bool committed = attack != null && attack.IsCommitted;

            movement?.Tick(
                stateMachine.Current,
                targetKnown ? target : null,
                deltaTime,
                committed);
            attack?.Tick(
                stateMachine.Current, targetKnown ? target : null, deltaTime);
        }

        private void UpdateDesiredState(float deltaTime)
        {
            // Waehrend Telegraph und Erholung steht der Zustand fest. Ohne
            // das wuerde ein ausweichender Spieler den Angriff schlicht
            // abbrechen: der Gegner faende sich ausserhalb der Reichweite,
            // wechselte nach Chase, und der begonnene Sprungbiss verschwaende
            // spurlos. Der Angriff soll stattdessen ins Leere gehen und den
            // Gegner offen zuruecklassen — das ist der Sinn des Ausweichens.
            //
            // Hurt und Dead werden nicht hier gesetzt, sondern direkt von der
            // Trefferreaktion und vom Tod; beide unterbrechen weiterhin.
            if (attack != null && attack.IsCommitted)
            {
                return;
            }

            // Der Rueckzug hat Vorrang vor der Wahrnehmung: er haengt an den
            // Lebenspunkten und nicht daran, ob das Ziel gerade zu sehen ist.
            if (retreat != null && retreat.IsRetreating)
            {
                stateMachine.TrySetState(EnemyFoundationState.Retreat);
                return;
            }

            // Nach dem Rueckzug wird erst wieder aufgeschlossen. Ein direkter
            // Weg zurueck in den Angriff waere ohnehin gesperrt; ohne diesen
            // Schritt bliebe der Gegner in Reichweite in Retreat stecken.
            if (stateMachine.Current == EnemyFoundationState.Retreat)
            {
                stateMachine.TrySetState(EnemyFoundationState.Chase);
                return;
            }

            if (perception == null || !perception.HasTarget)
            {
                suspicionTimer = 0f;
                stateMachine.TrySetState(EnemyFoundationState.Idle);
                return;
            }

            // Aus der Ruhe heraus wird das Ziel erst bemerkt.
            if (stateMachine.Current == EnemyFoundationState.Idle)
            {
                suspicionTimer = 0f;
                stateMachine.TrySetState(EnemyFoundationState.Alert);
                return;
            }

            // Verdachtsphase: erst beobachten, dann handeln. Sie laeuft nur in
            // Alert — wer einmal verfolgt oder getroffen wurde, schoepft nicht
            // erneut Verdacht, sondern reagiert sofort.
            if (stateMachine.Current == EnemyFoundationState.Alert &&
                suspicionDuration > 0f)
            {
                suspicionTimer += Mathf.Max(0f, deltaTime);

                if (suspicionTimer < suspicionDuration)
                {
                    return;
                }
            }

            bool inAttackRange =
                attack != null &&
                perception.DistanceToTarget <= attack.AttackRange;

            stateMachine.TrySetState(
                inAttackRange
                    ? EnemyFoundationState.Attack
                    : EnemyFoundationState.Chase);
        }

        private void HandleDied()
        {
            stateMachine?.TrySetState(EnemyFoundationState.Dead);
        }

        private void Unsubscribe()
        {
            if (!subscribed || health == null)
            {
                return;
            }

            health.Died -= HandleDied;
            subscribed = false;
        }

        private void ResolveReferences()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<EnemyStateMachine>();
            }

            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            if (perception == null)
            {
                perception = GetComponent<EnemyPerception>();
            }

            if (movement == null)
            {
                movement = GetComponent<EnemyMovement>();
            }

            if (attack == null)
            {
                attack = GetComponent<EnemyAttack>();
            }

            if (hitReaction == null)
            {
                hitReaction = GetComponent<EnemyHitReaction>();
            }

            if (retreat == null)
            {
                retreat = GetComponent<EnemyRetreat>();
            }

            if (leash == null)
            {
                leash = GetComponent<EnemyLeash>();
            }
        }
    }
}
