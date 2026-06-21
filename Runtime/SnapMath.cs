using UnityEngine;

namespace KidzDev.Unity.ScrollSnap
{
    public static class SnapMath
    {
        /// <summary>
        /// Returns the 1-D delta to apply to content.anchoredPosition so that
        /// the item's reference edge/center aligns with the viewport's matching edge/center.
        /// Positive delta moves content in the positive axis direction.
        /// </summary>
        public static float AlignOffset(
            float itemCenter, float itemExtent,
            float viewportCenter, float viewportExtent,
            SnapAlignment alignment)
        {
            float itemRef = alignment switch
            {
                SnapAlignment.Start  => itemCenter - itemExtent * 0.5f,
                SnapAlignment.End    => itemCenter + itemExtent * 0.5f,
                _                    => itemCenter
            };
            float vpRef = alignment switch
            {
                SnapAlignment.Start  => viewportCenter - viewportExtent * 0.5f,
                SnapAlignment.End    => viewportCenter + viewportExtent * 0.5f,
                _                    => viewportCenter
            };
            return vpRef - itemRef;
        }

        /// <summary>
        /// Returns 0 when the item is at focusCenter, 1 when it is >= range away.
        /// </summary>
        public static float FocusDistance01(float itemCenter, float focusCenter, float range)
        {
            if (range <= 0f) return 0f;
            return Mathf.Clamp01(Mathf.Abs(itemCenter - focusCenter) / range);
        }
    }
}
