using UnityEngine;

namespace Elyndor.UI
{
    /// <summary>
    /// Einfacher Kompass am oberen Bildrand: Die Nadel dreht sich mit der
    /// Kamera, sodass „N" immer die Weltrichtung Norden (+Z) anzeigt.
    /// </summary>
    public class CompassUI : MonoBehaviour
    {
        [SerializeField] private RectTransform needle;

        private Transform cameraTransform;

        private void LateUpdate()
        {
            if (needle == null)
            {
                return;
            }

            if (cameraTransform == null)
            {
                Camera mainCamera = Camera.main;

                if (mainCamera == null)
                {
                    return;
                }

                cameraTransform = mainCamera.transform;
            }

            needle.localRotation =
                Quaternion.Euler(0f, 0f, cameraTransform.eulerAngles.y);
        }
    }
}
