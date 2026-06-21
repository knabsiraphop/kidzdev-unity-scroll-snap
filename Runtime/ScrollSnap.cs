using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KidzDev.Unity.ScrollSnap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect), typeof(CanvasGroup))]
    public class ScrollSnap : UIBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [SerializeField] private ScrollSnapAxis  axis          = ScrollSnapAxis.Horizontal;
        [SerializeField] private SnapAlignment   alignment     = SnapAlignment.Start;
        [SerializeField] private int             startPage     = 0;
        [SerializeField] private bool            wrapAround    = false;
        [SerializeField] [Range(0f, 100f)] private float triggerPercent     = 20f;
        [SerializeField] [Range(0f, 10f)]  private float triggerAcceleration = 1f;
        [SerializeField] private float           snapDuration  = 0.2f;
        [SerializeField] private AnimationCurve  snapCurve     =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Peek")]
        [Tooltip("Pixels of the adjacent item visible on each side. Requires SnapAlignment.Center. Automatically sets content GridLayoutGroup padding.")]
        [SerializeField] [Min(0f)] private float peekAmount = 0f;

        [Header("Focus Effects")]
        [SerializeField] private bool  enableFocusEffects = false;
        [SerializeField] private float focusRange         = 1f; // in items, 1 = one cell away = 0 influence

        // ── Events ────────────────────────────────────────────────────────────

        public event Action              OnDragBegin;
        public event Action<int>         OnPageChanged;
        public event Action<int>         OnSnapBegin;
        public event Action<int>         OnSnapComplete;
        public event Action<int>         OnFocusChanged;

        // ── State ─────────────────────────────────────────────────────────────

        private ScrollRect   _scrollRect;
        private CanvasGroup  _canvasGroup;
        private RectTransform _content;
        private RectTransform _viewport;

        private int   _currentPage;
        private int   _focusedPage = -1;
        private bool  _isSnapping;
        private float _snapStartedAt;
        private Vector2 _snapFrom;
        private Vector2 _snapTarget;
        private bool    _dragAccelTriggered;
        private bool    _focusEffectDirty;

        // Seamless infinite loop support
        private float   _loopCycleDelta;  // content-space distance for one full loop cycle (active axis)
        private Vector2 _wrapOffset;      // additive correction applied to snapTarget when crossing an edge

        private readonly List<RectTransform>     _items      = new();
        private readonly List<IScrollSnapItem>   _effects    = new();
        private readonly List<IScrollSnapIndicator> _indicators = new();

        // ── Public API ────────────────────────────────────────────────────────

        public int  CurrentPage => _currentPage;
        public int  PageCount   => _items.Count;
        public bool WrapAround  => wrapAround;
        public SnapAlignment Alignment => alignment;

        public void SnapToNext() => SnapToPage(_currentPage + 1);
        public void SnapToPrev() => SnapToPage(_currentPage - 1);

        public void SnapToPage(int page)
        {
            Vector2 wrapOffset = Vector2.zero;
            if (wrapAround && PageCount > 1)
            {
                int wrapped = ((page % PageCount) + PageCount) % PageCount;
                if (page != wrapped && _loopCycleDelta != 0f)
                {
                    // Crossing an edge: animate through the virtual position, then teleport.
                    float corr = page >= PageCount ? _loopCycleDelta : -_loopCycleDelta;
                    wrapOffset = axis == ScrollSnapAxis.Horizontal
                        ? new Vector2(corr, 0f)
                        : new Vector2(0f, corr);
                }
                page = wrapped;
            }
            else
            {
                page = Mathf.Clamp(page, 0, Mathf.Max(0, PageCount - 1));
            }
            _currentPage = page;
            OnPageChanged?.Invoke(_currentPage);
            NotifyIndicators();
            BeginSnap(wrapOffset);
        }

        public void JumpToPage(int page)
        {
            page = Mathf.Clamp(page, 0, Mathf.Max(0, PageCount - 1));
            _currentPage = page;
            _isSnapping  = false;
            _canvasGroup.blocksRaycasts = true;
            _content.anchoredPosition  = MeasuredTargetPosition(page);
            OnPageChanged?.Invoke(_currentPage);
            NotifyIndicators();
            DriveFocusEffects(force: true);
        }

        public void SetPage(int page, bool animate)
        {
            if (animate) SnapToPage(page);
            else         JumpToPage(page);
        }

        public void Rebuild()
        {
            ApplyPeekPadding();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            RebuildItemCache();
            _loopCycleDelta = ComputeLoopCycleDelta();
            _currentPage = Mathf.Clamp(_currentPage, 0, Mathf.Max(0, PageCount - 1));
            JumpToPage(_currentPage);
            foreach (var ind in _indicators) ind.Setup(PageCount);
        }

        public void RegisterIndicator(IScrollSnapIndicator indicator)
        {
            if (_indicators.Contains(indicator)) return;
            _indicators.Add(indicator);
            indicator.Setup(PageCount);
            indicator.OnPageChanged(_currentPage);
        }

        public void UnregisterIndicator(IScrollSnapIndicator indicator) =>
            _indicators.Remove(indicator);

        // ── Unity lifecycle ───────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _scrollRect  = GetComponent<ScrollRect>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _content     = _scrollRect.content;
            _viewport    = _scrollRect.viewport != null
                ? _scrollRect.viewport
                : (RectTransform)_scrollRect.transform;
        }

        protected override void Start()
        {
            base.Start();
            ApplyPeekPadding();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            RebuildItemCache();
            _loopCycleDelta = ComputeLoopCycleDelta();
            _currentPage = Mathf.Clamp(startPage, 0, Mathf.Max(0, PageCount - 1));
            foreach (var ind in _indicators) ind.Setup(PageCount);
            JumpToPage(_currentPage);
        }

        private void LateUpdate()
        {
            if (_isSnapping)
            {
                float t = snapDuration > 0f
                    ? Mathf.Clamp01((Time.time - _snapStartedAt) / snapDuration)
                    : 1f;
                float e = snapCurve.Evaluate(t);
                _content.anchoredPosition = Vector2.LerpUnclamped(_snapFrom, _snapTarget, e);

                UpdateFocusPage();
                DriveFocusEffects(force: false);

                if (t >= 1f)
                {
                    _content.anchoredPosition = _snapTarget;

                    // Seamless loop: teleport content back to the actual item position.
                    if (_wrapOffset != Vector2.zero)
                    {
                        _content.anchoredPosition -= _wrapOffset;
                        _wrapOffset = Vector2.zero;
                    }

                    _isSnapping                 = false;
                    _canvasGroup.blocksRaycasts = true;
                    DriveFocusEffects(force: true);
                    OnSnapComplete?.Invoke(_currentPage);
                }
            }
            else if (_focusEffectDirty)
            {
                DriveFocusEffects(force: true);
                _focusEffectDirty = false;
            }
        }

        // ── Drag handlers ─────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData data)
        {
            _dragAccelTriggered = false;
            OnDragBegin?.Invoke();
        }

        public void OnDrag(PointerEventData data)
        {
            float delta = axis == ScrollSnapAxis.Horizontal ? data.delta.x : data.delta.y;
            float accel = Mathf.Abs(delta / (Time.deltaTime * 1000f));
            if (accel > triggerAcceleration && !float.IsPositiveInfinity(accel))
                _dragAccelTriggered = true;

            UpdateFocusPage();
            _focusEffectDirty = true;
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (ShouldChangePage(data))
            {
                float press   = Axis(data.pressPosition);
                float release = Axis(data.position);
                int dir = (press - release) > 0f ? 1 : -1;
                if (axis == ScrollSnapAxis.Vertical) dir = -dir;
                SnapToPage(_currentPage + dir * CalculateScrollAmount());
            }
            else
            {
                BeginSnap();
            }
        }

        // ── Measurement-based positioning ─────────────────────────────────────

        private Vector2 MeasuredTargetPosition(int page)
        {
            if (_items.Count == 0) return _content.anchoredPosition;

            page = Mathf.Clamp(page, 0, _items.Count - 1);
            RectTransform item = _items[page];

            // Item center in content local space
            Vector3 itemWorldCenter = item.TransformPoint(ItemLocalCenter(item));
            Vector2 itemInContent   = _content.InverseTransformPoint(itemWorldCenter);

            // Viewport center in content local space
            Vector3 vpWorldCenter   = _viewport.TransformPoint(VpLocalCenter());
            Vector2 vpInContent     = _content.InverseTransformPoint(vpWorldCenter);

            float itemC = axis == ScrollSnapAxis.Horizontal ? itemInContent.x : itemInContent.y;
            float vpC   = axis == ScrollSnapAxis.Horizontal ? vpInContent.x   : vpInContent.y;
            float itemE = axis == ScrollSnapAxis.Horizontal ? item.rect.width  : item.rect.height;
            float vpE   = axis == ScrollSnapAxis.Horizontal ? _viewport.rect.width : _viewport.rect.height;

            float delta = SnapMath.AlignOffset(itemC, itemE, vpC, vpE, alignment);

            Vector2 pos = _content.anchoredPosition;
            if (axis == ScrollSnapAxis.Horizontal) pos.x += delta;
            else                                   pos.y += delta;
            return pos;
        }

        // Item local center relative to item pivot (always Vector2.zero if pivot is at center,
        // but works with any pivot by reading the rect directly).
        private static Vector3 ItemLocalCenter(RectTransform rt) =>
            new Vector3(rt.rect.center.x, rt.rect.center.y, 0f);

        private Vector3 VpLocalCenter() =>
            new Vector3(_viewport.rect.center.x, _viewport.rect.center.y, 0f);

        // ── Peek & loop helpers ───────────────────────────────────────────────

        private void ApplyPeekPadding()
        {
            if (peekAmount <= 0f || _content == null) return;
            var glg = _content.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (glg == null) return;
            int p = Mathf.RoundToInt(peekAmount);
            if (axis == ScrollSnapAxis.Horizontal)
            {
                glg.padding.left  = p;
                glg.padding.right = p;
            }
            else
            {
                glg.padding.top    = p;
                glg.padding.bottom = p;
            }
        }

        // Content-position delta required to advance one full loop cycle.
        // Measured from actual child positions so it handles peek padding, spacing, variable sizes.
        private float ComputeLoopCycleDelta()
        {
            if (!wrapAround || _items.Count < 2) return 0f;
            Vector2 p0 = MeasuredTargetPosition(0);
            Vector2 p1 = MeasuredTargetPosition(1);
            float step = axis == ScrollSnapAxis.Horizontal ? (p1.x - p0.x) : (p1.y - p0.y);
            return step * _items.Count;
        }

        // ── Snapping internals ────────────────────────────────────────────────

        private void BeginSnap(Vector2 wrapOffset = default)
        {
            _wrapOffset    = wrapOffset;
            _snapFrom      = _content.anchoredPosition;
            _snapTarget    = MeasuredTargetPosition(_currentPage) + _wrapOffset;
            _snapStartedAt = Time.time;
            _canvasGroup.blocksRaycasts = false;
            _isSnapping    = true;
            OnSnapBegin?.Invoke(_currentPage);
        }

        private bool ShouldChangePage(PointerEventData data)
        {
            if (_dragAccelTriggered) { _dragAccelTriggered = false; return true; }
            return NormalizedDragOffset() * 100f > triggerPercent;
        }

        private int CalculateScrollAmount()
        {
            float norm = NormalizedDragOffset();
            int skip = Mathf.FloorToInt(norm);
            if (skip == 0) return 1;
            return (norm - skip) * 100f > triggerPercent ? skip + 1 : skip;
        }

        // How far the content has drifted from the current page's snap position, in item-lengths.
        private float NormalizedDragOffset()
        {
            if (_items.Count == 0) return 0f;
            Vector2 target = MeasuredTargetPosition(_currentPage);
            float diff = axis == ScrollSnapAxis.Horizontal
                ? _content.anchoredPosition.x - target.x
                : _content.anchoredPosition.y - target.y;
            float itemSize = axis == ScrollSnapAxis.Horizontal
                ? _items[_currentPage].rect.width
                : _items[_currentPage].rect.height;
            return itemSize > 0f ? Mathf.Abs(diff / itemSize) : 0f;
        }

        // ── Focus effects ─────────────────────────────────────────────────────

        private void UpdateFocusPage()
        {
            if (!enableFocusEffects || _items.Count == 0) return;
            int nearest = NearestVisiblePage();
            if (nearest == _focusedPage) return;
            _focusedPage = nearest;
            OnFocusChanged?.Invoke(_focusedPage);
        }

        private int NearestVisiblePage()
        {
            Vector3 vpWorldCenter = _viewport.TransformPoint(VpLocalCenter());
            float best = float.MaxValue;
            int idx = _currentPage;
            for (int i = 0; i < _items.Count; i++)
            {
                Vector3 ic = _items[i].TransformPoint(ItemLocalCenter(_items[i]));
                float d = axis == ScrollSnapAxis.Horizontal
                    ? Mathf.Abs(ic.x - vpWorldCenter.x)
                    : Mathf.Abs(ic.y - vpWorldCenter.y);
                if (d < best) { best = d; idx = i; }
            }
            return idx;
        }

        private void DriveFocusEffects(bool force)
        {
            if (!enableFocusEffects || _effects.Count == 0) return;
            if (!force && !_isSnapping) return;

            Vector3 vpWorldCenter = _viewport.TransformPoint(VpLocalCenter());
            float range = focusRange * (_items.Count > 0
                ? (axis == ScrollSnapAxis.Horizontal ? _items[0].rect.width : _items[0].rect.height)
                : 1f);

            for (int i = 0; i < _effects.Count && i < _items.Count; i++)
            {
                if (_effects[i] == null) continue;
                Vector3 ic = _items[i].TransformPoint(ItemLocalCenter(_items[i]));
                float dist = axis == ScrollSnapAxis.Horizontal
                    ? Mathf.Abs(ic.x - vpWorldCenter.x)
                    : Mathf.Abs(ic.y - vpWorldCenter.y);
                float d01 = SnapMath.FocusDistance01(dist, 0f, range);
                _effects[i].UpdateFocus(d01, i == _focusedPage);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RebuildItemCache()
        {
            _items.Clear();
            _effects.Clear();
            for (int i = 0; i < _content.childCount; i++)
            {
                var rt = _content.GetChild(i) as RectTransform;
                if (rt == null || !rt.gameObject.activeInHierarchy) continue;
                _items.Add(rt);
                _effects.Add(rt.GetComponent<IScrollSnapItem>());
            }
            if (enableFocusEffects && _focusedPage < 0)
                _focusedPage = _currentPage;
        }

        private float Axis(Vector2 v) =>
            axis == ScrollSnapAxis.Horizontal ? v.x : v.y;

        private void NotifyIndicators()
        {
            foreach (var ind in _indicators) ind.OnPageChanged(_currentPage);
        }
    }
}
