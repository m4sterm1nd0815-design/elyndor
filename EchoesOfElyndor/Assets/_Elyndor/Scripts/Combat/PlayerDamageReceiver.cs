using System;
using Elyndor.UIFoundation;
using UnityEngine;

namespace Elyndor.Combat
{
    /// <summary>
    /// In welcher Lage hat ein Treffer den Spieler erreicht? Bewusst als
    /// eigener Begriff und nicht als bool: der Kontext waechst spaeter um
    /// weitere Faelle (Rueckenangriff, Umgebungsschaden), und ein bool haette
    /// dafuer an jeder Aufrufstelle aufgebrochen werden muessen.
    /// </summary>
    public enum PlayerDamageContext
    {
        /// <summary>Ungeschuetzt getroffen.</summary>
        Normal = 0,

        /// <summary>Waehrend eines aktiven Blocks getroffen.</summary>
        Blocked = 1
    }

    /// <summary>Ergebnis eines eingehenden Treffers, fuer Feedback und Tests.</summary>
    public readonly struct PlayerDamageResult
    {
        public PlayerDamageResult(
            float rawAmount,
            float appliedAmount,
            PlayerDamageContext context,
            Vector3 sourcePosition)
        {
            RawAmount = rawAmount;
            AppliedAmount = appliedAmount;
            Context = context;
            SourcePosition = sourcePosition;
        }

        /// <summary>Schaden vor jeder Minderung.</summary>
        public float RawAmount { get; }

        /// <summary>Tatsaechlich abgezogener Gesundheitsschaden.</summary>
        public float AppliedAmount { get; }

        public PlayerDamageContext Context { get; }
        public Vector3 SourcePosition { get; }

        /// <summary>Wurde ueberhaupt etwas gemindert?</summary>
        public bool WasMitigated => AppliedAmount < RawAmount;
    }

    /// <summary>
    /// Meldet, ob der Spieler gerade blockt. Als Schnittstelle und nicht als
    /// direkter Verweis auf <see cref="PlayerCombat"/>, damit der Empfaenger
    /// ohne Waffe, ohne Eingabe und ohne Animator geprueft werden kann.
    /// </summary>
    public interface IPlayerBlockState
    {
        bool IsBlocking { get; }
    }

    /// <summary>
    /// Einzige Eintrittsstelle fuer Schaden am Spieler. Vorher hat jeder
    /// Angreifer <see cref="PlayerVitals.TakeDamage"/> direkt aufgerufen; damit
    /// haette die Blockregel in jedem Gegner einzeln nachgebaut werden muessen
    /// und waere beim naechsten Angreifer wieder vergessen worden.
    ///
    /// Die Minderung entscheidet der Verteidiger, nicht der Angreifer: ein
    /// Gegner soll nicht wissen muessen, ob gerade geblockt wird.
    ///
    /// Bricht die Ausdauer zusammen, endet der Block bereits in
    /// <see cref="PlayerCombat"/>. Der naechste Treffer kommt dann ohne
    /// Sonderfall hier als <see cref="PlayerDamageContext.Normal"/> an.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerVitals))]
    public sealed class PlayerDamageReceiver : MonoBehaviour
    {
        [Header("Block")]
        [Tooltip("Anteil des Gesundheitsschadens, den ein aktiver Block " +
                 "wegnimmt. 0,7 bedeutet 30 % Restschaden. Vorlaeufiger " +
                 "Balancingwert.")]
        [Range(0f, 1f)] [SerializeField] private float blockDamageReduction = 0.7f;

        [SerializeField] private PlayerVitals vitals;

        private IPlayerBlockState blockState;

        /// <summary>
        /// Dasselbe Objekt noch einmal als <see cref="MonoBehaviour"/>. Ueber
        /// die Schnittstelle allein liesse sich nicht erkennen, ob die
        /// Komponente zerstoert wurde: <c>!= null</c> auf einem
        /// Schnittstellenverweis ist ein gewoehnlicher Referenzvergleich und
        /// umgeht Unitys Lebensdauerpruefung.
        /// </summary>
        private MonoBehaviour blockStateOwner;

        /// <summary>Wird nach jedem eingehenden Treffer ausgeloest.</summary>
        public event Action<PlayerDamageResult> DamageTaken;

        /// <summary>
        /// Dasselbe statisch, damit die zentrale <c>SfxLibrary</c> den
        /// Unterschied zwischen einem durchgekommenen und einem geblockten
        /// Treffer hoerbar machen kann. Ein Block, der genauso klingt wie ein
        /// voller Treffer, lehrt dem Spieler nichts.
        /// </summary>
        public static event Action<PlayerDamageResult> AnyDamageTaken;

        public float BlockDamageReduction => blockDamageReduction;

        /// <summary>Blockt der Spieler in diesem Moment?</summary>
        public bool IsBlocking
        {
            get
            {
                ResolveBlockState();

                return blockStateOwner != null &&
                       blockState != null &&
                       blockState.IsBlocking;
            }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        /// <summary>Setzt die Blockminderung, etwa fuer Balancing oder Tests.</summary>
        public void Configure(float newBlockDamageReduction)
        {
            blockDamageReduction = Mathf.Clamp01(newBlockDamageReduction);
        }

        /// <summary>
        /// Nimmt einen Treffer entgegen, bestimmt seinen Kontext und wendet den
        /// geminderten Schaden an. Gibt das Ergebnis zurueck, damit Angreifer,
        /// Feedback und Tests dieselbe Wahrheit sehen.
        /// </summary>
        public PlayerDamageResult TakeHit(float amount, Vector3 sourcePosition)
        {
            ResolveReferences();

            float raw = Mathf.Max(0f, amount);

            PlayerDamageContext context = IsBlocking
                ? PlayerDamageContext.Blocked
                : PlayerDamageContext.Normal;

            float applied = context == PlayerDamageContext.Blocked
                ? raw * (1f - Mathf.Clamp01(blockDamageReduction))
                : raw;

            if (applied > 0f && vitals != null)
            {
                vitals.TakeDamage(applied);
            }

            var result = new PlayerDamageResult(
                raw, applied, context, sourcePosition);

            DamageTaken?.Invoke(result);
            AnyDamageTaken?.Invoke(result);

            return result;
        }

        private void ResolveReferences()
        {
            if (vitals == null)
            {
                vitals = GetComponent<PlayerVitals>();
            }

            ResolveBlockState();
        }

        /// <summary>
        /// Der Blockzustand darf fehlen: ohne <see cref="PlayerCombat"/> — etwa
        /// in einem Laufzeittest — wird schlicht nie geblockt.
        /// </summary>
        private void ResolveBlockState()
        {
            if (blockStateOwner != null)
            {
                return;
            }

            blockState = GetComponentInChildren<IPlayerBlockState>(true);
            blockStateOwner = blockState as MonoBehaviour;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            AnyDamageTaken = null;
        }
    }
}
