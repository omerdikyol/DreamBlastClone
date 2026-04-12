using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class SafeAreaPinnedRectTransform : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private bool respectLeft;
        [SerializeField] private bool respectRight;
        [SerializeField] private bool respectTop;
        [SerializeField] private bool respectBottom;
        [SerializeField] private bool captureCurrentPositionOnEnable = true;
        [SerializeField] private Vector2 baseAnchoredPosition;

        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private bool hasCapturedBasePosition;

        private void OnEnable()
        {
            ResolveTarget();

            if (captureCurrentPositionOnEnable && !hasCapturedBasePosition && target != null)
            {
                baseAnchoredPosition = target.anchoredPosition;
                hasCapturedBasePosition = true;
            }

            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea == lastSafeArea
                && Screen.width == lastScreenSize.x
                && Screen.height == lastScreenSize.y)
            {
                return;
            }

            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        public void Apply()
        {
            ResolveTarget();

            if (target == null)
            {
                return;
            }

            if (captureCurrentPositionOnEnable && !hasCapturedBasePosition)
            {
                baseAnchoredPosition = target.anchoredPosition;
                hasCapturedBasePosition = true;
            }

            var safeArea = Screen.safeArea;
            var scaleFactor = ResolveCanvasScaleFactor();
            var position = baseAnchoredPosition;

            if (respectLeft)
            {
                position.x += safeArea.xMin / scaleFactor;
            }

            if (respectRight)
            {
                position.x -= (Screen.width - safeArea.xMax) / scaleFactor;
            }

            if (respectBottom)
            {
                position.y += safeArea.yMin / scaleFactor;
            }

            if (respectTop)
            {
                position.y -= (Screen.height - safeArea.yMax) / scaleFactor;
            }

            target.anchoredPosition = position;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }

        private void ResolveTarget()
        {
            target ??= transform as RectTransform;
        }

        private float ResolveCanvasScaleFactor()
        {
            var canvas = target != null ? target.GetComponentInParent<Canvas>() : null;
            return canvas != null ? Mathf.Max(0.001f, canvas.scaleFactor) : 1f;
        }
    }
}
