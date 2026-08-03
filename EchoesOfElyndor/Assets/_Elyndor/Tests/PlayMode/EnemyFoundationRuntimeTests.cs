using System;
using System.Collections;
using System.Collections.Generic;
using Elyndor.Combat;
using Elyndor.Enemies;
using Elyndor.UIFoundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AllocationConstraints = UnityEngine.TestTools.Constraints.ConstraintExtensions;

namespace Elyndor.Tests
{
    /// <summary>
    /// Laufzeittests der Gegnergrundlage. Der Gegner wird vollstaendig zur
    /// Laufzeit zusammengesetzt — es wird keine Szene geladen, kein Prefab
    /// benoetigt und nichts gespeichert.
    ///
    /// Der Controller wird abgeschaltet und stattdessen ueber
    /// <see cref="EnemyController.Tick"/> von Hand getaktet. Damit haengt kein
    /// Test an einer Framezahl, an der Bildrate oder an der Reihenfolge, in der
    /// Unity Update-Aufrufe abarbeitet.
    /// </summary>
    public sealed class EnemyFoundationRuntimeTests
    {
        private const float Step = 0.1f;

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
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            spawned.Clear();
            enemy = null;
            player = null;
            vitals = null;
        }

        // ------------------------------------------------------------------
        // 1-4: Wahrnehmung
        // ------------------------------------------------------------------

        [Test]
        public void EnemyStartsIdle()
        {
            CreateEnemy(Vector3.zero);

            Assert.That(
                enemy.State, Is.EqualTo(EnemyFoundationState.Idle),
                "Ein frisch erzeugter Gegner ist nicht ruhend.");
            Assert.That(consoleErrors, Is.Empty);
        }

