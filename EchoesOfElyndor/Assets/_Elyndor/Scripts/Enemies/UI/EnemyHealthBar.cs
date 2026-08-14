using UnityEngine;

namespace Elyndor.Enemies.UI
{
    /// <summary>
    /// World-Space-Lebensanzeige eines Gegners. Die Anzeige haengt allein an
    /// <see cref="EnemyHealth"/> und dessen Ereignissen — der Lebenswert wird
    /// nie abgefragt, sondern ausschliesslich bei
    /// <see cref="EnemyHealth.HealthChanged"/> und
    /// <see cref="EnemyHealth.Died"/> uebernommen.
    ///
    /// Getaktet wird nur, was sich von selbst weiterbewegt: die Standzeit der
    /// Einblendung und die Ausrichtung zur Kamera. Beides laeuft in
    /// <see cref="LateUpdate"/>, damit Gegner- und Kamerabewegung des Frames
    /// bereits abgeschlossen sind. <see cref="Tick"/> ist oeffentlich, damit
    /// Tests ohne feste Bildrate schrittweise takten koennen.
    ///
    /// Die Komponente gehoert auf ein eigenes Kindobjekt des Gegners: sie
    /// setzt Position und Drehung ihres <c>barRoot</c> jeden Takt neu und darf
    /// dabei nicht den Gegner selbst drehen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        /// <summary>Toleranz, ab der ein Lebenswert als voll gilt.</summary>
        private const float FullHealthEpsilon = 0.0001f;

        /// <summary>
        /// Mindestabstand zwischen zwei Kamerasuchen. Ohne Drosselung wuerde
        /// eine fehlende Kamera in jedem Frame eine Suche ausloesen.
        /// </summary>
        private const float CameraSearchInterval = 0.5f;

        [Header("Bindung")]
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyHealthBarVisual visual;

        [Tooltip("Objekt, das versetzt und zur Kamera gedreht wird. " +
                 "Leer bedeutet dieses Objekt.")]
        [SerializeField] private Transform barRoot;

        [Tooltip("Bezugspunkt fuer den Hoehenversatz. " +
                 "Leer bedeutet das Objekt mit EnemyHealth.")]
        [SerializeField] private Transform anchor;

        [Tooltip("Leer bedeutet Camera.main.")]
        [SerializeField] private Camera viewCamera;

        [Header("Platzierung")]
        [Tooltip("Hoehe ueber dem Bezugspunkt in Metern.")]
        [SerializeField] private float heightOffset = 2.1f;

        [SerializeField] private bool faceCamera = true;

        [Header("Sichtbarkeit")]
        [Tooltip("Bei voller Gesundheit zunaechst verborgen.")]
        [SerializeField] private bool hiddenAtFullHealth = true;

        [Tooltip("Standzeit nach der letzten Aenderung in Sekunden. " +
                 "0 bedeutet unbegrenzt.")]
        [Min(0f)] [SerializeField] private float visibleDuration = 4f;

        [Tooltip("Auch verwundet nach Ablauf der Standzeit ausblenden.")]
        [SerializeField] private bool hideWhenIdle;

        [Header("Platzhalteroptik")]
        [Tooltip("Baut eine schlichte Anzeige, wenn keine zugewiesen ist.")]
        [SerializeField] private bool createPlaceholderVisual = true;

        [Tooltip("Groesse der Platzhalteranzeige in Metern.")]
        [SerializeField] private Vector2 placeholderSize = new Vector2(1f, 0.12f);

        private Camera activeCamera;
        private float cameraSearchCooldown;
        private float fill01 = 1f;
        private float visibleTimer;
        private bool revealed;
        private bool subscribed;
        private bool warnedAboutBarRoot;

        /// <summary>Zuletzt uebernommenes Lebensverhaeltnis zwischen 0 und 1.</summary>
        public float Fill01 => fill01;

        /// <summary>Ist die Anzeige gerade sichtbar?</summary>
        public bool IsBarVisible => visual != null && visual.IsVisible;

