using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamBlastClone.Views
{
    public sealed class WinCelebrationParticlePlayer : MonoBehaviour
    {
        [SerializeField] private RectTransform effectRoot;
        [SerializeField] private Sprite particleSprite;
        [SerializeField] private int initialBurstCount = 30;
        [SerializeField] private int ambientBurstCount = 5;
        [SerializeField] private float ambientSpawnInterval = 0.06f;
        [SerializeField] private float particleLifetime = 1.25f;
        [SerializeField] private float initialSpawnRadius = 118f;
        [SerializeField] private float ambientSpawnRadius = 142f;
        [SerializeField] private float travelDistance = 112f;
        [SerializeField] private float driftUpward = 30f;
        [SerializeField] private float initialStartSize = 68f;
        [SerializeField] private float ambientStartSize = 34f;
        [SerializeField] private float endSize = 18f;
        [SerializeField] private Color primaryColor = new Color(1f, 0.93f, 0.63f, 0.92f);
        [SerializeField] private Color secondaryColor = new Color(1f, 1f, 1f, 0.68f);

        private readonly List<ActiveParticle> activeParticles = new List<ActiveParticle>();
        private RectTransform currentAnchor;
        private bool isPlaying;
        private float ambientElapsedSeconds;
        private int spawnSequence;
        private Color runtimePrimaryColor;
        private Color runtimeSecondaryColor;
        private Sprite runtimeParticleSprite;

        public bool IsPlaying => isPlaying;

        public bool TryPlay(RectTransform starAnchor)
        {
            Stop();

            if (starAnchor == null || initialBurstCount <= 0 || particleLifetime <= 0f)
            {
                return false;
            }

            currentAnchor = starAnchor;
            effectRoot = EnsureEffectRoot();
            ResolveParticleVisuals(starAnchor);

            if (effectRoot == null || runtimeParticleSprite == null)
            {
                currentAnchor = null;
                return false;
            }

            effectRoot.SetSiblingIndex(0);
            isPlaying = true;
            ambientElapsedSeconds = 0f;
            spawnSequence = 0;
            SpawnBurst(initialBurstCount, initialSpawnRadius, initialStartSize, ambient: false);
            return activeParticles.Count > 0;
        }

        public void Advance(float deltaTime)
        {
            if (!isPlaying)
            {
                return;
            }

            var clampedDeltaTime = Mathf.Max(0f, deltaTime);
            ambientElapsedSeconds += clampedDeltaTime;

            if (ambientSpawnInterval > 0f && ambientBurstCount > 0)
            {
                while (ambientElapsedSeconds >= ambientSpawnInterval)
                {
                    ambientElapsedSeconds -= ambientSpawnInterval;
                    SpawnBurst(ambientBurstCount, ambientSpawnRadius, ambientStartSize, ambient: true);
                }
            }

            for (var index = activeParticles.Count - 1; index >= 0; index--)
            {
                var particle = activeParticles[index];

                if (particle.RectTransform == null || particle.Image == null)
                {
                    DestroyParticle(particle);
                    activeParticles.RemoveAt(index);
                    continue;
                }

                particle.Age += clampedDeltaTime;
                var progress = Mathf.Clamp01(particle.Age / particle.Duration);
                var easedProgress = EaseOutCubic(progress);

                particle.RectTransform.anchoredPosition = Vector2.Lerp(particle.StartPosition, particle.EndPosition, easedProgress);
                particle.RectTransform.sizeDelta = Vector2.Lerp(particle.StartSize, particle.EndSize, easedProgress);
                particle.RectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(particle.StartRotation, particle.EndRotation, easedProgress));

                var color = particle.BaseColor;
                color.a *= 1f - easedProgress;
                particle.Image.color = color;

                if (particle.Age >= particle.Duration)
                {
                    DestroyParticle(particle);
                    activeParticles.RemoveAt(index);
                }
            }

            if (activeParticles.Count == 0 && currentAnchor == null)
            {
                isPlaying = false;
            }
        }

        public void Stop()
        {
            for (var index = activeParticles.Count - 1; index >= 0; index--)
            {
                DestroyParticle(activeParticles[index]);
            }

            activeParticles.Clear();
            currentAnchor = null;
            ambientElapsedSeconds = 0f;
            spawnSequence = 0;
            isPlaying = false;
        }

        private void SpawnBurst(int count, float spawnRadius, float startSize, bool ambient)
        {
            if (currentAnchor == null || effectRoot == null || count <= 0)
            {
                return;
            }

            var center = GetAnchorCenterInRootSpace();
            for (var index = 0; index < count; index++)
            {
                var particleIndex = spawnSequence++;
                var seed = BuildSeed(particleIndex, ambient);
                var direction = BuildStarDirection(seed, particleIndex, ambient);
                var perpendicular = new Vector2(-direction.y, direction.x);
                var anchorRadius = GetAnchorRadius();
                var startDistance = Mathf.Lerp(anchorRadius * 0.2f, anchorRadius * 0.48f, Hash01(seed + 3));
                var travelDistanceScale = Mathf.Lerp(0.42f, 1f, Hash01(seed + 59));
                var tangentialOffset = perpendicular * Mathf.Lerp(-anchorRadius * 0.12f, anchorRadius * 0.12f, Hash01(seed + 37));
                var driftOffset = perpendicular * Mathf.Lerp(-spawnRadius * 0.18f, spawnRadius * 0.18f, Hash01(seed + 41));
                var startOffset = direction * startDistance + tangentialOffset * 0.35f;
                var endDistance = anchorRadius + spawnRadius * Mathf.Lerp(0.35f, 0.95f, Hash01(seed + 47));
                var endOffset = direction * endDistance + driftOffset;
                endOffset += direction * (travelDistance * travelDistanceScale * 0.35f);
                endOffset.y += driftUpward * Mathf.Lerp(0.4f, 1f, Hash01(seed + 71));

                var color = ambient || index % 3 != 0 ? runtimeSecondaryColor : runtimePrimaryColor;
                var sizeMultiplier = Mathf.Lerp(ambient ? 0.58f : 0.72f, ambient ? 0.95f : 1.18f, Hash01(seed + 11));
                var particleRoot = CreateParticleRoot(particleIndex, ambient);
                var rectTransform = particleRoot.GetComponent<RectTransform>();
                var image = particleRoot.GetComponent<Image>();
                var startPosition = center + startOffset;
                var endPosition = center + endOffset;
                var baseRotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                var startRotation = baseRotation + Mathf.Lerp(-12f, 12f, Hash01(seed + 19));
                var endRotation = startRotation + Mathf.Lerp(-24f, 24f, Hash01(seed + 23));
                var startSizeVector = Vector2.one * startSize * sizeMultiplier;
                var endSizeVector = Vector2.one * Mathf.Lerp(endSize * 0.9f, endSize * 1.45f, Hash01(seed + 29));

                rectTransform.anchoredPosition = startPosition;
                rectTransform.sizeDelta = startSizeVector;
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, startRotation);
                image.color = color;

                activeParticles.Add(new ActiveParticle(
                    particleRoot,
                    rectTransform,
                    image,
                    startPosition,
                    endPosition,
                    startSizeVector,
                    endSizeVector,
                    startRotation,
                    endRotation,
                    particleLifetime * Mathf.Lerp(0.9f, 1.12f, Hash01(seed + 31)),
                    color));
            }
        }

        private RectTransform EnsureEffectRoot()
        {
            if (effectRoot != null)
            {
                return effectRoot;
            }

            if (transform is not RectTransform rectTransform)
            {
                return null;
            }

            var rootObject = new GameObject("WinCelebrationParticles", typeof(RectTransform));
            var runtimeRoot = rootObject.GetComponent<RectTransform>();
            runtimeRoot.SetParent(rectTransform, worldPositionStays: false);
            runtimeRoot.anchorMin = Vector2.zero;
            runtimeRoot.anchorMax = Vector2.one;
            runtimeRoot.offsetMin = Vector2.zero;
            runtimeRoot.offsetMax = Vector2.zero;
            runtimeRoot.localScale = Vector3.one;
            effectRoot = runtimeRoot;
            return effectRoot;
        }

        private void ResolveParticleVisuals(RectTransform starAnchor)
        {
            runtimePrimaryColor = primaryColor;
            runtimeSecondaryColor = secondaryColor;
            runtimeParticleSprite = particleSprite;

            if (starAnchor == null)
            {
                return;
            }

            var starImage = starAnchor.GetComponent<Image>();
            if (starImage == null)
            {
                return;
            }

            var starColor = starImage.color;
            if (runtimeParticleSprite == null && starImage.sprite != null)
            {
                runtimeParticleSprite = starImage.sprite;
            }

            runtimePrimaryColor = new Color(starColor.r, starColor.g, starColor.b, primaryColor.a);
            runtimeSecondaryColor = new Color(starColor.r, starColor.g, starColor.b, secondaryColor.a);
        }

        private Vector2 GetAnchorCenterInRootSpace()
        {
            var worldCenter = currentAnchor.TransformPoint(currentAnchor.rect.center);
            return effectRoot.InverseTransformPoint(worldCenter);
        }

        private GameObject CreateParticleRoot(int particleIndex, bool ambient)
        {
            var root = new GameObject(
                ambient ? $"WinAmbientParticle_{particleIndex}" : $"WinBurstParticle_{particleIndex}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.SetParent(effectRoot, worldPositionStays: false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one;

            var image = root.GetComponent<Image>();
            image.sprite = runtimeParticleSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return root;
        }

        private static int BuildSeed(int index, bool ambient)
        {
            return index * 73856093 ^ (ambient ? 19349663 : 83492791);
        }

        private Vector2 BuildStarDirection(int seed, int particleIndex, bool ambient)
        {
            var spokeIndex = particleIndex % 5;
            var angle = (90f - spokeIndex * 72f) * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var spreadDegrees = ambient ? 16f : 10f;
            var offsetAngle = Mathf.Lerp(-spreadDegrees, spreadDegrees, Hash01(seed + 1)) * Mathf.Deg2Rad;
            var cos = Mathf.Cos(offsetAngle);
            var sin = Mathf.Sin(offsetAngle);
            var rotated = new Vector2(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos);
            return rotated.normalized;
        }

        private float GetAnchorRadius()
        {
            if (currentAnchor == null)
            {
                return 36f;
            }

            var size = currentAnchor.rect.size;
            var minDimension = Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.y));
            if (minDimension <= 0f)
            {
                return 36f;
            }

            return minDimension * 0.28f;
        }

        private static float Hash01(int seed)
        {
            unchecked
            {
                var value = (uint)seed;
                value ^= 2747636419u;
                value *= 2654435769u;
                value ^= value >> 16;
                value *= 2654435769u;
                value ^= value >> 16;
                value *= 2654435769u;
                return value / (float)uint.MaxValue;
            }
        }

        private static float EaseOutCubic(float progress)
        {
            var inverse = 1f - progress;
            return 1f - inverse * inverse * inverse;
        }

        private static void DestroyParticle(ActiveParticle particle)
        {
            if (particle.Root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(particle.Root);
            }
            else
            {
                DestroyImmediate(particle.Root);
            }
        }

        private sealed class ActiveParticle
        {
            public ActiveParticle(
                GameObject root,
                RectTransform rectTransform,
                Image image,
                Vector2 startPosition,
                Vector2 endPosition,
                Vector2 startSize,
                Vector2 endSize,
                float startRotation,
                float endRotation,
                float duration,
                Color baseColor)
            {
                Root = root;
                RectTransform = rectTransform;
                Image = image;
                StartPosition = startPosition;
                EndPosition = endPosition;
                StartSize = startSize;
                EndSize = endSize;
                StartRotation = startRotation;
                EndRotation = endRotation;
                Duration = duration;
                BaseColor = baseColor;
                Age = 0f;
            }

            public GameObject Root { get; }

            public RectTransform RectTransform { get; }

            public Image Image { get; }

            public Vector2 StartPosition { get; }

            public Vector2 EndPosition { get; }

            public Vector2 StartSize { get; }

            public Vector2 EndSize { get; }

            public float StartRotation { get; }

            public float EndRotation { get; }

            public float Duration { get; }

            public Color BaseColor { get; }

            public float Age { get; set; }
        }
    }
}
