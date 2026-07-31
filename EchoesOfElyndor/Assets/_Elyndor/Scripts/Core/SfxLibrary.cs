using Elyndor.Combat;
using Elyndor.Memory;
using Elyndor.UI;
using UnityEngine;

namespace Elyndor.Core
{
    /// <summary>
    /// Zentrale Soundeffekt-Anbindung: hört auf die bestehenden Spiel-Events
    /// (Narration, Memory Watch, Kampf, Inventar) und spielt zugewiesene
    /// Clips. Zusätzlich distanzbasierte Schritte für den Spieler.
    /// Keine Kopplung in die Gegenrichtung — Systeme kennen kein Audio.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SfxLibrary : MonoBehaviour
    {
        [Header("Erzählung und Erinnerung")]
        [SerializeField] private AudioClip narrationClip;
        [SerializeField] private AudioClip memoryStartClip;
        [SerializeField] private AudioClip memoryCompleteClip;

        [Header("Kampf")]
        [SerializeField] private AudioClip attackLightClip;
        [SerializeField] private AudioClip attackHeavyClip;
        [SerializeField] private AudioClip hitLightClip;
        [SerializeField] private AudioClip hitHeavyClip;

        [Header("Inventar")]
        [SerializeField] private AudioClip inventoryOpenClip;
        [SerializeField] private AudioClip inventoryCloseClip;

        [Header("Schritte")]
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private Transform footstepSource;
        [SerializeField] private float stepDistance = 1.9f;
        [SerializeField] private float footstepVolume = 0.35f;

        private AudioSource audioSource;
        private Vector3 lastFootstepPosition;
        private float accumulatedDistance;
        private int lastFootstepIndex = -1;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            if (footstepSource != null)
            {
                lastFootstepPosition = footstepSource.position;
            }
        }

        private void OnEnable()
        {
            NarrationEvents.MessageRequested += HandleNarration;
            MemorySite.AnyActivationStarted += HandleMemoryStarted;
            MemorySite.AnyActivationCompleted += HandleMemoryCompleted;
            PlayerCombat.AttackPerformed += HandleAttack;
            TrainingDummy.HitTaken += HandleHit;
            InventoryUI.Toggled += HandleInventoryToggled;
        }

        private void OnDisable()
        {
            NarrationEvents.MessageRequested -= HandleNarration;
            MemorySite.AnyActivationStarted -= HandleMemoryStarted;
            MemorySite.AnyActivationCompleted -= HandleMemoryCompleted;
            PlayerCombat.AttackPerformed -= HandleAttack;
            TrainingDummy.HitTaken -= HandleHit;
            InventoryUI.Toggled -= HandleInventoryToggled;
        }

        private void Update()
        {
            UpdateFootsteps();
        }

        // Distanzbasierte Schritte: robust ohne Animations-Events.
        private void UpdateFootsteps()
        {
            if (footstepSource == null || footstepClips == null || footstepClips.Length == 0)
            {
                return;
            }

            Vector3 currentPosition = footstepSource.position;
            Vector3 delta = currentPosition - lastFootstepPosition;
            delta.y = 0f;
            lastFootstepPosition = currentPosition;

            // Teleport (Regionswechsel) nicht als Schritt werten.
            if (delta.magnitude > 2f)
            {
                accumulatedDistance = 0f;
                return;
            }

            accumulatedDistance += delta.magnitude;

            if (accumulatedDistance >= stepDistance)
            {
                accumulatedDistance = 0f;

                int index;
                do
                {
                    index = Random.Range(0, footstepClips.Length);
                } while (footstepClips.Length > 1 && index == lastFootstepIndex);

                lastFootstepIndex = index;
                Play(footstepClips[index], footstepVolume);
            }
        }

        private void HandleNarration(string text, float duration) => Play(narrationClip, 0.7f);
        private void HandleMemoryStarted(MemorySite site) => Play(memoryStartClip, 0.9f);
        private void HandleMemoryCompleted(MemorySite site) => Play(memoryCompleteClip, 0.9f);

        private void HandleAttack(AttackType attackType)
        {
            Play(attackType == AttackType.Heavy ? attackHeavyClip : attackLightClip, 0.8f);
        }

        private void HandleHit(AttackType attackType)
        {
            Play(attackType == AttackType.Heavy ? hitHeavyClip : hitLightClip, 0.9f);
        }

        private void HandleInventoryToggled(bool open)
        {
            Play(open ? inventoryOpenClip : inventoryCloseClip, 0.8f);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
