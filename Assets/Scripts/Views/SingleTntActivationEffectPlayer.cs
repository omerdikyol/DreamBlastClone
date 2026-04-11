using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class SingleTntActivationEffectPlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float duration = 0.22f;
        // How long TNT blocks settle — just the initial impact pop.
        // Lingering particles keep animating during settle via Advance().
        [SerializeField] private float settleBlockingDuration = 0.06f;
        [SerializeField] private float effectZ = -0.18f;
        [SerializeField] private float pulseScaleMultiplier = 1.35f;
        [SerializeField] private float fieldParticleScaleMultiplier = 0.32f;
        [SerializeField] private float fieldParticleTravelDistance = 0.14f;
        [SerializeField] private int originSmokeCount = 6;
        [SerializeField] private float originSmokeScaleMultiplier = 0.85f;
        [SerializeField] private float originSmokeTravelDistance = 0.45f;
        [SerializeField] private float fieldSmokeScaleMultiplier = 0.62f;
        [SerializeField] private float fieldSmokeTravelDistance = 0.22f;
        [SerializeField] private int radialDebrisCount = 10;
        [SerializeField] private float radialDebrisScaleMultiplier = 0.42f;
        [SerializeField] private float radialDebrisTravelDistance = 0.9f;
        [SerializeField] private Sprite tntBurstSprite;
        [SerializeField] private Sprite tntDebrisSprite;
        [SerializeField] private Sprite tntSmokeSprite;

        private readonly List<ActiveVisual> activeVisuals = new List<ActiveVisual>();
        private float elapsed;

        public bool IsPlaying => activeVisuals.Count > 0;

        public float Duration => duration;

        public float SettleBlockingDuration => Mathf.Min(settleBlockingDuration, duration);

        public bool TryPlay(BoardView boardView, SingleTntActivationEffectDescriptor descriptor)
        {
            if (boardView is null)
            {
                throw new ArgumentNullException(nameof(boardView));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (duration <= 0f)
            {
                return false;
            }

            if (tntBurstSprite is null || tntDebrisSprite is null || tntSmokeSprite is null)
            {
                throw new InvalidOperationException("Single TNT effect requires burst, debris, and smoke sprites to be assigned.");
            }

            Stop();
            elapsed = 0f;

            var root = effectRoot is not null ? effectRoot : transform;
            CreatePulse(boardView, root, descriptor);
            CreateOriginSmoke(boardView, root, descriptor);
            CreateFieldSmoke(boardView, root, descriptor);
            CreateRadialDebris(boardView, root, descriptor);
            CreateFieldParticles(boardView, root, descriptor);
            ApplyCurrentState();
            return activeVisuals.Count > 0;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed = Mathf.Min(duration, elapsed + Mathf.Max(0f, deltaTime));
            ApplyCurrentState();

            if (elapsed >= duration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            for (var index = activeVisuals.Count - 1; index >= 0; index--)
            {
                DestroyObject(activeVisuals[index].Root);
            }

            activeVisuals.Clear();
            elapsed = 0f;
        }

        private void CreatePulse(BoardView boardView, Transform root, SingleTntActivationEffectDescriptor descriptor)
        {
            var pulseRoot = CreateRoot(root, "SingleTntPulse");
            var renderer = CreateSpriteRenderer(pulseRoot, tntBurstSprite, new Color(1f, 1f, 1f, 0.95f), 11);
            pulseRoot.transform.position = GetWorldPosition(boardView, descriptor.Origin, effectZ + 0.02f);
            var startScale = GetSpriteScale(tntBurstSprite, boardView.CellSize * 0.75f);
            var endScale = GetSpriteScale(tntBurstSprite, boardView.CellSize * pulseScaleMultiplier);
            pulseRoot.transform.localScale = startScale;
            activeVisuals.Add(new ActiveVisual(
                pulseRoot,
                renderer,
                startPosition: pulseRoot.transform.position,
                endPosition: pulseRoot.transform.position,
                startScale,
                endScale,
                startRotation: 0f,
                endRotation: 20f,
                renderer.color));
        }

        private void CreateOriginSmoke(BoardView boardView, Transform root, SingleTntActivationEffectDescriptor descriptor)
        {
            var smokeCount = Mathf.Max(0, originSmokeCount);
            var origin = GetWorldPosition(boardView, descriptor.Origin, effectZ + 0.018f);
            for (var index = 0; index < smokeCount; index++)
            {
                var smokeRoot = CreateRoot(root, $"SingleTntOriginSmoke_{index}");
                var renderer = CreateSpriteRenderer(
                    smokeRoot,
                    tntSmokeSprite,
                    new Color(0.92f, 0.90f, 0.88f, 0.78f),
                    8);

                var startPosition = origin + BuildOffset(index + 401, boardView.CellSize * 0.06f);
                var endPosition = startPosition + BuildOffset(index + 419, originSmokeTravelDistance);
                var startScale = GetSpriteScale(tntSmokeSprite, boardView.CellSize * originSmokeScaleMultiplier * Mathf.Lerp(0.75f, 1.1f, Hash01(index + 433)));
                var endScale = startScale * Mathf.Lerp(1.5f, 2.2f, Hash01(index + 449));
                var startRotation = Mathf.Lerp(-20f, 20f, Hash01(index + 461));
                var endRotation = startRotation + Mathf.Lerp(-35f, 35f, Hash01(index + 479));

                smokeRoot.transform.position = startPosition;
                smokeRoot.transform.localScale = startScale;
                smokeRoot.transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
                activeVisuals.Add(new ActiveVisual(
                    smokeRoot,
                    renderer,
                    startPosition,
                    endPosition,
                    startScale,
                    endScale,
                    startRotation,
                    endRotation,
                    renderer.color));
            }
        }

        private void CreateFieldSmoke(BoardView boardView, Transform root, SingleTntActivationEffectDescriptor descriptor)
        {
            for (var index = 0; index < descriptor.AffectedCoordinates.Count; index++)
            {
                var coordinate = descriptor.AffectedCoordinates[index];
                var smokeRoot = CreateRoot(root, $"SingleTntSmoke_{coordinate}_{index}");
                var renderer = CreateSpriteRenderer(
                    smokeRoot,
                    tntSmokeSprite,
                    new Color(0.88f, 0.87f, 0.86f, 0.62f),
                    7);

                var center = GetWorldPosition(boardView, coordinate, effectZ + 0.004f);
                var startPosition = center + BuildOffset(index + 503, boardView.CellSize * 0.05f);
                var endPosition = startPosition + BuildOffset(index + 521, fieldSmokeTravelDistance);
                var startScale = GetSpriteScale(tntSmokeSprite, boardView.CellSize * fieldSmokeScaleMultiplier * Mathf.Lerp(0.85f, 1.15f, Hash01(index + 541)));
                var endScale = startScale * Mathf.Lerp(1.3f, 1.8f, Hash01(index + 557));
                var startRotation = Mathf.Lerp(-15f, 15f, Hash01(index + 571));
                var endRotation = startRotation + Mathf.Lerp(-25f, 25f, Hash01(index + 587));

                smokeRoot.transform.position = startPosition;
                smokeRoot.transform.localScale = startScale;
                smokeRoot.transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
                activeVisuals.Add(new ActiveVisual(
                    smokeRoot,
                    renderer,
                    startPosition,
                    endPosition,
                    startScale,
                    endScale,
                    startRotation,
                    endRotation,
                    renderer.color));
            }
        }

        private void CreateRadialDebris(BoardView boardView, Transform root, SingleTntActivationEffectDescriptor descriptor)
        {
            var debrisSpawnCount = Mathf.Max(0, radialDebrisCount);
            if (debrisSpawnCount == 0)
            {
                return;
            }

            var origin = GetWorldPosition(boardView, descriptor.Origin, effectZ + 0.015f);
            for (var index = 0; index < debrisSpawnCount; index++)
            {
                var particleRoot = CreateRoot(root, $"SingleTntRadialDebris_{index}");
                var renderer = CreateSpriteRenderer(
                    particleRoot,
                    tntDebrisSprite,
                    new Color(1f, 0.92f, 0.82f, 0.82f),
                    10);

                var direction = BuildOffset(index + 211, 1f).normalized;
                var travelDistance = radialDebrisTravelDistance * Mathf.Lerp(0.6f, 1f, Hash01(index + 229));
                var endPosition = origin + direction * travelDistance;
                var startScale = GetSpriteScale(tntDebrisSprite, boardView.CellSize * radialDebrisScaleMultiplier * Mathf.Lerp(0.85f, 1.15f, Hash01(index + 241)));
                var endScale = startScale * Mathf.Lerp(0.45f, 0.7f, Hash01(index + 257));
                var startRotation = Mathf.Lerp(-30f, 30f, Hash01(index + 271));
                var endRotation = startRotation + Mathf.Lerp(80f, 160f, Hash01(index + 283));

                particleRoot.transform.position = origin;
                particleRoot.transform.localScale = startScale;
                particleRoot.transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
                activeVisuals.Add(new ActiveVisual(
                    particleRoot,
                    renderer,
                    origin,
                    endPosition,
                    startScale,
                    endScale,
                    startRotation,
                    endRotation,
                    renderer.color));
            }
        }

        private void CreateFieldParticles(BoardView boardView, Transform root, SingleTntActivationEffectDescriptor descriptor)
        {
            for (var index = 0; index < descriptor.AffectedCoordinates.Count; index++)
            {
                var coordinate = descriptor.AffectedCoordinates[index];
                CreateFieldParticle(boardView, root, coordinate, index, tntBurstSprite, "SingleTntBurst");
                CreateFieldParticle(boardView, root, coordinate, index + 101, tntDebrisSprite, "SingleTntDebris");
            }
        }

        private void CreateFieldParticle(
            BoardView boardView,
            Transform root,
            BoardCoordinate coordinate,
            int seed,
            Sprite sprite,
            string prefix)
        {
            var particleRoot = CreateRoot(root, $"{prefix}_{coordinate}_{seed}");
            var renderer = CreateSpriteRenderer(
                particleRoot,
                sprite,
                sprite == tntBurstSprite
                    ? new Color(1f, 1f, 1f, 0.82f)
                    : new Color(1f, 1f, 1f, 0.62f),
                sprite == tntBurstSprite ? 10 : 9);

            var center = GetWorldPosition(boardView, coordinate, sprite == tntBurstSprite ? effectZ + 0.01f : effectZ);
            var offset = BuildOffset(seed, boardView.CellSize * 0.08f);
            var drift = BuildOffset(seed + 37, fieldParticleTravelDistance);
            var startPosition = center + offset;
            var endPosition = startPosition + drift;
            var sizeMultiplier = sprite == tntBurstSprite ? 1f : 1.2f;
            var startScale = GetSpriteScale(sprite, boardView.CellSize * fieldParticleScaleMultiplier * sizeMultiplier);
            var endScale = startScale * (sprite == tntBurstSprite ? 1.45f : 1.65f);
            var startRotation = Mathf.Lerp(-18f, 18f, Hash01(seed + 11));
            var endRotation = startRotation + Mathf.Lerp(-40f, 40f, Hash01(seed + 29));

            particleRoot.transform.position = startPosition;
            particleRoot.transform.localScale = startScale;
            particleRoot.transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
            activeVisuals.Add(new ActiveVisual(
                particleRoot,
                renderer,
                startPosition,
                endPosition,
                startScale,
                endScale,
                startRotation,
                endRotation,
                renderer.color));
        }

        private void ApplyCurrentState()
        {
            var progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            var easedProgress = EaseOutCubic(progress);

            for (var index = 0; index < activeVisuals.Count; index++)
            {
                var visual = activeVisuals[index];
                if (visual.Root is null || visual.Renderer is null)
                {
                    continue;
                }

                visual.Root.transform.position = Vector3.Lerp(visual.StartPosition, visual.EndPosition, easedProgress);
                visual.Root.transform.localScale = Vector3.Lerp(visual.StartScale, visual.EndScale, easedProgress);
                visual.Root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(visual.StartRotation, visual.EndRotation, easedProgress));

                var color = visual.BaseColor;
                color.a *= 1f - easedProgress;
                visual.Renderer.color = color;
            }
        }

        private SpriteRenderer CreateSpriteRenderer(GameObject root, Sprite sprite, Color color, int sortingOrder)
        {
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            return renderer;
        }

        private static Vector3 GetWorldPosition(BoardView boardView, BoardCoordinate coordinate, float zOffset)
        {
            var worldPosition = boardView.GetCellCenterWorld(coordinate);
            worldPosition.z = boardView.transform.position.z + zOffset;
            return worldPosition;
        }

        private static Vector3 BuildOffset(int seed, float radius)
        {
            var angle = Hash01(seed) * Mathf.PI * 2f;
            var radialScale = Mathf.Lerp(0.35f, 1f, Hash01(seed + 1));
            return new Vector3(
                Mathf.Cos(angle) * radius * radialScale,
                Mathf.Sin(angle) * radius * radialScale,
                0f);
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

        private static Vector3 GetSpriteScale(Sprite sprite, float targetSize)
        {
            var bounds = sprite.bounds.size;
            var safeWidth = bounds.x > 0f ? bounds.x : 1f;
            var safeHeight = bounds.y > 0f ? bounds.y : 1f;
            return new Vector3(targetSize / safeWidth, targetSize / safeHeight, 1f);
        }

        private static GameObject CreateRoot(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, worldPositionStays: false);
            return root;
        }

        private static void DestroyObject(GameObject gameObject)
        {
            if (gameObject is null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private readonly struct ActiveVisual
        {
            public ActiveVisual(
                GameObject root,
                SpriteRenderer renderer,
                Vector3 startPosition,
                Vector3 endPosition,
                Vector3 startScale,
                Vector3 endScale,
                float startRotation,
                float endRotation,
                Color baseColor)
            {
                Root = root;
                Renderer = renderer;
                StartPosition = startPosition;
                EndPosition = endPosition;
                StartScale = startScale;
                EndScale = endScale;
                StartRotation = startRotation;
                EndRotation = endRotation;
                BaseColor = baseColor;
            }

            public GameObject Root { get; }

            public SpriteRenderer Renderer { get; }

            public Vector3 StartPosition { get; }

            public Vector3 EndPosition { get; }

            public Vector3 StartScale { get; }

            public Vector3 EndScale { get; }

            public float StartRotation { get; }

            public float EndRotation { get; }

            public Color BaseColor { get; }
        }
    }
}
