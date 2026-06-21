using UnityEngine;
using UnityEngine.UI;

namespace KidzDev.Unity.ScrollSnap
{
    public class ScrollSnapNavigator : MonoBehaviour
    {
        [SerializeField] private ScrollSnap target;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        private void Start()
        {
            if (target == null) target = GetComponentInParent<ScrollSnap>();
            if (target == null) return;
            if (prevButton != null) prevButton.onClick.AddListener(target.SnapToPrev);
            if (nextButton != null) nextButton.onClick.AddListener(target.SnapToNext);
            target.OnPageChanged += UpdateButtonStates;
            UpdateButtonStates(target.CurrentPage);
        }

        private void OnDestroy()
        {
            if (target == null) return;
            if (prevButton != null) prevButton.onClick.RemoveListener(target.SnapToPrev);
            if (nextButton != null) nextButton.onClick.RemoveListener(target.SnapToNext);
            target.OnPageChanged -= UpdateButtonStates;
        }

        private void UpdateButtonStates(int page)
        {
            if (target == null) return;
            bool wrap = target.WrapAround;
            if (prevButton != null) prevButton.interactable = wrap || page > 0;
            if (nextButton != null) nextButton.interactable = wrap || page < target.PageCount - 1;
        }
    }
}
