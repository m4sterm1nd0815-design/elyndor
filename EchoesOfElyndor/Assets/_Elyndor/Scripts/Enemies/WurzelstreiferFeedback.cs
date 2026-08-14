using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>
    /// Sichtbare Rueckmeldung des Wurzelstreifers: die tuerkise Resonanz seiner
    /// beschaedigten Erinnerung und ein kurzer Rinden-Staub bei Treffern.
    ///
    /// Gestalterische Regel aus der Art Direction: Tuerkis darf den Gegner
    /// nicht dauerhaft beleuchten, sonst verliert es seine Bedeutung. Es
    /// verstaerkt sich nur in den Momenten, in denen die Erinnerung
    /// tatsaechlich aufbricht — beim Bemerken, beim Ansetzen, beim Treffer,
    /// beim Straucheln und bei der Beruhigung.
    ///
    /// Bewusste Einschraenkung des Blockouts: die feinen Bruchlinien des
    /// Normalzustands fehlen. Sie brauchen eine Emissionsmaske auf dem Modell;
    /// ohne sie wuerde jede Grundhelligkeit den ganzen Koerper gleichmaessig
    /// einfaerben — genau das, was die Art Direction ausschliesst. Deshalb ist
    /// der Ruhewert hier 0 und die Maske eine Anforderung an das finale Asset.
    ///
    /// Geschrieben wird ueber einen <see cref="MaterialPropertyBlock"/>, damit
    /// kein gemeinsam genutztes Material veraendert wird.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class WurzelstreiferFeedback : MonoBehaviour
    {
        private static readonly int EmissionColorId =
            Shader.PropertyToID("_EmissionColor");

        [Header("Erinnerungsresonanz")]
        [Tooltip("Farbe der Bruchlinien. Tuerkis, nicht Neon.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color memoryColor =
            new Color(0.16f, 0.72f, 0.68f);

        [Tooltip("Helligkeit im Ruhezustand. 0 heisst: kein Dauerleuchten.")]
        [Min(0f)] [SerializeField] private float idleIntensity;

        [Tooltip("Helligkeit beim Bemerken des Ziels.")]
        [Min(0f)] [SerializeField] private float alertIntensity = 0.6f;

        [Tooltip("Helligkeit am Ende des Telegraphs.")]
        [Min(0f)] [SerializeField] private float telegraphIntensity = 2.2f;

        [Tooltip("Helligkeit beim Treffer.")]
        [Min(0f)] [SerializeField] private float hitIntensity = 3.2f;

        [Tooltip("Wie schnell die Helligkeit zurueckfaellt.")]
        [Min(0.01f)] [SerializeField] private float falloffPerSecond = 6f;

        [Header("Verweise")]
        [SerializeField] private EnemyController controller;
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("Kurzer Rinden-Staub beim Treffer. Darf fehlen.")]
        [SerializeField] private ParticleSystem hitParticles;

        private MaterialPropertyBlock propertyBlock;
        private float intensity;
        private bool subscribed;

        public float CurrentIntensity => intensity;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Ein Schritt der Rueckmeldung; oeffentlich fuer Tests.</summary>
        public void Tick(float deltaTime)
        {
            ResolveReferences();
            Subscribe();

            float target = SelectTargetIntensity();

            // Aufsteigen darf sofort geschehen, Abklingen nur allmaehlich:
            // ein Aufblitzen soll scharf sein, sein Nachhall weich.
            intensity = target > intensity
                ? target
                : Mathf.MoveTowards(
                    intensity, target, falloffPerSecond * Mathf.Max(0f, deltaTime));

            Apply();
        }

        private float SelectTargetIntensity()
        {
            if (controller == null)
            {
                return idleIntensity;
            }

            switch (controller.State)
            {
                case EnemyFoundationState.Dead:
                    // Beruhigung: die Risse schliessen sich.
                    return 0f;

                case EnemyFoundationState.Alert:
                    return alertIntensity;

                case EnemyFoundationState.Attack:
                    EnemyAttack attack = controller.Attack;

                    if (attack != null &&
                        attack.Phase == EnemyAttackPhase.Telegraph)
                    {
                        // Waehrend des Ansetzens waechst die Resonanz an —
                        // damit ist der Telegraph auch dann lesbar, wenn die
                        // Silhouette gerade verdeckt ist.
                        return Mathf.Lerp(
                            alertIntensity,
                            telegraphIntensity,
                            attack.PhaseProgress01);
                    }

                    return idleIntensity;

                default:
                    return idleIntensity;
            }
        }

        private void Apply()
        {
            if (targetRenderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(EmissionColorId, memoryColor * intensity);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void Subscribe()
        {
            if (subscribed || controller == null || controller.Health == null)
            {
                return;
            }

            controller.Health.Damaged += HandleDamaged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || controller == null || controller.Health == null)
            {
                return;
            }

            controller.Health.Damaged -= HandleDamaged;
            subscribed = false;
        }

        private void HandleDamaged(EnemyDamageInfo info)
        {
            intensity = hitIntensity;
            Apply();

            if (hitParticles != null)
            {
                hitParticles.Play(true);
            }
        }

        private void ResolveReferences()
        {
            if (controller == null)
            {
                controller = GetComponent<EnemyController>();
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }
        }
    }
}