        /// <summary>Liegt eine Lebensquelle vor?</summary>
        public bool HasHealthBinding => health != null;

        public EnemyHealth Health => health;
        public EnemyHealthBarVisual Visual => visual;
        public float HeightOffset => heightOffset;
        public float VisibleDuration => visibleDuration;

        private void Awake()
        {
            ResolveReferences();
            EnsureVisual();
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsureVisual();
            Subscribe();

            // Nach einer Pause kann sich der Lebenswert geaendert haben, ohne
            // dass ein Ereignis angekommen ist.
            SyncFromHealth();
            ApplyState();
            UpdateTransform();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Ein Schritt der Anzeige: Standzeit, Sichtbarkeit, Ausrichtung.</summary>
        public void Tick(float deltaTime)
        {
            float step = Mathf.Max(0f, deltaTime);

            if (visibleTimer > 0f)
            {
                visibleTimer = Mathf.Max(0f, visibleTimer - step);
            }

            if (cameraSearchCooldown > 0f)
            {
                cameraSearchCooldown = Mathf.Max(0f, cameraSearchCooldown - step);
            }

            ApplyState();
            UpdateTransform();
        }

        /// <summary>
        /// Bindet die Anzeige zur Laufzeit an eine andere Lebensquelle, etwa
        /// nach dem Zusammensetzen eines Gegners durch einen spaeteren Spawner.
        /// </summary>
        public void Bind(EnemyHealth newHealth)
        {
            if (health == newHealth)
            {
                return;
            }

            Unsubscribe();
            health = newHealth;

            // Der Bezugspunkt zeigte auf den alten Gegner.
            anchor = null;
            ResolveReferences();

            Subscribe();
            SyncFromHealth();
            ApplyState();
        }

        public void SetCamera(Camera newCamera)
        {
            viewCamera = newCamera;
            activeCamera = newCamera;
            cameraSearchCooldown = 0f;
        }

        public void SetHeightOffset(float newHeightOffset)
        {
            heightOffset = newHeightOffset;
        }

        /// <summary>Setzt die Sichtbarkeitsregeln, etwa fuer Tests oder Varianten.</summary>
        public void ConfigureVisibility(
            float newVisibleDuration,
            bool newHiddenAtFullHealth,
            bool newHideWhenIdle)
        {
            visibleDuration = Mathf.Max(0f, newVisibleDuration);
            hiddenAtFullHealth = newHiddenAtFullHealth;
            hideWhenIdle = newHideWhenIdle;

            ApplyState();
        }

        // ------------------------------------------------------------------
        // Bindung
        // ------------------------------------------------------------------

        private void Subscribe()
        {
            if (subscribed || health == null)
            {
                return;
            }

            health.HealthChanged += HandleHealthChanged;
            health.Died += HandleDied;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || health == null)
            {
                subscribed = false;
                return;
            }

            health.HealthChanged -= HandleHealthChanged;
            health.Died -= HandleDied;
            subscribed = false;
        }

        private void HandleHealthChanged(float current, float max)
        {
            fill01 = max <= 0f ? 0f : Mathf.Clamp01(current / max);

            if (fill01 < 1f - FullHealthEpsilon)
            {
                revealed = true;
            }

            // Auch eine Heilung haelt die Anzeige kurz sichtbar, damit die
            // Rueckkehr auf den vollen Wert ablesbar bleibt.
            visibleTimer = visibleDuration;

            ApplyState();
        }

        private void HandleDied()
        {
            fill01 = 0f;
            revealed = false;
            visibleTimer = 0f;

            ApplyState();
        }

        private void SyncFromHealth()
        {
            if (health == null)
            {
                return;
            }

            fill01 = health.Health01;
            revealed = !health.IsDead && fill01 < 1f - FullHealthEpsilon;
            visibleTimer = revealed ? visibleDuration : 0f;
        }

        // ------------------------------------------------------------------
        // Sichtbarkeit und Darstellung
        // ------------------------------------------------------------------

