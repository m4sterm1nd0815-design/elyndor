using Elyndor.Combat;
using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>
    /// Uebersetzt den Zustand eines Gegners in Animationszustaende.
    ///
    /// Bewusst ueber <see cref="Animator.CrossFadeInFixedTime(string,float)"/>
    /// statt ueber Parameter und Uebergangsbedingungen im Controller: die
    /// Wahrheit ueber den Zustand liegt bereits in der Zustandsmaschine. Sie
    /// ein zweites Mal als Bedingungsnetz im Animator nachzubauen hiesse, zwei
    /// Zustandsmaschinen synchron halten zu muessen — und die zweite waere nur
    /// im Editorfenster nachlesbar, nicht im Code und nicht im Test.
    ///
    /// Der Controller enthaelt deshalb nur Zustaende mit Clips und keine
    /// Uebergaenge.
    ///
    /// Fehlt der Animator oder ein Zustand, bleibt die Komponente still: ein
    /// Blockout ohne fertige Animation soll spielbar bleiben.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyAnimationDriver : MonoBehaviour
    {
        /// <summary>Zustandsnamen im Animator-Controller.</summary>
        public const string StateIdle = "Idle";
        public const string StateListen = "Lauschen";
        public const string StateWalk = "Schritt";
        public const string StateRun = "Lauf";
        public const string StateTelegraph = "Telegraph";
        public const string StateStrike = "Sprungbiss";
        public const string StateFlinch = "Flinch";
        public const string StateStagger = "Stagger";
        public const string StateFlee = "Flucht";
        public const string StateDefeat = "Niederlage";

        [SerializeField] private Animator animator;
        [SerializeField] private EnemyController controller;

        [Tooltip("Ueberblendzeit zwischen zwei Zustaenden.")]
        [Min(0f)] [SerializeField] private float crossFade = 0.12f;

        [Tooltip("Wie lange der Sprungbiss die Auswahl haelt, damit er nicht " +
                 "sofort von der Erholung ueberblendet wird.")]
        [Min(0f)] [SerializeField] private float strikeHold = 0.35f;

        private string currentState;
        private float strikeRemaining;
        private bool lastHitWasHeavy;
        private bool subscribed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Ein Schritt der Animationsauswahl; oeffentlich fuer Tests.</summary>
        public void Tick(float deltaTime)
        {
            ResolveReferences();
            Subscribe();

            if (strikeRemaining > 0f)
            {
                strikeRemaining -= Mathf.Max(0f, deltaTime);
                return;
            }

            Play(SelectState());
        }

        /// <summary>Welcher Zustand passt gerade?</summary>
        public string SelectState()
        {
            if (controller == null)
            {
                return StateIdle;
            }

            switch (controller.State)
            {
                case EnemyFoundationState.Dead:
                    return StateDefeat;

                case EnemyFoundationState.Hurt:
                    return lastHitWasHeavy ? StateStagger : StateFlinch;

                case EnemyFoundationState.Retreat:
                    return StateFlee;

                case EnemyFoundationState.Chase:
                    return StateRun;

                case EnemyFoundationState.Alert:
                    return StateListen;

                case EnemyFoundationState.Attack:
                    if (controller.Attack != null &&
                        controller.Attack.Phase == EnemyAttackPhase.Telegraph)
                    {
                        return StateTelegraph;
                    }

                    // In der Erholung steht der Gegner; dazwischen umkreist er.
                    bool strafing =
                        controller.Movement != null &&
                        controller.Movement.IsStrafing;

                    return strafing ? StateWalk : StateIdle;

                default:
                    return StateIdle;
            }
        }

        private void Play(string state)
        {
            if (animator == null || state == currentState)
            {
                return;
            }

            if (!animator.HasState(0, Animator.StringToHash(state)))
            {
                return;
            }

            animator.CrossFadeInFixedTime(state, crossFade);
            currentState = state;
        }

        private void Subscribe()
        {
            if (subscribed || controller == null)
            {
                return;
            }

            if (controller.Attack != null)
            {
                controller.Attack.AttackPerformed += HandleAttackPerformed;
            }

            if (controller.Health != null)
            {
                controller.Health.Damaged += HandleDamaged;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || controller == null)
            {
                return;
            }

            if (controller.Attack != null)
            {
                controller.Attack.AttackPerformed -= HandleAttackPerformed;
            }

            if (controller.Health != null)
            {
                controller.Health.Damaged -= HandleDamaged;
            }

            subscribed = false;
        }

        private void HandleAttackPerformed(EnemyAttackInfo info)
        {
            // Der Biss ist ein Moment, kein Zustand — er muss kurz gehalten
            // werden, sonst wird er sofort von der Erholung ueberblendet und
            // ist nie zu sehen.
            currentState = null;
            Play(StateStrike);
            strikeRemaining = strikeHold;
        }

        private void HandleDamaged(EnemyDamageInfo info)
        {
            lastHitWasHeavy = info.AttackType == AttackType.Heavy;

            // Ein Treffer bricht den gehaltenen Biss ab.
            strikeRemaining = 0f;
        }

        private void ResolveReferences()
        {
            if (controller == null)
            {
                controller = GetComponent<EnemyController>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
    }
}
