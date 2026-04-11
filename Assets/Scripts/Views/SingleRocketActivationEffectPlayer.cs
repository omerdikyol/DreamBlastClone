using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class SingleRocketActivationEffectPlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private float effectZ = -0.2f;
        [SerializeField] private float rocketOverflowCells = 2f;
        [SerializeField] private Sprite horizontalRocketPartLeftSprite;
        [SerializeField] private Sprite horizontalRocketPartRightSprite;
        [SerializeField] private Sprite verticalRocketPartTopSprite;
        [SerializeField] private Sprite verticalRocketPartBottomSprite;
        [SerializeField] private Sprite rocketParticleStarSprite;
        [SerializeField] private Sprite rocketParticleSmokeSprite;

        // --- star / smoke puff particles ---
        private const float StarSpawnInterval = 0.025f;
        private const float SmokeSpawnInterval = 0.025f;
        private const float StarParticleLifetime = 0.15f;
        private const float SmokeParticleLifetime = 0.35f;
        private const float StarParticleSizeMultiplier = 0.42f;
        private const float SmokeParticleSizeMultiplier = 1.1f;  // full cell-size cloud puffs
        private const float ParticleDriftDistance = 0.5f;
        private const float SmokeTrailOffset = 0.35f;
        private const float StarTrailOffset = 0.1f;
        private const float FrontParticleZOffset = 0.01f;
        private const float BackParticleZOffset = -0.01f;

        // --- streak / contrail particles ---
        // Spawned every StreakSpawnInterval as elongated blobs aligned with the travel direction,
        // creating a readable contrail behind each rocket half.
        private const float StreakSpawnInterval = 0.03f;
        private const float StreakParticleLifetime = 0.18f;
        private const float StreakLengthFactor = 1.0f;       // streak length as fraction of cellSize
        private const float StreakThicknessFactor = 0.2f;    // streak width as fraction of cellSize
        private const float StreakTrailOffset = 0.25f;       // how far behind the rocket head the streak starts
        private const float StreakZOffset = 0.005f;          // drawn in front of regular smoke

        // --- origin burst ---
        // Emitted once at split time: large smoke puffs that expand from the origin in all
        // directions, giving the "blast" feel of the rocket engine firing.
        private const int BurstParticleCount = 4;            // per half
        private const float BurstParticleLifetime = 0.3f;
        private const float BurstParticleSizeMin = 1.3f;     // relative to cellSize
        private const float BurstParticleSizeMax = 2.0f;
        private const float BurstDriftDistance = 0.55f;
        private const float BurstZOffset = -0.02f;           // behind the rocket parts

        // --- launch scale punch ---
        // The parts briefly overshoot their resting scale in the first LaunchPunchDuration seconds,
        // giving a "pop" feel at the split origin.
        private const float LaunchPunchDuration = 0.07f;
        private const float LaunchPunchPeak = 1.28f;

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
        private float nextStreakSpawnTime;
        private int particleSequence;
        private float runtimeDurationOverride = -1f;
        // Base scales captured at spawn time so the launch punch multiplies cleanly.
        private Vector3 baseNegativeScale;
        private Vector3 basePositiveScale;

        public bool IsPlaying => negativePartRenderer is not null && positivePartRenderer is not null;

        public float Duration => activeDuration > 0f ? activeDuration : ResolveConfiguredDuration();

        public float EffectZ => effectZ;

        public float RocketOverflowCells => rocketOverflowCells;

        public Sprite HorizontalRocketPartLeftSprite => horizontalRocketPartLeftSprite;

        public Sprite HorizontalRocketPartRightSprite => horizontalRocketPartRightSprite;

        public Sprite VerticalRocketPartTopSprite => verticalRocketPartTopSprite;

        public Sprite VerticalRocketPartBottomSprite => verticalRocketPartBottomSprite;

        public Sprite RocketParticleStarSprite => rocketParticleStarSprite;

        public Sprite RocketParticleSmokeSprite => rocketParticleSmokeSprite;

        public void ConfigureForRuntime(
            Transform runtimeEffectRoot,
            float durationOverride,
            float runtimeEffectZ,
            float runtimeRocketOverflowCells,
            Sprite runtimeHorizontalRocketPartLeftSprite,
            Sprite runtimeHorizontalRocketPartRightSprite,
            Sprite runtimeVerticalRocketPartTopSprite,
            Sprite runtimeVerticalRocketPartBottomSprite,
            Sprite runtimeRocketParticleStarSprite,
            Sprite runtimeRocketParticleSmokeSprite)
        {
            effectRoot = runtimeEffectRoot;
            runtimeDurationOverride = durationOverride;
            effectZ = runtimeEffectZ;
            rocketOverflowCells = runtimeRocketOverflowCells;
            horizontalRocketPartLeftSprite = runtimeHorizontalRocketPartLeftSprite;
            horizontalRocketPartRightSprite = runtimeHorizontalRocketPartRightSprite;
            verticalRocketPartTopSprite = runtimeVerticalRocketPartTopSprite;
            verticalRocketPartBottomSprite = runtimeVerticalRocketPartBottomSprite;
            rocketParticleStarSprite = runtimeRocketParticleStarSprite;
            rocketParticleSmokeSprite = runtimeRocketParticleSmokeSprite;
        }

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

            if (ResolveConfiguredDuration() <= 0f)
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

            // Store natural scales so the launch punch can scale relative to them.
            baseNegativeScale = negativePartRenderer.transform.localScale;
            basePositiveScale = positivePartRenderer.transform.localScale;

            ApplyCurrentPositions();
            nextStarSpawnTime = 0f;
            nextSmokeSpawnTime = 0f;
            nextStreakSpawnTime = 0f;
            particleSequence = 0;

            // Emit the origin burst before the regular trail starts ticking.
            EmitOriginBurst();
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
            nextStreakSpawnTime = 0f;
            particleSequence = 0;
            baseNegativeScale = default;
            basePositiveScale = default;
        }

        private void ApplyCurrentPositions()
        {
            // EaseOutCubic: fast initial burst with sustained, readable travel across the board.
            // More visible than EaseOutExpo — the player can follow the rocket's full path.
            var negativeProgress = negativeTravelDuration > 0f
                ? EaseOutCubic(Mathf.Clamp01(elapsed / negativeTravelDuration))
                : 1f;
            var positiveProgress = positiveTravelDuration > 0f
                ? EaseOutCubic(Mathf.Clamp01(elapsed / positiveTravelDuration))
                : 1f;

            negativePartRenderer.transform.position = Vector3.Lerp(originWorldPosition, negativeEndWorldPosition, negativeProgress);
            positivePartRenderer.transform.position = Vector3.Lerp(originWorldPosition, positiveEndWorldPosition, positiveProgress);

            // Launch scale punch: over the first LaunchPunchDuration seconds the parts quickly
            // overshoot to LaunchPunchPeak then settle at 1.0, giving the split a "pop" beat.
            var launchT = LaunchPunchDuration > 0f ? Mathf.Clamp01(elapsed / LaunchPunchDuration) : 1f;
            var launchScale = launchT < 0.45f
                ? Mathf.Lerp(1f, LaunchPunchPeak, launchT / 0.45f)
                : Mathf.Lerp(LaunchPunchPeak, 1f, (launchT - 0.45f) / 0.55f);
            negativePartRenderer.transform.localScale = baseNegativeScale * launchScale;
            positivePartRenderer.transform.localScale = basePositiveScale * launchScale;
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

            while (nextStreakSpawnTime <= elapsed)
            {
                if (nextStreakSpawnTime >= previousElapsed)
                {
                    EmitStreakParticlePair(nextStreakSpawnTime);
                }

                nextStreakSpawnTime += StreakSpawnInterval;
            }
        }

        // Emits large smoke puffs that blast outward from the split origin.
        // These are what give the "rocket engine firing" feel at the launch moment.
        private void EmitOriginBurst()
        {
            if (rocketParticleSmokeSprite is null)
            {
                return;
            }

            for (var index = 0; index < BurstParticleCount; index++)
            {
                EmitBurstParticle(index, isNegativePart: true);
                EmitBurstParticle(index, isNegativePart: false);
            }
        }

        private void EmitBurstParticle(int burstIndex, bool isNegativePart)
        {
            var direction = GetPartDirection(isNegativePart);
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var perpendicular = new Vector3(-direction.y, direction.x, 0f);

            // Spread burst particles across a full 360° fan — the cloud expands in all directions.
            var angleNorm = Hash01(BuildParticleSeed(isNegativePart, burstIndex * 0.137f, 11));
            var angleDeg = angleNorm * 360f;
            var angleRad = angleDeg * Mathf.Deg2Rad;
            var driftDir = (direction * Mathf.Cos(angleRad) + perpendicular * Mathf.Sin(angleRad)).normalized;

            var startPosition = originWorldPosition;
            startPosition.z += BurstZOffset;
            var driftAmount = BurstDriftDistance * Mathf.Lerp(0.7f, 1.3f, Hash01(BuildParticleSeed(isNegativePart, burstIndex * 0.137f, 12)));
            var endPosition = startPosition + driftDir * driftAmount;

            var sizeT = Hash01(BuildParticleSeed(isNegativePart, burstIndex * 0.137f, 13));
            var targetSize = activeBoardView.CellSize * Mathf.Lerp(BurstParticleSizeMin, BurstParticleSizeMax, sizeT);
            var startScale = GetSpriteScale(rocketParticleSmokeSprite, targetSize);
            // Burst puffs expand as they fade, like real smoke.
            var endScale = startScale * 1.5f;

            var startRotation = Mathf.Lerp(0f, 360f, Hash01(BuildParticleSeed(isNegativePart, burstIndex * 0.137f, 14)));
            var endRotation = startRotation + Mathf.Lerp(-25f, 25f, Hash01(BuildParticleSeed(isNegativePart, burstIndex * 0.137f, 15)));
            var color = new Color(1f, 1f, 1f, 0.78f);

            var renderer = CreateParticleRenderer(rocketParticleSmokeSprite, isNegativePart, "RocketParticleSmoke");
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
                BurstParticleLifetime,
                color));
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
            var lateralSpread = Mathf.Lerp(-0.05f, 0.05f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 1)));
            var driftScale = Mathf.Lerp(0.7f, 1f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 2)));
            var drift = (-direction * ParticleDriftDistance * driftScale) + (perpendicular * lateralSpread * 2f);
            var startPosition = partPosition - direction * trailOffset + perpendicular * (lateralSpread * 0.5f);
            startPosition.z += zOffset;
            var endPosition = startPosition + drift;
            var startScale = GetSpriteScale(sprite, activeBoardView.CellSize * sizeMultiplier * Mathf.Lerp(0.85f, 1.15f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 3))));
            // Smoke puffs expand as they drift backward.
            var endScale = startScale * Mathf.Lerp(1.3f, 1.7f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 4)));
            var startRotation = Mathf.Lerp(-20f, 20f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 5)));
            var endRotation = startRotation + Mathf.Lerp(-45f, 45f, Hash01(BuildParticleSeed(isNegativePart, spawnTime, 6)));
            var color = sprite == rocketParticleStarSprite
                ? new Color(1f, 1f, 1f, 0.95f)
                : new Color(1f, 1f, 1f, 0.82f);

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

        // Emits an elongated streak particle aligned with the travel direction.
        // These form the readable contrail/trace behind each rocket half.
        private void EmitStreakParticlePair(float spawnTime)
        {
            EmitStreakParticle(spawnTime, isNegativePart: true);
            EmitStreakParticle(spawnTime, isNegativePart: false);
        }

        private void EmitStreakParticle(float spawnTime, bool isNegativePart)
        {
            var travelDuration = isNegativePart ? negativeTravelDuration : positiveTravelDuration;
            if (travelDuration <= 0f || spawnTime > travelDuration)
            {
                return;
            }

            var direction = GetPartDirection(isNegativePart);
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var partPosition = GetPartWorldPosition(isNegativePart, spawnTime);
            var startPosition = partPosition - direction * StreakTrailOffset;
            startPosition.z += StreakZOffset;
            // Slight backward drift gives the streak a "smearing" feel as it ages.
            var endPosition = startPosition - direction * 0.08f;

            var spriteBounds = rocketParticleSmokeSprite.bounds.size;
            var safeW = Mathf.Max(spriteBounds.x, 0.001f);
            var safeH = Mathf.Max(spriteBounds.y, 0.001f);
            var cellSize = activeBoardView.CellSize;
            // Non-uniform scale: long in travel direction, thin perpendicular — forms the streak shape.
            var startScale = new Vector3(
                cellSize * StreakLengthFactor / safeW,
                cellSize * StreakThicknessFactor / safeH,
                1f);
            var endScale = startScale * 0.2f;

            // Rotate the sprite so its local X axis aligns with the travel direction.
            var dirAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // Warm exhaust tint distinguishes the streak from regular smoke puffs.
            var color = new Color(1f, 0.90f, 0.70f, 0.65f);

            var renderer = CreateParticleRenderer(rocketParticleSmokeSprite, isNegativePart, "RocketParticleSmoke");
            renderer.transform.position = startPosition;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, dirAngle);
            renderer.transform.localScale = startScale;
            renderer.color = color;

            activeParticles.Add(new ActiveParticle(
                renderer.gameObject,
                renderer,
                startPosition,
                endPosition,
                startScale,
                endScale,
                dirAngle,
                dirAngle,
                StreakParticleLifetime,
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
            var configuredDuration = ResolveConfiguredDuration();
            originWorldPosition = GetWorldPosition(activeDescriptor.Origin);
            negativeEndWorldPosition = GetWorldPosition(activeDescriptor.NegativeEnd);
            positiveEndWorldPosition = GetWorldPosition(activeDescriptor.PositiveEnd);

            // Extend past the grid edge so the rocket halves fly off-screen.
            // The board clip mask hides them once they cross the boundary.
            if (rocketOverflowCells > 0f)
            {
                var overflow = activeBoardView.CellSize * rocketOverflowCells;
                var negDir = (negativeEndWorldPosition - originWorldPosition).normalized;
                var posDir = (positiveEndWorldPosition - originWorldPosition).normalized;
                negativeEndWorldPosition += negDir * overflow;
                positiveEndWorldPosition += posDir * overflow;
            }

            var negativeDistance = Vector3.Distance(originWorldPosition, negativeEndWorldPosition);
            var positiveDistance = Vector3.Distance(originWorldPosition, positiveEndWorldPosition);
            var maxDistance = Mathf.Max(negativeDistance, positiveDistance);

            if (maxDistance <= Mathf.Epsilon)
            {
                negativeTravelDuration = 0f;
                positiveTravelDuration = 0f;
                activeDuration = configuredDuration;
                return;
            }

            // Both parts move at the same speed; the shorter side simply finishes earlier.
            negativeTravelDuration = configuredDuration * (negativeDistance / maxDistance);
            positiveTravelDuration = configuredDuration * (positiveDistance / maxDistance);
            activeDuration = configuredDuration;
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
            var rawProgress = travelDuration > 0f ? Mathf.Clamp01(time / travelDuration) : 1f;
            return Vector3.Lerp(
                originWorldPosition,
                isNegativePart ? negativeEndWorldPosition : positiveEndWorldPosition,
                EaseOutCubic(rawProgress));
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
            spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            spriteRenderer.transform.localScale = GetSpriteScale(sprite);
            return spriteRenderer;
        }

        private SpriteRenderer CreateParticleRenderer(Sprite sprite, bool isNegativePart, string namePrefix)
        {
            var gameObject = new GameObject($"{namePrefix}_{(isNegativePart ? "Negative" : "Positive")}_{particleSequence++}");
            gameObject.transform.SetParent(effectRoot is not null ? effectRoot : transform, worldPositionStays: false);
            var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
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

        // Fast initial burst that decelerates smoothly — rocket is visible across the full path.
        private static float EaseOutCubic(float progress)
        {
            var inverse = 1f - progress;
            return 1f - inverse * inverse * inverse;
        }

        private float ResolveConfiguredDuration()
        {
            return runtimeDurationOverride > 0f ? runtimeDurationOverride : duration;
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
