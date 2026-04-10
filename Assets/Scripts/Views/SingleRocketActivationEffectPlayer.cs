using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class SingleRocketActivationEffectPlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private float effectZ = -0.2f;
        [SerializeField] private Sprite horizontalRocketPartLeftSprite;
        [SerializeField] private Sprite horizontalRocketPartRightSprite;
        [SerializeField] private Sprite verticalRocketPartTopSprite;
        [SerializeField] private Sprite verticalRocketPartBottomSprite;
        [SerializeField] private Sprite rocketParticleStarSprite;
        [SerializeField] private Sprite rocketParticleSmokeSprite;

        private const float StarSpawnInterval = 0.03f;
        private const float SmokeSpawnInterval = 0.07f;
        private const float StarParticleLifetime = 0.11f;
        private const float SmokeParticleLifetime = 0.18f;
        private const float StarParticleSizeMultiplier = 0.26f;
        private const float SmokeParticleSizeMultiplier = 0.34f;
        private const float ParticleDriftDistance = 0.12f;
        private const float SmokeTrailOffset = 0.12f;
        private const float StarTrailOffset = 0.04f;
        private const float FrontParticleZOffset = 0.01f;
        private const float BackParticleZOffset = -0.01f;

        private readonly List<ActiveParticle> activeParticles = new List<ActiveParticle>();
        private BoardView activeBoardView;
        private SingleRocketActivationEffectDescriptor activeDescriptor;
        private SpriteRenderer negativePartRenderer;
        private SpriteRenderer positivePartRenderer;
        private Vector3 originWorldPosition;
        private Vector3 negativeEndWorldPosition;
        private Vector3 positiveEndWorldPosition;
        private float negativeTravelDuration;
        private float positiveTravelDuration;
        private float activeDuration;
        private float elapsed;
        private float nextStarSpawnTime;
        private float nextSmokeSpawnTime;
        private int particleSequence;

        public bool IsPlaying => negativePartRenderer is not null && positivePartRenderer is not null;

        public float Duration => activeDuration > 0f ? activeDuration : duration;

        public bool TryPlay(BoardView boardView, SingleRocketActivationEffectDescriptor descriptor)
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

            Stop();

            activeBoardView = boardView;
            activeDescriptor = descriptor;
            elapsed = 0f;

            var root = effectRoot is not null ? effectRoot : transform;
            var negativeSprite = ResolveNegativeSprite(descriptor.Orientation);
            var positiveSprite = ResolvePositiveSprite(descriptor.Orientation);
            negativePartRenderer = CreatePartRenderer(root, negativeSprite, $"{descriptor.Orientation}RocketNegativePart");
            positivePartRenderer = CreatePartRenderer(root, positiveSprite, $"{descriptor.Orientation}RocketPositivePart");
            InitializeTravelDurations();
            ApplyCurrentPositions();
            nextStarSpawnTime = 0f;
            nextSmokeSpawnTime = 0f;
            particleSequence = 0;
            EmitPendingParticles(elapsed);
            return true;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            var previousElapsed = elapsed;
            elapsed = Mathf.Min(activeDuration, elapsed + Mathf.Max(0f, deltaTime));
            ApplyCurrentPositions();
            EmitPendingParticles(previousElapsed);
            AdvanceParticles(deltaTime);

            if (elapsed >= activeDuration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            DestroyRenderer(ref negativePartRenderer);
            DestroyRenderer(ref positivePartRenderer);

            for (var index = activeParticles.Count - 1; index >= 0; index--)
            {
                DestroyObject(activeParticles[index].Root);
            }

            activeParticles.Clear();
            activeBoardView = null;
            activeDescriptor = null;
            originWorldPosition = default;
            negativeEndWorldPosition = default;
            positiveEndWorldPosition = default;
            negativeTravelDuration = 0f;
            positiveTravelDuration = 0f;
            activeDuration = 0f;
            elapsed = 0f;
            nextStarSpawnTime = 0f;
            nextSmokeSpawnTime = 0f;
            particleSequence = 0;
        }

        private void ApplyCurrentPositions()
        {
            var negativeProgress = negativeTravelDuration > 0f ? Mathf.Clamp01(elapsed / negativeTravelDuration) : 1f;
            var positiveProgress = positiveTravelDuration > 0f ? Mathf.Clamp01(elapsed / positiveTravelDuration) : 1f;
            negativePartRenderer.transform.position = Vector3.Lerp(originWorldPosition, negativeEndWorldPosition, negativeProgress);
            positivePartRenderer.transform.position = Vector3.Lerp(originWorldPosition, positiveEndWorldPosition, positiveProgress);
        }

        private void EmitPendingParticles(float previousElapsed)
        {
            if (rocketParticleStarSprite is null || rocketParticleSmokeSprite is null)
            {
                throw new InvalidOperationException("Rocket split effect requires star and smoke particle sprites to be assigned.");
            }

            while (nextStarSpawnTime <= elapsed)
            {
                if (nextStarSpawnTime >= previousElapsed)
                {
                    EmitParticlePair(nextStarSpawnTime, rocketParticleStarSprite, StarParticleLifetime, StarParticleSizeMultiplier, StarTrailOffset, FrontParticleZOffset);
                }

                nextStarSpawnTime += StarSpawnInterval;
            }

            while (nextSmokeSpawnTime <= elapsed)
            {
                if (nextSmokeSpawnTime >= previousElapsed)
                {
                    EmitParticlePair(nextSmokeSpawnTime, rocketParticleSmokeSprite, SmokeParticleLifetime, SmokeParticleSizeMultiplier, SmokeTrailOffset, BackParticleZOffset);
                }

                nextSmokeSpawnTime += SmokeSpawnInterval;
            }
        }

        private void EmitParticlePair(float spawnTime, Sprite sprite, float particleLifetime, float sizeMultiplier, float trailOffset, float zOffset)
        {
            EmitParticle(spawnTime, isNegativePart: true, sprite, particleLifetime, sizeMultiplier, trailOffset, zOffset);
            EmitParticle(spawnTime, isNegativePart: false, sprite, particleLifetime, sizeMultiplier, trailOffset, zOffset);
        }

        private void EmitParticle(
            float spawnTime,
            bool isNegativePart,
            Sprite sprite,
            float particleLifetime,
            float sizeMultiplier,
            float trailOffset,
            float zOffset)
        {
            var travelDuration = isNegativePart ? negativeTravelDuration : positiveTravelDuration;
            if (travelDuration <= 0f || spawnTime > travelDuration)
            {
                return;
            }

            var direction = GetPartDirection(isNegativePart);
            var partPosition = GetPartWorldPosition(isNegativePart, spawnTime);
            var perpendicular = new Vector3(-direction.y, direction.x, 0f);
            var lateralSpread = Mathf.Lerp(-0.04f, 0.04f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 1)));
            var driftScale = Mathf.Lerp(0.6f, 1f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 2)));
            var drift = (-direction * ParticleDriftDistance * driftScale) + (perpendicular * lateralSpread);
            var startPosition = partPosition - direction * trailOffset + perpendicular * (lateralSpread * 0.5f);
            startPosition.z += zOffset;
            var endPosition = startPosition + drift;
            var startScale = GetSpriteScale(sprite, activeBoardView.CellSize * sizeMultiplier * Mathf.Lerp(0.9f, 1.1f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 3))));
            var endScale = startScale * Mathf.Lerp(1.25f, 1.6f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 4)));
            var startRotation = Mathf.Lerp(-20f, 20f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 5)));
            var endRotation = startRotation + Mathf.Lerp(-45f, 45f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 6)));
            var color = sprite == rocketParticleStarSprite
                ? new Color(1f, 1f, 1f, 0.95f)
                : new Color(1f, 1f, 1f, 0.58f);

            var renderer = CreateParticleRenderer(sprite, isNegativePart, sprite == rocketParticleStarSprite ? "RocketParticleStar" : "RocketParticleSmoke");
            renderer.transform.position = startPosition;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
            renderer.transform.localScale = startScale;
            renderer.color = color;

            activeParticles.Add(new ActiveParticle(
                renderer.gameObject,
                renderer,
                startPosition,
                endPosition,
                startScale,
                endScale,
                startRotation,
                endRotation,
                particleLifetime,
                color));
        }

        private void AdvanceParticles(float deltaTime)
        {
            for (var index = activeParticles.Count - 1; index >= 0; index--)
            {
                var particle = activeParticles[index];
                var age = particle.Age + Mathf.Max(0f, deltaTime);
                if (age >= particle.Lifetime)
                {
                    DestroyObject(particle.Root);
                    activeParticles.RemoveAt(index);
                    continue;
                }

                var progress = particle.Lifetime > 0f ? Mathf.Clamp01(age / particle.Lifetime) : 1f;
                var easedProgress = EaseOutCubic(progress);
                particle.Renderer.transform.position = Vector3.Lerp(particle.StartPosition, particle.EndPosition, easedProgress);
                particle.Renderer.transform.localScale = Vector3.Lerp(particle.StartScale, particle.EndScale, easedProgress);
                particle.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(particle.StartRotation, particle.EndRotation, easedProgress));

                var color = particle.BaseColor;
                color.a *= 1f - easedProgress;
                particle.Renderer.color = color;
                activeParticles[index] = particle.WithAge(age);
            }
        }

        private void InitializeTravelDurations()
        {
            originWorldPosition = GetWorldPosition(activeDescriptor.Origin);
            negativeEndWorldPosition = GetWorldPosition(activeDescriptor.NegativeEnd);
            positiveEndWorldPosition = GetWorldPosition(activeDescriptor.PositiveEnd);

            var negativeDistance = Vector3.Distance(originWorldPosition, negativeEndWorldPosition);
            var positiveDistance = Vector3.Distance(originWorldPosition, positiveEndWorldPosition);
            var maxDistance = Mathf.Max(negativeDistance, positiveDistance);

            if (maxDistance <= Mathf.Epsilon)
            {
                negativeTravelDuration = 0f;
                positiveTravelDuration = 0f;
                activeDuration = duration;
                return;
            }

            // Both parts move at the same speed; the shorter side simply finishes earlier.
            negativeTravelDuration = duration * (negativeDistance / maxDistance);
            positiveTravelDuration = duration * (positiveDistance / maxDistance);
            activeDuration = duration;
        }

        private Vector3 GetWorldPosition(Core.BoardCoordinate coordinate)
        {
            var worldPosition = activeBoardView.GetCellCenterWorld(coordinate);
            worldPosition.z = activeBoardView.transform.position.z + effectZ;
            return worldPosition;
        }

        private Vector3 GetPartWorldPosition(bool isNegativePart, float time)
        {
            var travelDuration = isNegativePart ? negativeTravelDuration : positiveTravelDuration;
            var progress = travelDuration > 0f ? Mathf.Clamp01(time / travelDuration) : 1f;
            return Vector3.Lerp(
                originWorldPosition,
                isNegativePart ? negativeEndWorldPosition : positiveEndWorldPosition,
                progress);
        }

        private Vector3 GetPartDirection(bool isNegativePart)
        {
            var target = isNegativePart ? negativeEndWorldPosition : positiveEndWorldPosition;
            var direction = target - originWorldPosition;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            return direction.normalized;
        }

        private SpriteRenderer CreatePartRenderer(Transform parent, Sprite sprite, string name)
        {
            if (sprite is null)
            {
                throw new InvalidOperationException("Rocket split effect requires all part sprites to be assigned.");
            }

            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, worldPositionStays: false);
            var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.transform.localScale = GetSpriteScale(sprite);
            return spriteRenderer;
        }

        private SpriteRenderer CreateParticleRenderer(Sprite sprite, bool isNegativePart, string namePrefix)
        {
            var gameObject = new GameObject($"{namePrefix}_{(isNegativePart ? "Negative" : "Positive")}_{particleSequence++}");
            gameObject.transform.SetParent(effectRoot is not null ? effectRoot : transform, worldPositionStays: false);
            var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            return spriteRenderer;
        }

        private Vector3 GetSpriteScale(Sprite sprite)
        {
            var bounds = sprite.bounds.size;
            var safeWidth = bounds.x > 0f ? bounds.x : 1f;
            var safeHeight = bounds.y > 0f ? bounds.y : 1f;
            return new Vector3(activeBoardView.CellSize / safeWidth, activeBoardView.CellSize / safeHeight, 1f);
        }

        private Vector3 GetSpriteScale(Sprite sprite, float targetSize)
        {
            var bounds = sprite.bounds.size;
            var safeWidth = bounds.x > 0f ? bounds.x : 1f;
            var safeHeight = bounds.y > 0f ? bounds.y : 1f;
            return new Vector3(targetSize / safeWidth, targetSize / safeHeight, 1f);
        }

        private Sprite ResolveNegativeSprite(Core.RocketOrientation orientation)
        {
            return orientation == Core.RocketOrientation.Horizontal
                ? horizontalRocketPartLeftSprite
                : verticalRocketPartBottomSprite;
        }

        private Sprite ResolvePositiveSprite(Core.RocketOrientation orientation)
        {
            return orientation == Core.RocketOrientation.Horizontal
                ? horizontalRocketPartRightSprite
                : verticalRocketPartTopSprite;
        }

        private static void DestroyRenderer(ref SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer is null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(spriteRenderer.gameObject);
            }
            else
            {
                DestroyImmediate(spriteRenderer.gameObject);
            }

            spriteRenderer = null;
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

        private static int BuildParticleSeed(bool isNegativePart, float spawnTime, int salt)
        {
            var timeBucket = Mathf.RoundToInt(spawnTime * 1000f);
            return (isNegativePart ? 397 : 761) ^ (timeBucket * 48611) ^ (salt * 92821);
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

        private struct ActiveParticle
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
                float lifetime,
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
                Lifetime = lifetime;
                BaseColor = baseColor;
                Age = 0f;
            }

            public GameObject Root { get; }

            public SpriteRenderer Renderer { get; }

            public Vector3 StartPosition { get; }

            public Vector3 EndPosition { get; }

            public Vector3 StartScale { get; }

            public Vector3 EndScale { get; }

            public float StartRotation { get; }

            public float EndRotation { get; }

            public float Lifetime { get; }

            public Color BaseColor { get; }

            public float Age { get; private set; }

            public ActiveParticle WithAge(float age)
            {
                Age = age;
                return this;
            }
        }
    }
}
