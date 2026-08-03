using System;
using Elyndor.UIFoundation;
using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>Beschreibt einen ausgefuehrten Angriff fuer spaetere Animation und VFX.</summary>
    public readonly struct EnemyAttackInfo
    {
        public EnemyAttackInfo(
            Transform target, float damage, bool damageApplied)
        {
            Target = target;
            Damage = damage;
            DamageApplied = damageApplied;
        }

        public Transform Target { get; }
        public float Damage { get; }

        /// <summary>
        /// Ob tatsaechlich Schaden angekommen ist. Falsch, wenn das Ziel keine
        /// <see cref="PlayerVitals"/> hat — der Angriff selbst gilt trotzdem
        /// als ausgefuehrt.
        /// </summary>
        public bool DamageApplied { get; }
    }

    /// <summary>
    /// Nahangriff der Gegnergrundlage: feste Reichweite, Abklingzeit und genau
    /// ein Schadensereignis je Angriff. Der Schaden geht ueber das vorhandene
    /// <see cref="PlayerVitals.TakeDamage"/>; fehlt die Komponente, bleibt der
    /// Angriff folgenlos, ohne Fehler zu erzeugen.
    ///
    /// Die Abklingzeit laeuft ueber den uebergebenen Zeitschritt statt ueber
    /// <see cref="Time.time"/>, damit Tests sie ohne Wartezeit durchspielen
    /// koennen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAttack : MonoBehaviour
    {
        [Header("Angriff")]
        [Min(0f)] [SerializeField] private float attackRange = 2f;
        [Min(0f)] [SerializeField] private float attackCooldown = 1.6f;
        [Min(0f)] [SerializeField] private float damage = 8f;

        private Transform cachedTarget;
        private PlayerVitals cachedVitals;

        /// <summary>Wird nach jedem ausgefuehrten Angriff ausgeloest.</summary>
        public event Action<EnemyAttackInfo> AttackPerformed;

        public float AttackRange => attackRange;
        public float CooldownRemaining { get; private set; }
        public bool IsReady => CooldownRemaining <= 0f;

        /// <summary>Anzahl ausgefuehrter Angriffe; vor allem fuer Tests und Telemetrie.</summary>
        public int PerformedAttacks { get; private set; }

        /// <summary>Setzt Reichweite, Abklingzeit und Schaden, etwa fuer einen spaeteren Spawner.</summary>
        public void Configure(
            float newRange, float newCooldown, float newDamage)
        {
            attackRange = Mathf.Max(0f, newRange);
            attackCooldown = Mathf.Max(0f, newCooldown);
            damage = Mathf.Max(0f, newDamage);
        }

        /// <summary>
        /// Laesst die Abklingzeit laufen und greift an, sobald Zustand,
        /// Reichweite und Abklingzeit es zulassen.
        /// </summary>
        public void Tick(
            EnemyFoundationState state, Transform target, float deltaTime)
        {
            if (CooldownRemaining > 0f)
            {
                CooldownRemaining =
                    Mathf.Max(0f, CooldownRemaining - Mathf.Max(0f, deltaTime));
            }

            TryAttack(state, target);
        }

        /// <summary>
        /// Fuehrt einen Angriff aus, wenn alle Bedingungen erfuellt sind.
        /// Gibt zurueck, ob angegriffen wurde.
        /// </summary>
        public bool TryAttack(EnemyFoundationState state, Transform target)
        {
            if (state != EnemyFoundationState.Attack)
            {
                return false;
            }

            if (target == null || !IsReady)
            {
                return false;
            }

            if (PlanarDistanceTo(target.position) > attackRange)
            {
                return false;
            }

            CooldownRemaining = attackCooldown;
            PerformedAttacks++;

            bool applied = ApplyDamage(target);
            AttackPerformed?.Invoke(
                new EnemyAttackInfo(target, damage, applied));

            return true;
        }

        private bool ApplyDamage(Transform target)
        {
            PlayerVitals vitals = ResolveVitals(target);

            if (vitals == null || damage <= 0f)
            {
                return false;
            }

            vitals.TakeDamage(damage);
            return true;
        }

        private PlayerVitals ResolveVitals(Transform target)
        {
            if (cachedTarget == target && cachedVitals != null)
            {
                return cachedVitals;
            }

            cachedTarget = target;
            cachedVitals = target.GetComponentInParent<PlayerVitals>();

            return cachedVitals;
        }

        private float PlanarDistanceTo(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            offset.y = 0f;

            return offset.magnitude;
        }
    }
}
