using System;
using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>
    /// Einmaliges Loesen vom Ziel, wenn die Lebenspunkte unter eine Schwelle
    /// fallen. Der Rueckzug ist ein erzaehlerischer Moment, kein taktisches
    /// Dauerverhalten: ein Tier, das immer wieder wegspringt, sobald es
    /// getroffen wird, macht den Kampf zaeh statt lesbar. Deshalb ist er
    /// vorbelegt auf genau ein Mal.
    ///
    /// Die Komponente entscheidet nur, ob zurueckgezogen werden soll. Wohin
    /// und wie schnell, bleibt Sache von <see cref="EnemyMovement"/>; wann der
    /// Zustand gilt, entscheidet der <see cref="EnemyController"/>.
    ///
    /// Ohne diese Komponente zieht sich ein Gegner nie zurueck.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyRetreat : MonoBehaviour
    {
        [Header("Ausloesung")]
        [Tooltip("Anteil der Lebenspunkte, ab dem der Rueckzug greift.")]
        [Range(0f, 1f)] [SerializeField] private float healthThreshold01 = 0.3f;

        [Tooltip("Wie lange das Loesen dauert. Vorlaeufiger Balancingwert.")]
        [Min(0f)] [SerializeField] private float retreatDuration = 2.5f;

        [Tooltip("Nur ein einziges Mal pro Leben. Aus bedeutet Rueckzug bei " +
                 "jedem Unterschreiten der Schwelle.")]
        [SerializeField] private bool onlyOnce = true;

        [SerializeField] private EnemyHealth health;

        private float remaining;

        /// <summary>Wird ausgeloest, wenn ein Rueckzug beginnt.</summary>
        public event Action RetreatStarted;

        /// <summary>Laeuft gerade ein Rueckzug?</summary>
        public bool IsRetreating => remaining > 0f;

        /// <summary>Wurde in diesem Leben bereits zurueckgezogen?</summary>
        public bool HasRetreated { get; private set; }

        public float HealthThreshold01 => healthThreshold01;
        public float RetreatDuration => retreatDuration;

        private void Awake()
        {
            ResolveReferences();
        }

        /// <summary>Setzt Schwelle und Dauer, etwa aus einem Gegnerprofil.</summary>
        public void Configure(
            float newHealthThreshold01,
            float newRetreatDuration,
            bool newOnlyOnce = true)
        {
            healthThreshold01 = Mathf.Clamp01(newHealthThreshold01);
            retreatDuration = Mathf.Max(0f, newRetreatDuration);
            onlyOnce = newOnlyOnce;
        }

        /// <summary>Vergisst, dass bereits zurueckgezogen wurde.</summary>
        public void ResetRetreat()
        {
            remaining = 0f;
            HasRetreated = false;
        }

        /// <summary>
        /// Laesst einen laufenden Rueckzug ablaufen und loest bei Bedarf einen
        /// neuen aus. Wird vom <see cref="EnemyController"/> getaktet.
        /// </summary>
        public void Tick(float deltaTime)
        {
            ResolveReferences();

            if (remaining > 0f)
            {
                remaining = Mathf.Max(0f, remaining - Mathf.Max(0f, deltaTime));
                return;
            }

            if (retreatDuration <= 0f)
            {
                return;
            }

            if (onlyOnce && HasRetreated)
            {
                return;
            }

            if (health == null || health.IsDead)
            {
                return;
            }

            if (health.Health01 > healthThreshold01)
            {
                return;
            }

            remaining = retreatDuration;
            HasRetreated = true;
            RetreatStarted?.Invoke();
        }

        private void ResolveReferences()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }
        }
    }
}
