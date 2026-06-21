using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KidzDev.Unity.ScrollSnap
{
    /// <summary>
    /// Shared base for page indicators. Handles target registration, a pooled set of
    /// "slots", optional windowing for large page counts, clickable jump, and edit-mode preview.
    /// Derive and implement <see cref="CreateSlot"/> and <see cref="StyleSlot"/>.
    /// </summary>
    [ExecuteAlways]
    public abstract class ScrollSnapIndicatorBase : MonoBehaviour, IScrollSnapIndicator
    {
        [SerializeField] protected ScrollSnap target;
        [SerializeField] protected bool clickable = true;
        [Tooltip("Maximum slots shown at once. 0 = one slot per page (no windowing).")]
        [SerializeField] [Min(0)] protected int maxVisible = 0;

        [Header("Editor Preview")]
        [SerializeField] protected bool previewInEditor = true;
        [SerializeField] [Min(1)] protected int previewCount = 5;

        protected sealed class Slot
        {
            public GameObject go;
            public RectTransform rt;
            public int pageIndex;
        }

        protected readonly List<Slot> _slots = new List<Slot>();
        protected int _pageCount;
        protected int _currentPage;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected virtual void OnEnable()
        {
            if (Application.isPlaying)
            {
                if (target != null) target.RegisterIndicator(this);
            }
            else
            {
                ScheduleEditorPreview();
            }
        }

        protected virtual void OnDisable()
        {
            if (Application.isPlaying && target != null)
                target.UnregisterIndicator(this);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() => ScheduleEditorPreview();

        private void ScheduleEditorPreview()
        {
            if (Application.isPlaying || !previewInEditor) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                if (Application.isPlaying || !previewInEditor) return;
                RefreshPreview();
            };
        }

        private void RefreshPreview()
        {
            int count = previewCount;
            if (target != null)
            {
                var sr = target.GetComponent<ScrollRect>();
                if (sr != null && sr.content != null && sr.content.childCount > 0)
                    count = sr.content.childCount;
            }
            Setup(count);
        }
#else
        private void ScheduleEditorPreview() { }
#endif

        // ── IScrollSnapIndicator ──────────────────────────────────────────────

        public void Setup(int pageCount)
        {
            _pageCount = pageCount;
            BuildSlots();
            ApplyWindow(_currentPage);
        }

        public void OnPageChanged(int page)
        {
            _currentPage = page;
            ApplyWindow(page);
        }

        // ── Slot management ───────────────────────────────────────────────────

        private int SlotCount => maxVisible > 0 ? Mathf.Min(maxVisible, _pageCount) : _pageCount;

        private void BuildSlots()
        {
            ClearChildren();
            _slots.Clear();

            int n = SlotCount;
            for (int i = 0; i < n; i++)
            {
                GameObject go = CreateSlot();
                if (go == null) continue;
                if (!Application.isPlaying) MarkDontSave(go);

                var slot = new Slot { go = go, rt = go.transform as RectTransform, pageIndex = i };

                if (clickable && target != null)
                {
                    var graphic = go.GetComponent<Graphic>();
                    var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                    if (graphic != null) btn.targetGraphic = graphic;
                    btn.onClick.RemoveAllListeners();
                    var captured = slot;
                    btn.onClick.AddListener(() =>
                    {
                        if (target != null) target.SnapToPage(captured.pageIndex);
                    });
                }

                _slots.Add(slot);
            }
        }

        private void ApplyWindow(int page)
        {
            int n = _slots.Count;
            if (n == 0) return;

            bool windowed = maxVisible > 0 && _pageCount > maxVisible;
            int windowStart = 0;
            if (windowed)
                windowStart = Mathf.Clamp(page - maxVisible / 2, 0, _pageCount - maxVisible);

            for (int i = 0; i < n; i++)
            {
                int pageIdx = windowStart + i;
                _slots[i].pageIndex = pageIdx;

                bool isActive = pageIdx == page;
                bool isEdge = windowed &&
                              ((i == 0 && windowStart > 0) ||
                               (i == n - 1 && windowStart + n < _pageCount));

                StyleSlot(_slots[i], pageIdx, isActive, isEdge);
            }
        }

        private void ClearChildren()
        {
            // The indicator container is dedicated to slots, so clear everything under it.
            var t = transform;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private static void MarkDontSave(GameObject go)
        {
            go.hideFlags = HideFlags.DontSave;
            foreach (Transform child in go.transform)
                MarkDontSave(child.gameObject);
        }

        // ── To implement ──────────────────────────────────────────────────────

        /// <summary>Create one slot GameObject parented under this indicator.</summary>
        protected abstract GameObject CreateSlot();

        /// <summary>Style a slot for the page it currently represents.</summary>
        protected abstract void StyleSlot(Slot slot, int pageIndex, bool isActive, bool isEdge);
    }
}
