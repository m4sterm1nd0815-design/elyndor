using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Elyndor.Interaction;
using Elyndor.Player;
using Elyndor.UI;
using Elyndor.UIFoundation;

namespace Elyndor.EditorTools
{
    public static class HudGameplayBindingInstaller
    {
        [MenuItem("Elyndor/UI/Bind HUD to Gameplay")]
        public static void BindActiveScene()
        {
            HudFoundationMarker hudMarker = FindSceneObject<HudFoundationMarker>();
            if (hudMarker == null)
            {
                Debug.LogError(
                    "No Elyndor HUD foundation exists in the active scene.");
                return;
            }

            GameObject player = FindPlayer();
            if (player == null)
            {
                Debug.LogError(
                    "No Player object found. Tag or name the player 'Player'.");
                return;
            }

            PlayerVitals vitals = GetOrAdd<PlayerVitals>(player);
            QuickslotRuntimeInventory inventory =
                GetOrAdd<QuickslotRuntimeInventory>(player);

            HudVitalsSource hudSource =
                hudMarker.GetComponent<HudVitalsSource>();

            if (hudSource == null)
            {
                Debug.LogError(
                    "The HUD object has no HudVitalsSource component.");
                return;
            }

            HudGameplayBinder binder =
                GetOrAdd<HudGameplayBinder>(hudMarker.gameObject);

            binder.Configure(vitals, hudSource);

            QuickslotView[] views = hudMarker
                .GetComponentsInChildren<QuickslotView>(true)
                .OrderBy(view => view.name, StringComparer.Ordinal)
                .ToArray();

            if (views.Length != QuickslotRuntimeInventory.SlotCount)
            {
                Debug.LogError(
                    $"Expected 8 QuickslotView objects, found {views.Length}.");
                return;
            }

            QuickslotBarPresenter presenter =
                GetOrAdd<QuickslotBarPresenter>(hudMarker.gameObject);

            for (int i = 0; i < views.Length; i++)
            {
                ConfigureView(views[i], presenter, i);
            }

            presenter.Configure(inventory, vitals, views);

            PlayerInputReader inputReader =
                GetOrAdd<PlayerInputReader>(player);

            QuickslotInputController inputController =
                GetOrAdd<QuickslotInputController>(player);

            inputController.Configure(inputReader, presenter);
            ConfigureInteractionPrompt(hudMarker, player);

            EnsureEventSystem();

            SerializedObject inventoryObject =
                new SerializedObject(inventory);

            SerializedProperty slotsProperty =
                inventoryObject.FindProperty("slots");

            bool hasConfiguredSlot =
                slotsProperty != null &&
                slotsProperty.arraySize > 0 &&
                slotsProperty.GetArrayElementAtIndex(0)
                    .FindPropertyRelative("amount").intValue > 0;

            if (!hasConfiguredSlot)
            {
                Undo.RecordObject(inventory, "Configure prototype quickslots");
                inventory.ConfigurePrototypeLoadout();
                EditorUtility.SetDirty(inventory);
            }


            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(hudMarker.gameObject);
            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene());

            Debug.Log(
                "HUD gameplay binding installed. " +
                "PlayerVitals, quickslots and HUD are connected.");
        }

        private static void ConfigureInteractionPrompt(
            HudFoundationMarker hudMarker,
            GameObject player)
        {
            InteractionDetector detector =
                player.GetComponent<InteractionDetector>();

            InteractionPromptUI[] prompts =
                FindSceneObjects<InteractionPromptUI>();
            InteractionPromptUI source = prompts.FirstOrDefault();

            if (source == null || detector == null)
                return;

            SerializedObject sourceObject = new SerializedObject(source);
            GameObject promptRoot = sourceObject.FindProperty("promptRoot")
                .objectReferenceValue as GameObject;
            Text promptText = sourceObject.FindProperty("promptText")
                .objectReferenceValue as Text;

            InteractionPromptUI prompt = prompts.FirstOrDefault(candidate =>
                candidate.isActiveAndEnabled);

            if (prompt == null)
            {
                GameObject controller = new GameObject(
                    "InteractionPromptController");
                Undo.RegisterCreatedObjectUndo(
                    controller,
                    "Create interaction prompt controller");
                controller.transform.SetParent(hudMarker.transform, false);
                prompt = controller.AddComponent<InteractionPromptUI>();
            }

            Undo.RecordObject(prompt, "Configure interaction prompt");
            prompt.Configure(
                detector,
                promptRoot,
                promptText,
                hudMarker.transform as RectTransform);
            EditorUtility.SetDirty(prompt);
        }

        private static void EnsureEventSystem()
        {
            if (FindSceneObject<EventSystem>() != null)
            {
                return;
            }

            Type inputModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, " +
                "Unity.InputSystem");

            if (inputModuleType == null)
            {
                throw new InvalidOperationException(
                    "InputSystemUIInputModule type could not be resolved.");
            }

