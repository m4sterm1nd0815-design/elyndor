using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Elyndor.UIFoundation;

namespace Elyndor.EditorTools
{
    public static class HudFoundationBuilder
    {
        private const string Folder = "Assets/_Elyndor/Prefabs/UI";
        private const string PrefabPath = Folder + "/ElyndorHudFoundation.prefab";
        private const string ThemePath = Folder + "/ElyndorUITheme.asset";

        [MenuItem("Elyndor/UI/Rebuild Polished HUD Foundation")]
        public static void BuildPolished()
        {
            EnsureFolder("Assets/_Elyndor");
            EnsureFolder("Assets/_Elyndor/Prefabs");
            EnsureFolder(Folder);

            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            ElyndorUITheme theme = AssetDatabase.LoadAssetAtPath<ElyndorUITheme>(ThemePath);

            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<ElyndorUITheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            // Updated palette: almost-black surfaces and restrained gold accents.
            theme.panel = new Color(0.018f, 0.022f, 0.032f, 0.9f);
            theme.panelSoft = new Color(0.035f, 0.04f, 0.055f, 0.76f);
            theme.gold = new Color(0.82f, 0.68f, 0.30f, 1f);
            theme.goldSoft = new Color(0.50f, 0.40f, 0.18f, 0.78f);
            theme.health = new Color(0.62f, 0.08f, 0.10f, 1f);
            theme.stamina = new Color(0.20f, 0.58f, 0.25f, 1f);
            theme.memory = new Color(0.16f, 0.82f, 0.98f, 1f);
            theme.text = new Color(0.95f, 0.93f, 0.85f, 1f);
            theme.textMuted = new Color(0.70f, 0.71f, 0.74f, 1f);
            EditorUtility.SetDirty(theme);

            var root = new GameObject(
                "ElyndorHudFoundation",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(HudFoundationMarker),
                typeof(HudVitalsSource),
                typeof(HudVitalsPresenter),
                typeof(ElyndorUIAccessibilityBridge));

            Stretch(root.GetComponent<RectTransform>(), 0f);

            Image health;
            Image stamina;
            Image memory;
            CreateVitals(root.transform, sprite, theme, out health, out stamina, out memory);
            QuickslotFocusVisual[] focusVisuals = CreateQuickslots(root.transform, sprite, theme);

            var presenter = root.GetComponent<HudVitalsPresenter>();
            var presenterObject = new SerializedObject(presenter);
            presenterObject.FindProperty("source").objectReferenceValue = root.GetComponent<HudVitalsSource>();
            presenterObject.FindProperty("healthFill").objectReferenceValue = health;
            presenterObject.FindProperty("staminaFill").objectReferenceValue = stamina;
            presenterObject.FindProperty("memoryFill").objectReferenceValue = memory;
            presenterObject.ApplyModifiedPropertiesWithoutUndo();

            var accessibility = root.GetComponent<ElyndorUIAccessibilityBridge>();
            var accessibilityObject = new SerializedObject(accessibility);
            accessibilityObject.FindProperty("uiRoot").objectReferenceValue = root.GetComponent<RectTransform>();
            SerializedProperty focusArray = accessibilityObject.FindProperty("focusVisuals");
            focusArray.arraySize = focusVisuals.Length;
            for (int i = 0; i < focusVisuals.Length; i++)
                focusArray.GetArrayElementAtIndex(i).objectReferenceValue = focusVisuals[i];
            accessibilityObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log($"Polished HUD foundation created: {PrefabPath}");
        }

        private static void CreateVitals(
            Transform parent,
            Sprite sprite,
            ElyndorUITheme theme,
            out Image health,
            out Image stamina,
            out Image memory)
        {
            var frame = CreatePanel(
                parent, "VitalsFrame", sprite, theme.panel,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -30f), new Vector2(420f, 116f));

            CreateThinBorder(frame.transform, sprite, theme.goldSoft, 2f);

