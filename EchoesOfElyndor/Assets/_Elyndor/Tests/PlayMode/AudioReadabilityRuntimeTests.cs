using System.Collections.Generic;
using Elyndor.Combat;
using Elyndor.Core;
using Elyndor.Enemies;
using Elyndor.Puzzles;
using Elyndor.UIFoundation;
using NUnit.Framework;
using UnityEngine;

namespace Elyndor.Tests
{
    /// <summary>
    /// Prüft die Hörbarkeit der Spielregeln (P1.11A).
    ///
    /// Zwei Fragen stehen im Mittelpunkt. Erstens: löst jeder Zustand genau
    /// den richtigen Hinweis aus, und zwar genau einmal? Ein doppelter Ton
    /// liest sich als zwei Ereignisse, wo eines stattfindet. Zweitens, und
    /// wichtiger: <b>verrät der Ton am Brückenrätsel, welcher Anker falsch
    /// steht?</b> Wenn ja, wäre das Rätsel gelöst, ohne dass jemand es
    /// verstanden hätte.
    ///
    /// Geprüft wird über <see cref="SfxLibrary.LastCue"/> und
    /// <see cref="SfxLibrary.CueCount"/>. Der Hinweis wird dort auch dann
    /// gezählt, wenn ihm kein Clip zugewiesen ist — sonst wäre ein fehlender
    /// Clip von einem gar nicht ausgelösten Hinweis nicht zu unterscheiden.
    /// </summary>
    public sealed class AudioReadabilityRuntimeTests
    {
        private const string PuzzleId = "audio_test_bridge";
        private const float Step = 0.1f;

        private readonly List<GameObject> spawned = new List<GameObject>();

        private SfxLibrary sfx;

        [SetUp]
        public void SetUp()
        {
            PuzzleSessionState.Forget(PuzzleId);

            GameObject host = new GameObject("SfxTestHost");
            host.SetActive(false);
            spawned.Add(host);

            host.AddComponent<AudioSource>();
            sfx = host.AddComponent<SfxLibrary>();

            host.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            PuzzleSessionState.Forget(PuzzleId);

            foreach (GameObject instance in spawned)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            spawned.Clear();
            sfx = null;
        }

        // ------------------------------------------------------------------
        // Wurzelstreifer
        // ------------------------------------------------------------------

        [Test]
        public void DerTelegraph_KlingtGenauEinmalProAnsatz()
        {
            EnemyController enemy = CreateWurzelstreifer();
            CreatePlayer(enemy, new Vector3(0f, 0f, 1.5f));

            TickUntil(enemy, () => sfx.CueCount > 0);

            Assert.That(
                sfx.LastCue,
                Is.EqualTo(SfxCue.EnemyTelegraph),
                "Der erste hoerbare Hinweis des Kampfes muss der Telegraph " +
                "sein.");
            Assert.That(
                sfx.CueCount,
                Is.EqualTo(1),
                "Ein Ansatz darf nur einmal klingen.");

            // Ueber den gesamten Telegraph hinweg darf nichts nachkommen.
            for (int i = 0; i < 6; i++)
            {
                enemy.Tick(Step);

                Assert.That(
                    sfx.CueCount,
                    Is.EqualTo(1),
                    "Waehrend des Ansetzens kam ein zweiter Ton.");
            }
        }

        [Test]
        public void LeichteUndSchwereTreffer_KlingenUnterschiedlich()
        {
            EnemyController enemy = CreateWurzelstreifer();

            enemy.Health.TakeDamage(5f, AttackType.Light, Vector3.zero);

            Assert.That(sfx.LastCue, Is.EqualTo(SfxCue.EnemyHitLight));
            Assert.That(sfx.CueCount, Is.EqualTo(1));

            enemy.Health.TakeDamage(5f, AttackType.Heavy, Vector3.zero);

            Assert.That(
                sfx.LastCue,
                Is.EqualTo(SfxCue.EnemyHitHeavy),
                "Der schwere Treffer ist zugleich der hoerbare Stagger und " +
                "muss sich vom leichten unterscheiden.");
            Assert.That(sfx.CueCount, Is.EqualTo(2));
        }

