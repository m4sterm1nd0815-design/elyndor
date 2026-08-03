using System.Collections;
using UnityEngine;

namespace Elyndor.Player
{
    /// <summary>
    /// Setzt den Spieler beim Start einer Szene auf einen definierten
    /// Startpunkt und richtet ihn auf ein Ziel oder eine feste Drehung aus.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStartSetup : MonoBehaviour
    {
        [Header("Startpunkt")]

        [Tooltip(
            "Optionaler Transform für Position und Blickrichtung. " +
            "Am besten ein leeres GameObject namens PlayerStart.")]
        [SerializeField]
        private Transform startPoint;

        [Tooltip(
            "Spieler beim Szenenstart auf die Position des Startpunkts setzen.")]
        [SerializeField]
        private bool applyPosition = true;

        [Tooltip(
            "Spieler beim Szenenstart in die Richtung des Startpunkts drehen.")]
        [SerializeField]
        private bool applyRotation = true;

        [Header("Optionales Blickziel")]

        [Tooltip(
            "Wenn gesetzt, schaut Aren beim Start zu diesem Objekt. " +
            "Die Höhe wird ignoriert.")]
        [SerializeField]
        private Transform lookTarget;

        [Header("Kamera")]

        [Tooltip(
            "Die Kamera nach dem Setzen kurz auf Aren ausrichten.")]
        [SerializeField]
        private bool alignMainCamera = true;

        [Tooltip(
            "Höhe des Kamerablickpunkts über Aren.")]
        [SerializeField]
        private float cameraFocusHeight = 1.2f;

        private CharacterController characterController;

        private IEnumerator Start()
        {
            characterController = GetComponent<CharacterController>();

            SetPlayerStart();

            // Einen Frame warten, damit alle Kamera-Scripts initialisiert sind.
            yield return null;

            if (alignMainCamera)
            {
                AlignCamera();
            }
        }

        private void SetPlayerStart()
        {
            if (startPoint == null)
            {
                Debug.LogWarning(
                    "PlayerStartSetup: Kein Startpunkt zugewiesen.",
                    this);

                return;
            }

            bool controllerWasEnabled =
                characterController != null &&
                characterController.enabled;

            if (controllerWasEnabled)
            {
                characterController.enabled = false;
            }

            if (applyPosition)
            {
                transform.position = startPoint.position;
            }

            if (applyRotation)
            {
                Quaternion targetRotation =
                    GetDesiredRotation();

                transform.rotation = targetRotation;
            }

            if (controllerWasEnabled)
            {
                characterController.enabled = true;
            }
        }

        private Quaternion GetDesiredRotation()
        {
            if (lookTarget == null)
            {
                return Quaternion.Euler(
                    0f,
                    startPoint.eulerAngles.y,
                    0f);
            }

            Vector3 direction =
                lookTarget.position - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                return Quaternion.Euler(
                    0f,
                    startPoint.eulerAngles.y,
                    0f);
            }

            return Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);
        }

        private void AlignCamera()
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                return;
            }

            Vector3 focusPoint =
                transform.position +
                Vector3.up * cameraFocusHeight;

            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude <= 0.001f)
            {
                return;
            }

            mainCamera.transform.LookAt(focusPoint);
        }

        private void OnValidate()
        {
            cameraFocusHeight =
                Mathf.Max(0f, cameraFocusHeight);
        }
    }
}