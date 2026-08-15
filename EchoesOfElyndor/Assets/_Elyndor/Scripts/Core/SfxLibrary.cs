using Elyndor.Combat;
using Elyndor.Enemies;
using Elyndor.Memory;
using Elyndor.Puzzles;
using Elyndor.UI;
using UnityEngine;

namespace Elyndor.Core
{
    /// <summary>
    /// Welcher Hinweis zuletzt gespielt wurde. Bewusst ein eigener Begriff und
    /// kein String: an diesen Hinweisen haengt Spielbarkeit, und ein Test soll
    /// pruefen koennen, dass genau der richtige genau einmal kam.
    /// </summary>
    public enum SfxCue
    {
        None = 0,
        Narration = 1,
        MemoryStarted = 2,
        MemoryCompleted = 3,
        PlayerAttackLight = 4,
        PlayerAttackHeavy = 5,
        DummyHitLight = 6,
        DummyHitHeavy = 7,
        InventoryOpen = 8,
        InventoryClose = 9,
        Footstep = 10,

        /// <summary>Der Wurzelstreifer setzt zum Sprungbiss an.</summary>
        EnemyTelegraph = 11,

        /// <summary>Leichter Treffer am Gegner.</summary>
        EnemyHitLight = 12,

        /// <summary>Schwerer Treffer am Gegner; zugleich der Stagger.</summary>
        EnemyHitHeavy = 13,

        /// <summary>Der Gegner ist besiegt und beruhigt sich.</summary>
        EnemyDefeated = 14,

        /// <summary>Ein Treffer wurde geblockt.</summary>
        BlockedHit = 15,

        /// <summary>Ein Treffer kam ungeblockt durch.</summary>
        UnblockedHit = 16,

        /// <summary>Die Ankerstellung traegt.</summary>
        BridgeTensionHolds = 17,

        /// <summary>Die Ankerstellung traegt nicht.</summary>
        BridgeTensionSlack = 18,

        /// <summary>Der Wald antwortet auf Arens Verstehen.</summary>
        RegionRegenerated = 19
    }

    /// <summary>
    /// Zentrale Soundeffekt-Anbindung: hört auf die bestehenden Spiel-Events
    /// (Narration, Memory Watch, Kampf, Inventar) und spielt zugewiesene
    /// Clips. Zusätzlich distanzbasierte Schritte für den Spieler.
    /// Keine Kopplung in die Gegenrichtung — Systeme kennen kein Audio.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SfxLibrary : MonoBehaviour
    {
        [Header("Erzählung und Erinnerung")]
        [SerializeField] private AudioClip narrationClip;
        [SerializeField] private AudioClip memoryStartClip;
        [SerializeField] private AudioClip memoryCompleteClip;

        [Header("Kampf")]
        [SerializeField] private AudioClip attackLightClip;
        [SerializeField] private AudioClip attackHeavyClip;
        [SerializeField] private AudioClip hitLightClip;
        [SerializeField] private AudioClip hitHeavyClip;

        [Header("Wurzelstreifer")]
        [Tooltip("Eigener Ton vor dem Sprungbiss. Er endet, wenn der Biss " +
                 "kommt — der Telegraph bleibt damit auch dann lesbar, wenn " +
                 "die Silhouette gerade verdeckt steht.")]
        [SerializeField] private AudioClip enemyTelegraphClip;

        [SerializeField] private AudioClip enemyHitLightClip;

        [Tooltip("Zugleich der hoerbare Teil des schweren Staggers.")]
        [SerializeField] private AudioClip enemyHitHeavyClip;

        [Tooltip("Beruhigung statt Sterben; ohne harten Einschlag.")]
        [SerializeField] private AudioClip enemyDefeatedClip;

        [Header("Verteidigung")]
        [Tooltip("Gedaempft. Ein Block, der klingt wie ein voller Treffer, " +
                 "lehrt nichts.")]
        [SerializeField] private AudioClip blockedHitClip;

        [SerializeField] private AudioClip unblockedHitClip;

        [Header("Brueckenraetsel")]
        [Tooltip("Klarer Holz-/Seilton, wenn die Ankerstellung traegt.")]
        [SerializeField] private AudioClip bridgeTensionHoldsClip;

        [Tooltip("Dumpfes Stein-/Reibgeraeusch, wenn sie nicht traegt. " +
                 "Sagt nie, WELCHER Anker falsch steht.")]
        [SerializeField] private AudioClip bridgeTensionSlackClip;

        [Header("Regeneration")]
        [Tooltip("Leise. Der Wald antwortet, er kuendigt sich nicht an.")]
        [SerializeField] private AudioClip regionRegeneratedClip;

        [Header("Inventar")]
        [SerializeField] private AudioClip inventoryOpenClip;
        [SerializeField] private AudioClip inventoryCloseClip;

        [Header("Schritte")]
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private Transform footstepSource;
        [SerializeField] private float stepDistance = 1.9f;
        [SerializeField] private float footstepVolume = 0.35f;

        private AudioSource audioSource;
        private Vector3 lastFootstepPosition;
        private float accumulatedDistance;
        private int lastFootstepIndex = -1;

        /// <summary>Zuletzt ausgeloester Hinweis; fuer Tests und Telemetrie.</summary>
        public SfxCue LastCue { get; private set; } = SfxCue.None;

        /// <summary>Anzahl ausgeloester Hinweise; fuer Tests und Telemetrie.</summary>
        public int CueCount { get; private set; }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            if (footstepSource != null)
            {
                lastFootstepPosition = footstepSource.position;
            }
        }

