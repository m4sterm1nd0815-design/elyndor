using UnityEngine;

namespace Elyndor.Combat
{
    /// <summary>Alles, was Treffer einstecken kann (Übungspuppe, später Tiere und Vergessene).</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, AttackType attackType, Vector3 sourcePosition);
    }

    public enum AttackType
    {
        Light,
        Heavy
    }
}
