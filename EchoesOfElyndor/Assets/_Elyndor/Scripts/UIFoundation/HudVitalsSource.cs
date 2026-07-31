using System;
using UnityEngine;

namespace Elyndor.UIFoundation
{
    public sealed class HudVitalsSource : MonoBehaviour
    {
        [Min(1f)] [SerializeField] private float maxHealth = 100f;
        [Min(1f)] [SerializeField] private float maxStamina = 100f;
        [Min(1f)] [SerializeField] private float maxMemory = 100f;

        [SerializeField] private float health = 100f;
        [SerializeField] private float stamina = 100f;
        [SerializeField] private float memory = 100f;

        public event Action Changed;

        public float Health01 => Mathf.Clamp01(health / maxHealth);
        public float Stamina01 => Mathf.Clamp01(stamina / maxStamina);
        public float Memory01 => Mathf.Clamp01(memory / maxMemory);

        public void SetHealth(float value)
        {
            health = Mathf.Clamp(value, 0f, maxHealth);
            Changed?.Invoke();
        }

        public void SetStamina(float value)
        {
            stamina = Mathf.Clamp(value, 0f, maxStamina);
            Changed?.Invoke();
        }

        public void SetMemory(float value)
        {
            memory = Mathf.Clamp(value, 0f, maxMemory);
            Changed?.Invoke();
        }
    }
}
