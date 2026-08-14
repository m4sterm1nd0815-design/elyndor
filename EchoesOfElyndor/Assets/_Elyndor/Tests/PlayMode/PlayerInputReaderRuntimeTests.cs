using System.Collections;
using System.Reflection;
using Elyndor.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Regressionstests fuer den Laufzeitzustand des <see cref="PlayerInputReader"/>.
    ///
    /// Hintergrund: Das im Projekt verwendete Actions-Asset ist zugleich das
    /// projektweite Asset (ProjectSettings, com.unity.input.settings.actions).
    /// Unity aktiviert dieses Asset beim Start selbsttaetig. Der Reader klont
    /// es per Instantiate(); der Klon uebernahm dabei die Enabled-Flags, besass
    /// aber keinen gueltigen InputActionState. Das anschliessende Enable() der
    /// Gameplay-Map scheiterte deshalb mit "Map must be contained in state"
    /// bzw. "Map index on InputActionMap is out of range" — der Spieler war
    /// dadurch im Play Mode nicht steuerbar.
    ///
    /// Die Tests spiegeln genau diese Ausgangslage: Das Quell-Asset ist bereits
    /// aktiviert, bevor der Reader initialisiert wird.
    /// </summary>
    public sealed class PlayerInputReaderRuntimeTests : InputTestFixture
    {
        private GameObject playerObject;
        private PlayerInputReader reader;
        private InputActionAsset sourceAsset;
        private Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();

            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            sourceAsset = CreateGameplayAsset();
        }

        public override void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
                playerObject = null;
            }

            if (sourceAsset != null)
            {
                sourceAsset.Disable();
                Object.DestroyImmediate(sourceAsset);
                sourceAsset = null;
            }

            base.TearDown();
        }

        [UnityTest]
        public IEnumerator AktiviertesQuellAsset_GameplayMapWirdAktiv()
        {
            sourceAsset.Enable();
            Assert.That(
                sourceAsset.enabled,
                Is.True,
                "Vorbedingung: Das Quell-Asset muss aktiviert sein.");

            CreatePlayer();
            yield return null;

            Assert.That(
                reader.GameplayMapEnabled,
                Is.True,
                "Die Gameplay-Map wurde nicht aktiviert. Genau dieser Zustand " +
                "machte den Spieler im Play Mode unsteuerbar.");
        }

        [UnityTest]
        public IEnumerator AktiviertesQuellAsset_BewegungErreichtDenReader()
        {
            sourceAsset.Enable();
            CreatePlayer();
            yield return null;

            Press(keyboard.wKey);
            yield return null;

            Assert.That(
                reader.Move.y,
                Is.GreaterThan(0.5f),
                "Tastatureingabe erreicht den Reader nicht.");

            Release(keyboard.wKey);
            yield return null;

            Assert.That(reader.Move.y, Is.LessThan(0.5f));
        }

        [UnityTest]
        public IEnumerator ReaderVerwendetKlon_UndVeraendertDasQuellAssetNicht()
        {
            sourceAsset.Enable();
            int mapCountBefore = sourceAsset.actionMaps.Count;

            CreatePlayer();
            yield return null;

            InputActionAsset clone =
                GetPrivateField<InputActionAsset>(reader, "runtimeInputActions");

            Assert.That(
                clone,
                Is.Not.Null,
                "Es wurde kein Runtime-Klon erzeugt.");
            Assert.That(
                ReferenceEquals(clone, sourceAsset),
                Is.False,
                "Der Reader arbeitet direkt auf dem Quell-Asset.");
            Assert.That(
                sourceAsset.enabled,
                Is.True,
                "Das Quell-Asset wurde unerwartet deaktiviert.");
            Assert.That(
                sourceAsset.actionMaps.Count,
                Is.EqualTo(mapCountBefore),
                "Das Quell-Asset wurde strukturell veraendert.");
        }

        [UnityTest]
        public IEnumerator DeaktivierenUndErneutAktivieren_MapBleibtNutzbar()
        {
            sourceAsset.Enable();
            CreatePlayer();
            yield return null;

            playerObject.SetActive(false);
            yield return null;

            Assert.That(
                reader.GameplayMapEnabled,
                Is.False,
                "Die Gameplay-Map blieb nach OnDisable aktiv.");

            playerObject.SetActive(true);
            yield return null;

            Assert.That(
                reader.GameplayMapEnabled,
                Is.True,
                "Die Gameplay-Map liess sich nicht erneut aktivieren.");
        }

        [UnityTest]
        public IEnumerator ZerstoererterReader_GibtRuntimeKlonFrei()
        {
            sourceAsset.Enable();
            CreatePlayer();
            yield return null;

            InputActionAsset clone =
                GetPrivateField<InputActionAsset>(reader, "runtimeInputActions");
            Assert.That(clone, Is.Not.Null);

            Object.Destroy(playerObject);
            playerObject = null;
            yield return null;

            Assert.That(
                clone == null,
                Is.True,
                "Der Runtime-Klon wurde nicht freigegeben — wiederholtes " +
                "Erzeugen wuerde Assets im Speicher ansammeln.");
        }

        private void CreatePlayer()
        {
            playerObject = new GameObject("PlayerInputReaderTest");
            playerObject.SetActive(false);

            reader = playerObject.AddComponent<PlayerInputReader>();
            SetPrivateField(reader, "inputActions", sourceAsset);

            playerObject.SetActive(true);
        }

        /// <summary>
        /// Bildet die Gameplay-Map des echten Assets vollstaendig genug ab,
        /// damit <c>TryBindActions</c> greift. Fehlt auch nur eine Aktion,
        /// weicht der Reader auf die Fallback-Belegung aus und der hier
        /// getestete Asset-Pfad wird nie durchlaufen.
        /// </summary>
        private static InputActionAsset CreateGameplayAsset()
        {
            InputActionAsset asset =
                ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "TestGameplayActions";

            InputActionMap map = new InputActionMap("Player");
            asset.AddActionMap(map);

            map.AddAction("Move", InputActionType.Value)
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            map.AddAction("Look", InputActionType.Value)
                .AddBinding("<Mouse>/delta");
            map.AddAction("Sprint", InputActionType.Button)
                .AddBinding("<Keyboard>/leftShift");
            map.AddAction("Jump", InputActionType.Button)
                .AddBinding("<Keyboard>/space");
            map.AddAction("Roll", InputActionType.Button)
                .AddBinding("<Keyboard>/leftCtrl");
            map.AddAction("Interact", InputActionType.Button)
                .AddBinding("<Keyboard>/e");
            map.AddAction("QuickslotPrevious", InputActionType.Button)
                .AddBinding("<Keyboard>/q");
            map.AddAction("QuickslotNext", InputActionType.Button)
                .AddBinding("<Keyboard>/tab");
            map.AddAction("QuickslotUse", InputActionType.Button)
                .AddBinding("<Keyboard>/r");

            for (int index = 0; index < 8; index++)
            {
                map.AddAction($"Quickslot{index + 1}", InputActionType.Button)
                    .AddBinding($"<Keyboard>/digit{index + 1}");
            }

            return asset;
        }

        private static void SetPrivateField<T>(
            object target,
            string fieldName,
            T value)
        {
            FieldInfo field = ResolveField(target, fieldName);
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
            where T : class
        {
            FieldInfo field = ResolveField(target, fieldName);
            return field.GetValue(target) as T;
        }

        private static FieldInfo ResolveField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Fehlendes privates Feld {target.GetType().Name}.{fieldName}.");

            return field;
        }
    }
}