            var container = new GameObject("Vitals", typeof(RectTransform));
            container.transform.SetParent(frame.transform, false);
            Stretch(container.GetComponent<RectTransform>(), 13f);

            health = CreateVitalRow(container.transform, "Health", 0f, 90f, 26f, sprite, theme, theme.health, "Leben", HudGlyphType.Heart);
            stamina = CreateVitalRow(container.transform, "Stamina", 36f, 90f, 18f, sprite, theme, theme.stamina, "Ausdauer", HudGlyphType.Stamina);
            memory = CreateVitalRow(container.transform, "Memory", 68f, 90f, 18f, sprite, theme, theme.memory, "Erinnerung", HudGlyphType.Memory);
        }

        private static Image CreateVitalRow(
            Transform parent,
            string name,
            float y,
            float labelWidth,
            float height,
            Sprite sprite,
            ElyndorUITheme theme,
            Color fillColor,
            string labelText,
            HudGlyphType glyphType)
        {
            var row = new GameObject(name + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -y);
            rowRect.sizeDelta = new Vector2(0f, height);

            var glyph = new GameObject("Glyph", typeof(RectTransform), typeof(Text), typeof(HudIconGlyph));
            glyph.transform.SetParent(row.transform, false);

            RectTransform glyphRect = glyph.GetComponent<RectTransform>();
            glyphRect.anchorMin = new Vector2(0f, 0f);
            glyphRect.anchorMax = new Vector2(0f, 1f);
            glyphRect.pivot = new Vector2(0f, 0.5f);
            glyphRect.anchoredPosition = Vector2.zero;
            glyphRect.sizeDelta = new Vector2(22f, 0f);

            Text glyphText = glyph.GetComponent<Text>();
            glyphText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            glyphText.fontSize = 15;
            glyphText.alignment = TextAnchor.MiddleCenter;
            glyphText.color = fillColor;

            var glyphComponent = glyph.GetComponent<HudIconGlyph>();
            var glyphSerialized = new SerializedObject(glyphComponent);
            glyphSerialized.FindProperty("glyphType").enumValueIndex = (int)glyphType;
            glyphSerialized.FindProperty("label").objectReferenceValue = glyphText;
            glyphSerialized.ApplyModifiedPropertiesWithoutUndo();
            glyphComponent.Refresh();

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(row.transform, false);

            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(26f, 0f);
            labelRect.sizeDelta = new Vector2(labelWidth, 0f);

            Text labelComponent = label.GetComponent<Text>();
            labelComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelComponent.text = labelText;
            labelComponent.fontSize = 13;
            labelComponent.alignment = TextAnchor.MiddleLeft;
            labelComponent.color = theme.textMuted;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(row.transform, false);

            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0f);
            backgroundRect.anchorMax = new Vector2(1f, 1f);
            backgroundRect.offsetMin = new Vector2(labelWidth + 36f, 0f);
            backgroundRect.offsetMax = Vector2.zero;

            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = sprite;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.color = theme.panelSoft;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(background.transform, false);
            Stretch(fill.GetComponent<RectTransform>(), 3f);

            Image fillImage = fill.GetComponent<Image>();
            fillImage.sprite = sprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
            fillImage.color = fillColor;