        private void ApplyState()
        {
            if (visual == null)
            {
                return;
            }

            bool visible = EvaluateVisibility();

            visual.SetFill(fill01);
            visual.SetVisible(visible);

            // Wieder scharf stellen: eine ausgeblendete volle Leiste soll erst
            // der naechste Treffer erneut hervorholen.
            if (!visible && fill01 >= 1f - FullHealthEpsilon)
            {
                revealed = false;
            }
        }

        private bool EvaluateVisibility()
        {
            if (health == null || health.IsDead)
            {
                return false;
            }

            bool standing = visibleDuration <= 0f || visibleTimer > 0f;

            // Dauerhaft angezeigte Leiste: nur die Standzeit kann sie noch
            // ausblenden.
            if (!hiddenAtFullHealth)
            {
                return !hideWhenIdle || standing;
            }

            // Vor dem ersten Treffer bleibt die Leiste verborgen.
            if (!revealed)
            {
                return false;
            }

            bool full = fill01 >= 1f - FullHealthEpsilon;

            if (full)
            {
                return standing;
            }

            return !hideWhenIdle || standing;
        }

        private void UpdateTransform()
        {
            Transform root = barRoot;

            if (root == null || anchor == null)
            {
                return;
            }

            if (root == anchor)
            {
                WarnAboutBarRootOnce();
                return;
            }

            Vector3 position = anchor.position;
            position.y += heightOffset;
            root.position = position;

            if (!faceCamera)
            {
                return;
            }

            Camera view = ResolveCamera();

            if (view == null)
            {
                return;
            }

            // Parallel zur Bildebene statt zur Kameraposition: so kippt die
            // Leiste am Bildrand nicht weg.
            root.rotation = view.transform.rotation;
        }

        private Camera ResolveCamera()
        {
            if (activeCamera != null && activeCamera.isActiveAndEnabled)
            {
                return activeCamera;
            }

            if (viewCamera != null && viewCamera.isActiveAndEnabled)
            {
                activeCamera = viewCamera;
                return activeCamera;
            }

            if (cameraSearchCooldown > 0f)
            {
                return null;
            }

            cameraSearchCooldown = CameraSearchInterval;
            activeCamera = Camera.main;

            return activeCamera != null && activeCamera.isActiveAndEnabled
                ? activeCamera
                : null;
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        private void ResolveReferences()
        {
            if (health == null)
            {
                // Erst am eigenen Objekt, dann aufwaerts: die Anzeige haengt
                // in aller Regel als Kind unter dem Gegner.
                health = GetComponentInParent<EnemyHealth>();
            }

            if (barRoot == null)
            {
                barRoot = transform;
            }

            if (anchor == null && health != null)
            {
                anchor = health.transform;
            }
        }

        private void EnsureVisual()
        {
            if (visual != null)
            {
                return;
            }

            visual = GetComponentInChildren<EnemyHealthBarVisual>(true);

            if (visual != null || !createPlaceholderVisual)
            {
                return;
            }

            visual = EnemyHealthBarVisual.CreatePlaceholder(
                barRoot == null ? transform : barRoot, placeholderSize);

            // Frisch gebaut ist die Anzeige sichtbar; der erste Zustand
            // entscheidet gleich neu.
            visual.SetVisible(false);
        }

        private void WarnAboutBarRootOnce()
        {
            if (warnedAboutBarRoot)
            {
                return;
            }

            warnedAboutBarRoot = true;

            // Bewusst nur eine Warnung: ein Fehler wuerde die Console rot
            // faerben, obwohl der Gegner voll spielbar bleibt.
            Debug.LogWarning(
                "EnemyHealthBar: barRoot und anchor sind dasselbe Objekt. " +
                "Die Anzeige wird nicht versetzt und nicht gedreht, damit der " +
                "Gegner nicht selbst gedreht wird. Die Komponente gehoert auf " +
                "ein eigenes Kindobjekt.",
                this);
        }
    }
}