        private void OnEnable()
        {
            NarrationEvents.MessageRequested += HandleNarration;
            MemorySite.AnyActivationStarted += HandleMemoryStarted;
            MemorySite.AnyActivationCompleted += HandleMemoryCompleted;
            PlayerCombat.AttackPerformed += HandleAttack;
            TrainingDummy.HitTaken += HandleHit;
            InventoryUI.Toggled += HandleInventoryToggled;

            EnemyAttack.AnyTelegraphStarted += HandleEnemyTelegraph;
            EnemyHealth.AnyDamaged += HandleEnemyDamaged;
            EnemyHealth.AnyDied += HandleEnemyDefeated;
            PlayerDamageReceiver.AnyDamageTaken += HandlePlayerDamaged;
            BridgePuzzle.AnyTensionEvaluated += HandleBridgeTension;
            Elyndor.World.FinsterwaldRegeneration.AnyRegionRegenerated +=
                HandleRegionRegenerated;
        }

        private void OnDisable()
        {
            NarrationEvents.MessageRequested -= HandleNarration;
            MemorySite.AnyActivationStarted -= HandleMemoryStarted;
            MemorySite.AnyActivationCompleted -= HandleMemoryCompleted;
            PlayerCombat.AttackPerformed -= HandleAttack;
            TrainingDummy.HitTaken -= HandleHit;
            InventoryUI.Toggled -= HandleInventoryToggled;

            EnemyAttack.AnyTelegraphStarted -= HandleEnemyTelegraph;
            EnemyHealth.AnyDamaged -= HandleEnemyDamaged;
            EnemyHealth.AnyDied -= HandleEnemyDefeated;
            PlayerDamageReceiver.AnyDamageTaken -= HandlePlayerDamaged;
            BridgePuzzle.AnyTensionEvaluated -= HandleBridgeTension;
            Elyndor.World.FinsterwaldRegeneration.AnyRegionRegenerated -=
                HandleRegionRegenerated;
        }

        private void Update()
        {
            UpdateFootsteps();
        }

        // Distanzbasierte Schritte: robust ohne Animations-Events.
        private void UpdateFootsteps()
        {
            if (footstepSource == null || footstepClips == null || footstepClips.Length == 0)
            {
                return;
            }

            Vector3 currentPosition = footstepSource.position;
            Vector3 delta = currentPosition - lastFootstepPosition;
            delta.y = 0f;
            lastFootstepPosition = currentPosition;

            // Teleport (Regionswechsel) nicht als Schritt werten.
            if (delta.magnitude > 2f)
            {
                accumulatedDistance = 0f;
                return;
            }

            accumulatedDistance += delta.magnitude;

            if (accumulatedDistance >= stepDistance)
            {
                accumulatedDistance = 0f;

                int index;
                do
                {
                    index = Random.Range(0, footstepClips.Length);
                } while (footstepClips.Length > 1 && index == lastFootstepIndex);

                lastFootstepIndex = index;
                Play(SfxCue.Footstep, footstepClips[index], footstepVolume);
            }
        }

