using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class ChaliceBoxParticlePlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float duration = 0.36f;
        [SerializeField] private float effectZ = -0.1f;
        [SerializeField] private int doorDamageBurstCount = 4;
        [SerializeField] private int doorBreakDoorBurstCount = 14;
        [SerializeField] private int doorBreakChaliceBurstCount = 7;
        [SerializeField] private int chaliceDamageBaseBurstCount = 3;
        [SerializeField] private int chaliceDamageBurstPerAmount = 2;
        [SerializeField] private int chaliceCompleteBurstCount = 18;
        [SerializeField] private float spawnRadius = 0.24f;
        [SerializeField] private float travelDistance = 0.4f;
        [SerializeField] private float startSizeMultiplier = 0.42f;
        [SerializeField] private float endSizeMultiplier = 0.22f;
        [SerializeField] private float upwardBias = 0.11f;
        [SerializeField] private Sprite[] doorPhaseSprites;
        [SerializeField] private Sprite[] chalicePhaseSprites;

        private readonly List<ActiveParticle> activeParticles = new List<ActiveParticle>();
        private float elapsed;

        public bool IsPlaying => activeParticles.Count > 0;

        public float Duration => duration;

        public bool TryPlay(BoardView boardView, ChaliceBoxParticleDescriptor descriptor)
        {
            if (boardView is null)
            {
                throw new ArgumentNullException(nameof(boardView));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (duration <= 0f || !descriptor.HasAnyParticles)
            {
                return false;
            }

            if (!HasAnySprites(doorPhaseSprites) || !HasAnySprites(chalicePhaseSprites))
            {
                throw new InvalidOperationException("Chalice Box particles require both door-phase and chalice-phase sprite sets.");
            }

            Stop();
            elapsed = 0f;

            var root = effectRoot is not null ? effectRoot : transform;
            foreach (var particleEvent in descriptor.Events)
            {
                CreateEventBurst(boardView, root, particleEvent);
            }

            ApplyCurrentState();
            return activeParticles.Count > 0;
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
            for (var index = activeParticles.Count - 1; index >= 0; index--)
            {
                DestroyObject(activeParticles[index].Root);
            }

            activeParticles.Clear();
            elapsed = 0f;
        }

        private void CreateEventBurst(BoardView boardView, Transform root, ChaliceBoxParticleEvent particleEvent)
        {
            switch (particleEvent.EventType)
            {
                case ChaliceBoxParticleEventType.DoorDamage:
                    CreateBurst(
                        boardView,
                        root,
                        particleEvent.Anchor,
                        ChaliceBoxParticleEventType.DoorDamage,
                        doorPhaseSprites,
                        doorDamageBurstCount,
                        0.72f,
                        requireEverySprite: false);
                    break;
                case ChaliceBoxParticleEventType.DoorBreak:
                    CreateBurst(
                        boardView,
                        root,
                        particleEvent.Anchor,
                        ChaliceBoxParticleEventType.DoorBreak,
                        doorPhaseSprites,
                        doorBreakDoorBurstCount,
                        1.35f,
                        requireEverySprite: true);
                    CreateBurst(
                        boardView,
                        root,
                        particleEvent.Anchor,
                        ChaliceBoxParticleEventType.DoorBreak,
                        chalicePhaseSprites,
                        doorBreakChaliceBurstCount,
                        1.06f,
                        requireEverySprite: true);
                    break;
                case ChaliceBoxParticleEventType.ChaliceDamage:
                    CreateBurst(
                        boardView,
                        root,
                        particleEvent.Anchor,
                        ChaliceBoxParticleEventType.ChaliceDamage,
                        chalicePhaseSprites,
                        chaliceDamageBaseBurstCount + Mathf.Max(0, particleEvent.Amount) * chaliceDamageBurstPerAmount,
                        Mathf.Lerp(0.86f, 1.02f, Mathf.Clamp01(particleEvent.Amount / 4f)),
                        requireEverySprite: false);
                    break;
                case ChaliceBoxParticleEventType.ChaliceComplete:
                    CreateBurst(
                        boardView,
                        root,
                        particleEvent.Anchor,
                        ChaliceBoxParticleEventType.ChaliceComplete,
                        chalicePhaseSprites,
                        chaliceCompleteBurstCount,
                        1.45f,
                        requireEverySprite: true);
                    break;
            }
        }

        private void CreateBurst(
            BoardView boardView,
            Transform root,
            BoardCoordinate anchor,
            ChaliceBoxParticleEventType eventType,
            IReadOnlyList<Sprite> sprites,
            int particleCount,
            float intensity,
            bool requireEverySprite)
        {
            if (particleCount <= 0 || sprites.Count == 0)
            {
                return;
            }

            var effectiveParticleCount = requireEverySprite ? Mathf.Max(particleCount, sprites.Count) : particleCount;
            var center = GetFootprintCenter(boardView, anchor);
            center.z = boardView.transform.position.z + effectZ;

            for (var index = 0; index < effectiveParticleCount; index++)
            {
                var sprite = sprites[index % sprites.Count];
                var particleRoot = CreateParticleRoot(root, eventType, anchor, index);
                var renderer = particleRoot.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = eventType == ChaliceBoxParticleEventType.DoorDamage || eventType == ChaliceBoxParticleEventType.DoorBreak
                    ? 14
                    : 13;
                renderer.color = ResolveColor(eventType);

                var seed = BuildSeed(anchor, index, eventType);
                var startOffset = BuildOffset(seed, spawnRadius * intensity);
                var endOffset = BuildOffset(seed + 31, travelDistance * intensity);
                endOffset.y += upwardBias * intensity;

                var startPosition = center + startOffset;
                var endPosition = center + endOffset;
                var sizeScale = Mathf.Lerp(0.9f, 1.2f, Hash01(seed + 59));
                var startScale = GetSpriteScale(sprite, boardView.CellSize * startSizeMultiplier * intensity * sizeScale);
                var endScale = GetSpriteScale(sprite, boardView.CellSize * endSizeMultiplier * intensity * Mathf.Lerp(0.95f, 1.15f, Hash01(seed + 73)));
                var startRotation = Mathf.Lerp(-35f, 35f, Hash01(seed + 11));
                var endRotation = startRotation + Mathf.Lerp(-140f, 140f, Hash01(seed + 19));

                particleRoot.transform.position = startPosition;
                particleRoot.transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
                particleRoot.transform.localScale = startScale;

                activeParticles.Add(new ActiveParticle(
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
        }

        private void ApplyCurrentState()
        {
            var progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            var easedProgress = EaseOutCubic(progress);

            foreach (var particle in activeParticles)
            {
                if (particle.Root is null || particle.Renderer is null)
                {
                    continue;
                }

                particle.Root.transform.position = Vector3.Lerp(particle.StartPosition, particle.EndPosition, easedProgress);
                particle.Root.transform.localScale = Vector3.Lerp(particle.StartScale, particle.EndScale, easedProgress);
                particle.Root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(particle.StartRotation, particle.EndRotation, easedProgress));

                var color = particle.BaseColor;
                color.a *= 1f - easedProgress;
                particle.Renderer.color = color;
            }
        }

        private static bool HasAnySprites(IReadOnlyList<Sprite> sprites)
        {
            return sprites is { Count: > 0 } && sprites[0] is not null;
        }

        private static Color ResolveColor(ChaliceBoxParticleEventType eventType)
        {
            return eventType switch
            {
                ChaliceBoxParticleEventType.DoorDamage => new Color(1f, 1f, 1f, 0.82f),
                ChaliceBoxParticleEventType.DoorBreak => new Color(1f, 1f, 1f, 1f),
                ChaliceBoxParticleEventType.ChaliceDamage => new Color(1f, 1f, 1f, 0.84f),
                ChaliceBoxParticleEventType.ChaliceComplete => new Color(1f, 1f, 1f, 1f),
                _ => Color.white
            };
        }

        private static Vector3 GetFootprintCenter(BoardView boardView, BoardCoordinate anchor)
        {
            var bottomLeft = boardView.GetCellCenterWorld(anchor);
            var bottomRight = boardView.GetCellCenterWorld(anchor.Offset(1, 0));
            var topLeft = boardView.GetCellCenterWorld(anchor.Offset(0, 1));
            var topRight = boardView.GetCellCenterWorld(anchor.Offset(1, 1));
            return (bottomLeft + bottomRight + topLeft + topRight) / 4f;
        }

        private static GameObject CreateParticleRoot(
            Transform parent,
            ChaliceBoxParticleEventType eventType,
            BoardCoordinate anchor,
            int index)
        {
            var root = new GameObject($"ChaliceBoxParticle_{eventType}_{anchor}_{index}");
            root.transform.SetParent(parent, worldPositionStays: false);
            return root;
        }

        private static int BuildSeed(BoardCoordinate coordinate, int index, ChaliceBoxParticleEventType eventType)
        {
            return coordinate.X * 73856093 ^ coordinate.Y * 19349663 ^ index * 83492791 ^ (int)eventType * 29791;
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

        private static Vector3 GetSpriteScale(Sprite sprite, float targetSize)
        {
            var bounds = sprite.bounds.size;
            var safeWidth = bounds.x > 0f ? bounds.x : 1f;
            var safeHeight = bounds.y > 0f ? bounds.y : 1f;
            return new Vector3(targetSize / safeWidth, targetSize / safeHeight, 1f);
        }

        private static float EaseOutCubic(float progress)
        {
            var inverse = 1f - progress;
            return 1f - inverse * inverse * inverse;
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

        private readonly struct ActiveParticle
        {
            public ActiveParticle(
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
