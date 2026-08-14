using System;
using Elyndor.Core;
using Elyndor.UIFoundation;
using UnityEngine;

namespace Elyndor.Puzzles
{
    /// <summary>
    /// Die gefährliche Furt. Wer sie versucht, wird von der Strömung
    /// zurückgetragen — <b>ohne jeden Schaden</b>.
    ///
    /// Das ist eine bewusste Entscheidung und keine Bequemlichkeit: Tod und
    /// Respawn sind im Projekt noch nicht kanonisch entschieden. Würde die Furt
    /// Schaden machen oder töten, hätte dieses eine Rätsel die Entscheidung
    /// nebenbei getroffen — und zwar an der Stelle, an der sie am wenigsten
    /// überlegt wäre.
    ///
    /// Es gibt deshalb auch keinen unsichtbaren Todesrand: der Spieler wird
    /// sichtbar zurückgesetzt und darf es sofort wieder versuchen.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class FordReset : MonoBehaviour
    {
        [Header("Rücksetzung")]
        [Tooltip("Wohin die Strömung Aren zurückträgt. Ohne Angabe wird die " +
                 "Position beim Betreten verwendet.")]
        [SerializeField] private Transform safeReturn;

        [Tooltip("Wie lange die Strömung trägt, bevor zurückgesetzt wird. " +
                 "Kurz genug, um nicht zu ärgern, lang genug, um verstanden " +
                 "zu werden.")]
        [Min(0f)] [SerializeField] private float resetDelay = 0.8f;

        [Header("Rückmeldung")]
        [SerializeField, TextArea(2, 4)]
        private string message =
            "Die Strömung ist zu stark. Sie trägt Aren zurück ans Ufer.";

        [Min(1f)] [SerializeField] private float messageDuration = 4f;

        private GameObject player;
        private CharacterController playerBody;
        private Vector3 entryPosition;
        private float remaining;

        /// <summary>Wird nach jeder Rücksetzung ausgelöst; vor allem für Tests.</summary>
        public event Action Reset;

        /// <summary>Anzahl der Rücksetzungen; für Tests und Telemetrie.</summary>
        public int ResetCount { get; private set; }

        /// <summary>Trägt die Strömung Aren gerade?</summary>
        public bool IsCarrying => player != null;

        private void Awake()
        {
            Collider volume = GetComponent<Collider>();

            if (volume != null && !volume.isTrigger)
            {
                Debug.LogWarning(
                    $"{name}: Die Furt braucht einen Trigger-Collider.", this);
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Ein Schritt der Furtlogik; öffentlich für Tests.</summary>
        public void Tick(float deltaTime)
        {
            if (player == null)
            {
                return;
            }

            remaining -= Mathf.Max(0f, deltaTime);

            if (remaining > 0f)
            {
                return;
            }

            ReturnPlayer();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (player != null)
            {
                return;
            }

            PlayerVitals vitals = other == null
                ? null
                : other.GetComponentInParent<PlayerVitals>();

            if (vitals == null)
            {
                return;
            }

            player = vitals.gameObject;
            playerBody = player.GetComponent<CharacterController>();
            entryPosition = player.transform.position;
            remaining = resetDelay;

            NarrationEvents.RaiseMessage(message, messageDuration);
        }

        private void OnTriggerExit(Collider other)
        {
            if (player == null || other == null)
            {
                return;
            }

            PlayerVitals vitals = other.GetComponentInParent<PlayerVitals>();

            if (vitals != null && vitals.gameObject == player)
            {
                // Wer es aus eigener Kraft heraus schafft, wird nicht
                // nachträglich zurückgeholt.
                player = null;
                playerBody = null;
            }
        }

        /// <summary>Setzt den Spieler zurück; kein Schaden, kein Verlust.</summary>
        public void ReturnPlayer()
        {
            if (player == null)
            {
                return;
            }

            Vector3 target = safeReturn != null
                ? safeReturn.position
                : entryPosition;

            // Der CharacterController muss kurz aus, sonst schiebt er die
            // Position im selben Frame zurueck.
            bool wasEnabled = playerBody != null && playerBody.enabled;

            if (playerBody != null)
            {
                playerBody.enabled = false;
            }

            player.transform.position = target;

            if (playerBody != null)
            {
                playerBody.enabled = wasEnabled;
            }

            Physics.SyncTransforms();

            player = null;
            playerBody = null;
            ResetCount++;

            Reset?.Invoke();
        }
    }
}