        [Test]
        public void DerToedlicheTreffer_KlingtNurAlsNiederlage()
        {
            EnemyController enemy = CreateWurzelstreifer();

            enemy.Health.TakeDamage(999f, AttackType.Heavy, Vector3.zero);

            Assert.That(
                sfx.CueCount,
                Is.EqualTo(1),
                "Treffer und Beruhigung wuerden sonst im selben Moment " +
                "uebereinanderliegen.");
            Assert.That(
                sfx.LastCue, Is.EqualTo(SfxCue.EnemyDefeated));
        }

        [Test]
        public void DieNiederlage_KlingtGenauEinmal()
        {
            EnemyController enemy = CreateWurzelstreifer();

            enemy.Health.TakeDamage(999f, AttackType.Heavy, Vector3.zero);
            enemy.Health.TakeDamage(999f, AttackType.Heavy, Vector3.zero);
            enemy.Health.Kill(Vector3.zero);

            Assert.That(
                sfx.CueCount,
                Is.EqualTo(1),
                "Weitere Treffer auf einen besiegten Gegner duerfen nicht " +
                "erneut klingen.");
        }

        // ------------------------------------------------------------------
        // Block
        // ------------------------------------------------------------------

        [Test]
        public void GeblockteUndUngeblockteTreffer_KlingenUnterschiedlich()
        {
            PlayerDamageReceiver receiver = CreateReceiver(out BlockStateStub block);

            block.IsBlocking = false;
            receiver.TakeHit(10f, Vector3.zero);

            Assert.That(sfx.LastCue, Is.EqualTo(SfxCue.UnblockedHit));
            Assert.That(sfx.CueCount, Is.EqualTo(1));

            block.IsBlocking = true;
            receiver.TakeHit(10f, Vector3.zero);

            Assert.That(
                sfx.LastCue,
                Is.EqualTo(SfxCue.BlockedHit),
                "Ein Block, der klingt wie ein voller Treffer, lehrt nichts.");
            Assert.That(sfx.CueCount, Is.EqualTo(2));
        }

        // ------------------------------------------------------------------
        // Brückenrätsel
        // ------------------------------------------------------------------

        [Test]
        public void TragendeStellung_KlingtNachHolzUndSeil()
        {
            BridgePuzzle puzzle = CreatePuzzle();
            SetNotches(puzzle, 1, 2, 3);

            int raised = CountTensionEvents(() => puzzle.CheckTension());

            Assert.That(sfx.LastCue, Is.EqualTo(SfxCue.BridgeTensionHolds));
            Assert.That(
                raised, Is.EqualTo(1),
                "Eine Pruefung darf nur einmal klingen.");
        }

