using System;
using Elyndor.Combat;
using Elyndor.UIFoundation;
using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>Abschnitt eines Angriffszyklus; treibt Animation, VFX und Tests.</summary>
    public enum EnemyAttackPhase
    {
        /// <summary>Nicht angesetzt; wartet auf Reichweite und Abklingzeit.</summary>
        Ready = 0,

        /// <summary>Sichtbares Ansetzen vor dem Zuschlagen.</summary>
        Telegraph = 1,

        /// <summary>Offenes Fenster nach dem Zuschlagen.</summary>
        Recovery = 2
    }

    /// <summary>Beschreibt einen ausgefuehrten Angriff fuer spaetere Animation und VFX.</summary>
    public readonly struct EnemyAttackInfo
    {
        public EnemyAttackInfo(
            Transform target, float damage, bool damageApplied, bool connected)
        {
            Target = target;
            Damage = damage;
            DamageApplied = damageApplied;
            Connected = connected;
        }

        public Transform Target { get; }
        public float Damage { get; }

        /// <summary>
        /// Ob tatsaechlich Schaden angekommen ist. Falsch, wenn das Ziel keine
        /// <see cref="PlayerVitals"/> hat — der Angriff selbst gilt trotzdem
        /// als ausgefuehrt.
        /// </summary>
        public bool DamageApplied { get; }

        /// <summary>
        /// War das Ziel im Moment des Zuschlagens noch in Reichweite? Falsch
        /// bedeutet Fehlschlag — der Spieler ist waehrend des Telegraphs
        /// ausgewichen.
        /// </summary>
        public bool Connected { get; }
    }

    /// <summary>
    /// Nahangriff der Gegnergrundlage. Ein Zyklus laeuft in drei Abschnitten:
    /// sichtbarer Telegraph, ein einzelner Schlag, danach ein offenes
    /// Erholungsfenster.
    ///
    /// Der Schaden wird erst am Ende des Telegraphs aufgeloest und nur dann,
    /// wenn das Ziel dann noch in Reichweite steht. Genau daran haengt, dass
    /// Ausweichen eine echte Antwort ist: wer waehrend des Ansetzens aus der
    /// Reichweite rollt, wird nicht getroffen. Ein Angriff, der seinen Schaden
    /// schon beim Ansetzen verteilt, waere nicht lesbar, sondern nur schnell.
    ///
    /// Schaden geht ueber den <see cref="PlayerDamageReceiver"/>, damit die
    /// Blockminderung an einer Stelle liegt und nicht in jedem Angreifer
    /// wiederholt wird. Fehlt der Empfaenger, bleibt der direkte Weg ueber
    /// <see cref="PlayerVitals.TakeDamage"/> bestehen.
    ///
    /// Alle Zeiten laufen ueber den uebergebenen Zeitschritt statt ueber
    /// <see cref="Time.time"/>, damit Tests sie ohne Wartezeit durchspielen
    /// koennen. Telegraph und Erholung sind vorbelegt mit 0 — ein Gegner ohne
    /// eigene Werte verhaelt sich damit unveraendert.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAttack : MonoBehaviour
    {
        [Header("Angriff")]
        [Min(0f)] [SerializeField] private float attackRange = 2f;
        [Min(0f)] [SerializeField] private float attackCooldown = 1.6f;
        [Min(0f)] [SerializeField] private float damage = 8f;

        [Header("Lesbarkeit")]
        [Tooltip("Sichtbares Ansetzen vor dem Schlag. 0 schlaegt sofort zu.")]
        [Min(0f)] [SerializeField] private float telegraphDuration;

        [Tooltip("Offenes Fenster nach dem Schlag, in dem nicht gehandelt wird.")]
        [Min(0f)] [SerializeField] private float recoveryDuration;

        private Transform cachedTarget;
        private PlayerVitals cachedVitals;
        private PlayerDamageReceiver cachedReceiver;
        private float phaseRemaining;

        /// <summary>Wird ausgeloest, sobald der Telegraph beginnt.</summary>
        public event Action TelegraphStarted;

        /// <summary>Wird ausgeloest, wenn ein Telegraph abgebrochen wurde.</summary>
        public event Action TelegraphAborted;

        /// <summary>Wird nach jedem ausgefuehrten Schlag ausgeloest.</summary>
        public event Action<EnemyAttackInfo> AttackPerformed;

        public float AttackRange => attackRange;
        public float TelegraphDuration => telegraphDuration;
        public float RecoveryDuration => recoveryDuration;
        public float CooldownRemaining { get; private set; }

        /// <summary>Aktueller Abschnitt des Angriffszyklus.</summary>
        public EnemyAttackPhase Phase { get; private set; } =
            EnemyAttackPhase.Ready;

        /// <summary>
        /// Steckt der Gegner in einem Angriff fest? Waehrend Telegraph und
        /// Erholung darf er sich nicht mehr seitlich loesen — sonst waere das
        /// Ansetzen folgenlos und der Konter unverdient.
        /// </summary>
        public bool IsCommitted =>
            Phase == EnemyAttackPhase.Telegraph ||
            Phase == EnemyAttackPhase.Recovery;

        /// <summary>Kann jetzt ein neuer Zyklus beginnen?</summary>
        public bool IsReady =>
            CooldownRemaining <= 0f && Phase == EnemyAttackPhase.Ready;

        /// <summary>
        /// Fortschritt des laufenden Abschnitts von 0 bis 1; ausserhalb von
        /// Telegraph und Erholung 0. Fuer Animation und die tuerkise
        /// Rissverstaerkung waehrend des Ansetzens.
        /// </summary>
        public float PhaseProgress01
        {
            get
            {
                float duration = Phase switch
                {
                    EnemyAttackPhase.Telegraph => telegraphDuration,
                    EnemyAttackPhase.Recovery => recoveryDuration,
                    _ => 0f
                };

                if (duration <= 0f)
                {
                    return 0f;
                }

                return Mathf.Clamp01(1f - phaseRemaining / duration);
            }
        }

        /// <summary>Anzahl ausgefuehrter Schlaege; vor allem fuer Tests und Telemetrie.</summary>
        public int PerformedAttacks { get; private set; }

        /// <summary>Anzahl Schlaege, die ins Leere gingen.</summary>
        public int MissedAttacks { get; private set; }

        /// <summary>Setzt Reichweite, Abklingzeit und Schaden.</summary>
        public void Configure(
            float newRange, float newCooldown, float newDamage)
        {
            attackRange = Mathf.Max(0f, newRange);
            attackCooldown = Mathf.Max(0f, newCooldown);
            damage = Mathf.Max(0f, newDamage);
        }

        /// <summary>Setzt zusaetzlich die Lesbarkeitsfenster.</summary>
        public void Configure(
            float newRange,
            float newCooldown,
            float newDamage,
            float newTelegraphDuration,
            float newRecoveryDuration)
        {
            Configure(newRange, newCooldown, newDamage);

            telegraphDuration = Mathf.Max(0f, newTelegraphDuration);
            recoveryDuration = Mathf.Max(0f, newRecoveryDuration);
        }

        /// <summary>
        /// Laesst Abklingzeit und Abschnitte laufen und setzt an, sobald
        /// Zustand, Reichweite und Abklingzeit es zulassen.
        /// </summary>
        public void Tick(
            EnemyFoundationState state, Transform target, float deltaTime)
        {
            float step = Mathf.Max(0f, deltaTime);

            if (CooldownRemaining > 0f)
            {
                CooldownRemaining = Mathf.Max(0f, CooldownRemaining - step);
            }

            switch (Phase)
            {
                case EnemyAttackPhase.Telegraph:
                    TickTelegraph(state, target, step);
                    return;

                case EnemyAttackPhase.Recovery:
                    TickRecovery(step);
                    return;

                default:
                    TryAttack(state, target);
                    return;
            }
        }

        /// <summary>
        /// Beginnt einen Zyklus, wenn alle Bedingungen erfuellt sind. Gibt
        /// zurueck, ob angesetzt wurde. Ohne Telegraph faellt das mit dem
        /// Schlag zusammen.
        /// </summary>
        public bool TryAttack(EnemyFoundationState state, Transform target)
        {
            if (state != EnemyFoundationState.Attack)
            {
                return false;
            }

            if (target == null || !IsReady)
            {
                return false;
            }

            if (PlanarDistanceTo(target.position) > attackRange)
            {
                return false;
            }

            if (telegraphDuration > 0f)
            {
                Phase = EnemyAttackPhase.Telegraph;
                phaseRemaining = telegraphDuration;
                TelegraphStarted?.Invoke();

                return true;
            }

            ResolveStrike(target);

            return true;
        }

        private void TickTelegraph(
            EnemyFoundationState state, Transform target, float step)
        {
            // Eine Trefferreaktion oder ein verlorenes Ziel bricht das
            // Ansetzen ab. Genau das ist die Belohnung fuer einen Treffer im
            // Telegraphfenster.
            if (state != EnemyFoundationState.Attack || target == null)
            {
                AbortTelegraph();
                return;
            }

            phaseRemaining -= step;

            if (phaseRemaining > 0f)
            {
                return;
            }

            ResolveStrike(target);
        }

        private void TickRecovery(float step)
        {
            phaseRemaining -= step;

            if (phaseRemaining > 0f)
            {
                return;
            }

            phaseRemaining = 0f;
            Phase = EnemyAttackPhase.Ready;
        }

        private void AbortTelegraph()
        {
            phaseRemaining = 0f;
            Phase = EnemyAttackPhase.Ready;
            TelegraphAborted?.Invoke();
        }

        /// <summary>
        /// Loest den Schlag auf. Die Reichweite wird hier erneut geprueft —
        /// getroffen wird nur, wer jetzt noch dort steht.
        /// </summary>
        private void ResolveStrike(Transform target)
        {
            CooldownRemaining = attackCooldown;
            PerformedAttacks++;

            bool connected =
                target != null &&
                PlanarDistanceTo(target.position) <= attackRange;

            if (!connected)
            {
                MissedAttacks++;
            }

            bool applied = connected && ApplyDamage(target);

            phaseRemaining = recoveryDuration;
            Phase = recoveryDuration > 0f
                ? EnemyAttackPhase.Recovery
                : EnemyAttackPhase.Ready;

            AttackPerformed?.Invoke(
                new EnemyAttackInfo(target, damage, applied, connected));
        }

        private bool ApplyDamage(Transform target)
        {
            if (damage <= 0f)
            {
                return false;
            }

            ResolveTargetComponents(target);

            // Der Empfaenger kennt die Blockregel; der Angreifer nicht.
            if (cachedReceiver != null)
            {
                cachedReceiver.TakeHit(damage, transform.position);
                return true;
            }

            if (cachedVitals == null)
            {
                return false;
            }

            cachedVitals.TakeDamage(damage);

            return true;
        }

        private void ResolveTargetComponents(Transform target)
        {
            if (cachedTarget == target &&
                (cachedReceiver != null || cachedVitals != null))
            {
                return;
            }

            cachedTarget = target;
            cachedReceiver = target.GetComponentInParent<PlayerDamageReceiver>();
            cachedVitals = target.GetComponentInParent<PlayerVitals>();
        }

        private float PlanarDistanceTo(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            offset.y = 0f;

            return offset.magnitude;
        }
    }
}
