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

        [MenuItem("Elyndor/UI/Create HUD Foundation")]
        public static void Build()
        {
            EnsureFolder("Assets/_Elyndor");
            EnsureFolder("Assets/_Elyndor/Prefabs");
            EnsureFolder(Folder);

            Sprite uiSprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var root = new GameObject(
                "ElyndorHudFoundation",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(HudVitalsSource),
                typeof(HudVitalsPresenter));

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image health = null;
            Image stamina = null;
            Image memory = null;

            CreateVitals(
                root.transform,
                uiSprite,
                out health,
                out stamina,
                out memory);

            CreateQuickslots(root.transform, uiSprite);

            var presenter = root.GetComponent<HudVitalsPresenter>();
            var source = root.GetComponent<HudVitalsSource>();
            var serializedPresenter = new SerializedObject(presenter);

            serializedPresenter.FindProperty("source").objectReferenceValue = source;
            serializedPresenter.FindProperty("healthFill").objectReferenceValue = health;
            serializedPresenter.FindProperty("staminaFill").objectReferenceValue = stamina;
            serializedPresenter.FindProperty("memoryFill").objectReferenceValue = memory;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Debug.Log($"HUD foundation created: {PrefabPath}");
        }

        private static void CreateVitals(
            Transform parent,
            Sprite sprite,
            out Image health,
            out Image stamina,
            out Image memory)
        {
            var container = new GameObject(
                "Vitals",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));

            container.transform.SetParent(parent, false);

            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(28f, -28f);
            rect.sizeDelta = new Vector2(340f, 90f);

            VerticalLayoutGroup layout =
                container.GetComponent<VerticalLayoutGroup>();

            layout.spacing = 7f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            health = CreateBar(
                container.transform,
                "Health",
                24f,
                sprite,
                new Color(0.55f, 0.08f, 0.08f, 1f));

            stamina = CreateBar(
                container.transform,
                "Stamina",
                18f,
                sprite,
                new Color(0.18f, 0.55f, 0.22f, 1f));

            memory = CreateBar(
                container.transform,
                "Memory",
                18f,
                sprite,
                new Color(0.15f, 0.75f, 0.9f, 1f));
        }

        private static Image CreateBar(
            Transform parent,
            string name,
            float height,
            Sprite sprite,
            Color color)
        {
            var bar = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));

            bar.transform.SetParent(parent, false);

            LayoutElement layout = bar.GetComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;

            Image image = bar.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillAmount = 1f;
            image.color = color;

            return image;
        }

        private static void CreateQuickslots(Transform parent, Sprite sprite)
        {
            var bar = new GameObject(
                "Quickslots",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));

            bar.transform.SetParent(parent, false);

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 30f);
            rect.sizeDelta = new Vector2(820f, 96f);

            HorizontalLayoutGroup layout =
                bar.GetComponent<HorizontalLayoutGroup>();

            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < 8; i++)
            {
                var slot = new GameObject(
                    $"Quickslot_{i + 1}",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement),
                    typeof(QuickslotView));

                slot.transform.SetParent(bar.transform, false);

                LayoutElement slotLayout = slot.GetComponent<LayoutElement>();
                slotLayout.preferredWidth = 88f;
                slotLayout.preferredHeight = 88f;

                Image slotImage = slot.GetComponent<Image>();
                slotImage.sprite = sprite;
                slotImage.type = Image.Type.Sliced;
                slotImage.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

                var glyph = new GameObject(
                    "Glyph",
                    typeof(RectTransform),
                    typeof(Text));

                glyph.transform.SetParent(slot.transform, false);

                RectTransform glyphRect = glyph.GetComponent<RectTransform>();
                glyphRect.anchorMin = Vector2.zero;
                glyphRect.anchorMax = Vector2.one;
                glyphRect.offsetMin = Vector2.zero;
                glyphRect.offsetMax = Vector2.zero;

                Text glyphText = glyph.GetComponent<Text>();
                glyphText.text = (i + 1).ToString();
                glyphText.alignment = TextAnchor.MiddleCenter;
                glyphText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                glyphText.fontSize = 22;
                glyphText.color = Color.white;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent =
                System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");

            string name = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
