using System.Collections.Generic;
using Elyndor.Combat;
using Elyndor.Enemies;
using Elyndor.Enemies.UI;
using NUnit.Framework;
using UnityEngine;
using AllocationConstraints = UnityEngine.TestTools.Constraints.ConstraintExtensions;

namespace Elyndor.Tests
{
    /// <summary>
    /// Laufzeittests der Gegner-Lebensanzeige. Gegner und Anzeige werden
    /// vollstaendig zur Laufzeit zusammengesetzt — es wird keine Szene
    /// geladen, kein Prefab benoetigt und nichts gespeichert.
    ///
    /// Die Anzeige wird wie der Gegner selbst von Hand getaktet
    /// (<see cref="EnemyHealthBar.Tick"/>), damit kein Test an einer Framezahl
    /// oder an der Bildrate haengt.
    /// </summary>
    public sealed class EnemyHealthBarRuntimeTests
    {
        private const float Step = 0.1f;
        private const float MaxHealth = 30f;

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<string> consoleErrors = new List<string>();

        private EnemyHealth health;
        private EnemyHealthBar bar;
        private Camera camera;

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
            health = null;
            bar = null;
            camera = null;
        }

        // ------------------------------------------------------------------
        // 1-2: Sichtbarkeitsregeln
        // ------------------------------------------------------------------

        [Test]
        public void HealthBarStartsHiddenAtFullHealth()
        {
            CreateEnemyWithBar();

            Assert.That(
                bar.Fill01, Is.EqualTo(1f).Within(0.001f),
                "Ein unverletzter Gegner startet nicht mit voller Leiste.");
            Assert.That(
                bar.IsBarVisible, Is.False,
                "Die Leiste ist bei voller Gesundheit sichtbar.");
            Assert.That(consoleErrors, Is.Empty);
        }

        [Test]
        public void DamageMakesHealthBarVisible()
        {
            CreateEnemyWithBar();

            health.TakeDamage(6f, AttackType.Light, Vector3.zero);

            Assert.That(
                bar.IsBarVisible, Is.True,
                "Der erste Treffer blendet die Leiste nicht ein.");

            // Auch ohne weiteren Takt muss die Anzeige bereits stimmen: sie
            // haengt am Ereignis, nicht am naechsten Frame.
            Assert.That(
                bar.Visual.Fill, Is.EqualTo(0.8f).Within(0.001f),
                "Die Darstellung folgt dem Treffer erst verzoegert.");
            Assert.That(consoleErrors, Is.Empty);
        }

        // ------------------------------------------------------------------
        // 3-6: Fuellwert
        // ------------------------------------------------------------------

        [Test]
        public void FillMatchesHealthRatio()
        {
            CreateEnemyWithBar();

            health.TakeDamage(12f, AttackType.Light, Vector3.zero);

            Assert.That(
                bar.Fill01, Is.EqualTo(0.6f).Within(0.001f),
                "Der Fuellwert entspricht nicht dem Lebensverhaeltnis.");
            Assert.That(
                bar.Visual.Fill, Is.EqualTo(health.Health01).Within(0.001f),
                "Darstellung und Lebensverhaeltnis laufen auseinander.");
        }

        [Test]
        public void RepeatedHitsUpdateFill()
        {
            CreateEnemyWithBar();

            health.TakeDamage(5f, AttackType.Light, Vector3.zero);
            Assert.That(bar.Fill01, Is.EqualTo(25f / MaxHealth).Within(0.001f));

            health.TakeDamage(5f, AttackType.Light, Vector3.zero);
            Assert.That(bar.Fill01, Is.EqualTo(20f / MaxHealth).Within(0.001f));

            health.TakeDamage(5f, AttackType.Light, Vector3.zero);

            Assert.That(
                bar.Fill01, Is.EqualTo(0.5f).Within(0.001f),
                "Mehrere Treffer werden nicht sauber fortgeschrieben.");
            Assert.That(
                bar.Visual.Fill, Is.EqualTo(0.5f).Within(0.001f));
        }

