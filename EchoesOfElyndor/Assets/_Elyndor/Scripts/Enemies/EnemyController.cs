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
        [SerializeField] private EnemyStateMachine stateMachine;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private EnemyMovement movement;
        [SerializeField] private EnemyAttack attack;
        [SerializeField] private EnemyHitReaction hitReaction;

        private bool subscribed;

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

            bool recovering = hitReaction != null && hitReaction.IsRecovering;

            if (!recovering)
            {
                UpdateDesiredState();
            }

            Transform target = perception == null ? null : perception.Target;
            bool targetKnown = perception != null && perception.HasTarget;

            movement?.Tick(
                stateMachine.Current, targetKnown ? target : null, deltaTime);
            attack?.Tick(
                stateMachine.Current, targetKnown ? target : null, deltaTime);
        }

        private void UpdateDesiredState()
        {
            if (perception == null || !perception.HasTarget)
            {
                stateMachine.TrySetState(EnemyFoundationState.Idle);
                return;
            }

            // Aus der Ruhe heraus wird das Ziel erst bemerkt.
            if (stateMachine.Current == EnemyFoundationState.Idle)
            {
                stateMachine.TrySetState(EnemyFoundationState.Alert);
                return;
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
        }
    }
}
