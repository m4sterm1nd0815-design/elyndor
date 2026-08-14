using System.Collections.Generic;
using Elyndor.Combat;
using Elyndor.Enemies;
using Elyndor.UIFoundation;
using NUnit.Framework;
using UnityEngine;

namespace Elyndor.Tests
{
    /// <summary>
    /// Laufzeittests des Wurzelstreifers — des ersten echten Gegners.
    ///
    /// Der Gegner wird vollstaendig zur Laufzeit zusammengesetzt und von Hand
    /// getaktet: keine Szene, kein Prefab, keine Abhaengigkeit von der
    /// Bildrate. Damit pruefen diese Tests dasselbe Verhalten, das auch in der
    /// Szene laeuft, aber ohne dessen Zufaelle.
    ///
    /// Geprueft wird vor allem das, woran Lesbarkeit haengt: dass der Schaden
    /// erst am Ende des Telegraphs entsteht, dass er ausbleibt, wenn der
    /// Spieler bis dahin aus der Reichweite ist, und dass nach dem Schlag ein
    /// echtes Gegenfenster offen steht.
    /// </summary>
    public sealed class WurzelstreiferCombatRuntimeTests
    {
        private const float Step = 0.1f;
        private const int MaxSteps = 400;

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<string> consoleErrors = new List<string>();

        private EnemyController enemy;
        private GameObject player;
        private PlayerVitals vitals;

        [SetUp]
        public void SetUp()
        {
            consoleErrors.Clear();
            Application.logMessageReceived += CollectConsoleError;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CollectConsoleError;

            foreach (GameObject instance in spawned)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            spawned.Clear();
            enemy = null;
            player = null;
            vitals = null;
        }

        // ------------------------------------------------------------------
        // Profil
        // ------------------------------------------------------------------

        [Test]
        public void Profil_LiegtAufDerGegnergrundlage()
        {
            CreateWurzelstreifer(Vector3.zero);

            Assert.That(
                enemy.Perception.DetectionRadius,
                Is.EqualTo(Wurzelstreifer.SightRadius).Within(0.001f));
            Assert.That(
                enemy.Perception.HearingRadius,
                Is.EqualTo(Wurzelstreifer.HearingRadius).Within(0.001f));
            Assert.That(
                enemy.Perception.LoseTargetDelay,
                Is.EqualTo(Wurzelstreifer.LoseTargetDelay).Within(0.001f));
            Assert.That(
                enemy.SuspicionDuration,
                Is.EqualTo(Wurzelstreifer.SuspicionDuration).Within(0.001f));
            Assert.That(
                enemy.Attack.TelegraphDuration,
                Is.EqualTo(Wurzelstreifer.TelegraphDuration).Within(0.001f));
            Assert.That(
                enemy.Attack.RecoveryDuration,
                Is.EqualTo(Wurzelstreifer.RecoveryDuration).Within(0.001f));
            Assert.That(
                enemy.Health.MaxHealth,
                Is.EqualTo(Wurzelstreifer.MaxHealth).Within(0.001f));
            Assert.That(
                enemy.HitReaction.HurtDuration,
                Is.EqualTo(Wurzelstreifer.LightFlinchDuration).Within(0.001f));
            Assert.That(
                enemy.HitReaction.HeavyStaggerDuration,
                Is.EqualTo(Wurzelstreifer.HeavyStaggerDuration).Within(0.001f));
            Assert.That(consoleErrors, Is.Empty);
        }

        // ------------------------------------------------------------------
        // Wahrnehmung
        // ------------------------------------------------------------------

