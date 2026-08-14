using UnityEngine;
using UnityEngine.UI;

namespace Elyndor.Enemies.UI
{
    /// <summary>
    /// Reine Darstellung der Gegner-Lebensanzeige. Die Klasse kennt weder
    /// <see cref="EnemyHealth"/> noch Sichtbarkeitsregeln — sie nimmt nur einen
    /// Fuellwert und einen Sichtbarkeitswunsch entgegen. Damit bleibt die
    /// spaetere endgueltige Gegneroptik austauschbar: es genuegt, eine eigene
    /// Instanz mit anderen Bildern zu belegen.
    ///
    /// Die Sichtbarkeit schaltet bewusst den <see cref="Canvas"/> statt das
    /// GameObject: ein abgeschaltetes GameObject wuerde auch den steuernden
    /// <see cref="EnemyHealthBar"/> stilllegen, falls beide auf demselben
    /// Objekt liegen, und die Anzeige koennte nie wieder auftauchen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealthBarVisual : MonoBehaviour
    {
        private static readonly Color PlaceholderBackground =
            new Color(0.04f, 0.04f, 0.05f, 0.75f);

        private static readonly Color PlaceholderFill =
            new Color(0.56f, 0.08f, 0.09f, 1f);

        [SerializeField] private Canvas canvas;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;

        private static Sprite sharedPlaceholderSprite;

        /// <summary>Aktuell dargestellter Fuellwert zwischen 0 und 1.</summary>
        public float Fill => fillImage == null ? 0f : fillImage.fillAmount;

        public bool IsVisible
        {
            get
            {
                if (canvas != null)
                {
                    return canvas.enabled;
                }

                return fillImage != null && fillImage.enabled;
            }
        }

        public Image FillImage => fillImage;
        public Image BackgroundImage => backgroundImage;
        public Canvas Canvas => canvas;

        private void Awake()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }
        }

        /// <summary>Belegt die Darstellung nachtraeglich, etwa nach dem Aufbau.</summary>
        public void Assign(Canvas newCanvas, Image background, Image fill)
        {
            canvas = newCanvas;
            backgroundImage = background;
            fillImage = fill;
        }

        public void SetFill(float value01)
        {
            if (fillImage == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(value01);

            // Nur bei echter Aenderung schreiben: jede Zuweisung markiert das
            // Bild als schmutzig und laesst den Canvas neu bauen.
            if (!Mathf.Approximately(fillImage.fillAmount, clamped))
            {
                fillImage.fillAmount = clamped;
            }
        }

        public void SetVisible(bool visible)
        {
            if (canvas != null)
            {
                if (canvas.enabled != visible)
                {
                    canvas.enabled = visible;
                }

                return;
            }

            // Ohne eigenen Canvas bleibt der Rueckfallweg ueber die Grafiken.
            if (backgroundImage != null && backgroundImage.enabled != visible)
            {
                backgroundImage.enabled = visible;
            }

            if (fillImage != null && fillImage.enabled != visible)
            {
                fillImage.enabled = visible;
            }
        }

        /// <summary>
        /// Baut eine schlichte Platzhalteranzeige als Kind von
        /// <paramref name="parent"/>. Bewusst ohne Prefab und ohne Szene: die
        /// endgueltige Gegneroptik steht noch nicht fest, und ein Prefab wuerde
        /// eine Optik festschreiben, die spaeter ohnehin ersetzt wird.
        /// </summary>
        /// <param name="parent">Uebergeordneter Transform; darf null sein.</param>
        /// <param name="size">Groesse in Weltmetern (Breite, Hoehe).</param>
        public static EnemyHealthBarVisual CreatePlaceholder(
            Transform parent, Vector2 size)
        {
            GameObject root = new GameObject(
                "EnemyHealthBarPlaceholder",
                typeof(RectTransform),
                typeof(Canvas));

            RectTransform rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(
                Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));

            if (parent != null)
            {
                root.layer = parent.gameObject.layer;
            }

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            Image background = CreateStretchedImage(
                rect, "Background", PlaceholderBackground);

            Image fill = CreateStretchedImage(rect, "Fill", PlaceholderFill);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            EnemyHealthBarVisual visual =
                root.AddComponent<EnemyHealthBarVisual>();
            visual.Assign(canvas, background, fill);

            return visual;
        }

        private static Image CreateStretchedImage(
            RectTransform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            go.layer = parent.gameObject.layer;

            Image image = go.AddComponent<Image>();
            image.color = color;

            // Ohne Sprite zeichnet Image nur ein einfaches Rechteck und
            // ignoriert fillAmount vollstaendig. Ein geteiltes weisses Sprite
            // macht die Fuellung ueberhaupt erst sichtbar.
            image.sprite = GetPlaceholderSprite();
            image.raycastTarget = false;

            return image;
        }

        private static Sprite GetPlaceholderSprite()
        {
            if (sharedPlaceholderSprite != null)
            {
                return sharedPlaceholderSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;

            sharedPlaceholderSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);

            sharedPlaceholderSprite.name = "EnemyHealthBarPlaceholderSprite";
            sharedPlaceholderSprite.hideFlags = HideFlags.HideAndDontSave;

            return sharedPlaceholderSprite;
        }
    }
}
