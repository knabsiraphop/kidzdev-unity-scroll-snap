using UnityEngine;
using UnityEngine.UI;

namespace KidzDev.Unity.ScrollSnap
{
    public class DotIndicator : ScrollSnapIndicatorBase
    {
        [Header("Dot Style")]
        [SerializeField] private GameObject dotPrefab;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Vector2 dotSize = new Vector2(16f, 16f);
        [SerializeField] private float activeDotScale = 1.3f;
        [SerializeField] private float edgeDotScale = 0.6f;

        [Header("Sprites (optional)")]
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite inactiveSprite;

        [Header("Pill Mode")]
        [SerializeField] private bool pillMode = false;
        [SerializeField] private float activePillWidth = 36f;

        protected override GameObject CreateSlot()
        {
            GameObject go;
            if (dotPrefab != null)
            {
                go = Instantiate(dotPrefab, transform);
            }
            else
            {
                go = new GameObject("Dot", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
            }
            if (go.GetComponent<Image>() == null) go.AddComponent<Image>();
            return go;
        }

        protected override void StyleSlot(Slot slot, int pageIndex, bool isActive, bool isEdge)
        {
            var img = slot.go.GetComponent<Image>();

            float width = (pillMode && isActive) ? activePillWidth : dotSize.x;
            slot.rt.sizeDelta = new Vector2(width, dotSize.y);

            if (img != null)
            {
                if (activeSprite != null || inactiveSprite != null)
                    img.sprite = isActive ? activeSprite : inactiveSprite;
                img.color = isActive ? activeColor : inactiveColor;
            }

            float scale = isEdge
                ? edgeDotScale
                : (isActive && !pillMode ? activeDotScale : 1f);
            slot.rt.localScale = Vector3.one * scale;
        }
    }
}
