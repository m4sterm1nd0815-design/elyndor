using System;
using UnityEngine;

namespace Elyndor.UIFoundation
{
    public sealed class PlayerVitals : MonoBehaviour
    {
        [Header("Maximum values")]
        [Min(1f)] [SerializeField] private float maxHealth = 100f;
        [Min(1f)] [SerializeField] private float maxStamina = 100f;
        [Min(1f)] [SerializeField] private float maxMemory = 100f;

        [Header("Current values")]
        [SerializeField] private float health = 100f;
        [SerializeField] private float stamina = 100f;
        [SerializeField] private float memory = 25f;

        public event Action Changed;

        public float Health => health;
        public float Stamina => stamina;
        public float Memory => memory;

        public float Health01 => Mathf.Clamp01(health / maxHealth);
        public float Stamina01 => Mathf.Clamp01(stamina / maxStamina);
        public float Memory01 => Mathf.Clamp01(memory / maxMemory);

        private void Awake()
        {
            ClampAll();
        }

        private void OnValidate()
        {
            ClampAll();
        }

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

        public void ApplyHealth(float delta)
        {
            SetHealth(health + delta);
        }

        public void ApplyStamina(float delta)
        {
            SetStamina(stamina + delta);
        }

        public void ApplyMemory(float delta)
        {
            SetMemory(memory + delta);
        }

        public bool TrySpendStamina(float amount)
        {
            amount = Mathf.Max(0f, amount);
            if (stamina < amount)
                return false;

            SetStamina(stamina - amount);
            return true;
        }

        public void TakeDamage(float amount)
        {
            ApplyHealth(-Mathf.Abs(amount));
        }

        public void ResetToFull()
        {
            health = maxHealth;
            stamina = maxStamina;
            memory = maxMemory;
            Changed?.Invoke();
        }

        private void ClampAll()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            maxStamina = Mathf.Max(1f, maxStamina);
            maxMemory = Mathf.Max(1f, maxMemory);

            health = Mathf.Clamp(health, 0f, maxHealth);
            stamina = Mathf.Clamp(stamina, 0f, maxStamina);
            memory = Mathf.Clamp(memory, 0f, maxMemory);
        }
    }
}
