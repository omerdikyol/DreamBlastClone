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
        [SerializeField] private float maxParticleLifetime = 4f;
        [SerializeField] private float effectZ = -0.16f;
        [SerializeField] private int particlesPerBurst = 4;
        [SerializeField] private float spawnRadius = 0.12f;
        [SerializeField] private float travelDistance = 0.35f;
        [SerializeField] private float startSizeMultiplier = 0.28f;
        [SerializeField] private float endSizeMultiplier = 0.12f;
        [SerializeField] private float upwardBias = 0.08f;
        [SerializeField] private float gravityAcceleration = 7.5f;
        [SerializeField] private float horizontalDamping = 2.4f;
        [SerializeField] private float destroyBelowPadding = 0.4f;
        [SerializeField] private Sprite redParticleSprite;
        [SerializeField] private Sprite greenParticleSprite;
        [SerializeField] private Sprite blueParticleSprite;
        [SerializeField] private Sprite yellowParticleSprite;

        private readonly List<ActiveParticle> activeParticles = new List<ActiveParticle>();
        private float destroyBelowWorldY;

        public bool IsPlaying => activeParticles.Count > 0;

        public float Duration => duration;

        public bool TryPlay(BoardView boardView, CubeBlastParticleDescriptor descriptor, float destroyBelowWorldY)
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

            Stop();
            this.destroyBelowWorldY = destroyBelowWorldY - destroyBelowPadding;

            var root = effectRoot is not null ? effectRoot : transform;

            for (var groupIndex = 0; groupIndex < descriptor.BurstGroups.Count; groupIndex++)
            {
                var burstGroup = descriptor.BurstGroups[groupIndex];
                if (burstGroup.BurstCoordinates.Count == 0)
                {
                    continue;
                }

                var sprite = ResolveParticleSprite(burstGroup.CubeColor);
                if (sprite is null)
                {
                    throw new InvalidOperationException($"Cube blast particles require a sprite for cube color '{burstGroup.CubeColor}'.");
                }

                foreach (var coordinate in burstGroup.BurstCoordinates)
                {
                    for (var index = 0; index < particlesPerBurst; index++)
                    {
                        var particleObject = new GameObject($"CubeBlastParticle_{burstGroup.CubeColor}_{coordinate}_{index}");
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
                        var angularVelocity = Mathf.Lerp(-320f, 320f, Hash01(burstSeed + 5));

                        var startPosition = center + startOffset;
                        var initialVelocity = travelOffset * Mathf.Lerp(3.8f, 5.2f, Hash01(burstSeed + 6));
                        particleObject.transform.position = startPosition;
                        particleObject.transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
                        particleObject.transform.localScale = startScale;

                        activeParticles.Add(new ActiveParticle(
                            particleObject,
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
            destroyBelowWorldY = 0f;
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
