using System;
using System.Collections.Generic;
using Elyndor.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Elyndor.Combat
{
    /// <summary>
    /// Kampfsteuerung: leichter Angriff (Linksklick / rechter Trigger),
    /// schwerer Angriff (gedrückt halten, dann loslassen), Blocken
    /// (Q / linker Trigger). Nur aktiv, wenn eine Waffe in der Haupthand
    /// liegt. Trefferprüfung über eine Kugel vor dem Spieler —
    /// Animationen folgen in einem eigenen Pass mit Sichtprüfung.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Angriff")]
        [SerializeField] private float lightDamage = 10f;
        [SerializeField] private float heavyDamage = 25f;
        [SerializeField] private float heavyHoldThreshold = 0.45f;
        [SerializeField] private float attackCooldown = 0.6f;
        [SerializeField] private float attackRange = 1.4f;
        [SerializeField] private float attackRadius = 1f;

        private float nextAttackTime;
        private float pressStartTime;
        private bool buttonHeld;
        private Animator animator;
        private bool animatorHasCombatParameters;

        private void Awake()
        {
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

        /// <summary>Blockt der Spieler gerade? (Für Gegner-Logik und Tutorial.)</summary>
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

            IsBlocking = ReadBlockHeld();

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
                    PerformAttack(heavy ? AttackType.Heavy : AttackType.Light);
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

        // Direkter Device-Zugriff wie im restlichen Gameplay-Code (M3-Refactor folgt).
        private static bool ReadAttackPressed()
        {
            bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool gamepad = Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame;
            return mouse || gamepad;
        }

        private static bool ReadAttackReleased()
        {
            bool mouse = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            bool gamepad = Gamepad.current != null && Gamepad.current.rightTrigger.wasReleasedThisFrame;
            return mouse || gamepad;
        }

        private static bool ReadBlockHeld()
        {
            bool keyboard = Keyboard.current != null && Keyboard.current.qKey.isPressed;
            bool gamepad = Gamepad.current != null && Gamepad.current.leftTrigger.isPressed;
            return keyboard || gamepad;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            AttackPerformed = null;
        }
    }
}