        /// <summary>
        /// <see cref="EnemyHealth"/> kennt bewusst keine eigene Heilung. Die
        /// Anzeige muss trotzdem jede Erhoehung uebernehmen — hier ueber
        /// <see cref="EnemyHealth.Configure"/>, das den Lebenswert wieder
        /// auffuellt und dasselbe Ereignis meldet.
        /// </summary>
        [Test]
        public void HealingUpdatesFill()
        {
            CreateEnemyWithBar();

            health.TakeDamage(15f, AttackType.Light, Vector3.zero);
            Assert.That(bar.Fill01, Is.EqualTo(0.5f).Within(0.001f));

            health.Configure(MaxHealth);

            Assert.That(
                bar.Fill01, Is.EqualTo(1f).Within(0.001f),
                "Eine Heilung wird nicht uebernommen.");
            Assert.That(
                bar.Visual.Fill, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void FillNeverExceedsMaximum()
        {
            CreateEnemyWithBar();

            health.TakeDamage(20f, AttackType.Light, Vector3.zero);

            // Neues, kleineres Maximum mit vollem Auffuellen: der Fuellwert
            // darf 1 nicht ueberschreiten.
            health.Configure(8f);

            Assert.That(
                bar.Fill01, Is.LessThanOrEqualTo(1f),
                "Der Fuellwert ueberschreitet das Maximum.");
            Assert.That(
                bar.Fill01, Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                bar.Visual.Fill, Is.LessThanOrEqualTo(1f),
                "Die Darstellung ueberschreitet das Maximum.");
        }

        // ------------------------------------------------------------------
        // 7: Tod
        // ------------------------------------------------------------------

        [Test]
        public void DeathHidesHealthBar()
        {
            CreateEnemyWithBar();

            health.TakeDamage(6f, AttackType.Light, Vector3.zero);
            Assert.That(bar.IsBarVisible, Is.True);

            health.TakeDamage(999f, AttackType.Heavy, Vector3.zero);

            Assert.That(
                bar.IsBarVisible, Is.False,
                "Die Leiste bleibt nach dem Tod stehen.");

            // Auch spaeteres Takten holt sie nicht zurueck.
            Tick(30);

            Assert.That(
                bar.IsBarVisible, Is.False,
                "Die Leiste eines toten Gegners taucht wieder auf.");
            Assert.That(consoleErrors, Is.Empty);
        }

        // ------------------------------------------------------------------
        // Ein- und Ausblenden
        // ------------------------------------------------------------------

        [Test]
        public void FullHealthHidesBarAgainAfterDelay()
        {
            CreateEnemyWithBar();
            bar.ConfigureVisibility(1f, true, false);

            health.TakeDamage(15f, AttackType.Light, Vector3.zero);
            health.Configure(MaxHealth);
            Assert.That(
                bar.IsBarVisible, Is.True,
                "Direkt nach der Heilung ist die Leiste nicht mehr sichtbar.");

            // Ueber die Standzeit von 1 s hinaus takten.
            Tick(15);

            Assert.That(
                bar.IsBarVisible, Is.False,
                "Die volle Leiste verschwindet nach der Standzeit nicht.");

            // Erst ein neuer Treffer holt sie zurueck.
            health.TakeDamage(3f, AttackType.Light, Vector3.zero);

            Assert.That(
                bar.IsBarVisible, Is.True,
                "Nach dem Ausblenden reagiert die Leiste nicht mehr auf Treffer.");
        }

        [Test]
        public void WoundedBarStaysVisibleByDefault()
        {
            CreateEnemyWithBar();

            health.TakeDamage(15f, AttackType.Light, Vector3.zero);

            // Deutlich laenger als die voreingestellte Standzeit.
            Tick(200);

            Assert.That(
                bar.IsBarVisible, Is.True,
                "Ein verwundeter Gegner verliert seine Leiste.");
        }

        [Test]
        public void WoundedBarHidesAfterDelayWhenConfigured()
        {
            CreateEnemyWithBar();
            bar.ConfigureVisibility(1f, true, true);

            health.TakeDamage(15f, AttackType.Light, Vector3.zero);
            Assert.That(bar.IsBarVisible, Is.True);

            Tick(15);

            Assert.That(
                bar.IsBarVisible, Is.False,
                "Die Standzeit blendet die Leiste nicht aus.");
        }

        // ------------------------------------------------------------------
        // Platzierung und Ausrichtung
        // ------------------------------------------------------------------

        [Test]
        public void BarSitsAboveEnemyByHeightOffset()
        {
            CreateEnemyWithBar(new Vector3(3f, 1f, -2f));
            bar.SetHeightOffset(2.5f);

            Tick(1);

            Vector3 expected = health.transform.position + new Vector3(0f, 2.5f, 0f);

            Assert.That(
                (bar.transform.position - expected).magnitude,
                Is.LessThan(0.001f),
                "Der Hoehenversatz wird nicht angewendet.");
        }

        [Test]
        public void BarFacesActiveCamera()
        {
            CreateEnemyWithBar();
            CreateCamera(new Vector3(5f, 3f, -7f));
            camera.transform.rotation = Quaternion.Euler(15f, 200f, 0f);
            bar.SetCamera(camera);

            Tick(1);

            Assert.That(
                Vector3.Angle(bar.transform.forward, camera.transform.forward),
                Is.LessThan(0.5f),
                "Die Leiste richtet sich nicht zur Kamera aus.");
        }

        // ------------------------------------------------------------------
        // Robustheit
        // ------------------------------------------------------------------

        [Test]
        public void MissingCameraRaisesNoConsoleErrors()
        {
            CreateEnemyWithBar();
            bar.SetCamera(null);

            health.TakeDamage(6f, AttackType.Light, Vector3.zero);
            Tick(30);

            Assert.That(
                consoleErrors, Is.Empty,
                "Eine fehlende Kamera erzeugt rote Console-Meldungen.");
            Assert.That(
                bar.IsBarVisible, Is.True,
                "Ohne Kamera verschwindet die Leiste.");
        }

        [Test]
        public void MissingEnemyHealthRaisesNoConsoleErrors()
        {
            GameObject lonely = new GameObject("HealthBarWithoutEnemy");
            lonely.SetActive(false);
            spawned.Add(lonely);

            EnemyHealthBar lonelyBar = lonely.AddComponent<EnemyHealthBar>();
            lonely.SetActive(true);

            Assert.That(
                lonelyBar.HasHealthBinding, Is.False,
                "Ohne Gegner wurde trotzdem eine Lebensquelle gefunden.");

            for (int i = 0; i < 30; i++)
            {
                lonelyBar.Tick(Step);
            }

            Assert.That(
                lonelyBar.IsBarVisible, Is.False,
                "Ohne Lebensquelle wird trotzdem etwas angezeigt.");
            Assert.That(
                consoleErrors, Is.Empty,
                "Eine fehlende Lebensquelle erzeugt rote Console-Meldungen.");
        }

        /// <summary>
        /// Nachtraegliche Bindung, wie sie ein spaeterer Spawner braucht: die
        /// Anzeige muss danach ohne Neuaufbau folgen.
        /// </summary>
        [Test]
        public void BindAttachesToAnotherEnemy()
        {
            GameObject lonely = new GameObject("HealthBarWithoutEnemy");
            lonely.SetActive(false);
            spawned.Add(lonely);

            EnemyHealthBar lonelyBar = lonely.AddComponent<EnemyHealthBar>();
            lonely.SetActive(true);

            CreateEnemyWithBar();
            lonelyBar.Bind(health);

            Assert.That(lonelyBar.HasHealthBinding, Is.True);

            health.TakeDamage(15f, AttackType.Light, Vector3.zero);

            Assert.That(
                lonelyBar.Fill01, Is.EqualTo(0.5f).Within(0.001f),
                "Die nachtraeglich gebundene Anzeige folgt dem Gegner nicht.");
            Assert.That(consoleErrors, Is.Empty);
        }

        [Test]
        public void DisabledBarUnsubscribesFromHealthEvents()
        {
            CreateEnemyWithBar();

            health.TakeDamage(15f, AttackType.Light, Vector3.zero);
            Assert.That(bar.Fill01, Is.EqualTo(0.5f).Within(0.001f));

            bar.enabled = false;
            health.TakeDamage(6f, AttackType.Light, Vector3.zero);

            Assert.That(
                bar.Fill01, Is.EqualTo(0.5f).Within(0.001f),
                "Die abgemeldete Anzeige empfaengt weiterhin Ereignisse.");

            // Beim Einschalten wird der aktuelle Stand nachgezogen.
            bar.enabled = true;

            Assert.That(
                bar.Fill01, Is.EqualTo(health.Health01).Within(0.001f),
                "Nach dem Einschalten wird der Lebenswert nicht nachgezogen.");
            Assert.That(
                bar.IsBarVisible, Is.True,
                "Nach dem Einschalten bleibt die verwundete Leiste verborgen.");

            // Und die Anmeldung greift genau einmal.
            health.TakeDamage(3f, AttackType.Light, Vector3.zero);

            Assert.That(
                bar.Fill01, Is.EqualTo(health.Health01).Within(0.001f),
                "Nach dem Einschalten stimmt der Fuellwert nicht mehr.");
        }

        [Test]
        public void DestroyedBarDoesNotBreakFurtherDamage()
        {
            CreateEnemyWithBar();

            health.TakeDamage(6f, AttackType.Light, Vector3.zero);
            Object.DestroyImmediate(bar.gameObject);
            bar = null;

            health.TakeDamage(6f, AttackType.Light, Vector3.zero);

            Assert.That(
                health.CurrentHealth, Is.EqualTo(18f).Within(0.001f));
            Assert.That(
                consoleErrors, Is.Empty,
                "Eine zerstoerte Anzeige stoert den weiteren Schadenlauf.");
        }

        [Test]
        public void FullCycleRaisesNoConsoleErrors()
        {
            CreateEnemyWithBar();
            CreateCamera(new Vector3(0f, 2f, -6f));
            bar.SetCamera(camera);

            Tick(5);
            health.TakeDamage(6f, AttackType.Light, Vector3.zero);
            Tick(5);
            health.TakeDamage(6f, AttackType.Light, Vector3.zero);
            Tick(5);
            health.Configure(MaxHealth);
            Tick(60);
            health.TakeDamage(999f, AttackType.Heavy, Vector3.zero);
            Tick(20);

            Assert.That(
                consoleErrors, Is.Empty,
                "Der normale Ablauf erzeugt rote Console-Meldungen.");
        }

        /// <summary>
        /// Der Takt laeuft in jedem Frame und darf keinen Muell erzeugen.
        /// Geprueft wird mit Unitys Allokationsrekorder statt ueber
        /// GC-Zaehler, weil eine Messung mit <c>GC.GetTotalMemory</c>
        /// kurzlebigen Muell nicht zuverlaessig erkennt.
        /// </summary>
        [Test]
        public void RepeatedTicksDoNotAllocate()
        {
            CreateEnemyWithBar();
            CreateCamera(new Vector3(0f, 2f, -6f));
            bar.SetCamera(camera);

            health.TakeDamage(6f, AttackType.Light, Vector3.zero);

            // Aufwaermen: Kamera aufloesen, Canvas aufbauen, Code jitten.
            for (int i = 0; i < 20; i++)
            {
                bar.Tick(Step);
            }

            EnemyHealthBar target = bar;

            Assert.That(
                () =>
                {
                    for (int i = 0; i < 50; i++)
                    {
                        target.Tick(Step);
                    }
                },
                AllocationConstraints.AllocatingGCMemory(Is.Not),
                "Die Lebensanzeige allokiert pro Takt Speicher.");
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        /// <summary>
        /// Baut einen minimalen Gegner mit Anzeige. Die Anzeige haengt als
        /// eigenes Kindobjekt unter dem Gegner — so, wie sie auch im Editor
        /// zusammengesetzt wird. Alles wird auf inaktiven Objekten angelegt
        /// und erst danach aktiviert, damit Awake eine vollstaendige
        /// Komposition vorfindet.
        /// </summary>
        private void CreateEnemyWithBar()
        {
            CreateEnemyWithBar(Vector3.zero);
        }

        private void CreateEnemyWithBar(Vector3 position)
        {
            GameObject root = new GameObject("EnemyHealthBarTestSubject");
            root.SetActive(false);
            spawned.Add(root);
            root.transform.position = position;

            health = root.AddComponent<EnemyHealth>();

            GameObject barObject = new GameObject("HealthBar");
            barObject.transform.SetParent(root.transform, false);
            bar = barObject.AddComponent<EnemyHealthBar>();

            root.SetActive(true);
        }

        private void CreateCamera(Vector3 position)
        {
            GameObject cameraObject = new GameObject("HealthBarTestCamera");
            spawned.Add(cameraObject);
            cameraObject.transform.position = position;
            camera = cameraObject.AddComponent<Camera>();
        }

        private void Tick(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                bar.Tick(Step);
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
