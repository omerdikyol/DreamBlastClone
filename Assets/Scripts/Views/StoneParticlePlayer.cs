using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class StoneParticlePlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float duration = 0.24f;
        [SerializeField] private float maxParticleLifetime = 4f;
        [SerializeField] private float effectZ = -0.18f;
        [SerializeField] private int particlesPerBurst = 8;
        [SerializeField] private float spawnRadius = 0.12f;
        [SerializeField] private float travelDistance = 0.28f;
        [SerializeField] private float startSizeMultiplier = 0.3f;
        [SerializeField] private float endSizeMultiplier = 0.14f;
        [SerializeField] private float gravityAcceleration = 8.5f;
        [SerializeField] private float horizontalDamping = 2.6f;
        [SerializeField] private float destroyBelowPadding = 0.4f;
        [SerializeField] private Sprite mainChunkSprite;
        [SerializeField] private Sprite fragmentSprite;
        [SerializeField] private Sprite dustSprite;

        private readonly List<ActiveParticle> activeParticles = new List<ActiveParticle>();
        private float destroyBelowWorldY;

        public bool IsPlaying => activeParticles.Count > 0;

        public float Duration => duration;

        public bool TryPlay(BoardView boardView, StoneParticleDescriptor descriptor, float destroyBelowWorldY)
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

            if (mainChunkSprite is null || fragmentSprite is null || dustSprite is null)
            {
                throw new InvalidOperationException("Stone particles require all Stone particle sprites to be assigned.");
            }

            Stop();
            this.destroyBelowWorldY = destroyBelowWorldY - destroyBelowPadding;

            var root = effectRoot is not null ? effectRoot : transform;
            foreach (var coordinate in descriptor.RemovedCoordinates)
            {
                CreateBurst(boardView, root, coordinate);
            }

            return activeParticles.Count > 0;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            var safeDeltaTime = Mathf.Max(0f, deltaTime);
            for (var index = activeParticles.Count - 1; index >= 0; index--)
            {
                var particle = activeParticles[index];
                particle.Age += safeDeltaTime;
                particle.Velocity += Vector3.down * gravityAcceleration * safeDeltaTime;
                particle.Velocity = new Vector3(
                    Mathf.Lerp(particle.Velocity.x, 0f, Mathf.Clamp01(horizontalDamping * safeDeltaTime)),
                    particle.Velocity.y,
                    0f);
                particle.Position += particle.Velocity * safeDeltaTime;
                particle.Rotation += particle.AngularVelocity * safeDeltaTime;

                particle.Renderer.transform.position = particle.Position;
                particle.Renderer.transform.localScale = Vector3.Lerp(
                    particle.StartScale,
                    particle.EndScale,
                    0.12f);
                particle.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, particle.Rotation);
                particle.Renderer.color = particle.BaseColor;

                if (particle.Position.y <= destroyBelowWorldY || particle.Age >= maxParticleLifetime)
                {
                    DestroyObject(particle.Root);
                    activeParticles.RemoveAt(index);
                }
            }
        }

        public void Stop()
        {
            for (var index = activeParticles.Count - 1; index >= 0; index--)
            {
                DestroyObject(activeParticles[index].Root);
            }

            activeParticles.Clear();
            destroyBelowWorldY = 0f;
        }

        private void CreateBurst(BoardView boardView, Transform root, BoardCoordinate coordinate)
        {
            for (var index = 0; index < particlesPerBurst; index++)
            {
                var sprite = ResolveSprite(index);
                var particleRoot = CreateParticleRoot(root, coordinate, index);
                var renderer = particleRoot.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = sprite == dustSprite ? 12 : 13;
                renderer.color = sprite == dustSprite
                    ? new Color(1f, 1f, 1f, 0.6f)
                    : new Color(1f, 1f, 1f, 0.9f);

                var seed = BuildSeed(coordinate, index);
                var center = boardView.GetCellCenterWorld(coordinate);
                center.z = boardView.transform.position.z + effectZ;
                var startOffset = BuildOffset(seed, spawnRadius);
                var endOffset = BuildOffset(seed + 37, travelDistance);
                endOffset.y += 0.05f;

                var startPosition = center + startOffset;
                var initialVelocity = endOffset * Mathf.Lerp(3.9f, 5.4f, Hash01(seed + 97));
                var spriteSizeMultiplier = index == 0 ? 1.25f : 1f;
                var startScale = GetSpriteScale(sprite, boardView.CellSize * startSizeMultiplier * spriteSizeMultiplier * Mathf.Lerp(0.9f, 1.2f, Hash01(seed + 71)));
                var endScale = GetSpriteScale(sprite, boardView.CellSize * endSizeMultiplier * spriteSizeMultiplier * Mathf.Lerp(0.95f, 1.25f, Hash01(seed + 83)));
                var startRotation = Mathf.Lerp(-28f, 28f, Hash01(seed + 11));
                var angularVelocity = Mathf.Lerp(-340f, 340f, Hash01(seed + 19));

                particleRoot.transform.position = startPosition;
                particleRoot.transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
                particleRoot.transform.localScale = startScale;

                activeParticles.Add(new ActiveParticle(
                    particleRoot,
                    renderer,
                    startPosition,
                    initialVelocity,
                    startScale,
                    endScale,
                    startRotation,
                    angularVelocity,
                    renderer.color));
            }
        }

        private Sprite ResolveSprite(int index)
        {
            if (index == 0)
            {
                return mainChunkSprite;
            }

            return index % 2 == 0
                ? fragmentSprite
                : dustSprite;
        }

        private static GameObject CreateParticleRoot(Transform parent, BoardCoordinate coordinate, int index)
        {
            var root = new GameObject($"StoneParticle_{coordinate}_{index}");
            root.transform.SetParent(parent, worldPositionStays: false);
            return root;
        }

        private static int BuildSeed(BoardCoordinate coordinate, int index)
        {
            return coordinate.X * 73856093 ^ coordinate.Y * 19349663 ^ index * 83492791 ^ 41251;
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

        private sealed class ActiveParticle
        {
            public ActiveParticle(
                GameObject root,
                SpriteRenderer renderer,
                Vector3 startPosition,
                Vector3 velocity,
                Vector3 startScale,
                Vector3 endScale,
                float rotation,
                float angularVelocity,
                Color baseColor)
            {
                Root = root;
                Renderer = renderer;
                Position = startPosition;
                Velocity = velocity;
                StartScale = startScale;
                EndScale = endScale;
                Rotation = rotation;
                AngularVelocity = angularVelocity;
                BaseColor = baseColor;
                Age = 0f;
            }

            public GameObject Root { get; }

            public SpriteRenderer Renderer { get; }

            public Vector3 Position { get; set; }

            public Vector3 Velocity { get; set; }

            public Vector3 StartScale { get; }

            public Vector3 EndScale { get; }

            public float Rotation { get; set; }

            public float AngularVelocity { get; }

            public Color BaseColor { get; }

            public float Age { get; set; }
        }
    }
}
