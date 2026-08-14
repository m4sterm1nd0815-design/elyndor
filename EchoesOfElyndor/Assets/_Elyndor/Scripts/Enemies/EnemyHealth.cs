using System;
using Elyndor.Combat;
using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>Beschreibt einen einzelnen Treffer fuer Reaktion, Animation und VFX.</summary>
    public readonly struct EnemyDamageInfo
    {
        public EnemyDamageInfo(
            float amount,
            AttackType attackType,
            Vector3 sourcePosition,
            bool isLethal)
        {
            Amount = amount;
            AttackType = attackType;
            SourcePosition = sourcePosition;
            IsLethal = isLethal;
        }

        public float Amount { get; }
        public AttackType AttackType { get; }
        public Vector3 SourcePosition { get; }

        /// <summary>Hat dieser Treffer den Gegner getoetet?</summary>
        public bool IsLethal { get; }
    }

    /// <summary>
    /// Lebenspunkte der Gegnergrundlage. Implementiert <see cref="IDamageable"/>,
    /// damit der vorhandene <see cref="PlayerCombat"/> ohne Anpassung trifft.
    /// Der Tod wird genau einmal ausgeloest.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Lebenspunkte")]
        [Min(1f)] [SerializeField] private float maxHealth = 30f;
        [Min(0f)] [SerializeField] private float currentHealth = 30f;

        /// <summary>Einzelner Treffer inklusive Angabe, ob er toedlich war.</summary>
        public event Action<EnemyDamageInfo> Damaged;

        /// <summary>Aktuelle und maximale Lebenspunkte nach einer Aenderung.</summary>
        public event Action<float, float> HealthChanged;

        /// <summary>Wird genau einmal ausgeloest.</summary>
        public event Action Died;

        /// <summary>
        /// Treffer und Tod zusaetzlich statisch, damit die zentrale
        /// <c>SfxLibrary</c> zuhoeren kann, ohne jeden Gegner zu kennen.
        /// Dieselbe Reihenfolge und dieselbe Einmaligkeit wie bei den
        /// Instanzereignissen.
        /// </summary>
        public static event Action<EnemyDamageInfo> AnyDamaged;

        /// <summary>Wird je Gegner genau einmal ausgeloest.</summary>
        public static event Action AnyDied;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float Health01 => Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
        public bool IsDead { get; private set; }

        private void Awake()
        {
            ClampValues();
            IsDead = currentHealth <= 0f;
        }

        private void OnValidate()
        {
            ClampValues();
        }

        /// <summary>Setzt Maximum und aktuelle Lebenspunkte, etwa fuer einen spaeteren Spawner.</summary>
        public void Configure(float newMaxHealth, bool refill = true)
        {
            maxHealth = Mathf.Max(1f, newMaxHealth);

            if (refill)
            {
                currentHealth = maxHealth;
                IsDead = false;
            }

            ClampValues();
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(
            float amount, AttackType attackType, Vector3 sourcePosition)
        {
            // Ein toter Gegner nimmt keinen Schaden mehr — damit kann der Tod
            // auch bei mehreren Treffern im selben Frame nur einmal eintreten.
            if (IsDead)
            {
                return;
            }

            float applied = Mathf.Abs(amount);

            if (applied <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - applied);
            bool lethal = currentHealth <= 0f;

            // Reihenfolge ist wichtig: erst der Treffer (fuer Reaktion und VFX),
            // dann der Tod. Die Trefferreaktion erkennt am Flag, dass sie den
            // toedlichen Treffer nicht in Hurt uebersetzen darf.
            if (lethal)
            {
                IsDead = true;
            }

            var info = new EnemyDamageInfo(
                applied, attackType, sourcePosition, lethal);

            Damaged?.Invoke(info);
            AnyDamaged?.Invoke(info);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (lethal)
            {
                Died?.Invoke();
                AnyDied?.Invoke();
            }
        }

        /// <summary>Toetet den Gegner sofort; wirkt nur beim ersten Aufruf.</summary>
        public void Kill(Vector3 sourcePosition)
        {
            if (IsDead)
            {
                return;
            }

            TakeDamage(
                Mathf.Max(currentHealth, 1f), AttackType.Heavy, sourcePosition);
        }

        private void ClampValues()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            AnyDamaged = null;
            AnyDied = null;
        }
    }
}
