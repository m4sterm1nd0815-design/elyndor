using System;
using System.Collections.Generic;
using Elyndor.Inventory;
using Elyndor.Player;
using Elyndor.UIFoundation;
using UnityEngine;

namespace Elyndor.Combat
{
    /// <summary>
    /// Kampfsteuerung: leichter Angriff (Linksklick / rechter Trigger),
    /// schwerer Angriff (gedrückt halten, dann loslassen), Blocken
    /// (Q / linker Trigger). Nur aktiv, wenn eine Waffe in der Haupthand
    /// liegt. Trefferprüfung über eine Kugel vor dem Spieler —
    /// Animationen folgen in einem eigenen Pass mit Sichtprüfung.
    /// </summary>
    public class PlayerCombat : MonoBehaviour, IPlayerBlockState
    {
        [Header("Angriff")]
        [SerializeField] private float lightDamage = 10f;
        [SerializeField] private float heavyDamage = 25f;
        [SerializeField] private float heavyHoldThreshold = 0.45f;
        [SerializeField] private float attackCooldown = 0.6f;
        [SerializeField] private float attackRange = 1.4f;
        [SerializeField] private float attackRadius = 1f;

        [Header("Stamina")]
        [Tooltip("Ausdauer je leichtem Angriff. Vorlaeufiger Balancingwert.")]
        [Min(0f)] [SerializeField] private float lightAttackStaminaCost = 8f;

        [Tooltip("Ausdauer je schwerem Angriff. Vorlaeufiger Balancingwert.")]
        [Min(0f)] [SerializeField] private float heavyAttackStaminaCost = 18f;

        [Tooltip("Ausdauerverbrauch pro Sekunde beim Blocken. Vorlaeufiger " +
                 "Balancingwert.")]
        [Min(0f)] [SerializeField] private float blockStaminaPerSecond = 10f;

        private float nextAttackTime;
        private float pressStartTime;
        private bool buttonHeld;
        private Animator animator;
        private bool animatorHasCombatParameters;
        private PlayerInputReader inputReader;
        private PlayerVitals playerVitals;

        private void Awake()
        {
            inputReader = GetComponent<PlayerInputReader>()
                ?? PlayerInputReader.FindInLoadedScenes();

            playerVitals = GetComponent<PlayerVitals>();

            animator = GetComponentInChildren<Animator>();

            if (animator != null)
            {
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                {
                    if (parameter.name == "AttackLight")
                    {
                        animatorHasCombatParameters = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Blockt der Spieler gerade? (Für Gegner-Logik und Tutorial.)
        ///
        /// Zugleich die Umsetzung von <see cref="IPlayerBlockState"/>: der
        /// <see cref="PlayerDamageReceiver"/> liest hier ab, ob ein
        /// eingehender Treffer gemindert wird.
        /// </summary>
        public bool IsBlocking { get; private set; }

        /// <summary>Wird nach jedem ausgeführten Angriff ausgelöst.</summary>
        public static event Action<AttackType> AttackPerformed;

        private void Update()
        {
            if (PlayerInventory.GetEquipped(EquipmentSlot.MainHand) == null)
            {
                IsBlocking = false;
                return;
            }

            IsBlocking = ReadBlockHeld() && TryMaintainBlock();

            if (animatorHasCombatParameters)
            {
                animator.SetBool("Block", IsBlocking);
            }

            if (IsBlocking)
            {
                buttonHeld = false;
                return;
            }

            if (ReadAttackPressed())
            {
                buttonHeld = true;
                pressStartTime = Time.time;
            }

            if (buttonHeld && ReadAttackReleased())
            {
                buttonHeld = false;

                if (Time.time >= nextAttackTime)
                {
                    bool heavy = Time.time - pressStartTime >= heavyHoldThreshold;
                    AttackType attackType =
                        heavy ? AttackType.Heavy : AttackType.Light;

                    if (TrySpendAttackStamina(attackType))
                    {
                        PerformAttack(attackType);
                    }
                }
            }
        }

        private void PerformAttack(AttackType attackType)
        {
            nextAttackTime = Time.time + attackCooldown;

            if (animatorHasCombatParameters)
            {
                animator.SetTrigger(attackType == AttackType.Heavy ? "AttackHeavy" : "AttackLight");
            }

            float damage = attackType == AttackType.Heavy ? heavyDamage : lightDamage;
            Vector3 center = transform.position + transform.forward * attackRange + Vector3.up * 0.9f;

            HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

            foreach (Collider hit in Physics.OverlapSphere(center, attackRadius))
            {
                IDamageable damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable != null && alreadyHit.Add(damageable))
                {
                    damageable.TakeDamage(damage, attackType, transform.position);
                }
            }

            AttackPerformed?.Invoke(attackType);
        }

        /// <summary>
        /// Blocken kostet laufend Ausdauer. Ist sie leer, faellt der Block in
        /// sich zusammen, statt kostenlos weiterzulaufen — sonst waere Halten
        /// die dominante Antwort auf jeden Angriff.
        /// </summary>
        private bool TryMaintainBlock()
        {
            if (playerVitals == null)
            {
                return true;
            }

            return playerVitals.TrySpendStamina(
                blockStaminaPerSecond * Time.deltaTime);
        }

        /// <summary>
        /// Ein Angriff wird nur ausgefuehrt, wenn seine Ausdauer vollstaendig
        /// bezahlt werden kann. Ein halb bezahlter Schlag existiert nicht.
        /// </summary>
        private bool TrySpendAttackStamina(AttackType attackType)
        {
            if (playerVitals == null)
            {
                return true;
            }

            float cost = attackType == AttackType.Heavy
                ? heavyAttackStaminaCost
                : lightAttackStaminaCost;

            return playerVitals.TrySpendStamina(cost);
        }

        /// <summary>
        /// Eingabe kommt ausschliesslich ueber den <see cref="PlayerInputReader"/>.
        /// Vorher las diese Klasse Maus, Tastatur und Gamepad direkt; die
        /// Belegung (Maustaste / rechter Trigger fuer Angriff, Q / linker
        /// Trigger fuer Block) stand dadurch nur hier im Code und nicht im
        /// Actions-Asset — nicht umbelegbar und nicht ueber die Action-Schicht
        /// testbar.
        /// </summary>
        private bool ReadAttackPressed() =>
            inputReader != null && inputReader.AttackPressedThisFrame;

        private bool ReadAttackReleased() =>
            inputReader != null && inputReader.AttackReleasedThisFrame;

        private bool ReadBlockHeld() =>
            inputReader != null && inputReader.BlockHeld;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            AttackPerformed = null;
        }
    }
}
