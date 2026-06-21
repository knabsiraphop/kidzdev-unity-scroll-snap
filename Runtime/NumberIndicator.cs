using UnityEngine;
using TMPro;

namespace KidzDev.Unity.ScrollSnap
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class NumberIndicator : MonoBehaviour, IScrollSnapIndicator
    {
        [SerializeField] private ScrollSnap target;
        [SerializeField] private string format = "{0} / {1}";

        private TextMeshProUGUI _text;
        private int _pageCount;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            if (target == null) target = GetComponentInParent<ScrollSnap>();
        }

        private void OnEnable()
        {
            if (target != null) target.RegisterIndicator(this);
        }

        private void OnDisable()
        {
            if (target != null) target.UnregisterIndicator(this);
        }

        public void Setup(int pageCount) => _pageCount = pageCount;

        public void OnPageChanged(int page)
        {
            if (_text != null)
                _text.text = string.Format(format, page + 1, _pageCount);
        }
    }
}
