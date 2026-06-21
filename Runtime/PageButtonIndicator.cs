using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KidzDev.Unity.ScrollSnap
{
    /// <summary>
    /// A row of clickable numbered page buttons (1)(2)(3)… with active styling.
    /// </summary>
    public class PageButtonIndicator : ScrollSnapIndicatorBase
    {
        [Header("Button Style")]
        [SerializeField] private GameObject buttonPrefab; // optional; must contain a TextMeshProUGUI
        [SerializeField] private Vector2 buttonSize = new Vector2(56f, 56f);
        [SerializeField] private float fontSize = 28f;
        [SerializeField] private Color activeBackground = new Color(0.49f, 0.78f, 1f, 1f);
        [SerializeField] private Color inactiveBackground = new Color(0.18f, 0.19f, 0.26f, 1f);
        [SerializeField] private Color activeText = Color.black;
        [SerializeField] private Color inactiveText = Color.white;
        [SerializeField] private float edgeScale = 0.85f;

        protected override GameObject CreateSlot()
        {
            GameObject go;
            if (buttonPrefab != null)
            {
                go = Instantiate(buttonPrefab, transform);
                if (go.GetComponent<Image>() == null) go.AddComponent<Image>();
            }
            else
            {
                go = new GameObject("PageBtn", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);

                var lbl = new GameObject("Lbl", typeof(RectTransform));
                lbl.transform.SetParent(go.transform, false);
                var lblRt = (RectTransform)lbl.transform;
                lblRt.anchorMin = Vector2.zero;
                lblRt.anchorMax = Vector2.one;
                lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;

                var tmp = lbl.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = fontSize;
            }

            ((RectTransform)go.transform).sizeDelta = buttonSize;
            return go;
        }

        protected override void StyleSlot(Slot slot, int pageIndex, bool isActive, bool isEdge)
        {
            var img = slot.go.GetComponent<Image>();
            if (img != null) img.color = isActive ? activeBackground : inactiveBackground;

            var tmp = slot.go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = (pageIndex + 1).ToString();
                tmp.color = isActive ? activeText : inactiveText;
            }

            slot.rt.localScale = Vector3.one * (isEdge ? edgeScale : 1f);
        }
    }
}
