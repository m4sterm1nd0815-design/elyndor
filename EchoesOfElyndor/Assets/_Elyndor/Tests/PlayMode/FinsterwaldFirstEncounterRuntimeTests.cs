using System;
using System.Collections;
using System.Collections.Generic;
using Elyndor.Enemies;
using Elyndor.Inventory;
using Elyndor.UIFoundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Abnahme der ersten echten Begegnung — im echten Finsterwald, mit dem
    /// echten Spieler, ueber die ausgelieferte Tastenbelegung.
    ///
    /// Die uebrigen Kampftests bauen ihren Gegner zur Laufzeit zusammen; das
    /// ist praezise, sagt aber nichts darueber, ob der Kampf in der Szene
    /// tatsaechlich stattfindet. Hier laeuft die Szene, wie sie ausgeliefert
    /// wird: Arens Prefab-Zusammenstellung, seine Kamera, sein Boden, sein
    /// Wurzelstreifer auf der Lichtung.
    ///
    /// Die Eingabe kommt aus simulierten Geraeten ueber das
    /// <c>InputSystem_Actions</c>-Asset der Szene — dieselben Bindungen, die
    /// auch ein Spieler benutzt. Nach jedem <c>Press</c> folgt ein Frame,
    /// weil <see cref="InputTestFixture"/> Ereignisse nur einreiht.
    ///
    /// Was diese Tests nicht ersetzen: den Blick eines Menschen auf
    /// Lesbarkeit, Stimmung und Kameragefuehl.
    /// </summary>
    public sealed class FinsterwaldFirstEncounterRuntimeTests : InputTestFixture
    {
        private const string SceneName = "Finsterwald";
        private const int MaxWaitFrames = 900;

        private readonly List<string> consoleErrors = new List<string>();

        private Keyboard keyboard;
        private Gamepad gamepad;
        private Mouse mouse;

        private GameObject player;
        private PlayerVitals vitals;
        private CharacterController playerBody;
        private EnemyController enemy;

        public override void Setup()
        {
            base.Setup();

            keyboard = InputSystem.AddDevice<Keyboard>();
            gamepad = InputSystem.AddDevice<Gamepad>();
            mouse = InputSystem.AddDevice<Mouse>();

            consoleErrors.Clear();
            Application.logMessageReceived += CollectConsoleError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= CollectConsoleError;
            LogAssert.ignoreFailingMessages = false;
            base.TearDown();
        }

        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator DerGegnerBemerktAren_UndBeobachtetErst()
        {
            yield return EnterEncounter(distance: 6f);

            yield return WaitUntil(
                () => enemy.State != EnemyFoundationState.Idle,
                "Der Wurzelstreifer bemerkt Aren nicht.");

            Assert.That(
                enemy.State,
                Is.EqualTo(EnemyFoundationState.Alert),
                "Der Gegner muss erst beobachten, bevor er handelt.");

            // Die Verdachtsphase dauert zwei Sekunden; nach der Haelfte darf
            // noch nichts geschehen sein.
            yield return WaitSeconds(1f);

            Assert.That(
                enemy.State,
                Is.EqualTo(EnemyFoundationState.Alert),
                "Die Verdachtsphase ist zu kurz, um lesbar zu sein.");

            yield return WaitUntil(
                () => enemy.State == EnemyFoundationState.Chase ||
                      enemy.State == EnemyFoundationState.Attack,
                "Nach der Verdachtsphase handelt der Gegner nicht.");
        }

        [UnityTest]
        public IEnumerator DerSprungbiss_IstVorherLesbar()
        {
            yield return EnterEncounter(distance: 2f);

            yield return WaitUntil(
                () => enemy.Attack.Phase == EnemyAttackPhase.Telegraph,
                "Der Gegner setzt nicht sichtbar an.");

            float healthAtTelegraph = vitals.Health;
            float telegraphStart = Time.time;

            yield return WaitUntil(
                () => enemy.Attack.PerformedAttacks > 0,
                "Der angesetzte Biss kommt nie.");

            float readableFor = Time.time - telegraphStart;

            Assert.That(
                readableFor,
                Is.GreaterThan(0.6f),
                "Der Telegraph muss mindestens 0,6 s vorher lesbar sein.");
            Assert.That(
                vitals.Health,
                Is.LessThan(healthAtTelegraph),
                "Der Biss ist ohne Wirkung geblieben.");
        }

        [UnityTest]
        public IEnumerator DieRolle_IstEineGueltigeAntwort()
        {
            // Bekannte Fremdmeldung, hier bewusst toleriert.
            //
            // Meldung: „Cached unprocessed value unexpectedly became outdated
            //           for unknown reason".
            // Quelle:  com.unity.inputsystem 1.19.0,
            //          InputSystem/Controls/InputControl.cs:1410
            // Unity:   6000.4.5f1
            //
            // Ursache: Die Zeile steht in einem `#if DEBUG`-Block hinter
            // `paranoidReadValueCachingChecksEnabled`. Dieses Flag setzt
            // ausschliesslich `Tests/TestFixture/InputTestFixture.cs:156` —
            // die Selbstpruefung des Pakets laeuft also nur unter der
            // Test-Fixture und niemals im Spiel. Sie liest den Steuerwert
            // zusaetzlich frisch aus und meldet, wenn er vom
            // zwischengespeicherten abweicht, ohne dass das Veraltet-Kennzeichen
            // gesetzt war. Das passiert hier, weil dieser Test zusaetzlich eine
            // Szene laedt und der Geraetezustand dabei ueber einen Pfad
            // wechselt, der das Kennzeichen nicht setzt.
            //
            // Folge fuer uns: keine. Weder Elyndor-Code noch das Spiel sind
            // betroffen, und die Zusicherungen unten laufen unveraendert durch.
            //
            // Streng auf diesen einen Test begrenzt — die uebrige Suite prueft
            // weiterhin ausdruecklich auf rote Meldungen. Bei einem
            // Paket-Update ist zuerst zu pruefen, ob die Toleranz entfallen
            // kann.
            LogAssert.ignoreFailingMessages = true;

            yield return EnterEncounter(distance: 2f);

            yield return WaitUntil(
                () => enemy.Attack.Phase == EnemyAttackPhase.Telegraph,
                "Kein Telegraph, auf den geantwortet werden koennte.");

            float healthBefore = vitals.Health;
            int missesBefore = enemy.Attack.MissedAttacks;

            // Die Rolle folgt der Bewegungsrichtung; ohne Eingabe rollt Aren
            // nach vorn — in den Gegner hinein. Ein Spieler rollt rueckwaerts,
            // also wird die Richtung hier auch so gegeben.
            Press(keyboard.sKey);
            yield return null;

            // Rolle: im Projekt-Asset liegt sie noch auf der Action "Crouch".
            // Jede Flanke bekommt ihren eigenen Frame — zwei Tasten im selben
            // Frame loszulassen bringt das Input-System dazu, seinen
            // zwischengespeicherten Wert als veraltet zu melden.
            Press(keyboard.cKey);
            yield return null;

            Release(keyboard.cKey);
            yield return null;

            Release(keyboard.sKey);
            yield return null;

            yield return WaitUntil(
                () => enemy.Attack.PerformedAttacks > 0,
                "Der Biss wurde nie aufgeloest.");

            Assert.That(
                enemy.Attack.MissedAttacks,
                Is.EqualTo(missesBefore + 1),
                "Die Rolle haette den Biss ins Leere laufen lassen muessen.");
            Assert.That(
                vitals.Health,
                Is.EqualTo(healthBefore).Within(0.001f),
                "Wer rechtzeitig rollt, darf keinen Schaden nehmen.");
        }

        [UnityTest]
        public IEnumerator DerBlock_IstEineGueltigeAberTeurereAntwort()
        {
            yield return EnterEncounter(distance: 2f);

            yield return WaitUntil(
                () => enemy.Attack.Phase == EnemyAttackPhase.Telegraph,
                "Kein Telegraph, auf den geantwortet werden koennte.");

            float healthBefore = vitals.Health;
            float staminaBefore = vitals.Stamina;

            Press(keyboard.qKey);
            yield return null;

            yield return WaitUntil(
                () => enemy.Attack.PerformedAttacks > 0,
                "Der Biss wurde nie aufgeloest.");

            // Noch einen Frame halten, damit der Schaden angekommen ist.
            yield return null;

            float taken = healthBefore - vitals.Health;

            Release(keyboard.qKey);
            yield return null;

            Assert.That(
                taken,
                Is.GreaterThan(0f),
                "Block darf nicht vollstaendig negieren.");
            Assert.That(
                taken,
                Is.EqualTo(Wurzelstreifer.AttackDamage * 0.3f).Within(0.51f),
                "Ein geblockter Biss muss rund 30 % Restschaden lassen.");
            Assert.That(
                vitals.Stamina,
                Is.LessThan(staminaBefore),
                "Blocken muss Ausdauer kosten — sonst waere Halten immer " +
                "die beste Antwort.");
        }

        [UnityTest]
        public IEnumerator TastaturUndGamepad_TreffenBeide()
        {
            yield return EnterEncounter(distance: 1.6f);

            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return WaitSeconds(0.4f);

            float healthAfterMouse = enemy.Health.CurrentHealth;

            Assert.That(
                healthAfterMouse,
                Is.LessThan(Wurzelstreifer.MaxHealth),
                "Der Angriff ueber Maus/Tastatur kommt nicht an.");

            // Abklingzeit des Spielerangriffs abwarten.
            yield return WaitSeconds(0.9f);

            Press(gamepad.rightTrigger);
            yield return null;
            Release(gamepad.rightTrigger);
            yield return WaitSeconds(0.4f);

            Assert.That(
                enemy.Health.CurrentHealth,
                Is.LessThan(healthAfterMouse),
                "Der Angriff ueber den Controller kommt nicht an.");
        }

        [UnityTest]
        public IEnumerator MehrereDurchlaeufe_OhneFehlerBisZurNiederlage()
        {
            yield return EnterEncounter(distance: 1.6f);

            int swings = 0;

            // Genug Schlaege fuer 40 Lebenspunkte bei 10 Schaden je leichtem
            // Treffer, mit Reserve fuer Fehlschlaege waehrend des Rueckzugs.
            while (!enemy.Health.IsDead && swings < 40)
            {
                Press(mouse.leftButton);
                yield return null;
                Release(mouse.leftButton);

                swings++;

                yield return WaitSeconds(0.75f);

                // Dem Gegner folgen, damit die Reichweite erhalten bleibt.
                yield return FacePlayerToEnemy(1.6f);
            }

            Assert.That(
                enemy.Health.IsDead,
                Is.True,
                $"Der Kampf endete nach {swings} Schlaegen nicht.");
            Assert.That(
                enemy.State,
                Is.EqualTo(EnemyFoundationState.Dead));

            // Der besiegte Gegner darf danach nichts mehr tun.
            float healthAfterFight = vitals.Health;
            yield return WaitSeconds(1.5f);

            Assert.That(
                vitals.Health,
                Is.EqualTo(healthAfterFight).Within(0.001f),
                "Ein besiegter Wurzelstreifer verursacht weiter Schaden.");

            AssertNoConsoleErrors();
        }

        /// <summary>
        /// Die Lebensanzeige muss am Gegner selbst hängen, nicht irgendwo in
        /// einer Testszene. Geprüft wird deshalb am Exemplar auf der Lichtung.
        /// </summary>
        [UnityTest]
        public IEnumerator DieLebensanzeige_HaengtAmWurzelstreifer()
        {
            yield return EnterEncounter(distance: 6f);

            var bars = enemy.GetComponentsInChildren<
                Elyndor.Enemies.UI.EnemyHealthBar>(true);

            Assert.That(
                bars.Length,
                Is.EqualTo(1),
                "Der Wurzelstreifer braucht genau eine Lebensanzeige.");

            var bar = bars[0];

            Assert.That(
                bar.HasHealthBinding,
                Is.True,
                "Die Anzeige ist an keine Lebensquelle gebunden.");
            Assert.That(
                bar.gameObject,
                Is.Not.EqualTo(enemy.gameObject),
                "Die Anzeige gehoert auf ein eigenes Kindobjekt — sonst " +
                "wuerde sie den Gegner zur Kamera drehen.");

            // Unberuehrt: verborgen.
            bar.Tick(0f);

            Assert.That(
                bar.IsBarVisible,
                Is.False,
                "Bei voller unberuehrter Gesundheit bleibt die Anzeige " +
                "verborgen.");

            // Nach dem ersten Treffer: sichtbar und im richtigen Verhaeltnis.
            enemy.Health.TakeDamage(
                Wurzelstreifer.MaxHealth * 0.5f,
                Elyndor.Combat.AttackType.Light,
                Vector3.zero);

            bar.Tick(0f);

            Assert.That(bar.IsBarVisible, Is.True);
            Assert.That(
                bar.Fill01,
                Is.EqualTo(0.5f).Within(0.001f),
                "Das Lebensverhaeltnis stimmt nicht.");

            // Nach dem Tod: wieder verborgen.
            enemy.Health.TakeDamage(
                999f, Elyndor.Combat.AttackType.Heavy, Vector3.zero);

            bar.Tick(0f);

            Assert.That(
                bar.IsBarVisible,
                Is.False,
                "Ein besiegter Gegner zeigt keine Leiste mehr.");
        }

        /// <summary>
        /// Die Anzeige bringt ihren eigenen World-Space-Canvas mit. Sie darf
        /// dabei die HUD-Struktur der Szene nicht verdoppeln.
        /// </summary>
        [UnityTest]
        public IEnumerator DieLebensanzeige_VerdoppeltNichtDasHud()
        {
            yield return EnterEncounter(distance: 6f);

            Canvas[] enemyCanvases =
                enemy.GetComponentsInChildren<Canvas>(true);

            Assert.That(
                enemyCanvases.Length,
                Is.EqualTo(1),
                "Der Gegner darf genau einen eigenen Canvas mitbringen.");
            Assert.That(
                enemyCanvases[0].renderMode,
                Is.EqualTo(RenderMode.WorldSpace),
                "Die Leiste gehoert in die Welt, nicht ins HUD.");

            foreach (Canvas canvas in
                     UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas == enemyCanvases[0])
                {
                    continue;
                }

                Assert.That(
                    canvas.transform.IsChildOf(enemy.transform),
                    Is.False,
                    $"Der Canvas '{canvas.name}' haengt unerwartet am Gegner.");
            }
        }

        [UnityTest]
        public IEnumerator GrosserAbstand_BeruhigtDenGegnerWieder()
        {
            yield return EnterEncounter(distance: 4f);

            yield return WaitUntil(
                () => enemy.State != EnemyFoundationState.Idle,
                "Der Gegner bemerkt Aren nicht.");

            Vector3 enemyStart = enemy.transform.position;

            // Weit ausserhalb von Sicht- und Geraeuschradius.
            Teleport(enemyStart + new Vector3(0f, 0f, 40f));

            yield return WaitUntil(
                () => enemy.State == EnemyFoundationState.Idle,
                "Der Gegner verliert Aren nicht wieder aus der Wahrnehmung.");

            // Er darf die Grenze kurz ueberschreiten — entscheidend ist, dass
            // er danach an seine Lichtung zurueckkehrt, statt irgendwo im
            // Wald stehen zu bleiben.
            yield return WaitUntil(
                () => !enemy.Leash.IsReturning,
                "Der Gegner kehrt nicht an seine Lichtung zurueck.");

            Assert.That(
                enemy.Leash.DistanceFromHome,
                Is.LessThan(Wurzelstreifer.LeashRadius),
                "Nach der Rueckkehr muss er wieder in seinem Bereich stehen.");
            Assert.That(
                Vector3.Distance(enemy.transform.position, enemyStart),
                Is.LessThan(4f),
                "Die Begegnung findet sonst nicht mehr dort statt, wo sie " +
                "entworfen wurde.");

            AssertNoConsoleErrors();
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        /// <summary>
        /// Laedt die Szene, ruestet eine Waffe aus und stellt Aren in den
        /// gewuenschten Abstand vor den Wurzelstreifer.
        /// </summary>
        private IEnumerator EnterEncounter(float distance)
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

            vitals = player.GetComponent<PlayerVitals>();
            playerBody = player.GetComponent<CharacterController>();

            Assert.That(
                player.GetComponent<Elyndor.Combat.PlayerDamageReceiver>(),
                Is.Not.Null,
                "Ohne Schadensempfaenger liefe der Biss am Block vorbei.");

            enemy = UnityEngine.Object.FindAnyObjectByType<EnemyController>();

            Assert.That(
                enemy,
                Is.Not.Null,
                "Auf der Lichtung steht kein Wurzelstreifer.");

            Assert.That(
                UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include).Length,
                Is.EqualTo(1),
                "Der erste Kampf ist Unterricht: genau ein Gegner.");

            // Ohne Waffe in der Haupthand bleibt PlayerCombat still.
            PlayerInventory.Equip(new InventoryItem(
                "test_blade", "Pruefklinge", EquipmentSlot.MainHand));

            vitals.ResetToFull();

            yield return FacePlayerToEnemy(distance);
        }

        /// <summary>
        /// Stellt Aren in den gewuenschten Abstand vor den Gegner. Der
        /// CharacterController muss dafuer kurz aus, sonst schiebt er die
        /// Position im selben Frame zurueck.
        /// </summary>
        private IEnumerator FacePlayerToEnemy(float distance)
        {
            Vector3 target =
                enemy.transform.position + new Vector3(0f, 0f, -distance);

            Teleport(new Vector3(target.x, enemy.transform.position.y, target.z));

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
            for (int frame = 0; frame < MaxWaitFrames; frame++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail(message);
        }

        private void AssertNoConsoleErrors()
        {
            Assert.That(
                consoleErrors,
                Is.Empty,
                "Der Kampf hat rote Meldungen hinterlassen:\n" +
                string.Join("\n", consoleErrors));
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