        [Test]
        public void NichtTragendeStellung_KlingtNachStein()
        {
            BridgePuzzle puzzle = CreatePuzzle();
            SetNotches(puzzle, 3, 2, 1);

            int raised = CountTensionEvents(() => puzzle.CheckTension());

            Assert.That(sfx.LastCue, Is.EqualTo(SfxCue.BridgeTensionSlack));
            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void EineFehlgeschlageneFreigabe_KlingtGenauEinmal()
        {
            BridgePuzzle puzzle = CreatePuzzle();
            SetNotches(puzzle, 3, 2, 1);

            int raised = CountTensionEvents(() => puzzle.TryRelease());

            Assert.That(
                raised,
                Is.EqualTo(1),
                "Das Verkanten darf nicht doppelt klingen — einmal von der " +
                "Pruefung und einmal von der Freigabe.");
            Assert.That(sfx.LastCue, Is.EqualTo(SfxCue.BridgeTensionSlack));
        }

        [Test]
        public void EineGelungeneFreigabe_KlingtGenauEinmal()
        {
            BridgePuzzle puzzle = CreatePuzzle();
            SetNotches(puzzle, 1, 2, 3);

            bool released = false;
            int raised = CountTensionEvents(() => released = puzzle.TryRelease());

            Assert.That(released, Is.True);
            Assert.That(raised, Is.EqualTo(1));
            Assert.That(sfx.LastCue, Is.EqualTo(SfxCue.BridgeTensionHolds));
        }

        /// <summary>
        /// Der eigentliche Grund für dieses Testpaket.
        ///
        /// Wäre der Ton auch nur in Lautstärke, Auswahl oder Anzahl von der
        /// konkreten Fehlstellung abhängig, könnte ein Spieler die Anker
        /// einzeln durchhören und das Rätsel lösen, ohne es zu verstehen.
        /// Deshalb müssen alle 26 falschen Kombinationen exakt dasselbe
        /// erzeugen.
        /// </summary>
        [Test]
        public void AlleFalschenStellungen_KlingenVoellingGleich()
        {
            BridgePuzzle puzzle = CreatePuzzle();

            int checkedCombinations = 0;

            for (int south = 1; south <= 3; south++)
            for (int side = 1; side <= 3; side++)
            for (int north = 1; north <= 3; north++)
            {
                if (BridgePuzzleRules.IsCorrect(south, side, north))
                {
                    continue;
                }

                SetNotches(puzzle, south, side, north);

                int raised = CountTensionEvents(() => puzzle.CheckTension());

                Assert.That(
                    sfx.LastCue,
                    Is.EqualTo(SfxCue.BridgeTensionSlack),
                    $"Kombination {south}-{side}-{north} klingt anders als " +
                    "die uebrigen falschen.");
                Assert.That(
                    raised,
                    Is.EqualTo(1),
                    $"Kombination {south}-{side}-{north} erzeugt eine andere " +
                    "Anzahl Toene — auch das waere ein Hinweis.");

                checkedCombinations++;
            }

            Assert.That(
                checkedCombinations,
                Is.EqualTo(BridgePuzzleRules.TotalCombinations - 1),
                "Es muessen 26 falsche Kombinationen geprueft worden sein.");
        }

        [Test]
        public void DerHinweisTraegtKeineAnkerinformation()
        {
            // Der Vertrag selbst: das Ereignis fuehrt genau ein bool. Was
            // nicht uebergeben wird, kann nicht hoerbar werden.
            var raised = new List<bool>();

            void Listener(bool holds) => raised.Add(holds);

            BridgePuzzle.AnyTensionEvaluated += Listener;

            try
            {
                BridgePuzzle puzzle = CreatePuzzle();

                SetNotches(puzzle, 1, 2, 1);
                puzzle.CheckTension();

                SetNotches(puzzle, 1, 1, 3);
                puzzle.CheckTension();

                SetNotches(puzzle, 3, 3, 3);
                puzzle.CheckTension();

                Assert.That(raised, Is.EqualTo(new[] { false, false, false }),
                    "Drei verschiedene Fehlstellungen muessen dieselbe " +
                    "Meldung erzeugen.");
            }
            finally
            {
                BridgePuzzle.AnyTensionEvaluated -= Listener;
            }
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        private sealed class BlockStateStub : MonoBehaviour, IPlayerBlockState
        {
            public bool IsBlocking { get; set; }
        }

        /// <summary>
        /// Zaehlt die Spannungsmeldungen einer Handlung am Ereignis selbst.
        ///
        /// Nicht ueber <see cref="SfxLibrary.CueCount"/>: die Pruefung schreibt
        /// zusaetzlich eine Erzaehlmeldung, und die bedient dieselbe Bibliothek.
        /// Die Frage lautet aber, ob die <em>Spannungsmeldung</em> genau einmal
        /// kommt.
        /// </summary>
        private static int CountTensionEvents(System.Action action)
        {
            int raised = 0;

            void Listener(bool holds) => raised++;

            BridgePuzzle.AnyTensionEvaluated += Listener;

            try
            {
                action();
            }
            finally
            {
                BridgePuzzle.AnyTensionEvaluated -= Listener;
            }

            return raised;
        }

        private EnemyController CreateWurzelstreifer()
        {
            GameObject root = new GameObject("AudioTestWurzelstreifer");
            root.SetActive(false);
            spawned.Add(root);

            root.AddComponent<EnemyStateMachine>();
            root.AddComponent<EnemyHealth>();
            root.AddComponent<EnemyPerception>();
            root.AddComponent<EnemyMovement>();
            root.AddComponent<EnemyAttack>();
            root.AddComponent<EnemyHitReaction>();
            root.AddComponent<EnemyRetreat>();
            root.AddComponent<EnemyLeash>();
            EnemyController enemy = root.AddComponent<EnemyController>();
            root.AddComponent<Wurzelstreifer>();

            root.SetActive(true);
            enemy.enabled = false;

            return enemy;
        }

        private void CreatePlayer(EnemyController enemy, Vector3 position)
        {
            GameObject player = new GameObject("AudioTestPlayer");
            player.SetActive(false);
            spawned.Add(player);
            player.transform.position = position;
            player.AddComponent<PlayerVitals>();
            player.SetActive(true);

            enemy.Perception.SetTarget(player.transform);
            Physics.SyncTransforms();
        }

        private PlayerDamageReceiver CreateReceiver(out BlockStateStub block)
        {
            GameObject host = new GameObject("AudioTestReceiver");
            host.SetActive(false);
            spawned.Add(host);

            host.AddComponent<PlayerVitals>();
            block = host.AddComponent<BlockStateStub>();
            PlayerDamageReceiver receiver =
                host.AddComponent<PlayerDamageReceiver>();

            host.SetActive(true);

            return receiver;
        }

        private BridgePuzzle CreatePuzzle()
        {
            GameObject root = new GameObject("AudioTestBridge");
            root.SetActive(false);
            spawned.Add(root);

            BridgePuzzle puzzle = root.AddComponent<BridgePuzzle>();
            SetPrivateField(puzzle, "puzzleId", PuzzleId);

            BridgeAnchor south = CreateAnchor(root, BridgeAnchorId.SouthDeep);
            BridgeAnchor side = CreateAnchor(root, BridgeAnchorId.Side);
            BridgeAnchor north = CreateAnchor(root, BridgeAnchorId.North);

            GameObject log = new GameObject("Stamm");
            log.transform.SetParent(root.transform, false);

            GameObject pose = new GameObject("Zielpose");
            pose.transform.SetParent(root.transform, false);

            SetPrivateField(puzzle, "fallenLog", log.transform);
            SetPrivateField(puzzle, "deployedPose", pose.transform);
            SetPrivateArray(
                puzzle, "anchors", new Object[] { south, side, north });

            root.SetActive(true);

            puzzle.SetPlayerInZone(true);
            puzzle.NotifyEchoObserved();

            return puzzle;
        }

        private static BridgeAnchor CreateAnchor(
            GameObject root, BridgeAnchorId id)
        {
            GameObject stone = new GameObject("Anker_" + id);
            stone.transform.SetParent(root.transform, false);

            SphereCollider trigger = stone.AddComponent<SphereCollider>();
            trigger.isTrigger = true;

            BridgeAnchor anchor = stone.AddComponent<BridgeAnchor>();
            SetPrivateField(anchor, "anchorId", (int)id);

            return anchor;
        }

        private static void SetNotches(
            BridgePuzzle puzzle, int south, int side, int north)
        {
            foreach (BridgeAnchor anchor in
                     puzzle.GetComponentsInChildren<BridgeAnchor>(true))
            {
                int notches = anchor.AnchorId switch
                {
                    BridgeAnchorId.SouthDeep => south,
                    BridgeAnchorId.Side => side,
                    _ => north
                };

                anchor.SetSetting(
                    BridgePuzzleRules.SettingForNotches(notches), true);
            }
        }

        private static void TickUntil(
            EnemyController enemy, System.Func<bool> condition)
        {
            for (int i = 0; i < 400; i++)
            {
                if (condition())
                {
                    return;
                }

                enemy.Tick(Step);
            }

            Assert.Fail("Die erwartete Bedingung trat nicht ein.");
        }

        private static void SetPrivateField(
            Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Feld '{fieldName}' fehlt.");
            field.SetValue(target, value);
        }

        private static void SetPrivateArray(
            Object target, string fieldName, Object[] values)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Feld '{fieldName}' fehlt.");

            System.Array typed = System.Array.CreateInstance(
                field.FieldType.GetElementType()!, values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                typed.SetValue(values[i], i);
            }

            field.SetValue(target, typed);
        }
    }
}