            GameObject eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem));

            eventSystemObject.AddComponent(inputModuleType);

            Undo.RegisterCreatedObjectUndo(
                eventSystemObject,
                "Create HUD EventSystem");
        }

        [MenuItem("Elyndor/QA/Validate HUD Gameplay Binding")]
        public static void Validate()
        {
            int errors = 0;

            HudFoundationMarker hud = FindSceneObject<HudFoundationMarker>();
            PlayerVitals vitals = FindSceneObject<PlayerVitals>();
            QuickslotRuntimeInventory inventory =
                FindSceneObject<QuickslotRuntimeInventory>();
            HudGameplayBinder binder =
                FindSceneObject<HudGameplayBinder>();
            QuickslotBarPresenter presenter =
                FindSceneObject<QuickslotBarPresenter>();
            PlayerInputReader inputReader =
                FindSceneObject<PlayerInputReader>();

            errors += Require(hud, "HudFoundationMarker");
            errors += Require(vitals, "PlayerVitals");
            errors += Require(inventory, "QuickslotRuntimeInventory");
            errors += Require(binder, "HudGameplayBinder");
            errors += Require(presenter, "QuickslotBarPresenter");
            errors += Require(inputReader, "PlayerInputReader");

            QuickslotInputController[] inputControllers =
                FindSceneObjects<QuickslotInputController>();

            if (inputControllers.Length != 1)
            {
                Debug.LogError(
                    "Expected exactly one QuickslotInputController, " +
                    $"found {inputControllers.Length}.");
                errors++;
            }

            QuickslotSlotRelay[] relays =
                FindSceneObjects<QuickslotSlotRelay>();

            if (relays.Length != QuickslotRuntimeInventory.SlotCount)
            {
                Debug.LogError(
                    $"Expected 8 quickslot relays, found {relays.Length}.");
                errors++;
            }

            if (FindSceneObject<EventSystem>() == null)
            {
                Debug.LogError("No EventSystem exists in the active scene.");
                errors++;
            }

            Debug.Log(
                $"HUD gameplay QA finished with {errors} blocking errors.");
        }

        [MenuItem("Elyndor/Debug/HUD/Take 25 Damage")]
        public static void TestDamage()
        {
            PlayerVitals vitals = FindSceneObject<PlayerVitals>();
            vitals?.TakeDamage(25f);
        }

        [MenuItem("Elyndor/Debug/HUD/Spend 30 Stamina")]
        public static void TestStamina()
        {
            PlayerVitals vitals = FindSceneObject<PlayerVitals>();
            vitals?.TrySpendStamina(30f);
        }

        [MenuItem("Elyndor/Debug/HUD/Gain 25 Memory")]
        public static void TestMemory()
        {
            PlayerVitals vitals = FindSceneObject<PlayerVitals>();
            vitals?.ApplyMemory(25f);
        }

        [MenuItem("Elyndor/Debug/HUD/Reset Vitals")]
        public static void TestReset()
        {
            PlayerVitals vitals = FindSceneObject<PlayerVitals>();
            vitals?.ResetToFull();
        }

        private static void ConfigureView(
            QuickslotView view,
            QuickslotBarPresenter presenter,
            int index)
        {
            Button button = GetOrAdd<Button>(view.gameObject);
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;

            Text glyphLabel = view
                .GetComponentsInChildren<Text>(true)
                .FirstOrDefault(text => text.name == "IconGlyph");

            Text amountLabel = view
                .GetComponentsInChildren<Text>(true)
                .FirstOrDefault(text => text.name == "Amount");

            if (amountLabel == null)
                amountLabel = CreateAmountLabel(view.transform);

            QuickslotFocusVisual focusVisual =
                view.GetComponent<QuickslotFocusVisual>();

            SerializedObject viewObject = new SerializedObject(view);
            viewObject.FindProperty("glyphLabel").objectReferenceValue =
                glyphLabel;
            viewObject.FindProperty("amountLabel").objectReferenceValue =
                amountLabel;
            viewObject.FindProperty("focusVisual").objectReferenceValue =
                focusVisual;
            viewObject.ApplyModifiedPropertiesWithoutUndo();

            QuickslotSlotRelay relay =
                GetOrAdd<QuickslotSlotRelay>(view.gameObject);

            relay.Configure(index, presenter);

            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(relay);
        }

        private static Text CreateAmountLabel(Transform parent)
        {
            GameObject labelObject = new GameObject(
                "Amount",
                typeof(RectTransform),
                typeof(Text));

            Undo.RegisterCreatedObjectUndo(
                labelObject,
                "Create quickslot amount label");

            labelObject.transform.SetParent(parent, false);

            RectTransform rect =
                labelObject.GetComponent<RectTransform>();

            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-5f, 4f);
            rect.sizeDelta = new Vector2(30f, 20f);

            Text text = labelObject.GetComponent<Text>();
            text.font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 13;
            text.alignment = TextAnchor.LowerRight;
            text.color = Color.white;
            text.raycastTarget = false;

            return text;
        }

        private static GameObject FindPlayer()
        {
            GameObject player = null;

            try
            {
                player = GameObject.FindWithTag("Player");
            }
            catch (UnityException)
            {
                // Continue with the object name fallback.
            }

            return player != null ? player : GameObject.Find("Player");
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : Undo.AddComponent<T>(target);
        }

        private static int Require<T>(T value, string name)
            where T : UnityEngine.Object
        {
            if (value != null)
                return 0;

            Debug.LogError($"Missing required component: {name}");
            return 1;
        }

        private static T FindSceneObject<T>()
            where T : Component
        {
            return FindSceneObjects<T>().FirstOrDefault();
        }

        private static T[] FindSceneObjects<T>()
            where T : Component
        {
            return Resources.FindObjectsOfTypeAll<T>()
                .Where(component =>
                    component != null &&
                    component.gameObject.scene.IsValid())
                .ToArray();
        }
    }
}