        private void HandleNarration(string text, float duration) =>
            Play(SfxCue.Narration, narrationClip, 0.7f);

        private void HandleMemoryStarted(MemorySite site) =>
            Play(SfxCue.MemoryStarted, memoryStartClip, 0.9f);

        private void HandleMemoryCompleted(MemorySite site) =>
            Play(SfxCue.MemoryCompleted, memoryCompleteClip, 0.9f);

        private void HandleAttack(AttackType attackType)
        {
            bool heavy = attackType == AttackType.Heavy;

            Play(
                heavy ? SfxCue.PlayerAttackHeavy : SfxCue.PlayerAttackLight,
                heavy ? attackHeavyClip : attackLightClip,
                0.8f);
        }

        private void HandleHit(AttackType attackType)
        {
            bool heavy = attackType == AttackType.Heavy;

            Play(
                heavy ? SfxCue.DummyHitHeavy : SfxCue.DummyHitLight,
                heavy ? hitHeavyClip : hitLightClip,
                0.9f);
        }

        private void HandleInventoryToggled(bool open)
        {
            Play(
                open ? SfxCue.InventoryOpen : SfxCue.InventoryClose,
                open ? inventoryOpenClip : inventoryCloseClip,
                0.8f);
        }

        // ------------------------------------------------------------------
        // Lesbarkeit von Kampf und Raetsel
        // ------------------------------------------------------------------

        private void HandleEnemyTelegraph() =>
            Play(SfxCue.EnemyTelegraph, enemyTelegraphClip, 0.95f);

        /// <summary>
        /// Der toedliche Treffer bekommt bewusst keinen Trefferton: sonst
        /// laegen Treffer und Beruhigung im selben Moment uebereinander, und
        /// der Spieler hoerte zwei Ereignisse, wo eines stattfindet.
        /// </summary>
        private void HandleEnemyDamaged(EnemyDamageInfo info)
        {
            if (info.IsLethal)
            {
                return;
            }

            bool heavy = info.AttackType == AttackType.Heavy;

            Play(
                heavy ? SfxCue.EnemyHitHeavy : SfxCue.EnemyHitLight,
                heavy ? enemyHitHeavyClip : enemyHitLightClip,
                0.9f);
        }

        private void HandleEnemyDefeated() =>
            Play(SfxCue.EnemyDefeated, enemyDefeatedClip, 0.85f);

        private void HandlePlayerDamaged(PlayerDamageResult result)
        {
            bool blocked = result.Context == PlayerDamageContext.Blocked;

            Play(
                blocked ? SfxCue.BlockedHit : SfxCue.UnblockedHit,
                blocked ? blockedHitClip : unblockedHitClip,
                blocked ? 0.8f : 0.9f);
        }

        /// <summary>
        /// Traegt die Ankerstellung, klingt Holz und Seil; traegt sie nicht,
        /// reibt Stein. Der Ton haengt allein an diesem einen Ja oder Nein —
        /// er kann deshalb gar nicht verraten, welcher Anker falsch steht.
        /// </summary>
        private void HandleBridgeTension(bool holds)
        {
            Play(
                holds ? SfxCue.BridgeTensionHolds : SfxCue.BridgeTensionSlack,
                holds ? bridgeTensionHoldsClip : bridgeTensionSlackClip,
                0.85f);
        }

        /// <summary>
        /// Leiser als alles andere: der Wald antwortet, er kuendigt sich nicht
        /// an. Ein lauter Ton machte aus einer Beobachtung eine Belohnung.
        /// </summary>
        private void HandleRegionRegenerated() =>
            Play(SfxCue.RegionRegenerated, regionRegeneratedClip, 0.55f);

        /// <summary>
        /// Der Hinweis wird auch dann gezaehlt, wenn ihm noch kein Clip
        /// zugewiesen ist. Sonst waere ein fehlender Clip von einem gar nicht
        /// ausgeloesten Hinweis nicht zu unterscheiden — und genau das soll
        /// ein Test sehen koennen.
        /// </summary>
        private void Play(SfxCue cue, AudioClip clip, float volume)
        {
            LastCue = cue;
            CueCount++;

            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
