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
    /// Reproduktion gegen das <b>echte projektweite</b> Actions-Asset.
    ///
    /// Bewusst ohne <c>InputTestFixture</c>: Diese Fixture setzt das
    /// Input-System zurueck und ersetzt dabei genau den Zustand, um den es hier
    /// geht. Ein synthetisch gebautes Asset reproduziert den Fehler nicht — der
    /// Ausloeser steckt in der Behandlung des projektweiten Assets, das Unity
    /// beim Start selbst aktiviert.
    /// </summary>
    public sealed class PlayerInputReaderProjectAssetTests
    {
        private GameObject playerObject;

        [TearDown]
        public void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
                playerObject = null;
            }
        }

        [UnityTest]
        public IEnumerator ProjektweitesAsset_GameplayMapWirdAktiv()
        {
            // Ueber den Snapshot, nicht ueber InputSystem.actions: Eine
            // vorher gelaufene InputTestFixture haette die Referenz sonst
            // geloescht und dieser Regressionstest wuerde sich still
            // ueberspringen.
            InputActionAsset projectActions = ProjectInputActions.Current;

            if (projectActions == null)
            {
                Assert.Ignore(
                    "Kein projektweites Actions-Asset gesetzt — Test nicht anwendbar.");
                yield break;
            }

            // Ausgangslage wie im Spiel: Unity hat das Asset bereits aktiviert.
            projectActions.Enable();

            playerObject = new GameObject("ProjectAssetInputReaderTest");
            playerObject.SetActive(false);

            PlayerInputReader reader =
                playerObject.AddComponent<PlayerInputReader>();
            SetPrivateField(reader, "inputActions", projectActions);

            playerObject.SetActive(true);
            yield return null;

            InputActionAsset clone =
                GetPrivateField<InputActionAsset>(reader, "runtimeInputActions");

            Assert.That(
                clone,
                Is.Not.Null,
                "Der Reader hat das projektweite Asset nicht geklont — dann " +
                "greift er direkt darauf zu, was ebenfalls unerwuenscht ist.");

            Assert.That(
                reader.GameplayMapEnabled,
                Is.True,
                "Die Gameplay-Map des Klons liess sich nicht aktivieren. " +
                "Genau dieser Zustand macht den Spieler im Play Mode " +
                "unsteuerbar (\"Map must be contained in state\").");
        }

        private static void SetPrivateField<T>(
            object target,
            string fieldName,
            T value)
        {
            ResolveField(target, fieldName).SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
            where T : class
        {
            return ResolveField(target, fieldName).GetValue(target) as T;
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
