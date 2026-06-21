using UnityEngine;

namespace KidzDev.Unity.ScrollSnap
{
    [DisallowMultipleComponent]
    public class ScrollSnapItemScaler : MonoBehaviour, IScrollSnapItem
    {
        [SerializeField] private AnimationCurve scaleCurve =
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.75f));

        [SerializeField] private bool affectAlpha = true;
        [SerializeField] private AnimationCurve alphaCurve =
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.5f));

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            if (affectAlpha)
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        public void UpdateFocus(float distance01, bool isFocused)
        {
            float s = scaleCurve.Evaluate(distance01);
            transform.localScale = new Vector3(s, s, 1f);

            if (affectAlpha && _canvasGroup != null)
                _canvasGroup.alpha = alphaCurve.Evaluate(distance01);
        }
    }
}
