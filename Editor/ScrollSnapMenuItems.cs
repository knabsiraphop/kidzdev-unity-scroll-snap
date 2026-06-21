using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace KidzDev.Unity.ScrollSnap.Editor
{
    internal static class ScrollSnapMenuItems
    {
        [MenuItem("GameObject/UI/Scroll Snap/Horizontal Carousel", false, 2000)]
        static void CreateHorizontal(MenuCommand cmd) =>
            Create(cmd, ScrollSnapAxis.Horizontal, SnapAlignment.Start);

        [MenuItem("GameObject/UI/Scroll Snap/Peek Carousel", false, 2001)]
        static void CreatePeek(MenuCommand cmd) =>
            Create(cmd, ScrollSnapAxis.Horizontal, SnapAlignment.Center, peekAmount: 60f);

        [MenuItem("GameObject/UI/Scroll Snap/Vertical Picker", false, 2002)]
        static void CreateVertical(MenuCommand cmd) =>
            Create(cmd, ScrollSnapAxis.Vertical, SnapAlignment.Center);

        static void Create(MenuCommand cmd,
                           ScrollSnapAxis axis,
                           SnapAlignment alignment,
                           float peekAmount = 0f)
        {
            var parent = GetParent(cmd);

            // ── Root ──────────────────────────────────────────────────────────
            var root = new GameObject("ScrollSnap",
                typeof(RectTransform), typeof(ScrollRect), typeof(CanvasGroup));
            GameObjectUtility.SetParentAndAlign(root, parent);

            var rootRt         = (RectTransform)root.transform;
            rootRt.anchorMin   = new Vector2(0.05f, 0.1f);
            rootRt.anchorMax   = new Vector2(0.95f, 0.9f);
            rootRt.offsetMin   = rootRt.offsetMax = Vector2.zero;

            var scrollRect           = root.GetComponent<ScrollRect>();
            scrollRect.inertia       = false;
            scrollRect.movementType  = ScrollRect.MovementType.Unrestricted;
            scrollRect.horizontal    = axis == ScrollSnapAxis.Horizontal;
            scrollRect.vertical      = axis == ScrollSnapAxis.Vertical;

            // ── Viewport ──────────────────────────────────────────────────────
            var viewport = new GameObject("Viewport",
                typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            var vpRt        = (RectTransform)viewport.transform;
            vpRt.anchorMin  = Vector2.zero;
            vpRt.anchorMax  = Vector2.one;
            vpRt.offsetMin  = vpRt.offsetMax = Vector2.zero;
            scrollRect.viewport = vpRt;

            // ── Content ───────────────────────────────────────────────────────
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = (RectTransform)content.transform;

            var glg = content.AddComponent<GridLayoutGroup>();
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            if (axis == ScrollSnapAxis.Horizontal)
            {
                contentRt.anchorMin = new Vector2(0, 0);
                contentRt.anchorMax = new Vector2(0, 1);
                contentRt.pivot     = new Vector2(0, 0.5f);
                glg.constraint      = GridLayoutGroup.Constraint.FixedRowCount;
                glg.constraintCount = 1;
                glg.cellSize        = new Vector2(300, 200);
            }
            else
            {
                contentRt.anchorMin = new Vector2(0, 1);
                contentRt.anchorMax = new Vector2(1, 1);
                contentRt.pivot     = new Vector2(0.5f, 1f);
                glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = 1;
                glg.cellSize        = new Vector2(200, 80);
            }
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
            glg.spacing         = new Vector2(10, 10);
            scrollRect.content  = contentRt;

            // ── Sample items ──────────────────────────────────────────────────
            Color[] colors = {
                new Color(0.80f, 0.30f, 0.30f),
                new Color(0.30f, 0.70f, 0.45f),
                new Color(0.30f, 0.50f, 0.80f),
            };
            for (int i = 0; i < colors.Length; i++)
            {
                var item = new GameObject($"Item_{i + 1}",
                    typeof(RectTransform), typeof(Image));
                item.transform.SetParent(content.transform, false);
                item.GetComponent<Image>().color = colors[i];
            }

            // ── ScrollSnap component ──────────────────────────────────────────
            var snap = root.AddComponent<ScrollSnap>();
            var so   = new SerializedObject(snap);
            so.FindProperty("axis").enumValueIndex      = (int)axis;
            so.FindProperty("alignment").enumValueIndex = (int)alignment;
            if (peekAmount > 0f)
                so.FindProperty("peekAmount").floatValue = peekAmount;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── DotIndicator ──────────────────────────────────────────────────
            var dotRow = new GameObject("DotIndicator",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            dotRow.transform.SetParent(root.transform, false);
            var dotRt             = (RectTransform)dotRow.transform;
            dotRt.anchorMin       = new Vector2(0.5f, 0f);
            dotRt.anchorMax       = new Vector2(0.5f, 0f);
            dotRt.pivot           = new Vector2(0.5f, 1f);
            dotRt.anchoredPosition = new Vector2(0, -12f);
            dotRt.sizeDelta       = new Vector2(160f, 20f);
            var hlg = dotRow.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.spacing                = 8f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth      = false;
            hlg.childControlHeight     = false;
            dotRow.AddComponent<DotIndicator>(); // auto-resolves target via GetComponentInParent

            Undo.RegisterCreatedObjectUndo(root, "Create ScrollSnap");
            Selection.activeGameObject = root;
        }

        static GameObject GetParent(MenuCommand cmd)
        {
            var ctx = cmd.context as GameObject;
            if (ctx != null && ctx.GetComponentInParent<Canvas>() != null)
                return ctx;

            // Fall back to finding or creating a Canvas in the scene
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) return canvas.gameObject;

            var canvasGo = new GameObject("Canvas");
            var c        = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                // Prefer InputSystem module when available, fall back to legacy.
                var inputModuleType =
                    System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem")
                    ?? typeof(StandaloneInputModule);
                esGo.AddComponent(inputModuleType);
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
            }

            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
            return canvasGo;
        }
    }
}
