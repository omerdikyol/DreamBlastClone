using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    [RequireComponent(typeof(Camera))]
    public sealed class ReferenceWidthOrthographicCamera : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        [SerializeField] private float referenceOrthographicSize = 5f;

        private Camera cachedCamera;
        private int lastPixelWidth = -1;
        private int lastPixelHeight = -1;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (cachedCamera == null)
            {
                cachedCamera = GetComponent<Camera>();
            }

            if (cachedCamera == null)
            {
                return;
            }

            if (cachedCamera.pixelWidth != lastPixelWidth || cachedCamera.pixelHeight != lastPixelHeight)
            {
                Apply();
            }
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Apply();
        }

        public static float ResolveOrthographicSize(float referenceSize, Vector2 referenceResolution, float currentAspect)
        {
            if (referenceSize <= 0f)
            {
                return 0f;
            }

            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f || currentAspect <= 0f)
            {
                return referenceSize;
            }

            var referenceAspect = referenceResolution.x / referenceResolution.y;
            if (currentAspect >= referenceAspect)
            {
                return referenceSize;
            }

            // Narrower devices expand the orthographic size so the reference gameplay width remains visible.
            return referenceSize * (referenceAspect / currentAspect);
        }

        private void Apply()
        {
            if (cachedCamera == null)
            {
                cachedCamera = GetComponent<Camera>();
            }

            if (cachedCamera == null || !cachedCamera.orthographic)
            {
                return;
            }

            var pixelWidth = cachedCamera.pixelWidth;
            var pixelHeight = cachedCamera.pixelHeight;
            var aspect = pixelHeight > 0 ? (float)pixelWidth / pixelHeight : 0f;
            cachedCamera.orthographicSize = ResolveOrthographicSize(referenceOrthographicSize, referenceResolution, aspect);
            lastPixelWidth = pixelWidth;
            lastPixelHeight = pixelHeight;
        }
    }
}
