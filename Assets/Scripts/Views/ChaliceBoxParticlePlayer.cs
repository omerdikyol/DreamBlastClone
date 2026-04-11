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
        [SerializeField] private float maxParticleLifetime = 4.5f;
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
        [SerializeField] private float gravityAcceleration = 7f;
        [SerializeField] private float horizontalDamping = 2.2f;
        [SerializeField] private float destroyBelowPadding = 0.45f;
        [SerializeField] private Sprite[] doorPhaseSprites;
        [SerializeField] private Sprite chaliceDamageSprite;
        [SerializeField] private Sprite[] chalicePhaseSprites;

        private readonly List<ActiveParticle> activeParticles = new List<ActiveParticle>();
        private float destroyBelowWorldY;

        public bool IsPlaying => activeParticles.Count > 0;

        public float Duration => duration;

        public bool TryPlay(BoardView boardView, ChaliceBoxParticleDescriptor descriptor, float destroyBelowWorldY)
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
            this.destroyBelowWorldY = destroyBelowWorldY - destroyBelowPadding;

            var root = effectRoot is not null ? effectRoot : transform;
            foreach (var particleEvent in descriptor.Events)
            {
                CreateEventBurst(boardView, root, particleEvent);
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
                        ResolveChaliceDamageSprites(),
                        ResolveChaliceDamageParticleCount(particleEvent.Amount),
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
                var initialVelocity = endOffset * Mathf.Lerp(3.3f, 4.8f, Hash01(seed + 97));
                var sizeScale = Mathf.Lerp(0.9f, 1.2f, Hash01(seed + 59));
                var startScale = GetSpriteScale(sprite, boardView.CellSize * startSizeMultiplier * intensity * sizeScale);
                var endScale = GetSpriteScale(sprite, boardView.CellSize * endSizeMultiplier * intensity * Mathf.Lerp(0.95f, 1.15f, Hash01(seed + 73)));
                var startRotation = Mathf.Lerp(-35f, 35f, Hash01(seed + 11));
                var angularVelocity = Mathf.Lerp(-320f, 320f, Hash01(seed + 19));

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

        private static bool HasAnySprites(IReadOnlyList<Sprite> sprites)
        {
            return sprites is { Count: > 0 } && sprites[0] is not null;
        }

        private IReadOnlyList<Sprite> ResolveChaliceDamageSprites()
        {
            return chaliceDamageSprite != null
                ? new[] { chaliceDamageSprite }
                : chalicePhaseSprites;
        }

        private int ResolveChaliceDamageParticleCount(int damageAmount)
        {
            return chaliceDamageSprite != null
                ? Mathf.Max(0, damageAmount)
                : chaliceDamageBaseBurstCount + Mathf.Max(0, damageAmount) * chaliceDamageBurstPerAmount;
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
