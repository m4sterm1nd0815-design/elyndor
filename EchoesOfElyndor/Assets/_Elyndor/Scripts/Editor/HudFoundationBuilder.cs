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

        [MenuItem("Elyndor/UI/Rebuild Styled HUD Foundation")]
        public static void BuildStyled()
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

            var root = new GameObject(
                "ElyndorHudFoundation",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(HudFoundationMarker),
                typeof(HudVitalsSource),
                typeof(HudVitalsPresenter));

            Stretch(root.GetComponent<RectTransform>(), 0f);

            Image health;
            Image stamina;
            Image memory;

            CreateVitals(root.transform, sprite, theme, out health, out stamina, out memory);
            CreateQuickslots(root.transform, sprite, theme);

            var presenter = root.GetComponent<HudVitalsPresenter>();
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("source").objectReferenceValue = root.GetComponent<HudVitalsSource>();
            serialized.FindProperty("healthFill").objectReferenceValue = health;
            serialized.FindProperty("staminaFill").objectReferenceValue = stamina;
            serialized.FindProperty("memoryFill").objectReferenceValue = memory;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log($"Styled HUD foundation created: {PrefabPath}");
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
                parent,
                "VitalsFrame",
                sprite,
                theme.panel,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(30f, -30f),
                new Vector2(420f, 112f));

            CreateOutline(frame.transform, sprite, theme.goldSoft, 2f);

            var container = new GameObject("Vitals", typeof(RectTransform));
            container.transform.SetParent(frame.transform, false);
            Stretch(container.GetComponent<RectTransform>(), 14f);

            health = CreateVitalRow(container.transform, "Health", 0f, 72f, 26f, sprite, theme, theme.health, "Leben");
            stamina = CreateVitalRow(container.transform, "Stamina", 36f, 72f, 18f, sprite, theme, theme.stamina, "Ausdauer");
            memory = CreateVitalRow(container.transform, "Memory", 68f, 72f, 18f, sprite, theme, theme.memory, "Erinnerung");
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
            string labelText)
        {
            var row = new GameObject(name + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -y);
            rowRect.sizeDelta = new Vector2(0f, height);

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(row.transform, false);

            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(labelWidth, 0f);

            Text labelTextComponent = label.GetComponent<Text>();
            labelTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelTextComponent.text = labelText;
            labelTextComponent.fontSize = 13;
            labelTextComponent.alignment = TextAnchor.MiddleLeft;
            labelTextComponent.color = theme.textMuted;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(row.transform, false);

            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0f);
            backgroundRect.anchorMax = new Vector2(1f, 1f);
            backgroundRect.offsetMin = new Vector2(labelWidth + 8f, 0f);
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

            CreateOutline(background.transform, sprite, theme.goldSoft, 1f);
            return fillImage;
        }

        private static void CreateQuickslots(
            Transform parent,
            Sprite sprite,
            ElyndorUITheme theme)
        {
            var frame = CreatePanel(
                parent,
                "QuickslotFrame",
                sprite,
                theme.panel,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 24f),
                new Vector2(700f, 82f));

            CreateOutline(frame.transform, sprite, theme.goldSoft, 2f);

            const float slotWidth = 76f;
            const float slotHeight = 62f;
            const float spacing = 8f;
            float totalWidth = 8f * slotWidth + 7f * spacing;
            float startX = -totalWidth * 0.5f + slotWidth * 0.5f;

            for (int i = 0; i < 8; i++)
            {
                CreateQuickslot(
                    frame.transform,
                    i,
                    sprite,
                    theme,
                    new Vector2(startX + i * (slotWidth + spacing), 0f),
                    new Vector2(slotWidth, slotHeight));
            }
        }

        private static void CreateQuickslot(
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
                typeof(QuickslotView));

            slot.transform.SetParent(parent, false);

            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image slotImage = slot.GetComponent<Image>();
            slotImage.sprite = sprite;
            slotImage.type = Image.Type.Sliced;
            slotImage.color = theme.panelSoft;

            CreateOutline(slot.transform, sprite, theme.goldSoft, 1f);

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

            var icon = new GameObject("IconPlaceholder", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(slot.transform, false);

            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, -2f);
            iconRect.sizeDelta = new Vector2(28f, 28f);

            Image iconImage = icon.GetComponent<Image>();
            iconImage.sprite = sprite;
            iconImage.type = Image.Type.Sliced;
            iconImage.color = new Color(theme.memory.r, theme.memory.g, theme.memory.b, 0.18f);
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

        private static void CreateOutline(
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