        [Test]
        public void TargetInsideRadiusLeadsToAlertOrChase()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 6f));

            Tick(3);

            Assert.That(
                enemy.State,
                Is.EqualTo(EnemyFoundationState.Alert)
                    .Or.EqualTo(EnemyFoundationState.Chase),
                "Ein Ziel im Wahrnehmungsradius wird nicht bemerkt.");
        }

        [Test]
        public void TargetOutsideRadiusKeepsEnemyIdle()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 40f));

            Tick(5);

            Assert.That(
                enemy.State, Is.EqualTo(EnemyFoundationState.Idle),
                "Ein Ziel ausserhalb des Radius weckt den Gegner.");
            Assert.That(enemy.Perception.HasTarget, Is.False);
        }

        [Test]
        public void BlockedLineOfSightPreventsDetection()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 6f));

            // Wand zwischen Gegner und Ziel.
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawned.Add(wall);
            wall.transform.position = new Vector3(0f, 1f, 3f);
            wall.transform.localScale = new Vector3(6f, 4f, 0.5f);
            Physics.SyncTransforms();

            Tick(5);

            Assert.That(
                enemy.Perception.IsTargetVisible, Is.False,
                "Die Sichtlinie wird nicht geprueft.");
            Assert.That(
                enemy.State, Is.EqualTo(EnemyFoundationState.Idle),
                "Ein verdecktes Ziel loest trotzdem eine Reaktion aus.");
        }

        // ------------------------------------------------------------------
        // 5-7: Angriff
        // ------------------------------------------------------------------

        [Test]
        public void EnemyEntersAttackWhenTargetIsInRange()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            Tick(3);

            Assert.That(
                enemy.State, Is.EqualTo(EnemyFoundationState.Attack),
                "Der Gegner greift in Reichweite nicht an.");
        }

        [Test]
        public void AttackAppliesDamageExactlyOnce()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            int damageEvents = 0;
            enemy.Attack.AttackPerformed += _ => damageEvents++;

            float before = vitals.Health;
            Tick(3);

            Assert.That(
                enemy.Attack.PerformedAttacks, Is.EqualTo(1),
                "Ein Angriffszyklus hat mehr als einmal zugeschlagen.");
            Assert.That(
                damageEvents, Is.EqualTo(1),
                "Das Angriffsereignis kam nicht genau einmal.");
            Assert.That(
                before - vitals.Health, Is.EqualTo(8f).Within(0.001f),
                "Der Angriff hat nicht genau einmal Schaden verursacht.");
        }

        [Test]
        public void AttackCooldownPreventsRepeatedDamage()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            // Erster Angriff.
            Tick(3);
            float afterFirst = vitals.Health;
            Assert.That(enemy.Attack.PerformedAttacks, Is.EqualTo(1));

            // Deutlich weniger als die Abklingzeit von 1,6 s.
            Tick(5);

            Assert.That(
                enemy.Attack.PerformedAttacks, Is.EqualTo(1),
                "Die Abklingzeit verhindert den zweiten Angriff nicht.");
            Assert.That(
                vitals.Health, Is.EqualTo(afterFirst).Within(0.001f),
                "Waehrend der Abklingzeit kam weiterer Schaden an.");

            // Abklingzeit abwarten, dann folgt der naechste Angriff.
            Tick(20);

            Assert.That(
                enemy.Attack.PerformedAttacks, Is.EqualTo(2),
                "Nach der Abklingzeit greift der Gegner nicht erneut an.");
        }

        // ------------------------------------------------------------------
        // Bewegung — belegt zugleich, dass die Hurt-Sperre unten aussagekraeftig ist
        // ------------------------------------------------------------------

        [Test]
        public void EnemyClosesInOnTargetWhileChasing()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 10f));

            float distanceBefore = enemy.Perception.DistanceToTarget;
            Tick(10);

            Assert.That(
                enemy.State, Is.EqualTo(EnemyFoundationState.Chase),
                "Der Gegner verfolgt das Ziel nicht.");
            Assert.That(
                enemy.Perception.DistanceToTarget,
                Is.LessThan(distanceBefore - 1f),
                "Der Gegner naehert sich dem Ziel nicht.");
        }

        [Test]
        public void EnemyKeepsItsStopDistance()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 10f));

            // Lange genug takten, dass der Weg sicher zurueckgelegt waere.
            Tick(120);

            Assert.That(
                enemy.Perception.DistanceToTarget,
                Is.GreaterThanOrEqualTo(enemy.Movement.StopDistance - 0.05f),
                "Der Gegner laeuft in das Ziel hinein.");
        }

        // ------------------------------------------------------------------
        // 8-9: Trefferreaktion
        // ------------------------------------------------------------------

        [Test]
        public void DamageLeadsToHurt()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 6f));

            Tick(3);
            Assert.That(enemy.State, Is.Not.EqualTo(EnemyFoundationState.Idle));

            enemy.Health.TakeDamage(5f, AttackType.Light, player.transform.position);

            Assert.That(
                enemy.State, Is.EqualTo(EnemyFoundationState.Hurt),
                "Ein Treffer loest keine Trefferreaktion aus.");
        }

        [Test]
        public void HurtBlocksMovementAndAttack()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            Tick(3);
            int attacksBefore = enemy.Attack.PerformedAttacks;

            enemy.Health.TakeDamage(5f, AttackType.Light, player.transform.position);
            Assert.That(enemy.State, Is.EqualTo(EnemyFoundationState.Hurt));

            Vector3 positionBefore = enemy.transform.position;

            // Weniger Schritte als die Hurt-Dauer von 0,45 s.
            Tick(3);

            Assert.That(
                enemy.State, Is.EqualTo(EnemyFoundationState.Hurt),
                "Die Trefferreaktion endet zu frueh.");
            Assert.That(
                enemy.transform.position, Is.EqualTo(positionBefore),
                "Der Gegner bewegt sich waehrend der Trefferreaktion.");
            Assert.That(
                enemy.Attack.PerformedAttacks, Is.EqualTo(attacksBefore),
                "Der Gegner greift waehrend der Trefferreaktion an.");
        }

        [Test]
        public void HurtReturnsToChaseWhileTargetIsKnown()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 6f));

            Tick(3);
            enemy.Health.TakeDamage(5f, AttackType.Light, player.transform.position);

            // Ueber die Hurt-Dauer hinaus takten.
            Tick(8);

            Assert.That(
                enemy.State,
                Is.EqualTo(EnemyFoundationState.Chase)
                    .Or.EqualTo(EnemyFoundationState.Attack),
                "Nach der Trefferreaktion wird die Verfolgung nicht aufgenommen.");
        }

        // ------------------------------------------------------------------
        // 10-12: Tod
        // ------------------------------------------------------------------

        [Test]
        public void LethalDamageLeadsToDead()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 6f));

            int deathEvents = 0;
            enemy.Health.Died += () => deathEvents++;

            Tick(2);
            enemy.Health.TakeDamage(999f, AttackType.Heavy, player.transform.position);

            Assert.That(
                enemy.State, Is.EqualTo(EnemyFoundationState.Dead),
                "Toedlicher Schaden fuehrt nicht in den Endzustand.");
            Assert.That(
                enemy.Health.IsDead, Is.True);
            Assert.That(
                deathEvents, Is.EqualTo(1),
                "Der Tod wurde nicht genau einmal gemeldet.");
        }

        [Test]
        public void FurtherDamageDoesNotRaiseDeathTwice()
        {
            CreateEnemy(Vector3.zero);

            int deathEvents = 0;
            enemy.Health.Died += () => deathEvents++;

            enemy.Health.TakeDamage(999f, AttackType.Heavy, Vector3.zero);
            enemy.Health.TakeDamage(999f, AttackType.Heavy, Vector3.zero);
            enemy.Health.Kill(Vector3.zero);

            Assert.That(
                deathEvents, Is.EqualTo(1),
                "Der Tod wurde mehrfach ausgeloest.");
        }

        [Test]
        public void DeadIsNeverLeftAgain()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            enemy.Health.TakeDamage(999f, AttackType.Heavy, Vector3.zero);
            Assert.That(enemy.State, Is.EqualTo(EnemyFoundationState.Dead));

            // Weder Takten noch ein direkter Zustandswechsel loesen den Tod auf.
            Tick(10);

            foreach (EnemyFoundationState state in
                     (EnemyFoundationState[])Enum.GetValues(
                         typeof(EnemyFoundationState)))
            {
                Assert.That(
                    enemy.StateMachine.TrySetState(state), Is.False,
                    $"Aus Dead heraus wurde {state} zugelassen.");
            }

            Assert.That(
                enemy.State, Is.EqualTo(EnemyFoundationState.Dead),
                "Der Endzustand wurde verlassen.");
        }

        [Test]
        public void DeadEnemyDoesNotAttack()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 1.5f));

            enemy.Health.TakeDamage(999f, AttackType.Heavy, Vector3.zero);

            float health = vitals.Health;
            int attacks = enemy.Attack.PerformedAttacks;

            Tick(20);

            Assert.That(
                enemy.Attack.PerformedAttacks, Is.EqualTo(attacks),
                "Ein toter Gegner greift weiterhin an.");
            Assert.That(
                vitals.Health, Is.EqualTo(health).Within(0.001f),
                "Ein toter Gegner verursacht weiterhin Schaden.");
        }

        [Test]
        public void DeathTakesPrecedenceOverHurt()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 6f));

            Tick(3);
            enemy.Health.TakeDamage(999f, AttackType.Heavy, Vector3.zero);

            Assert.That(
                enemy.State, Is.EqualTo(EnemyFoundationState.Dead),
                "Der toedliche Treffer wurde als Trefferreaktion behandelt.");
        }

        // ------------------------------------------------------------------
        // 13-14: Robustheit
        // ------------------------------------------------------------------

        [Test]
        public void MissingPlayerRaisesNoConsoleErrors()
        {
            CreateEnemy(Vector3.zero);

            Tick(20);

            Assert.That(
                enemy.State, Is.EqualTo(EnemyFoundationState.Idle),
                "Ohne Ziel bleibt der Gegner nicht ruhend.");
            Assert.That(
                consoleErrors, Is.Empty,
                "Ein fehlender Spieler erzeugt rote Console-Meldungen.");
        }

        [Test]
        public void MissingPlayerVitalsRaisesNoConsoleErrors()
        {
            CreateEnemy(Vector3.zero);

            // Ziel ohne PlayerVitals: der Angriff muss folgenlos bleiben.
            GameObject bare = new GameObject("ZielOhneVitals");
            spawned.Add(bare);
            bare.transform.position = new Vector3(0f, 0f, 1.5f);
            enemy.Perception.SetTarget(bare.transform);

            Tick(20);

            Assert.That(
                enemy.Attack.PerformedAttacks, Is.GreaterThan(0),
                "Der Angriff wurde gar nicht erst ausgefuehrt.");
            Assert.That(
                consoleErrors, Is.Empty,
                "Ein Ziel ohne PlayerVitals erzeugt rote Console-Meldungen.");
        }

        /// <summary>
        /// Die Wahrnehmung fragt in jedem Tick die Physik ab. Geprueft wird
        /// mit Unitys Allokationsrekorder statt ueber GC-Zaehler: eine
        /// Messung mit <see cref="GC.GetTotalMemory"/> erkennt kurzlebigen
        /// Muell nicht zuverlaessig und wuerde gruen bleiben, obwohl
        /// allokiert wird.
        /// </summary>
        [Test]
        public void RepeatedPerceptionTicksDoNotAllocate()
        {
            CreateEnemy(Vector3.zero);
            CreatePlayer(new Vector3(0f, 0f, 6f));

            EnemyPerception perception = enemy.Perception;

            // Aufwaermen: Ziel aufloesen, Puffer anlegen, Code jitten.
            for (int i = 0; i < 20; i++)
            {
                perception.Tick(Step);
            }

            Assert.That(
                () =>
                {
                    for (int i = 0; i < 50; i++)
                    {
                        perception.Tick(Step);
                    }
                },
                AllocationConstraints.AllocatingGCMemory(Is.Not),
                "Die Wahrnehmung allokiert pro Tick Speicher.");
        }

        [Test]
        public void StateMachineIgnoresRedundantTransitions()
        {
            CreateEnemy(Vector3.zero);

            int changes = 0;
            enemy.StateMachine.StateChanged += (_, _) => changes++;

            Assert.That(
                enemy.StateMachine.TrySetState(EnemyFoundationState.Idle),
                Is.False,
                "Der bereits aktive Zustand wurde erneut gesetzt.");
            Assert.That(changes, Is.Zero);

            Assert.That(
                enemy.StateMachine.TrySetState(EnemyFoundationState.Alert),
                Is.True);
            Assert.That(changes, Is.EqualTo(1));
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        /// <summary>
        /// Baut einen vollstaendigen Gegner zur Laufzeit. Die Komponenten
        /// werden auf einem inaktiven Objekt angelegt und erst danach
        /// aktiviert, damit alle Awake-Aufrufe eine vollstaendige Komposition
        /// vorfinden.
        /// </summary>
        private void CreateEnemy(Vector3 position)
        {
            GameObject root = new GameObject("EnemyFoundationTestSubject");
            root.SetActive(false);
            spawned.Add(root);
            root.transform.position = position;

            root.AddComponent<EnemyStateMachine>();
            root.AddComponent<EnemyHealth>();
            root.AddComponent<EnemyPerception>();
            root.AddComponent<EnemyMovement>();
            root.AddComponent<EnemyAttack>();
            root.AddComponent<EnemyHitReaction>();
            enemy = root.AddComponent<EnemyController>();

            root.SetActive(true);

            // Von Hand takten statt auf Update warten.
            enemy.enabled = false;
        }

        private void CreatePlayer(Vector3 position)
        {
            player = new GameObject("PlayerTestSubject");
            spawned.Add(player);
            player.transform.position = position;
            vitals = player.AddComponent<PlayerVitals>();

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