            CreateThinBorder(background.transform, sprite, theme.goldSoft, 1f);
            return fillImage;
        }

        private static QuickslotFocusVisual[] CreateQuickslots(
            Transform parent,
            Sprite sprite,
            ElyndorUITheme theme)
        {
            var frame = CreatePanel(
                parent, "QuickslotFrame", sprite, theme.panel,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 24f), new Vector2(692f, 82f));

            CreateThinBorder(frame.transform, sprite, theme.goldSoft, 2f);

            const float slotWidth = 74f;
            const float slotHeight = 60f;
            const float spacing = 8f;
            float totalWidth = 8f * slotWidth + 7f * spacing;
            float startX = -totalWidth * 0.5f + slotWidth * 0.5f;

            QuickslotFocusVisual[] focusVisuals = new QuickslotFocusVisual[8];

            for (int i = 0; i < 8; i++)
            {
                focusVisuals[i] = CreateQuickslot(
                    frame.transform,
                    i,
                    sprite,
                    theme,
                    new Vector2(startX + i * (slotWidth + spacing), 0f),
                    new Vector2(slotWidth, slotHeight));
            }

            return focusVisuals;
        }

        private static QuickslotFocusVisual CreateQuickslot(
            Transform parent,
            int index,
            Sprite sprite,
            ElyndorUITheme theme,
            Vector2 position,
            Vector2 size)
        {
            var slot = new GameObject(
                $"Quickslot_{index + 1}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(QuickslotView),
                typeof(QuickslotFocusVisual));

            slot.transform.SetParent(parent, false);

            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image background = slot.GetComponent<Image>();
            background.sprite = sprite;
            background.type = Image.Type.Sliced;
            background.color = theme.panelSoft;

            var focusFrameObject = new GameObject("FocusFrame", typeof(RectTransform), typeof(Image));
            focusFrameObject.transform.SetParent(slot.transform, false);
            Stretch(focusFrameObject.GetComponent<RectTransform>(), -2f);

            Image focusFrame = focusFrameObject.GetComponent<Image>();
            focusFrame.sprite = sprite;
            focusFrame.type = Image.Type.Sliced;
            focusFrame.color = theme.memory;
            focusFrame.raycastTarget = false;
            focusFrame.enabled = false;

            CreateThinBorder(slot.transform, sprite, theme.goldSoft, 1f);

            var glyph = new GameObject("Glyph", typeof(RectTransform), typeof(Text));
            glyph.transform.SetParent(slot.transform, false);

            RectTransform glyphRect = glyph.GetComponent<RectTransform>();
            glyphRect.anchorMin = new Vector2(0f, 1f);
            glyphRect.anchorMax = new Vector2(0f, 1f);
            glyphRect.pivot = new Vector2(0f, 1f);
            glyphRect.anchoredPosition = new Vector2(6f, -4f);
            glyphRect.sizeDelta = new Vector2(20f, 18f);

            Text glyphText = glyph.GetComponent<Text>();
            glyphText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            glyphText.text = (index + 1).ToString();
            glyphText.fontSize = 13;
            glyphText.alignment = TextAnchor.UpperLeft;
            glyphText.color = theme.text;

            var icon = new GameObject("IconGlyph", typeof(RectTransform), typeof(Text), typeof(HudIconGlyph));
            icon.transform.SetParent(slot.transform, false);

            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, -2f);
            iconRect.sizeDelta = new Vector2(30f, 30f);

            Text iconText = icon.GetComponent<Text>();
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 20;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = new Color(theme.memory.r, theme.memory.g, theme.memory.b, 0.55f);

            var iconGlyph = icon.GetComponent<HudIconGlyph>();
            var iconSerialized = new SerializedObject(iconGlyph);
            iconSerialized.FindProperty("glyphType").enumValueIndex = (int)HudGlyphType.EmptySlot;
            iconSerialized.FindProperty("label").objectReferenceValue = iconText;
            iconSerialized.ApplyModifiedPropertiesWithoutUndo();
            iconGlyph.Refresh();

            var focusVisual = slot.GetComponent<QuickslotFocusVisual>();
            var focusSerialized = new SerializedObject(focusVisual);
            focusSerialized.FindProperty("focusFrame").objectReferenceValue = focusFrame;
            focusSerialized.ApplyModifiedPropertiesWithoutUndo();

            return focusVisual;
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return panel;
        }

        private static void CreateThinBorder(
            Transform parent,
            Sprite sprite,
            Color color,
            float inset)
        {
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(parent, false);
            Stretch(border.GetComponent<RectTransform>(), inset);

            Image image = border.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.color = color;
            image.raycastTarget = false;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
