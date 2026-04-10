using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Systems;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class SpecialItemComboPresentationPlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float rocketRocketDuration = 0.22f;
        [SerializeField] private float tntRocketDuration = 0.24f;
        [SerializeField] private float tntTntDuration = 0.26f;
        [SerializeField] private float effectZ = -0.18f;
        [SerializeField] private float flashScaleMultiplier = 0.92f;
        [SerializeField] private float tntPulseScaleMultiplier = 1.2f;
        [SerializeField] private Sprite horizontalRocketPartLeftSprite;
        [SerializeField] private Sprite horizontalRocketPartRightSprite;
        [SerializeField] private Sprite verticalRocketPartTopSprite;
        [SerializeField] private Sprite verticalRocketPartBottomSprite;
        [SerializeField] private Sprite tntSprite;

        private readonly List<GameObject> spawnedRoots = new List<GameObject>();
        private readonly List<ActiveFlashVisual> activeFlashVisuals = new List<ActiveFlashVisual>();
        private readonly List<ActiveRocketSweepVisual> activeRocketSweeps = new List<ActiveRocketSweepVisual>();
        private readonly List<SpriteRenderer> activeRocketRenderers = new List<SpriteRenderer>();

        private ActiveTntPulseVisual activeTntPulse;
        private BoardView activeBoardView;
        private float activeDuration;
        private float elapsed;
        private Texture2D runtimeFlashTexture;
        private Sprite runtimeFlashSprite;

        public bool IsPlaying => activeDuration > 0f;

        public float Duration => activeDuration;

        private void OnDestroy()
        {
            DestroyRuntimeFlashAssets();
        }

        public bool TryPlay(BoardView boardView, SpecialItemComboPresentationDescriptor descriptor)
        {
            if (boardView is null)
            {
                throw new ArgumentNullException(nameof(boardView));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var duration = ResolveDuration(descriptor.ComboType);
            if (duration <= 0f)
            {
                return false;
            }

            Stop();

            activeBoardView = boardView;
            activeDuration = duration;
            elapsed = 0f;

            var root = effectRoot is not null ? effectRoot : transform;
            CreateFlashVisuals(root, descriptor);
            CreateRocketSweepVisuals(root, descriptor);
            CreateTntPulseVisual(root, descriptor);

            if (spawnedRoots.Count == 0)
            {
                Stop();
                return false;
            }

            ApplyCurrentState();
            return true;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed = Mathf.Min(activeDuration, elapsed + Mathf.Max(0f, deltaTime));
            ApplyCurrentState();

            if (elapsed >= activeDuration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            for (var index = spawnedRoots.Count - 1; index >= 0; index--)
            {
                if (Application.isPlaying)
                {
                    Destroy(spawnedRoots[index]);
                }
                else
                {
                    DestroyImmediate(spawnedRoots[index]);
                }
            }

            spawnedRoots.Clear();
            activeFlashVisuals.Clear();
            activeRocketSweeps.Clear();
            activeRocketRenderers.Clear();
            activeTntPulse = default;
            activeBoardView = null;
            activeDuration = 0f;
            elapsed = 0f;
        }

        private void ApplyCurrentState()
        {
            if (activeDuration <= 0f)
            {
                return;
            }

            var progress = Mathf.Clamp01(elapsed / activeDuration);
            var easedProgress = EaseOutCubic(progress);

            ApplyFlashState(easedProgress);
            ApplyRocketSweepState();
            ApplyTntPulseState(easedProgress);
        }

        private void ApplyFlashState(float easedProgress)
        {
            var alphaMultiplier = 1f - easedProgress;
            var scaleMultiplier = Mathf.Lerp(0.8f, 1.06f, easedProgress);

            for (var index = 0; index < activeFlashVisuals.Count; index++)
            {
                var visual = activeFlashVisuals[index];
                if (visual.Renderer is null)
                {
                    continue;
                }

                visual.Renderer.transform.localScale = visual.InitialScale * scaleMultiplier;
                var color = visual.BaseColor;
                color.a *= alphaMultiplier;
                visual.Renderer.color = color;
            }
        }

        private void ApplyRocketSweepState()
        {
            var alphaMultiplier = 1f - Mathf.Clamp01(elapsed / activeDuration);

            for (var index = 0; index < activeRocketSweeps.Count; index++)
            {
                var sweep = activeRocketSweeps[index];
                var negativeProgress = sweep.NegativeTravelDuration > 0f
                    ? Mathf.Clamp01(elapsed / sweep.NegativeTravelDuration)
                    : 1f;
                var positiveProgress = sweep.PositiveTravelDuration > 0f
                    ? Mathf.Clamp01(elapsed / sweep.PositiveTravelDuration)
                    : 1f;

                sweep.NegativeRenderer.transform.position = Vector3.Lerp(
                    sweep.OriginWorldPosition,
                    sweep.NegativeEndWorldPosition,
                    EaseOutCubic(negativeProgress));
                sweep.PositiveRenderer.transform.position = Vector3.Lerp(
                    sweep.OriginWorldPosition,
                    sweep.PositiveEndWorldPosition,
                    EaseOutCubic(positiveProgress));
            }

            for (var index = 0; index < activeRocketRenderers.Count; index++)
            {
                var renderer = activeRocketRenderers[index];
                if (renderer is null)
                {
                    continue;
                }

                var color = renderer.color;
                color.a = alphaMultiplier;
                renderer.color = color;
            }
        }

        private void ApplyTntPulseState(float easedProgress)
        {
            if (activeTntPulse.Renderer is null)
            {
                return;
            }

            activeTntPulse.Renderer.transform.localScale = activeTntPulse.InitialScale * Mathf.Lerp(0.7f, 1.55f, easedProgress);
            var color = activeTntPulse.BaseColor;
            color.a *= 1f - easedProgress;
            activeTntPulse.Renderer.color = color;
        }

        private void CreateFlashVisuals(Transform root, SpecialItemComboPresentationDescriptor descriptor)
        {
            if (descriptor.FlashCoordinates.Count == 0)
            {
                return;
            }

            var flashColor = ResolveFlashColor(descriptor.ComboType);
            var flashSprite = GetOrCreateRuntimeFlashSprite();

            for (var index = 0; index < descriptor.FlashCoordinates.Count; index++)
            {
                var coordinate = descriptor.FlashCoordinates[index];
                var visualRoot = CreateRoot(root, $"ComboFlash_{coordinate}");
                var renderer = visualRoot.AddComponent<SpriteRenderer>();
                renderer.sprite = flashSprite;
                renderer.color = flashColor;
                renderer.sortingOrder = 5;
                visualRoot.transform.position = GetWorldPosition(coordinate, effectZ + 0.01f);
                visualRoot.transform.localScale = GetSpriteScale(renderer.sprite, activeBoardView.CellSize * flashScaleMultiplier);
                activeFlashVisuals.Add(new ActiveFlashVisual(renderer, visualRoot.transform.localScale, flashColor));
            }
        }

        private void CreateRocketSweepVisuals(Transform root, SpecialItemComboPresentationDescriptor descriptor)
        {
            for (var index = 0; index < descriptor.RocketSweeps.Count; index++)
            {
                var sweepDescriptor = descriptor.RocketSweeps[index];
                var sweepRoot = CreateRoot(root, $"ComboRocketSweep_{sweepDescriptor.Orientation}_{index}");
                var negativeRenderer = CreateRocketPartRenderer(
                    sweepRoot.transform,
                    ResolveNegativeSprite(sweepDescriptor.Orientation),
                    $"{sweepDescriptor.Orientation}NegativePart");
                var positiveRenderer = CreateRocketPartRenderer(
                    sweepRoot.transform,
                    ResolvePositiveSprite(sweepDescriptor.Orientation),
                    $"{sweepDescriptor.Orientation}PositivePart");

                var originWorldPosition = GetWorldPosition(sweepDescriptor.Origin, effectZ + 0.03f);
                var negativeEndWorldPosition = GetWorldPosition(sweepDescriptor.NegativeEnd, effectZ + 0.03f);
                var positiveEndWorldPosition = GetWorldPosition(sweepDescriptor.PositiveEnd, effectZ + 0.03f);

                negativeRenderer.transform.position = originWorldPosition;
                positiveRenderer.transform.position = originWorldPosition;

                var negativeDistance = Vector3.Distance(originWorldPosition, negativeEndWorldPosition);
                var positiveDistance = Vector3.Distance(originWorldPosition, positiveEndWorldPosition);
                var maxDistance = Mathf.Max(negativeDistance, positiveDistance);
                var negativeTravelDuration = maxDistance > Mathf.Epsilon
                    ? activeDuration * (negativeDistance / maxDistance)
                    : 0f;
                var positiveTravelDuration = maxDistance > Mathf.Epsilon
                    ? activeDuration * (positiveDistance / maxDistance)
                    : 0f;

                activeRocketSweeps.Add(new ActiveRocketSweepVisual(
                    negativeRenderer,
                    positiveRenderer,
                    originWorldPosition,
                    negativeEndWorldPosition,
                    positiveEndWorldPosition,
                    negativeTravelDuration,
                    positiveTravelDuration));
                activeRocketRenderers.Add(negativeRenderer);
                activeRocketRenderers.Add(positiveRenderer);
            }
        }

        private void CreateTntPulseVisual(Transform root, SpecialItemComboPresentationDescriptor descriptor)
        {
            if (!descriptor.HasTntPulse)
            {
                return;
            }

            if (tntSprite is null)
            {
                throw new InvalidOperationException("Combo presentation requires TNT sprite to be assigned for TNT-based combos.");
            }

            var pulseRoot = CreateRoot(root, "ComboTntPulse");
            var renderer = pulseRoot.AddComponent<SpriteRenderer>();
            renderer.sprite = tntSprite;
            renderer.color = ResolveTntPulseColor(descriptor.ComboType);
            renderer.sortingOrder = 8;
            pulseRoot.transform.position = GetPulseWorldPosition(descriptor.ParticipatingSpecialCoordinates);
            pulseRoot.transform.localScale = GetSpriteScale(tntSprite, activeBoardView.CellSize * tntPulseScaleMultiplier);

            activeTntPulse = new ActiveTntPulseVisual(renderer, pulseRoot.transform.localScale, renderer.color);
        }

        private GameObject CreateRoot(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, worldPositionStays: false);
            spawnedRoots.Add(root);
            return root;
        }

        private SpriteRenderer CreateRocketPartRenderer(Transform parent, Sprite sprite, string name)
        {
            if (sprite is null)
            {
                throw new InvalidOperationException("Combo presentation requires all rocket part sprites to be assigned.");
            }

            var rendererObject = new GameObject(name);
            rendererObject.transform.SetParent(parent, worldPositionStays: false);
            var renderer = rendererObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 7;
            renderer.transform.localScale = GetSpriteScale(sprite, activeBoardView.CellSize);
            return renderer;
        }

        private Sprite ResolveNegativeSprite(RocketOrientation orientation)
        {
            return orientation == RocketOrientation.Horizontal
                ? horizontalRocketPartLeftSprite
                : verticalRocketPartBottomSprite;
        }

        private Sprite ResolvePositiveSprite(RocketOrientation orientation)
        {
            return orientation == RocketOrientation.Horizontal
                ? horizontalRocketPartRightSprite
                : verticalRocketPartTopSprite;
        }

        private Vector3 GetWorldPosition(BoardCoordinate coordinate, float zOffset)
        {
            var worldPosition = activeBoardView.GetCellCenterWorld(coordinate);
            worldPosition.z = activeBoardView.transform.position.z + zOffset;
            return worldPosition;
        }

        private Vector3 GetPulseWorldPosition(IReadOnlyList<BoardCoordinate> participatingSpecialCoordinates)
        {
            if (participatingSpecialCoordinates.Count == 0)
            {
                return GetWorldPosition(default, effectZ + 0.04f);
            }

            var total = Vector3.zero;
            for (var index = 0; index < participatingSpecialCoordinates.Count; index++)
            {
                total += activeBoardView.GetCellCenterWorld(participatingSpecialCoordinates[index]);
            }

            var average = total / participatingSpecialCoordinates.Count;
            average.z = activeBoardView.transform.position.z + effectZ + 0.04f;
            return average;
        }

        private Vector3 GetSpriteScale(Sprite sprite, float targetSize)
        {
            var spriteBounds = sprite.bounds.size;
            var safeWidth = spriteBounds.x > 0f ? spriteBounds.x : 1f;
            var safeHeight = spriteBounds.y > 0f ? spriteBounds.y : 1f;
            return new Vector3(targetSize / safeWidth, targetSize / safeHeight, 1f);
        }

        private float ResolveDuration(SpecialItemComboType comboType)
        {
            return comboType switch
            {
                SpecialItemComboType.RocketRocket => rocketRocketDuration,
                SpecialItemComboType.TntRocket => tntRocketDuration,
                SpecialItemComboType.TntTnt => tntTntDuration,
                _ => 0f
            };
        }

        private static Color ResolveFlashColor(SpecialItemComboType comboType)
        {
            return comboType switch
            {
                SpecialItemComboType.RocketRocket => new Color(1f, 0.95f, 0.72f, 0.38f),
                SpecialItemComboType.TntTnt => new Color(1f, 0.62f, 0.42f, 0.42f),
                SpecialItemComboType.TntRocket => new Color(1f, 0.78f, 0.45f, 0.4f),
                _ => new Color(1f, 1f, 1f, 0.35f)
            };
        }

        private static Color ResolveTntPulseColor(SpecialItemComboType comboType)
        {
            return comboType == SpecialItemComboType.TntTnt
                ? new Color(1f, 0.86f, 0.8f, 0.95f)
                : new Color(1f, 0.9f, 0.72f, 0.9f);
        }

        private Sprite GetOrCreateRuntimeFlashSprite()
        {
            if (runtimeFlashSprite is not null)
            {
                return runtimeFlashSprite;
            }

            runtimeFlashTexture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            runtimeFlashTexture.SetPixel(0, 0, Color.white);
            runtimeFlashTexture.Apply();

            runtimeFlashSprite = Sprite.Create(
                runtimeFlashTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            return runtimeFlashSprite;
        }

        private void DestroyRuntimeFlashAssets()
        {
            if (runtimeFlashSprite is not null)
            {
                if (Application.isPlaying)
                {
                    Destroy(runtimeFlashSprite);
                }
                else
                {
                    DestroyImmediate(runtimeFlashSprite);
                }

                runtimeFlashSprite = null;
            }

            if (runtimeFlashTexture is not null)
            {
                if (Application.isPlaying)
                {
                    Destroy(runtimeFlashTexture);
                }
                else
                {
                    DestroyImmediate(runtimeFlashTexture);
                }

                runtimeFlashTexture = null;
            }
        }

        private static float EaseOutCubic(float progress)
        {
            var inverse = 1f - progress;
            return 1f - inverse * inverse * inverse;
        }

        private readonly struct ActiveFlashVisual
        {
            public ActiveFlashVisual(SpriteRenderer renderer, Vector3 initialScale, Color baseColor)
            {
                Renderer = renderer;
                InitialScale = initialScale;
                BaseColor = baseColor;
            }

            public SpriteRenderer Renderer { get; }

            public Vector3 InitialScale { get; }

            public Color BaseColor { get; }
        }

        private readonly struct ActiveRocketSweepVisual
        {
            public ActiveRocketSweepVisual(
                SpriteRenderer negativeRenderer,
                SpriteRenderer positiveRenderer,
                Vector3 originWorldPosition,
                Vector3 negativeEndWorldPosition,
                Vector3 positiveEndWorldPosition,
                float negativeTravelDuration,
                float positiveTravelDuration)
            {
                NegativeRenderer = negativeRenderer;
                PositiveRenderer = positiveRenderer;
                OriginWorldPosition = originWorldPosition;
                NegativeEndWorldPosition = negativeEndWorldPosition;
                PositiveEndWorldPosition = positiveEndWorldPosition;
                NegativeTravelDuration = negativeTravelDuration;
                PositiveTravelDuration = positiveTravelDuration;
            }

            public SpriteRenderer NegativeRenderer { get; }

            public SpriteRenderer PositiveRenderer { get; }

            public Vector3 OriginWorldPosition { get; }

            public Vector3 NegativeEndWorldPosition { get; }

            public Vector3 PositiveEndWorldPosition { get; }

            public float NegativeTravelDuration { get; }

            public float PositiveTravelDuration { get; }
        }

        private readonly struct ActiveTntPulseVisual
        {
            public ActiveTntPulseVisual(SpriteRenderer renderer, Vector3 initialScale, Color baseColor)
            {
                Renderer = renderer;
                InitialScale = initialScale;
                BaseColor = baseColor;
            }

            public SpriteRenderer Renderer { get; }

            public Vector3 InitialScale { get; }

            public Color BaseColor { get; }
        }
    }
}
