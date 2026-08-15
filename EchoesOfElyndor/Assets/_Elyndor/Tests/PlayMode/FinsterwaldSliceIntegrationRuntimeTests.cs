using System;
using System.Collections;
using System.Collections.Generic;
using Elyndor.Companion;
using Elyndor.Enemies;
using Elyndor.Interaction;
using Elyndor.Inventory;
using Elyndor.Memory;
using Elyndor.Narration;
using Elyndor.Puzzles;
using Elyndor.UIFoundation;
using Elyndor.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// P1.14 — der Kernbogen des Vertical Slice als ein zusammenhaengender
    /// Ablauf, im echten Finsterwald.
    ///
    /// Die uebrigen Suiten pruefen je ein System fuer sich: der Kampf hier,
    /// das Raetsel dort, die Regeneration wieder woanders. Jedes davon kann
    /// gruen sein, waehrend der Bogen als Ganzes nicht traegt — weil eine
    /// Station die naechste nicht freischaltet, weil eine spaetere einen
    /// notwendigen Schritt ueberspringt, oder weil zwei Systeme denselben
    /// Zustand verschieden lesen.
    ///
    /// Deshalb faehrt diese Suite den Weg, den ein Spieler faehrt:
    ///
    ///   Start und Bewegung -> Wurzelstreifer -> Resonanzzone -> Memory Watch
    ///   -> Brueckenraetsel -> Regeneration
    ///
    /// <b>Echte Pfade, wo immer es geht.</b> Der Kampf laeuft ueber simulierte
    /// Geraete und die ausgelieferte Tastenbelegung. Jede Interaktion laeuft
    /// ueber den <see cref="InteractionDetector"/>: Aren wird in den Trigger
    /// des Objekts gestellt, es muss von selbst zum aktiven Ziel werden, und
    /// erst dann faellt der Tastendruck. Kein Test ruft <c>Interact</c>
    /// direkt auf, und keiner setzt einen Raetselzustand von Hand.
    ///
    /// <b>Was doch direkt gesetzt wird</b>, und warum:
    ///
    /// <list type="bullet">
    /// <item><description>
    /// <c>MemorySessionState.Forget</c> und <c>RegionRegenerationState.Forget</c>
    /// im Setup. Beide halten Sitzungszustand ueber Szenenwechsel hinweg —
    /// ohne Zuruecksetzen pruefte der zweite Test auf dem Ergebnis des
    /// ersten. Das setzt keinen Fortschritt, es nimmt ihn zurueck.
    /// </description></item>
    /// <item><description>
    /// Arens Position. Ein Test, der die Strecke ablaufen laesst, misst die
    /// Wegfindung, nicht den Bogen. Bewegung selbst deckt
    /// <c>FinsterwaldStartGuidanceRuntimeTests</c> ab.
    /// </description></item>
    /// </list>
    ///
    /// <b>Was diese Suite nicht kann:</b> beurteilen, ob das Raetsel
    /// herleitbar ist, ob die Toene tragen oder ob die Regeneration wirkt.
    /// Das steht in <c>FINSTERWALD_HUMAN_ACCEPTANCE.md</c> und bleibt offen.
    /// </summary>
    public sealed class FinsterwaldSliceIntegrationRuntimeTests : InputTestFixture
    {
        private const string SceneName = "Finsterwald";
        private const string MemorySiteId = "finsterwald_bruecke_01";
        private const string PuzzleId = "finsterwald_bridge_memory_puzzle_v1";
        private const string RegionStateId = "finsterwald_bruecke_regeneration";

        /// <summary>Zeitschranken in Sekunden — nie in Frames, siehe QA_GATES.md.</summary>
        private const float ArcTimeout = 20f;
        private const float SettleTimeout = 5f;

        /// <summary>Notbremse gegen eine stehende Uhr.</summary>
        private const int FrameBrake = 100000;

        private readonly List<string> consoleErrors = new List<string>();

        private Keyboard keyboard;
        private Mouse mouse;

        private GameObject player;
        private CharacterController playerBody;
        private InteractionDetector detector;
        private PlayerVitals vitals;

        private BridgePuzzle puzzle;
        private FinsterwaldRegeneration regeneration;
        private MemorySite memorySite;

        public override void Setup()
        {
            base.Setup();

            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();

            ForgetSessionProgress();

            consoleErrors.Clear();
            Application.logMessageReceived += CollectConsoleError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= CollectConsoleError;
            base.TearDown();
        }

        /// <summary>
        /// Nimmt allen Sitzungsfortschritt zurueck.
        ///
        /// Alle drei Zustaende ueberleben einen Szenenwechsel — das ist ihr
        /// Zweck. Fuer eine Testreihe heisst das: ohne Zuruecknehmen prueft
        /// jeder Test auf dem Ergebnis seines Vorgaengers. Genau das ist beim
        /// ersten Lauf passiert; der Wald hatte bereits geantwortet, bevor der
        /// Bogen ueberhaupt begann.
        /// </summary>
        private static void ForgetSessionProgress()
        {
            MemorySessionState.Forget(MemorySiteId);
            PuzzleSessionState.Forget(PuzzleId);
            RegionRegenerationState.Forget(RegionStateId);
        }

        private void CollectConsoleError(
            string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error ||
                type == LogType.Exception ||
                type == LogType.Assert)
            {
                consoleErrors.Add($"{type}: {condition}");
            }
        }

        // ==================================================================
        // A und B — Reihenfolge und kein Softlock
        // ==================================================================

        [UnityTest]
        public IEnumerator DerKernbogen_LaeuftVonAnfangBisEnde()
        {
            yield return LoadSlice();

            // --- Station 1: die Welt ist noch unveraendert -----------------
            Assert.That(
                regeneration.IsRegenerated,
                Is.False,
                "Der Wald hat geantwortet, bevor irgendetwas geschehen ist.");

            // --- Station 2: der Wurzelstreifer ----------------------------
            EnemyController enemy = RequireSingleEnemy();
            yield return DefeatEnemy(enemy);

            Assert.That(
                enemy.Health.IsDead,
                Is.True,
                "Die Begegnung laesst sich nicht abschliessen — der Bogen " +
                "endet hier fuer jeden Spieler.");

            // --- Station 3: die Resonanzzone ------------------------------
            yield return EnterResonanceZone();

            Assert.That(
                puzzle.WatchAvailable,
                Is.True,
                "Die Watch meldet an der Bruecke keine Bereitschaft.");

            // --- Station 4: die Erinnerung schaltet das Raetsel frei ------
            Assert.That(
                puzzle.CanTurnAnchors,
                Is.False,
                "Die Anker liessen sich drehen, bevor Aren die Erinnerung " +
                "gesehen hat.");

            yield return ActivateMemorySite();

            Assert.That(
                puzzle.CanTurnAnchors,
                Is.True,
                "Die Erinnerung hat das Raetsel nicht freigeschaltet.");

            // --- Station 5: das Raetsel -----------------------------------
            yield return SolvePuzzle();

            Assert.That(
                puzzle.IsSolved,
                Is.True,
                "Das Raetsel laesst sich nicht abschliessen.");

            // --- Station 6: die Antwort des Waldes ------------------------
            yield return WaitUntil(
                () => regeneration.IsRegenerated,
                "Beide Bedingungen sind erfuellt, der Wald antwortet nicht.");

            Assert.That(
                consoleErrors,
                Is.Empty,
                "Der Durchlauf hat rote Meldungen erzeugt:\n" +
                string.Join("\n", consoleErrors));
        }

        [UnityTest]
        public IEnumerator DasRaetsel_BleibtVorDerErinnerungVerschlossen()
        {
            yield return LoadSlice();
            yield return EnterResonanceZone();

            Assert.That(
                puzzle.CanTurnAnchors,
                Is.False,
                "Die Anker sind ohne Erinnerung drehbar.");
            Assert.That(
                puzzle.CanRelease,
                Is.False,
                "Der Stamm liesse sich ohne Erinnerung freigeben.");

            // Der Seilbock ist Absicht: er darf befragt werden, sobald
            // ueberhaupt konfiguriert wird — er verraet nichts.
            foreach (BridgeAnchor anchor in AllAnchors())
            {
                int before = anchor.Setting;
                yield return TryInteractWith(anchor, expectReachable: false);

                Assert.That(
                    anchor.Setting,
                    Is.EqualTo(before),
                    $"Anker {anchor.AnchorId} liess sich vorzeitig drehen.");
            }
        }

        // ==================================================================
        // C — der Wurzelstreifer
        // ==================================================================

        [UnityTest]
        public IEnumerator DerWurzelstreifer_IstEinLehrstueckUndKeinRudel()
        {
            yield return LoadSlice();

            EnemyController enemy = RequireSingleEnemy();
            Vector3 home = enemy.Leash.Home;

            yield return FacePlayerToEnemy(enemy, distance: 6f);

            yield return WaitUntil(
                () => enemy.State != EnemyFoundationState.Idle,
                "Der Wurzelstreifer bemerkt Aren nicht.");

            Assert.That(
                enemy.State,
                Is.EqualTo(EnemyFoundationState.Alert),
                "Er handelt, bevor er beobachtet hat.");

            yield return WaitUntil(
                () => enemy.Attack.Phase == EnemyAttackPhase.Telegraph ||
                      enemy.State == EnemyFoundationState.Chase ||
                      enemy.State == EnemyFoundationState.Attack,
                "Nach der Verdachtsphase handelt er nicht.");

            yield return DefeatEnemy(enemy);

            Assert.That(
                enemy.Health.IsDead, Is.True, "Er laesst sich nicht besiegen.");

            Assert.That(
                Vector3.Distance(home, enemy.Leash.Home),
                Is.LessThan(0.01f),
                "Die Ortsbindung hat ihren Heimatpunkt verloren.");
        }

        // ==================================================================
        // D — das Raetsel
        // ==================================================================

        [UnityTest]
        public IEnumerator DasRaetsel_IstOhneLinkUndOhneLoreLoesbar()
        {
            yield return LoadSlice();

            // Link vollstaendig aus dem Spiel nehmen.
            int removedCompanions = 0;
            foreach (LinkCompanion link in
                     UnityEngine.Object.FindObjectsByType<LinkCompanion>(FindObjectsInactive.Include))
            {
                link.gameObject.SetActive(false);
                removedCompanions++;
            }

            Assert.That(
                removedCompanions,
                Is.GreaterThan(0),
                "Kein Link in der Szene — der Test pruefte nichts.");

            // Jede optionale Lore ebenfalls.
            foreach (CataloguedExaminable lore in
                     UnityEngine.Object.FindObjectsByType<CataloguedExaminable>(FindObjectsInactive.Include))
            {
                lore.gameObject.SetActive(false);
            }

            yield return EnterResonanceZone();
            yield return ActivateMemorySite();
            yield return SolvePuzzle();

            Assert.That(
                puzzle.IsSolved,
                Is.True,
                "Ohne Link und ohne optionale Lore ist das Raetsel nicht " +
                "mehr loesbar — dann traegt es seine eigene Loesung nicht.");
        }

        [UnityTest]
        public IEnumerator EineFalscheKombination_SetztFolgenlosZurueck()
        {
            yield return LoadSlice();
            yield return EnterResonanceZone();
            yield return ActivateMemorySite();

            float healthBefore = vitals.Health;

            // Eine bewusst falsche Stellung — und zwar eine, die jeden Anker
            // wirklich bewegt. Die Loesung ist 1/2/3; „alle auf eine Kerbe"
            // waere zwar falsch, liesse den Sued-Anker aber unberuehrt, und
            // das Raetsel verlaesst den Zustand `EchoObserved` erst, wenn
            // tatsaechlich gedreht wurde. Der Stamm liesse sich dann gar
            // nicht freigeben, und der Test pruefte die Umkehrbarkeit nie.
            yield return SetAnchors(3, 1, 2);

            Assert.That(
                puzzle.IsConfigurationCorrect(),
                Is.False,
                "Die absichtlich falsche Stellung gilt als richtig.");
            Assert.That(
                puzzle.CanRelease,
                Is.True,
                "Nach dem Drehen laesst sich der Stamm nicht freigeben — " +
                "ohne Freigabe gibt es keinen Fehlversuch zu pruefen.");

            yield return InteractWith(FindPart(BridgePartRole.Release));

            yield return WaitUntil(
                () => puzzle.CanTurnAnchors,
                "Nach dem Fehlversuch bleibt das Raetsel gesperrt — " +
                "das waere ein Softlock.");

            Assert.That(
                puzzle.IsSolved, Is.False, "Ein Fehlversuch hat geloest.");
            Assert.That(
                vitals.Health,
                Is.EqualTo(healthBefore),
                "Der Fehlversuch hat Aren Leben gekostet.");

            // Und danach traegt der richtige Weg weiterhin.
            yield return SolvePuzzle();

            Assert.That(
                puzzle.IsSolved,
                Is.True,
                "Nach einem Fehlversuch ist das Raetsel nicht mehr loesbar.");
        }

        [UnityTest]
        public IEnumerator KeinHinweis_NenntDenFalschenAnker()
        {
            yield return LoadSlice();
            yield return EnterResonanceZone();
            yield return ActivateMemorySite();

            List<bool> verdicts = new List<bool>();

            void Collect(bool holds) => verdicts.Add(holds);

            BridgePuzzle.AnyTensionEvaluated += Collect;

            try
            {
                // Genau ein Anker steht falsch. Wuerde die Rueckmeldung
                // verraten welcher, waere das Raetsel in drei Versuchen
                // erledigt statt in siebenundzwanzig.
                yield return SetAnchors(1, 2, 1);
                yield return InteractWith(FindPart(BridgePartRole.TensionCheck));

                Assert.That(
                    verdicts,
                    Is.Not.Empty,
                    "Der Seilbock hat gar nichts gemeldet.");
                Assert.That(
                    verdicts[verdicts.Count - 1],
                    Is.False,
                    "Eine falsche Stellung meldet, dass sie traegt.");
            }
            finally
            {
                BridgePuzzle.AnyTensionEvaluated -= Collect;
            }

            // Die Rueckmeldung ist ein einzelnes Ja/Nein. Es gibt keinen
            // oeffentlichen Weg, aus ihr den falschen Anker abzulesen.
            Assert.That(
                typeof(BridgePuzzle).GetMethod("WrongAnchor"),
                Is.Null,
                "Es gibt eine Schnittstelle, die den falschen Anker nennt.");
        }

        // ==================================================================
        // E und F — Erinnerung, Lore und die Antwort des Waldes
        // ==================================================================

        [UnityTest]
        public IEnumerator DieRegeneration_VerlangtBeideBedingungen()
        {
            yield return LoadSlice();
            yield return EnterResonanceZone();
            yield return ActivateMemorySite();
            yield return SolvePuzzle();

            yield return WaitUntil(
                () => regeneration.IsRegenerated,
                "Mit beiden Bedingungen antwortet der Wald nicht.");

            // Gegenprobe von vorn: allen Fortschritt zuruecknehmen, sonst
            // traegt der Sitzungszustand das geloeste Raetsel mit ins zweite
            // Laden hinein.
            ForgetSessionProgress();

            yield return LoadSlice();
            yield return EnterResonanceZone();

            Assert.That(
                regeneration.PuzzleSolved,
                Is.False,
                "Das Raetsel gilt nach dem Neuladen als geloest.");
            Assert.That(
                regeneration.MemorySeen,
                Is.False,
                "Die Erinnerung gilt nach dem Vergessen als gesehen.");

            // Nur die Erinnerung, ohne Raetsel.
            yield return ActivateMemorySite();

            Assert.That(
                regeneration.MemorySeen, Is.True, "Die Erinnerung zaehlt nicht.");
            Assert.That(
                regeneration.PuzzleSolved, Is.False, "Das Raetsel ist geloest.");

            yield return WaitSeconds(1f);

            Assert.That(
                regeneration.ConditionsMet,
                Is.False,
                "Eine Bedingung allein gilt als beide.");
            Assert.That(
                regeneration.IsRegenerated,
                Is.False,
                "Der Wald antwortet schon auf die halbe Leistung. Damit " +
                "reagierte er auf einen Schritt statt auf ein Verstehen.");
        }

        [UnityTest]
        public IEnumerator DieRegeneration_GeschiehtGenauEinmal()
        {
            yield return LoadSlice();

            int events = 0;

            void Count() => events++;

            FinsterwaldRegeneration.AnyRegionRegenerated += Count;

            try
            {
                yield return EnterResonanceZone();
                yield return ActivateMemorySite();
                yield return SolvePuzzle();

                yield return WaitUntil(
                    () => regeneration.IsRegenerated,
                    "Der Wald antwortet nicht.");

                // Mehrfach nachtreten: weder ein zweiter Aufruf noch weitere
                // Ticks duerfen ein zweites Ereignis ausloesen.
                regeneration.Regenerate();
                regeneration.Tick();
                regeneration.Tick();

                yield return WaitSeconds(0.5f);

                Assert.That(
                    events,
                    Is.EqualTo(1),
                    $"Die Regeneration hat {events}-mal gemeldet.");
            }
            finally
            {
                FinsterwaldRegeneration.AnyRegionRegenerated -= Count;
            }
        }

        [UnityTest]
        public IEnumerator DieWiederherstellung_LoestKeineKaskadeAus()
        {
            yield return LoadSlice();

            // Die Region gilt bereits als regeneriert — so, wie sie es nach
            // einem Szenenwechsel innerhalb der Sitzung tut.
            RegionRegenerationState.MarkRegenerated(regeneration.RegionStateId);

            int events = 0;

            void Count() => events++;

            FinsterwaldRegeneration.AnyRegionRegenerated += Count;

            try
            {
                yield return LoadSlice();
                yield return WaitSeconds(0.5f);

                Assert.That(
                    regeneration.IsRegenerated,
                    Is.True,
                    "Der wiederhergestellte Zustand ging verloren.");
                Assert.That(
                    events,
                    Is.EqualTo(0),
                    "Die Wiederherstellung hat das Ereignis erneut gefeuert — " +
                    "der Ton erklaenge bei jedem Betreten neu.");
            }
            finally
            {
                FinsterwaldRegeneration.AnyRegionRegenerated -= Count;
            }
        }

        [UnityTest]
        public IEnumerator DieErzaehlung_NimmtNichtsVorweg()
        {
            yield return LoadSlice();

            foreach (CataloguedExaminable lore in
                     UnityEngine.Object.FindObjectsByType<CataloguedExaminable>(FindObjectsInactive.Include))
            {
                AssertNoForbiddenTerm(lore.gameObject.name);
            }

            foreach (MemoryEchoNarration echo in
                     UnityEngine.Object.FindObjectsByType<MemoryEchoNarration>(FindObjectsInactive.Include))
            {
                AssertNoForbiddenTerm(echo.gameObject.name);
            }

            yield return null;
        }

        private static void AssertNoForbiddenTerm(string text)
        {
            foreach (string forbidden in NarrationKeys.ForbiddenTerms)
            {
                Assert.That(
                    text.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase),
                    Is.LessThan(0),
                    $"'{text}' enthaelt den gesperrten Begriff '{forbidden}'.");
            }
        }

        // ==================================================================
        // Der Weg des Spielers
        // ==================================================================

        private IEnumerator LoadSlice()
        {
            AsyncOperation load =
                SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;

            player = GameObject.Find("Player");
            Assert.That(player, Is.Not.Null, "Player fehlt in der Szene.");

            playerBody = player.GetComponent<CharacterController>();
            detector = player.GetComponent<InteractionDetector>();
            vitals = player.GetComponent<PlayerVitals>();

            Assert.That(
                detector,
                Is.Not.Null,
                "Ohne InteractionDetector gibt es keinen echten Bedienweg.");

            puzzle = UnityEngine.Object.FindAnyObjectByType<BridgePuzzle>();
            regeneration =
                UnityEngine.Object.FindAnyObjectByType<FinsterwaldRegeneration>();

            Assert.That(puzzle, Is.Not.Null, "Kein Brueckenraetsel in der Szene.");
            Assert.That(
                regeneration, Is.Not.Null, "Keine Regeneration in der Szene.");

            memorySite = null;
            foreach (MemorySite site in
                     UnityEngine.Object.FindObjectsByType<MemorySite>(FindObjectsInactive.Include))
            {
                if (site.SiteId == MemorySiteId)
                {
                    memorySite = site;
                }
            }

            Assert.That(
                memorySite,
                Is.Not.Null,
                $"Die Memory Site '{MemorySiteId}' fehlt — an ihr haengen " +
                "sowohl die Raetselfreigabe als auch die Regeneration.");

            // Ohne Waffe in der Haupthand bleibt PlayerCombat still.
            PlayerInventory.Equip(new InventoryItem(
                "test_blade", "Pruefklinge", EquipmentSlot.MainHand));

            vitals.ResetToFull();

            yield return null;
        }

        private EnemyController RequireSingleEnemy()
        {
            EnemyController[] enemies =
                UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include);

            Assert.That(
                enemies.Length,
                Is.EqualTo(1),
                "Der erste Kampf ist Unterricht: genau ein Gegner.");

            return enemies[0];
        }

        private IEnumerator EnterResonanceZone()
        {
            WatchResonanceZone zone =
                UnityEngine.Object.FindAnyObjectByType<WatchResonanceZone>();

            Assert.That(zone, Is.Not.Null, "Keine Resonanzzone in der Szene.");

            Teleport(zone.transform.position);

            yield return WaitUntil(
                () => puzzle.PlayerInZone,
                "Aren steht in der Resonanzzone, das Raetsel merkt es nicht.");
        }

        private IEnumerator ActivateMemorySite()
        {
            yield return InteractWith(memorySite);

            yield return WaitUntil(
                () => memorySite.IsActivated,
                "Die Memory Site laesst sich nicht aktivieren.");
        }

        /// <summary>
        /// Loest das Raetsel auf dem Weg, den ein Spieler nimmt: Anker
        /// drehen, Spannung pruefen, Stamm freigeben, Bohlen legen.
        /// </summary>
        private IEnumerator SolvePuzzle()
        {
            // Die Loesung wird bei den Regeln erfragt, nicht hier wiederholt.
            // Sonst pruefte der Test seine eigene Abschrift.
            yield return SetAnchors(
                BridgePuzzleRules.RequiredNotches(BridgeAnchorId.SouthDeep),
                BridgePuzzleRules.RequiredNotches(BridgeAnchorId.Side),
                BridgePuzzleRules.RequiredNotches(BridgeAnchorId.North));

            yield return InteractWith(FindPart(BridgePartRole.TensionCheck));
            yield return InteractWith(FindPart(BridgePartRole.Release));

            yield return WaitUntil(
                () => puzzle.CanPlacePlanks,
                "Der Stamm kommt nach der Freigabe nicht zur Ruhe.");

            int guard = 0;
            while (!puzzle.IsSolved && guard < 10)
            {
                BridgePuzzlePart plank = FindPart(BridgePartRole.Plank);

                if (plank == null)
                {
                    break;
                }

                yield return InteractWith(plank);
                guard++;
            }
        }

        /// <summary>Dreht jeden Anker ueber echte Bedienung auf seine Kerbenzahl.</summary>
        private IEnumerator SetAnchors(int south, int side, int north)
        {
            foreach (BridgeAnchor anchor in AllAnchors())
            {
                int wanted = anchor.AnchorId switch
                {
                    BridgeAnchorId.SouthDeep => south,
                    BridgeAnchorId.Side => side,
                    _ => north
                };

                int turns = 0;

                while (anchor.Notches != wanted && turns < 6)
                {
                    yield return InteractWith(anchor);
                    turns++;
                }

                Assert.That(
                    anchor.Notches,
                    Is.EqualTo(wanted),
                    $"Anker {anchor.AnchorId} laesst sich nicht auf " +
                    $"{wanted} Kerben stellen.");
            }
        }

        private IEnumerable<BridgeAnchor> AllAnchors()
        {
            return UnityEngine.Object.FindObjectsByType<BridgeAnchor>(FindObjectsInactive.Include);
        }

        private BridgePuzzlePart FindPart(BridgePartRole role)
        {
            foreach (BridgePuzzlePart part in
                     UnityEngine.Object.FindObjectsByType<BridgePuzzlePart>(FindObjectsInactive.Include))
            {
                if (part.Role == role && part.CanInteract(player))
                {
                    return part;
                }
            }

            return null;
        }

        // ==================================================================
        // Bedienung ueber den echten Weg
        // ==================================================================

        /// <summary>
        /// Stellt Aren an ein Objekt, wartet bis der Detector es von selbst
        /// als Ziel fuehrt, und drueckt dann die Interaktionstaste. Das ist
        /// derselbe Weg, den ein Spieler nimmt — inklusive der Frage, ob das
        /// Objekt ueberhaupt erreichbar ist.
        /// </summary>
        private IEnumerator InteractWith(Component target)
        {
            Assert.That(
                target, Is.Not.Null, "Es gibt kein Objekt zum Bedienen.");

            yield return TryInteractWith(target, expectReachable: true);
        }

        private IEnumerator TryInteractWith(
            Component target, bool expectReachable)
        {
            if (target == null)
            {
                yield break;
            }

            IInteractable wanted = target as IInteractable;

            Assert.That(
                wanted,
                Is.Not.Null,
                $"'{target.name}' ist gar kein Interaktionsobjekt.");

            Teleport(target.transform.position);

            bool reached = false;
            float deadline = Time.time + SettleTimeout;
            int frames = 0;

            while (Time.time < deadline && frames < FrameBrake)
            {
                yield return null;
                frames++;

                // Ausdruecklich gegen null pruefen. Ein blosser Vergleich
                // haette null == null als Treffer gewertet und danach ins
                // Leere gedrueckt — der Test waere an einer spaeteren,
                // voellig harmlos aussehenden Stelle gescheitert.
                IInteractable current = detector.CurrentTarget;

                if (current != null && ReferenceEquals(current, wanted))
                {
                    reached = true;
                    break;
                }
            }

            if (!expectReachable)
            {
                yield break;
            }

            Assert.That(
                reached,
                Is.True,
                $"'{target.name}' wird nicht zum Interaktionsziel — fuer " +
                "einen Spieler waere es damit unbedienbar. Der Detector " +
                $"fuehrt stattdessen: {DescribeTarget(detector.CurrentTarget)}.");

            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);
            yield return null;
        }

        /// <summary>Nennt das aktuelle Ziel beim Namen, damit ein Fehlschlag erklaert ist.</summary>
        private static string DescribeTarget(IInteractable target)
        {
            if (target == null)
            {
                return "nichts";
            }

            return target is Component component
                ? $"'{component.gameObject.name}' ({component.GetType().Name})"
                : target.GetType().Name;
        }

        private IEnumerator DefeatEnemy(EnemyController enemy)
        {
            int swings = 0;

            while (!enemy.Health.IsDead && swings < 40)
            {
                yield return FacePlayerToEnemy(enemy, distance: 1.6f);

                Press(mouse.leftButton);
                yield return null;
                Release(mouse.leftButton);

                swings++;

                yield return WaitSeconds(0.75f);
            }

            Assert.That(
                swings,
                Is.LessThan(40),
                "Der Wurzelstreifer liess sich in vierzig Schlaegen nicht " +
                "besiegen.");
        }

        private IEnumerator FacePlayerToEnemy(
            EnemyController enemy, float distance)
        {
            Vector3 target =
                enemy.transform.position + new Vector3(0f, 0f, -distance);

            Teleport(new Vector3(
                target.x, enemy.transform.position.y, target.z));

            player.transform.rotation =
                Quaternion.LookRotation(Vector3.forward, Vector3.up);

            yield return null;
        }

        private void Teleport(Vector3 position)
        {
            bool wasEnabled = playerBody != null && playerBody.enabled;

            if (playerBody != null)
            {
                playerBody.enabled = false;
            }

            player.transform.position = position;

            if (playerBody != null)
            {
                playerBody.enabled = wasEnabled;
            }

            Physics.SyncTransforms();
        }

        private IEnumerator WaitSeconds(float seconds)
        {
            float until = Time.time + seconds;

            while (Time.time < until)
            {
                yield return null;
            }
        }

        private IEnumerator WaitUntil(Func<bool> condition, string message)
        {
            float deadline = Time.time + ArcTimeout;
            int frames = 0;

            while (Time.time < deadline && frames < FrameBrake)
            {
                if (condition())
                {
                    yield break;
                }

                frames++;
                yield return null;
            }

            if (condition())
            {
                yield break;
            }

            Assert.Fail($"{message} (nach {ArcTimeout:F0} s)");
        }
    }
}
