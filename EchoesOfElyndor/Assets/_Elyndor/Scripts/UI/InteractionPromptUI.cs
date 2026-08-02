using Elyndor.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace Elyndor.UI
{
    /// <summary>
    /// Zeigt den kontextabhängigen Interaktionshinweis des aktiven Ziels an.
    /// Kennt nur den <see cref="InteractionDetector"/>, keine Weltobjekte.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private InteractionDetector detector;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private RectTransform presentationRoot;

        public void Configure(
            InteractionDetector interactionDetector,
            GameObject root,
            Text text,
            RectTransform targetPresentationRoot)
        {
            detector = interactionDetector;
            promptRoot = root;
            promptText = text;
            presentationRoot = targetPresentationRoot;
        }

        private void Awake()
        {
            if (promptRoot == null || presentationRoot == null)
                return;

            RectTransform promptTransform =
                promptRoot.transform as RectTransform;

            if (promptTransform == null ||
                promptTransform.parent == presentationRoot)
            {
                return;
            }

            promptTransform.SetParent(presentationRoot, false);
            promptTransform.SetAsLastSibling();
        }

        private void OnEnable()
        {
            if (detector == null)
            {
                Debug.LogWarning($"{name}: Kein InteractionDetector zugewiesen.", this);
                return;
            }

            detector.TargetChanged += HandleTargetChanged;
            HandleTargetChanged(detector.CurrentTarget);
        }

        private void OnDisable()
        {
            if (detector != null)
            {
                detector.TargetChanged -= HandleTargetChanged;
            }
        }

        private void HandleTargetChanged(IInteractable target)
        {
            if (promptRoot == null || promptText == null)
                return;

            bool hasTarget = target != null;
            promptRoot.SetActive(hasTarget);

            if (hasTarget)
            {
                promptText.text = $"[E] {target.InteractionPrompt}";
            }
        }
    }
}
