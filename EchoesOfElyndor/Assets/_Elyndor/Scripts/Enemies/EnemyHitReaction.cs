using Elyndor.Combat;
using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>
    /// Kurze Trefferreaktion: ein Treffer schiebt den Gegner nach
    /// <see cref="EnemyFoundationState.Hurt"/> und unterbricht damit
    /// Verfolgung und Angriff. Nach Ablauf der Dauer kehrt er in einen
    /// sinnvollen Zustand zurueck — Verfolgung, wenn das Ziel noch bekannt
    /// ist, sonst Ruhe.
    ///
    /// Der Tod hat Vorrang: ein toedlicher Treffer erzeugt keine
    /// Trefferreaktion.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHitReaction : MonoBehaviour
    {
        [Tooltip("Kurzes Zusammenzucken nach einem leichten Treffer.")]
        [Min(0f)] [SerializeField] private float hurtDuration = 0.45f;

        [Tooltip("Laengeres Straucheln nach einem schweren Treffer. Dies ist " +
                 "das Fenster, das ein schwerer Angriff kauft.")]
        [Min(0f)] [SerializeField] private float heavyStaggerDuration = 0.45f;

        [SerializeField] private EnemyStateMachine stateMachine;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyPerception perception;

        private float remaining;
        private bool subscribed;

        /// <summary>Laeuft die Trefferreaktion gerade?</summary>
        public bool IsRecovering => remaining > 0f;

        public float HurtDuration => hurtDuration;
        public float HeavyStaggerDuration => heavyStaggerDuration;

        /// <summary>Dauer, die ein Treffer dieser Art ausloest.</summary>
        public float DurationFor(AttackType attackType) =>
            attackType == AttackType.Heavy
                ? heavyStaggerDuration
                : hurtDuration;

        private void Awake()
        {
            EnsureSubscribed();
        }

        private void OnDestroy()
        {
            if (!subscribed || health == null)
            {
                return;
            }

            health.Damaged -= HandleDamaged;
            subscribed = false;
        }

        /// <summary>
        /// Bindet die Trefferquelle an. Bewusst in Awake und zusaetzlich beim
        /// Takten: Treffer muessen auch dann ankommen, wenn der Gegner von
        /// aussen getaktet oder erst nachtraeglich vervollstaendigt wird.
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

            health.Damaged += HandleDamaged;
            subscribed = true;
        }

        public void Configure(float newHurtDuration)
        {
            hurtDuration = Mathf.Max(0f, newHurtDuration);
        }

        /// <summary>Setzt Flinch und Stagger getrennt.</summary>
        public void Configure(
            float newHurtDuration, float newHeavyStaggerDuration)
        {
            Configure(newHurtDuration);

            heavyStaggerDuration = Mathf.Max(0f, newHeavyStaggerDuration);
        }

        /// <summary>Laesst die Trefferreaktion ablaufen; wird vom Controller getaktet.</summary>
        public void Tick(float deltaTime)
        {
            EnsureSubscribed();

            if (!IsRecovering)
            {
                return;
            }

            remaining -= Mathf.Max(0f, deltaTime);

            if (remaining > 0f)
            {
                return;
            }

            remaining = 0f;

            if (stateMachine == null || stateMachine.IsDead)
            {
                return;
            }

            bool stillKnowsTarget = perception != null && perception.HasTarget;

            stateMachine.TrySetState(
                stillKnowsTarget
                    ? EnemyFoundationState.Chase
                    : EnemyFoundationState.Idle);
        }

        private void HandleDamaged(EnemyDamageInfo info)
        {
            // Der Tod gewinnt: kein Hurt fuer den toedlichen Treffer.
            if (info.IsLethal || stateMachine == null || stateMachine.IsDead)
            {
                return;
            }

            // Ein schwerer Treffer straucheln laesst laenger als ein leichter.
            // Genau darin liegt der Sinn des schweren Angriffs: er kauft Zeit,
            // nicht nur Schaden.
            float duration = DurationFor(info.AttackType);

            if (stateMachine.TrySetState(EnemyFoundationState.Hurt))
            {
                remaining = duration;
                return;
            }

            // Bereits in Hurt: die Dauer beginnt von vorn.
            if (stateMachine.Current == EnemyFoundationState.Hurt)
            {
                remaining = duration;
            }
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
        }
    }
}
