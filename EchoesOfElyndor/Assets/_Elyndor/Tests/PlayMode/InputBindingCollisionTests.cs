using System.Linq;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Elyndor.Tests
{
    /// <summary>
    /// Gegenproben gegen das <b>echte projektweite</b> Actions-Asset.
    ///
    /// Bewusst <b>ohne</b> <see cref="InputTestFixture"/>: Deren Setup setzt das
    /// Input-System zurueck und loescht dabei genau die Referenz
    /// <see cref="InputSystem.actions"/>, um die es hier geht — die Tests wuerden
    /// sich sonst still selbst ueberspringen und die eigentliche Absicherung
    /// waere wirkungslos.
    ///
    /// Hintergrund: <c>Interact</c> und das Inventar lagen am Controller beide
    /// auf <c>buttonNorth</c>. Ein Druck oeffnete das Inventar und untersuchte
    /// gleichzeitig das Objekt davor. Die Tests der Eingabeschicht laufen gegen
    /// ein synthetisches Asset; ohne diese Gegenprobe koennte die ausgelieferte
    /// Belegung erneut kollidieren, ohne dass ein Test rot wird.
    /// </summary>
    public sealed class InputBindingCollisionTests
    {
        [Test]
        public void ProjektAsset_InteractUndInventoryTeilenKeinControl()
        {
            InputActionMap map = RequirePlayerMap();

            InputAction interact = map.FindAction("Interact", false);
            InputAction inventory = map.FindAction("Inventory", false);

            Assert.That(interact, Is.Not.Null, "Interact fehlt.");
            Assert.That(
                inventory,
                Is.Not.Null,
                "Inventory fehlt. Ohne eigene Action teilt sich das Inventar " +
                "wieder eine Taste mit Interact.");

            string[] interactPaths = interact.bindings
                .Select(binding => binding.path).ToArray();
            string[] inventoryPaths = inventory.bindings
                .Select(binding => binding.path).ToArray();

            Assert.That(
                interactPaths.Intersect(inventoryPaths),
                Is.Empty,
                "Interact und Inventory duerfen kein Control teilen. " +
                $"Interact: {string.Join(", ", interactPaths)} | " +
                $"Inventory: {string.Join(", ", inventoryPaths)}");
        }

        /// <summary>
        /// Kein Gamepad-Control darf zwei verschiedene Aktionen bedienen.
        /// Deckt auch kuenftige Belegungen ab, nicht nur das bekannte Paar.
        /// </summary>
        [Test]
        public void ProjektAsset_KeinGamepadControlBedientZweiAktionen()
        {
            InputActionMap map = RequirePlayerMap();

            string[] collisions = map.bindings
                .Where(binding =>
                    binding.path != null &&
                    binding.path.StartsWith("<Gamepad>") &&
                    !binding.isComposite)
                .GroupBy(binding => binding.path)
                .Select(group => new
                {
                    Path = group.Key,
                    Actions = group
                        .Select(binding => binding.action)
                        .Distinct()
                        .ToArray()
                })
                .Where(entry => entry.Actions.Length > 1)
                .Select(entry =>
                    $"{entry.Path} -> {string.Join(" + ", entry.Actions)}")
                .ToArray();

            Assert.That(
                collisions,
                Is.Empty,
                "Doppelt belegte Gamepad-Controls: " +
                string.Join(" | ", collisions));
        }

        /// <summary>
        /// Die Belegungen, die der Gameplay-Code frueher fest verdrahtet hatte,
        /// muessen im Asset stehen — sonst laesst sich das Spiel nach der
        /// Vereinheitlichung schlicht nicht mehr wie vorher bedienen.
        /// </summary>
        [Test]
        [TestCase("Attack", "<Mouse>/leftButton")]
        [TestCase("Attack", "<Gamepad>/rightTrigger")]
        [TestCase("Block", "<Keyboard>/q")]
        [TestCase("Block", "<Gamepad>/leftTrigger")]
        [TestCase("Inventory", "<Keyboard>/i")]
        [TestCase("Inventory", "<Gamepad>/select")]
        public void ProjektAsset_EnthaeltBisherigeBelegung(
            string actionName,
            string expectedPath)
        {
            InputActionMap map = RequirePlayerMap();
            InputAction action = map.FindAction(actionName, false);

            Assert.That(action, Is.Not.Null, $"Action '{actionName}' fehlt.");
            Assert.That(
                action.bindings.Select(binding => binding.path),
                Does.Contain(expectedPath),
                $"'{actionName}' hat '{expectedPath}' verloren.");
        }

        /// <summary>
        /// <c>start</c> bleibt fuer Pause reserviert und darf nicht nebenbei
        /// mit einer Gameplay-Aktion belegt werden.
        /// </summary>
        [Test]
        public void ProjektAsset_StartTasteBleibtFuerPauseFrei()
        {
            InputActionMap map = RequirePlayerMap();

            string[] onStart = map.bindings
                .Where(binding => binding.path == "<Gamepad>/start")
                .Select(binding => binding.action)
                .ToArray();

            Assert.That(
                onStart,
                Is.Empty,
                "Auf <Gamepad>/start liegt bereits: " +
                string.Join(", ", onStart));
        }

        private static InputActionMap RequirePlayerMap()
        {
            InputActionAsset projectActions = ProjectInputActions.Current;

            Assert.That(
                projectActions,
                Is.Not.Null,
                "Kein projektweites Actions-Asset gesetzt. Ohne dieses Asset " +
                "prueft dieser Test nichts — deshalb bewusst ein Fehlschlag " +
                "statt eines stillen Ignore.");

            InputActionMap map = projectActions.FindActionMap("Player", false);

            Assert.That(map, Is.Not.Null, "Player-Map fehlt im Projekt-Asset.");
            return map;
        }
    }
}
