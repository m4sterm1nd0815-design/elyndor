using System;
using Elyndor.Interaction;
using UnityEngine;

namespace Elyndor.Puzzles
{
    /// <summary>
    /// Ein drehbarer Ankerstein der geteilten Brücke. Drei stabile Stellungen,
    /// erkennbar an ein, zwei oder drei Kerben.
    ///
    /// Der Anker weiß nicht, ob seine Stellung richtig ist. Er meldet nur, dass
    /// er gedreht wurde — die Bewertung liegt beim <see cref="BridgePuzzle"/>.
    /// Wüsste er es, wäre die Versuchung gross, die Rückmeldung am Stein selbst
    /// zu zeigen; damit wäre das Rätsel kein Rätsel mehr, sondern eine
    /// Kontrollleuchte.
    /// </summary>
    public sealed class BridgeAnchor : InteractableBase
    {
        [Header("Anker")]
        [SerializeField] private BridgeAnchorId anchorId = BridgeAnchorId.SouthDeep;

        [Tooltip("Aktuelle Stellung: 0, 1 oder 2 — also eine, zwei oder drei Kerben.")]
        [Range(0, BridgePuzzleRules.SettingsPerAnchor - 1)]
        [SerializeField] private int setting;

        [Header("Darstellung")]
        [Tooltip("Der Teil, der sich sichtbar dreht. Leer lassen fuer dieses Objekt.")]
        [SerializeField] private Transform rotatingPart;

        [Tooltip("Drehung je Stellung in Grad.")]
        [SerializeField] private float degreesPerSetting = 120f;

        [Tooltip("Wie schnell sich der Stein in seine Stellung dreht.")]
        [Min(1f)] [SerializeField] private float turnSpeedDegrees = 220f;

        [SerializeField] private BridgePuzzle puzzle;

        private Quaternion baseRotation;

        /// <summary>Wird nach jeder Drehung ausgelöst.</summary>
        public event Action<BridgeAnchor> Turned;

        public BridgeAnchorId AnchorId => anchorId;

        /// <summary>Aktuelle Stellung, 0 bis 2.</summary>
        public int Setting => setting;

        /// <summary>Kerbenzahl der aktuellen Stellung, 1 bis 3.</summary>
        public int Notches => BridgePuzzleRules.NotchesForSetting(setting);

        public override string InteractionPrompt =>
            $"Anker drehen ({Notches} Kerben)";

        public override bool CanInteract(GameObject interactor)
        {
            return puzzle == null || puzzle.CanTurnAnchors;
        }

        protected override void Awake()
        {
            base.Awake();

            if (rotatingPart == null)
            {
                rotatingPart = transform;
            }

            baseRotation = rotatingPart.localRotation;

            if (puzzle == null)
            {
                puzzle = GetComponentInParent<BridgePuzzle>();
            }

            ApplyRotationInstant();
        }

        private void Update()
        {
            // Weich in die Zielstellung drehen, damit die Aenderung sichtbar
            // ist und nicht springt.
            rotatingPart.localRotation = Quaternion.RotateTowards(
                rotatingPart.localRotation,
                TargetRotation(),
                turnSpeedDegrees * Time.deltaTime);
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            Turn();
        }

        /// <summary>Dreht auf die nächste Stellung weiter und meldet das.</summary>
        public void Turn()
        {
            setting = (setting + 1) % BridgePuzzleRules.SettingsPerAnchor;
            Turned?.Invoke(this);
        }

        /// <summary>Setzt die Stellung unmittelbar, etwa beim Wiederherstellen.</summary>
        public void SetSetting(int newSetting, bool notify = false)
        {
            setting = Mathf.Clamp(
                newSetting, 0, BridgePuzzleRules.SettingsPerAnchor - 1);

            ApplyRotationInstant();

            if (notify)
            {
                Turned?.Invoke(this);
            }
        }

        private Quaternion TargetRotation()
        {
            return baseRotation *
                   Quaternion.Euler(0f, degreesPerSetting * setting, 0f);
        }

        private void ApplyRotationInstant()
        {
            if (rotatingPart != null)
            {
                rotatingPart.localRotation = TargetRotation();
            }
        }
    }
}
