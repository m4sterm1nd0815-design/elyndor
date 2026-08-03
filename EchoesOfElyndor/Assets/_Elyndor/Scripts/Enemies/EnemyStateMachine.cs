using System;
using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>
    /// Haelt den aktuellen Zustand eines Gegners und laesst nur Wechsel zu,
    /// die <see cref="EnemyStateRules"/> erlaubt. Der Zustand ist oeffentlich
    /// lesbar, damit Tests, spaetere Animationen und UI daran andocken
    /// koennen, ohne die Logik zu kennen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyStateMachine : MonoBehaviour
    {
        /// <summary>Jeder Gegner beginnt ruhend.</summary>
        public EnemyFoundationState Current { get; private set; } =
            EnemyFoundationState.Idle;

        /// <summary>Alter und neuer Zustand nach einem erfolgten Wechsel.</summary>
        public event Action<EnemyFoundationState, EnemyFoundationState> StateChanged;

        public bool IsDead => Current == EnemyFoundationState.Dead;

        /// <summary>Kann der Gegner sich in diesem Zustand bewegen?</summary>
        public bool AllowsMovement =>
            Current != EnemyFoundationState.Hurt &&
            Current != EnemyFoundationState.Attack &&
            Current != EnemyFoundationState.Dead;

        /// <summary>Darf der Gegner in diesem Zustand angreifen?</summary>
        public bool AllowsAttack =>
            Current != EnemyFoundationState.Hurt &&
            Current != EnemyFoundationState.Dead;

        /// <summary>
        /// Setzt den Zustand, sofern der Wechsel erlaubt ist. Gibt an, ob
        /// tatsaechlich gewechselt wurde — ein Wechsel auf den bereits
        /// aktiven Zustand gilt als nicht erfolgt und loest kein Ereignis aus.
        /// </summary>
        public bool TrySetState(EnemyFoundationState next)
        {
            if (!EnemyStateRules.CanTransition(Current, next))
            {
                return false;
            }

            EnemyFoundationState previous = Current;
            Current = next;
            StateChanged?.Invoke(previous, next);

            return true;
        }

        /// <summary>Direkter Weg in den Endzustand; aus jedem lebenden Zustand erlaubt.</summary>
        public bool Kill()
        {
            return TrySetState(EnemyFoundationState.Dead);
        }
    }
}
