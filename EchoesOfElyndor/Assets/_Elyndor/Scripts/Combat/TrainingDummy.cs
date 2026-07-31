using System;
using System.Collections;
using UnityEngine;

namespace Elyndor.Combat
{
    /// <summary>
    /// Übungspuppe für das Kampf-Tutorial: steckt unbegrenzt Treffer ein,
    /// wackelt als Feedback und meldet jeden Treffer über ein statisches
    /// Event an das Tutorial.
    /// </summary>
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private Transform shakeTarget;
        [SerializeField] private float shakeDuration = 0.35f;
        [SerializeField] private float shakeAngle = 14f;

        private Coroutine shakeCoroutine;
        private Quaternion restRotation;

        /// <summary>Wird bei jedem Treffer auf eine Übungspuppe ausgelöst.</summary>
        public static event Action<AttackType> HitTaken;

        private void Awake()
        {
            if (shakeTarget == null)
            {
                shakeTarget = transform;
            }

            restRotation = shakeTarget.localRotation;
        }

        public void TakeDamage(float amount, AttackType attackType, Vector3 sourcePosition)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeTarget.localRotation = restRotation;
            }

            Vector3 away = (shakeTarget.position - sourcePosition).normalized;
            shakeCoroutine = StartCoroutine(Shake(away, attackType == AttackType.Heavy ? 1.6f : 1f));

            HitTaken?.Invoke(attackType);
        }

        private IEnumerator Shake(Vector3 direction, float strength)
        {
            Vector3 shakeAxis = Vector3.Cross(Vector3.up, direction);
            float elapsedTime = 0f;

            while (elapsedTime < shakeDuration)
            {
                float falloff = 1f - elapsedTime / shakeDuration;
                float angle = Mathf.Sin(elapsedTime * 40f) * shakeAngle * strength * falloff;
                shakeTarget.localRotation = restRotation * Quaternion.AngleAxis(angle, shakeAxis);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            shakeTarget.localRotation = restRotation;
            shakeCoroutine = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            HitTaken = null;
        }
    }
}
