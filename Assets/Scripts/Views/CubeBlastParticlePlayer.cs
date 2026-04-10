using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class CubeBlastParticlePlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float duration = 0.18f;
        [SerializeField] private float effectZ = -0.16f;
        [SerializeField] private int particlesPerBurst = 4;
        [SerializeField] private float spawnRadius = 0.12f;
        [SerializeField] private float travelDistance = 0.35f;
        [SerializeField] private float startSizeMultiplier = 0.28f;
        [SerializeField] private float endSizeMultiplier = 0.12f;
        [SerializeField] private float upwardBias = 0.08f;
        [SerializeField] private Sprite redParticleSprite;
        [SerializeField] private Sprite greenParticleSprite;
        [SerializeField] private Sprite blueParticleSprite;
        [SerializeField] private Sprite yellowParticleSprite;

        private readonly List<ActiveParticle> activeParticles = new List<ActiveParticle>();
        private float elapsed;

        public bool IsPlaying => activeParticles.Count > 0;

        public float Duration => duration;

        public bool TryPlay(BoardView boardView, CubeBlastParticleDescriptor descriptor)
        {
            if (boardView is null)
            {
                throw new ArgumentNullException(nameof(boardView));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (duration <= 0f || particlesPerBurst <= 0 || !descriptor.HasAnyParticles)
            {
                return false;
            }

            var sprite = ResolveParticleSprite(descriptor.CubeColor);
            if (sprite is null)
            {
                throw new InvalidOperationException($"Cube blast particles require a sprite for cube color '{descriptor.CubeColor}'.");
            }

            Stop();
            elapsed = 0f;

            var root = effectRoot is not null ? effectRoot : transform;

            foreach (var coordinate in descriptor.BurstCoordinates)
            {
                for (var index = 0; index < particlesPerBurst; index++)
                {
                    var particleObject = new GameObject($"CubeBlastParticle_{coordinate}_{index}");
                    particleObject.transform.SetParent(root, worldPositionStays: false);
                    var renderer = particleObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.color = Color.white;
                    renderer.sortingOrder = 9;

                    var burstSeed = BuildSeed(coordinate, index);
                    var startOffset = BuildOffset(Hash01(burstSeed), spawnRadius);
                    var travelOffset = BuildOffset(Hash01(burstSeed + 1), travelDistance);
                    var center = boardView.GetCellCenterWorld(coordinate);
                    center.z = boardView.transform.position.z + effectZ;
                    travelOffset.y += upwardBias * (0.75f + Hash01(burstSeed + 2) * 0.5f);

                    var sizeMultiplier = Mathf.Lerp(0.85f, 1.15f, Hash01(burstSeed + 3));
                    var startScale = GetSpriteScale(sprite, boardView.CellSize * startSizeMultiplier * sizeMultiplier);
                    var endScale = GetSpriteScale(sprite, boardView.CellSize * endSizeMultiplier * sizeMultiplier);
                    var startRotation = Mathf.Lerp(-35f, 35f, Hash01(burstSeed + 4));
                    var endRotation = startRotation + Mathf.Lerp(-110f, 110f, Hash01(burstSeed + 5));

                    var startPosition = center + startOffset;
                    particleObject.transform.position = startPosition;
                    particleObject.transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
                    particleObject.transform.localScale = startScale;

                    activeParticles.Add(new ActiveParticle(
                        particleObject,
                        renderer,
                        startPosition,
                        center + travelOffset,
                        startScale,
                        endScale,
                        startRotation,
                        endRotation));
                }
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
                var root = activeParticles[index].Root;
                if (Application.isPlaying)
                {
                    Destroy(root);
                }
                else
                {
                    DestroyImmediate(root);
                }
            }

            activeParticles.Clear();
            elapsed = 0f;
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

                var color = particle.Renderer.color;
                color.a = 1f - easedProgress;
                particle.Renderer.color = color;
            }
        }

        private Sprite ResolveParticleSprite(CubeColor cubeColor)
        {
            return cubeColor switch
            {
                CubeColor.Red => redParticleSprite,
                CubeColor.Green => greenParticleSprite,
                CubeColor.Blue => blueParticleSprite,
                CubeColor.Yellow => yellowParticleSprite,
                _ => null
            };
        }

        private static Vector3 BuildOffset(float hash, float radius)
        {
            var angle = hash * Mathf.PI * 2f;
            var radialScale = 0.35f + Hash01((int)(hash * 100000f) + 17) * 0.65f;
            return new Vector3(
                Mathf.Cos(angle) * radius * radialScale,
                Mathf.Sin(angle) * radius * radialScale,
                0f);
        }

        private static int BuildSeed(BoardCoordinate coordinate, int index)
        {
            return coordinate.X * 73856093 ^ coordinate.Y * 19349663 ^ index * 83492791;
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
                float endRotation)
            {
                Root = root;
                Renderer = renderer;
                StartPosition = startPosition;
                EndPosition = endPosition;
                StartScale = startScale;
                EndScale = endScale;
                StartRotation = startRotation;
                EndRotation = endRotation;
            }

            public GameObject Root { get; }

            public SpriteRenderer Renderer { get; }

            public Vector3 StartPosition { get; }

            public Vector3 EndPosition { get; }

            public Vector3 StartScale { get; }

            public Vector3 EndScale { get; }

            public float StartRotation { get; }

            public float EndRotation { get; }
        }
    }
}