        [Test]
        public void Verdachtsphase_HandeltNichtSofort()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 5f));

            // Erster Tick bemerkt, danach wird beobachtet.
            Tick(2);

            Assert.That(
                enemy.State,
                Is.EqualTo(EnemyFoundationState.Alert),
                "Der Gegner muss das Ziel erst beobachten.");

            // Kurz vor Ablauf der Verdachtsphase noch immer nur beobachten.
            Tick(15);

            Assert.That(
                enemy.State,
                Is.EqualTo(EnemyFoundationState.Alert),
                "Die Verdachtsphase endet zu frueh — die Begegnung waere " +
                "nicht mehr lesbar.");

            Tick(10);

            Assert.That(
                enemy.State,
                Is.EqualTo(EnemyFoundationState.Chase)
                    .Or.EqualTo(EnemyFoundationState.Attack),
                "Nach der Verdachtsphase muss gehandelt werden.");
        }

        [Test]
        public void Geraeusch_WirktOhneSichtlinie()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 4f));

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawned.Add(wall);
            wall.transform.position = new Vector3(0f, 1f, 2f);
            wall.transform.localScale = new Vector3(6f, 4f, 0.5f);
            Physics.SyncTransforms();

            Tick(3);

            Assert.That(
                enemy.Perception.IsTargetVisible,
                Is.False,
                "Vorbedingung: die Sichtlinie ist verdeckt.");
            Assert.That(
                enemy.Perception.IsTargetHeard,
                Is.True,
                "Innerhalb des Geraeuschradius muss der Gegner hoeren.");
            Assert.That(
                enemy.Perception.HasTarget,
                Is.True,
                "Ein gehoertes Ziel gilt als bekannt — sonst waere jeder " +
                "Busch ein Versteck.");
        }

        // ------------------------------------------------------------------
        // Telegraph und Trefferfenster
        // ------------------------------------------------------------------

        [Test]
        public void Sprungbiss_TrifftErstNachDemTelegraph()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            TickUntil(() => enemy.Attack.Phase == EnemyAttackPhase.Telegraph);

            float healthAtTelegraphStart = vitals.Health;

            // Waehrend des gesamten Ansetzens darf nichts ankommen.
            for (int i = 0; i < 6; i++)
            {
                enemy.Tick(Step);

                Assert.That(
                    vitals.Health,
                    Is.EqualTo(healthAtTelegraphStart).Within(0.001f),
                    "Waehrend des Telegraphs darf kein Schaden entstehen — " +
                    "sonst waere das Ansetzen nur Zierde.");
            }

            TickUntil(() => enemy.Attack.PerformedAttacks > 0);

            Assert.That(
                healthAtTelegraphStart - vitals.Health,
                Is.EqualTo(Wurzelstreifer.AttackDamage).Within(0.001f),
                "Am Ende des Telegraphs muss der Biss ankommen.");
        }

        [Test]
        public void AusweichenWaehrendDesTelegraphs_VermeidetDenTreffer()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            TickUntil(() => enemy.Attack.Phase == EnemyAttackPhase.Telegraph);

            float healthBefore = vitals.Health;

            // Eine Rolle traegt rund 4 m; hier reicht es, die Reichweite zu
            // verlassen, bevor der Biss aufgeloest wird.
            player.transform.position = new Vector3(0f, 0f, 6f);
            Physics.SyncTransforms();

            TickUntil(() => enemy.Attack.PerformedAttacks > 0);

            Assert.That(
                enemy.Attack.MissedAttacks,
                Is.EqualTo(1),
                "Der Biss haette ins Leere gehen muessen.");
            Assert.That(
                vitals.Health,
                Is.EqualTo(healthBefore).Within(0.001f),
                "Wer die Reichweite rechtzeitig verlaesst, darf nicht " +
                "getroffen werden — sonst ist Ausweichen wertlos.");
        }

        [Test]
        public void NachDemBiss_StehtEinGegenfensterOffen()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            TickUntil(() => enemy.Attack.PerformedAttacks > 0);

            Assert.That(
                enemy.Attack.Phase,
                Is.EqualTo(EnemyAttackPhase.Recovery),
                "Nach dem Biss muss der Gegner offen stehen.");

            int attacksAfterFirst = enemy.Attack.PerformedAttacks;

            // Deutlich weniger als die Abklingzeit von 2,6 s.
            Tick(11);

            Assert.That(
                enemy.Attack.PerformedAttacks,
                Is.EqualTo(attacksAfterFirst),
                "Im Gegenfenster darf kein zweiter Biss kommen.");
        }

        [Test]
        public void TrefferImTelegraph_BrichtDenAngriffAb()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            TickUntil(() => enemy.Attack.Phase == EnemyAttackPhase.Telegraph);

            float healthBefore = vitals.Health;

            enemy.Health.TakeDamage(
                5f, AttackType.Heavy, player.transform.position);

            // Der Zustandswechsel nach Hurt wirkt erst im naechsten Tick auf
            // den Angriff.
            enemy.Tick(Step);

            Assert.That(
                enemy.Attack.Phase,
                Is.EqualTo(EnemyAttackPhase.Ready),
                "Ein Treffer im Ansetzen muss den Angriff abbrechen — das " +
                "ist die Belohnung fuer das Lesen des Telegraphs.");
            Assert.That(
                enemy.Attack.PerformedAttacks,
                Is.Zero,
                "Der abgebrochene Angriff darf nicht als ausgefuehrt zaehlen.");
            Assert.That(
                vitals.Health,
                Is.EqualTo(healthBefore).Within(0.001f),
                "Der abgebrochene Biss darf keinen Schaden machen.");
        }

        [Test]
        public void AusserhalbDerReichweite_WirdNichtAngesetzt()
        {
            CreateWurzelstreifer(Vector3.zero);

            // Weit ausserhalb der Bisstreichweite, aber innerhalb der Sicht.
            CreatePlayer(new Vector3(0f, 0f, 8f));

            // Bewegung abschalten, damit der Abstand erhalten bleibt.
            enemy.Movement.Configure(0f, Wurzelstreifer.StopDistance);

            Tick(60);

            Assert.That(
                enemy.Attack.PerformedAttacks,
                Is.Zero,
                "Ausserhalb der Reichweite darf nicht zugeschlagen werden.");
            Assert.That(
                vitals.Health,
                Is.EqualTo(100f).Within(0.001f),
                "Ein Gegner darf nie ausserhalb seines Trefferfensters " +
                "Schaden machen.");
        }

        // ------------------------------------------------------------------
        // Trefferreaktion
        // ------------------------------------------------------------------

        [Test]
        public void SchwererTreffer_StaggertLaengerAlsEinLeichter()
        {
            Assert.That(
                Wurzelstreifer.HeavyStaggerDuration,
                Is.GreaterThan(Wurzelstreifer.LightFlinchDuration),
                "Vorbedingung des Entwurfs.");

            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 5f));

            TickUntil(() => enemy.State == EnemyFoundationState.Chase);

            enemy.Health.TakeDamage(
                5f, AttackType.Light, player.transform.position);
            Assert.That(enemy.State, Is.EqualTo(EnemyFoundationState.Hurt));

            // 0,3 s liegen ueber dem leichten Flinch von 0,18 s.
            Tick(3);

            Assert.That(
                enemy.State,
                Is.Not.EqualTo(EnemyFoundationState.Hurt),
                "Ein leichter Treffer darf nur kurz unterbrechen.");

            TickUntil(() => enemy.State == EnemyFoundationState.Chase ||
                            enemy.State == EnemyFoundationState.Attack);

            enemy.Health.TakeDamage(
                5f, AttackType.Heavy, player.transform.position);

            Tick(3);

            Assert.That(
                enemy.State,
                Is.EqualTo(EnemyFoundationState.Hurt),
                "Ein schwerer Treffer muss laenger straucheln lassen — genau " +
                "das kauft der schwere Angriff.");
        }

        // ------------------------------------------------------------------
        // Rueckzug
        // ------------------------------------------------------------------

        [Test]
        public void UnterDreissigProzent_ZiehtSichEinmalZurueck()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 5f));

            TickUntil(() => enemy.State == EnemyFoundationState.Chase ||
                            enemy.State == EnemyFoundationState.Attack);

            // 40 auf 10 Lebenspunkte: unter die Schwelle von 30 %.
            enemy.Health.TakeDamage(
                30f, AttackType.Heavy, player.transform.position);

            float distanceBefore = enemy.Perception.DistanceToTarget;

            TickUntil(() => enemy.State == EnemyFoundationState.Retreat);

            Assert.That(
                enemy.Retreat.HasRetreated,
                Is.True,
                "Unter der Schwelle muss ein Rueckzug ausgeloest werden.");

            Tick(10);

            Assert.That(
                enemy.Perception.DistanceToTarget,
                Is.GreaterThan(distanceBefore),
                "Im Rueckzug muss sich der Gegner tatsaechlich loesen.");

            // Rueckzug abwarten, danach wird wieder aufgeschlossen.
            TickUntil(() => enemy.State != EnemyFoundationState.Retreat);

            Assert.That(
                enemy.State,
                Is.Not.EqualTo(EnemyFoundationState.Attack),
                "Aus dem Rueckzug heraus darf nicht direkt angegriffen " +
                "werden.");
        }

        [Test]
        public void Rueckzug_GeschiehtNurEinmal()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 5f));

            TickUntil(() => enemy.State == EnemyFoundationState.Chase ||
                            enemy.State == EnemyFoundationState.Attack);

            enemy.Health.TakeDamage(
                30f, AttackType.Heavy, player.transform.position);

            TickUntil(() => enemy.State == EnemyFoundationState.Retreat);
            TickUntil(() => enemy.State != EnemyFoundationState.Retreat);

            // Noch ein Treffer, weiterhin unter der Schwelle.
            enemy.Health.TakeDamage(
                2f, AttackType.Light, player.transform.position);

            Tick(60);

            Assert.That(
                enemy.State,
                Is.Not.EqualTo(EnemyFoundationState.Retreat),
                "Ein zweiter Rueckzug macht den Kampf zaeh statt lesbar.");
        }

        [Test]
        public void ImRueckzug_WirdNichtAngegriffen()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            TickUntil(() => enemy.State == EnemyFoundationState.Attack);

            enemy.Health.TakeDamage(
                30f, AttackType.Heavy, player.transform.position);

            TickUntil(() => enemy.State == EnemyFoundationState.Retreat);

            int attacksBefore = enemy.Attack.PerformedAttacks;
            float healthBefore = vitals.Health;

            Tick(20);

            Assert.That(
                enemy.Attack.PerformedAttacks,
                Is.EqualTo(attacksBefore),
                "Ein sich loesender Gegner darf nicht gleichzeitig beissen.");
            Assert.That(
                vitals.Health, Is.EqualTo(healthBefore).Within(0.001f));
        }

        // ------------------------------------------------------------------
        // Bindung an die Begegnung
        // ------------------------------------------------------------------

        [Test]
        public void AusserhalbSeinesBereichs_KehrtErZurueck()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 5f));

            LurePastLeash();

            Assert.That(
                enemy.Leash.IsReturning,
                Is.True,
                "Jenseits der Grenze muss die Rueckkehr beginnen.");

            // Zurueck laufen lassen; das Ziel bleibt weit weg.
            TickUntil(() => !enemy.Leash.IsReturning);

            Assert.That(
                enemy.Leash.DistanceFromHome,
                Is.LessThan(Wurzelstreifer.LeashRadius),
                "Der Gegner kehrt nicht in seinen Bereich zurueck.");
            Assert.That(
                enemy.State,
                Is.EqualTo(EnemyFoundationState.Idle),
                "Waehrend der Rueckkehr darf kein Ziel verfolgt werden.");
        }

        [Test]
        public void WaehrendDerRueckkehr_WirdNichtAngegriffen()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 5f));

            LurePastLeash();

            Assert.That(enemy.Leash.IsReturning, Is.True);

            // Der Spieler stellt sich dem Rueckkehrer direkt in den Weg.
            player.transform.position = enemy.transform.position +
                                        new Vector3(0f, 0f, 1f);
            Physics.SyncTransforms();

            int attacksBefore = enemy.Attack.PerformedAttacks;
            float healthBefore = vitals.Health;

            Tick(10);

            Assert.That(
                enemy.Attack.PerformedAttacks,
                Is.EqualTo(attacksBefore),
                "Ein zurueckkehrender Gegner darf nicht nebenbei zubeissen.");
            Assert.That(
                vitals.Health, Is.EqualTo(healthBefore).Within(0.001f));
        }

        // ------------------------------------------------------------------
        // Blockregel im Zusammenspiel
        // ------------------------------------------------------------------

        [Test]
        public void GeblockterBiss_MachtNurDreissigProzentSchaden()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f), withBlock: true);

            BlockStateStub block = player.GetComponent<BlockStateStub>();
            block.IsBlocking = true;

            TickUntil(() => enemy.Attack.PerformedAttacks > 0);

            Assert.That(
                100f - vitals.Health,
                Is.EqualTo(Wurzelstreifer.AttackDamage * 0.3f).Within(0.001f),
                "Der Biss des Wurzelstreifers muss durch den gemeinsamen " +
                "Schadensvertrag laufen, nicht am Block vorbei.");
        }

        [Test]
        public void UngeblockterBiss_MachtVollenSchaden()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f), withBlock: true);

            player.GetComponent<BlockStateStub>().IsBlocking = false;

            TickUntil(() => enemy.Attack.PerformedAttacks > 0);

            Assert.That(
                100f - vitals.Health,
                Is.EqualTo(Wurzelstreifer.AttackDamage).Within(0.001f));
        }

        [Test]
        public void KeineRotenMeldungen_UeberEinenGanzenKampf()
        {
            CreateWurzelstreifer(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            Tick(200);

            enemy.Health.TakeDamage(999f, AttackType.Heavy, Vector3.zero);

            Tick(50);

            Assert.That(
                consoleErrors,
                Is.Empty,
                "Ein vollstaendiger Kampf darf keine roten Meldungen " +
                "hinterlassen.");
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        private sealed class BlockStateStub : MonoBehaviour, IPlayerBlockState
        {
            public bool IsBlocking { get; set; }
        }

        private void CreateWurzelstreifer(Vector3 position)
        {
            GameObject root = new GameObject("WurzelstreiferTestSubject");
            root.SetActive(false);
            spawned.Add(root);
            root.transform.position = position;

            root.AddComponent<EnemyStateMachine>();
            root.AddComponent<EnemyHealth>();
            root.AddComponent<EnemyPerception>();
            root.AddComponent<EnemyMovement>();
            root.AddComponent<EnemyAttack>();
            root.AddComponent<EnemyHitReaction>();
            root.AddComponent<EnemyRetreat>();
            root.AddComponent<EnemyLeash>();
            enemy = root.AddComponent<EnemyController>();
            root.AddComponent<Wurzelstreifer>();

            root.SetActive(true);

            // Von Hand takten statt auf Update warten.
            enemy.enabled = false;
        }

        private void CreatePlayer(Vector3 position, bool withBlock = false)
        {
            player = new GameObject("PlayerTestSubject");
            player.SetActive(false);
            spawned.Add(player);
            player.transform.position = position;

            vitals = player.AddComponent<PlayerVitals>();

            if (withBlock)
            {
                player.AddComponent<BlockStateStub>();
                player.AddComponent<PlayerDamageReceiver>();
            }

            player.SetActive(true);
            vitals.SetHealth(100f);

            enemy.Perception.SetTarget(player.transform);
            Physics.SyncTransforms();
        }

        private void Tick(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                enemy.Tick(Step);
            }
        }

        /// <summary>
        /// Lockt den Gegner Schritt fuer Schritt ueber seine Grenze hinaus.
        /// Der Spieler zieht dabei bewusst langsamer weg, als der Gegner
        /// laufen kann — sonst reisst die Wahrnehmung ab, der Gegner bleibt
        /// stehen und die Grenze wird nie erreicht.
        /// </summary>
        private void LurePastLeash()
        {
            for (int i = 0; i < 300 && !enemy.Leash.IsReturning; i++)
            {
                player.transform.position += new Vector3(0f, 0f, 0.3f);
                Physics.SyncTransforms();
                enemy.Tick(Step);
            }

            Assert.That(
                enemy.Leash.IsReturning,
                Is.True,
                "Vorbedingung: der Gegner liess sich nicht ueber seine " +
                "Grenze locken.");
        }

        /// <summary>
        /// Taktet, bis die Bedingung eintritt. Scheitert ausdruecklich, statt
        /// stumm weiterzulaufen — ein Test, der auf ein nie eintretendes
        /// Ereignis wartet, soll das auch sagen.
        /// </summary>
        private void TickUntil(System.Func<bool> condition)
        {
            for (int i = 0; i < MaxSteps; i++)
            {
                if (condition())
                {
                    return;
                }

                enemy.Tick(Step);
            }

            Assert.Fail(
                $"Die erwartete Bedingung trat in {MaxSteps * Step:0.0} s " +
                "nicht ein.");
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
    }
}
