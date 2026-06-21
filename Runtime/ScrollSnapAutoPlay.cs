using UnityEngine;

namespace KidzDev.Unity.ScrollSnap
{
    [DisallowMultipleComponent]
    public class ScrollSnapAutoPlay : MonoBehaviour
    {
        [SerializeField] private ScrollSnap target;
        [SerializeField] private float interval = 3f;
        [SerializeField] private float resumeDelay = 1f;

        private float _timer;
        private bool _paused;
        private float _resumeAt;

        private void OnEnable()
        {
            if (target == null) return;
            target.OnDragBegin    += OnDragBegin;
            target.OnSnapComplete += OnSnapComplete;
            _timer = interval;
        }

        private void OnDisable()
        {
            if (target == null) return;
            target.OnDragBegin    -= OnDragBegin;
            target.OnSnapComplete -= OnSnapComplete;
        }

        private void Update()
        {
            if (target == null) return;

            if (_paused)
            {
                if (Time.time >= _resumeAt) _paused = false;
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _timer = interval;

            bool atEnd = target.CurrentPage >= target.PageCount - 1;
            if (atEnd && !target.WrapAround) return;
            target.SnapToNext();
        }

        private void OnDragBegin()
        {
            _paused   = true;
            _resumeAt = float.MaxValue;
        }

        private void OnSnapComplete(int _)
        {
            if (_paused)
                _resumeAt = Time.time + resumeDelay;
            _timer = interval;
        }
    }
}
