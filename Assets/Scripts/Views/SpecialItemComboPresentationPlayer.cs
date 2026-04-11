using System;
using System.Collections.Generic;
using DG.Tweening;
using DreamBlastClone.Core;
using DreamBlastClone.Systems;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class SpecialItemComboPresentationPlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float rocketRocketDuration = 0.45f;
        [SerializeField] private float tntRocketDuration = 0.5f;
        [SerializeField] private float tntTntDuration = 0.38f;
        [SerializeField] private float effectZ = -0.18f;
        [SerializeField] private float rocketOverflowCells = 2f;
        [SerializeField] private float flashScaleMultiplier = 0.92f;
        [SerializeField] private float tntPulseScaleMultiplier = 1.2f;
        [SerializeField] private Sprite horizontalRocketPartLeftSprite;
        [SerializeField] private Sprite horizontalRocketPartRightSprite;
        [SerializeField] private Sprite verticalRocketPartTopSprite;
        [SerializeField] private Sprite verticalRocketPartBottomSprite;
        [SerializeField] private Sprite rocketParticleStarSprite;
        [SerializeField] private Sprite rocketParticleSmokeSprite;
        [SerializeField] private Sprite tntBurstSprite;
        [SerializeField] private Sprite tntDebrisSprite;

        private readonly List<GameObject> spawnedRoots = new List<GameObject>();
        private readonly List<ActiveFlashVisual> activeFlashVisuals = new List<ActiveFlashVisual>();
        private readonly List<SingleRocketActivationEffectPlayer> activeRocketSweepPlayers = new List<SingleRocketActivationEffectPlayer>();
        private readonly List<Tween> activeTweens = new List<Tween>();

        private ActiveTntAreaVisual activeTntPulse;
        private ActiveTntAreaVisual activeTntExplosion;
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
            PresentationTweenBootstrap.EnsureInitialized();

            activeBoardView = boardView;
            activeDuration = duration;
            elapsed = 0f;

            var root = effectRoot is not null ? effectRoot : transform;
            CreateFlashVisuals(root, descriptor);
            CreateRocketSweepVisuals(root, descriptor);
            CreateTntExplosionVisuals(root, descriptor);

            if (spawnedRoots.Count == 0)
            {
                Stop();
                return false;
            }

            LaunchFlashTweens();
            LaunchTntAreaTweens();
            return true;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed = Mathf.Min(activeDuration, elapsed + Mathf.Max(0f, deltaTime));

            for (var index = 0; index < activeRocketSweepPlayers.Count; index++)
            {
                activeRocketSweepPlayers[index]?.Advance(deltaTime);
            }

            if (elapsed >= activeDuration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            for (var index = activeTweens.Count - 1; index >= 0; index--)
            {
                activeTweens[index]?.Kill();
            }

            activeTweens.Clear();

            for (var index = activeRocketSweepPlayers.Count - 1; index >= 0; index--)
            {
                activeRocketSweepPlayers[index]?.Stop();
            }

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
            activeRocketSweepPlayers.Clear();
            activeTntPulse = default;
            activeTntExplosion = default;
            activeBoardView = null;
            activeDuration = 0f;
            elapsed = 0f;
        }

        private void LaunchFlashTweens()
        {
            for (var index = 0; index < activeFlashVisuals.Count; index++)
            {
                var visual = activeFlashVisuals[index];
                if (visual.Renderer is null)
                {
                    continue;
                }

                // Start compact, punch out with overshoot while fading
                visual.Renderer.transform.localScale = visual.InitialScale * 0.7f;
                var renderer = visual.Renderer;
                var seq = DOTween.Sequence()
                    .Join(TweenRendererAlpha(renderer, 0f, activeDuration).SetEase(Ease.InQuad))
                    .Join(renderer.transform.DOScale(visual.InitialScale * 1.12f, activeDuration * 0.4f).SetEase(Ease.OutBack))
                    .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                    .SetLink(renderer.gameObject, LinkBehaviour.KillOnDestroy);
                activeTweens.Add(seq);
            }
        }

        private void LaunchTntAreaTweens()
        {
            if (activeTntPulse.Renderer is null)
            {
            }
            else
            {
                var renderer = activeTntPulse.Renderer;
                var endScale = activeTntPulse.InitialScale * 1.15f;
                var seq = DOTween.Sequence()
                    .Append(renderer.transform.DOScale(endScale, activeDuration * 0.55f).SetEase(Ease.OutBack))
                    .Join(TweenRendererAlpha(renderer, 0f, activeDuration).SetEase(Ease.InQuad))
                    .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                    .SetLink(renderer.gameObject, LinkBehaviour.KillOnDestroy);
                activeTweens.Add(seq);
            }

            if (activeTntExplosion.Renderer is null)
            {
                return;
            }

            {
                var renderer = activeTntExplosion.Renderer;
                var endScale = activeTntExplosion.InitialScale * 1.12f;
                var seq = DOTween.Sequence()
                    .Append(renderer.transform.DOScale(endScale, activeDuration * 0.25f).SetEase(Ease.OutCubic))
                    .Join(TweenRendererAlpha(renderer, 0f, activeDuration * 0.8f).SetEase(Ease.InQuad))
                    .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                    .SetLink(renderer.gameObject, LinkBehaviour.KillOnDestroy);
                activeTweens.Add(seq);
            }
        }

        private static Tween TweenRendererAlpha(SpriteRenderer renderer, float targetAlpha, float fadeDuration)
        {
            return DOTween.To(
                () => renderer.color.a,
                value =>
                {
                    var color = renderer.color;
                    color.a = value;
                    renderer.color = color;
                },
                targetAlpha,
                fadeDuration);
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
            var rocketVisualSource = ResolveRocketVisualSource();

            for (var index = 0; index < descriptor.RocketSweeps.Count; index++)
            {
                var sweepDescriptor = descriptor.RocketSweeps[index];
                var sweepRoot = CreateRoot(root, $"ComboRocketSweep_{sweepDescriptor.Orientation}_{index}");
                var rocketPlayer = sweepRoot.AddComponent<SingleRocketActivationEffectPlayer>();
                rocketPlayer.ConfigureForRuntime(
                    sweepRoot.transform,
                    activeDuration,
                    (rocketVisualSource?.EffectZ ?? effectZ) + 0.03f,
                    rocketVisualSource?.RocketOverflowCells ?? rocketOverflowCells,
                    ResolveSprite(horizontalRocketPartLeftSprite, rocketVisualSource?.HorizontalRocketPartLeftSprite),
                    ResolveSprite(horizontalRocketPartRightSprite, rocketVisualSource?.HorizontalRocketPartRightSprite),
                    ResolveSprite(verticalRocketPartTopSprite, rocketVisualSource?.VerticalRocketPartTopSprite),
                    ResolveSprite(verticalRocketPartBottomSprite, rocketVisualSource?.VerticalRocketPartBottomSprite),
                    ResolveSprite(rocketParticleStarSprite, rocketVisualSource?.RocketParticleStarSprite),
                    ResolveSprite(rocketParticleSmokeSprite, rocketVisualSource?.RocketParticleSmokeSprite));

                if (rocketPlayer.TryPlay(activeBoardView, sweepDescriptor))
                {
                    activeRocketSweepPlayers.Add(rocketPlayer);
                }
            }
        }

        private SingleRocketActivationEffectPlayer ResolveRocketVisualSource()
        {
            return GetComponent<SingleRocketActivationEffectPlayer>();
        }

        private static Sprite ResolveSprite(Sprite primary, Sprite fallback)
        {
            return primary != null ? primary : fallback;
        }

        private void CreateTntExplosionVisuals(Transform root, SpecialItemComboPresentationDescriptor descriptor)
        {
            if (!descriptor.HasTntPulse)
            {
                return;
            }

            var tntVisualSource = ResolveTntVisualSource();
            var burstSprite = ResolveSprite(tntBurstSprite, tntVisualSource?.TntBurstSprite);
            var debrisSprite = ResolveSprite(tntDebrisSprite, tntVisualSource?.TntDebrisSprite);
            if (burstSprite is null || debrisSprite is null)
            {
                throw new InvalidOperationException("Combo presentation requires TNT burst and debris sprites to be assigned for TNT-based combos.");
            }

            var areaCenter = GetAreaWorldCenter(descriptor.FlashCoordinates, effectZ + 0.04f);
            var areaDiameter = GetAreaDiameter(descriptor.FlashCoordinates);

            var pulseRoot = CreateRoot(root, "ComboTntPulse");
            var pulseRenderer = pulseRoot.AddComponent<SpriteRenderer>();
            pulseRenderer.sprite = burstSprite;
            pulseRenderer.color = ResolveTntPulseColor(descriptor.ComboType);
            pulseRenderer.sortingOrder = 8;
            pulseRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            pulseRoot.transform.position = areaCenter;
            pulseRoot.transform.localScale = GetSpriteScale(burstSprite, areaDiameter * tntPulseScaleMultiplier);

            var explosionRoot = CreateRoot(root, "ComboTntExplosion");
            var explosionRenderer = explosionRoot.AddComponent<SpriteRenderer>();
            explosionRenderer.sprite = debrisSprite;
            explosionRenderer.color = ResolveTntExplosionColor(descriptor.ComboType);
            explosionRenderer.sortingOrder = 9;
            explosionRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            explosionRoot.transform.position = areaCenter;
            explosionRoot.transform.localScale = GetSpriteScale(debrisSprite, areaDiameter * 0.98f);

            activeTntPulse = new ActiveTntAreaVisual(pulseRenderer, pulseRoot.transform.localScale, pulseRenderer.color);
            activeTntExplosion = new ActiveTntAreaVisual(explosionRenderer, explosionRoot.transform.localScale, explosionRenderer.color);
        }

        private SingleTntActivationEffectPlayer ResolveTntVisualSource()
        {
            return GetComponent<SingleTntActivationEffectPlayer>();
        }

        private GameObject CreateRoot(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, worldPositionStays: false);
            spawnedRoots.Add(root);
            return root;
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

        private Vector3 GetAreaWorldCenter(IReadOnlyList<BoardCoordinate> coordinates, float zOffset)
        {
            if (coordinates.Count == 0)
            {
                return GetPulseWorldPosition(System.Array.Empty<BoardCoordinate>());
            }

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            for (var index = 0; index < coordinates.Count; index++)
            {
                var coordinate = coordinates[index];
                minX = Math.Min(minX, coordinate.X);
                minY = Math.Min(minY, coordinate.Y);
                maxX = Math.Max(maxX, coordinate.X);
                maxY = Math.Max(maxY, coordinate.Y);
            }

            var bottomLeft = GetWorldPosition(new BoardCoordinate(minX, minY), zOffset);
            var topRight = GetWorldPosition(new BoardCoordinate(maxX, maxY), zOffset);
            return (bottomLeft + topRight) * 0.5f;
        }

        private float GetAreaDiameter(IReadOnlyList<BoardCoordinate> coordinates)
        {
            if (coordinates.Count == 0)
            {
                return activeBoardView.CellSize;
            }

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            for (var index = 0; index < coordinates.Count; index++)
            {
                var coordinate = coordinates[index];
                minX = Math.Min(minX, coordinate.X);
                minY = Math.Min(minY, coordinate.Y);
                maxX = Math.Max(maxX, coordinate.X);
                maxY = Math.Max(maxY, coordinate.Y);
            }

            var width = (maxX - minX + 1) * activeBoardView.CellSize;
            var height = (maxY - minY + 1) * activeBoardView.CellSize;
            return Mathf.Max(width, height);
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

        private static Color ResolveTntExplosionColor(SpecialItemComboType comboType)
        {
            return comboType == SpecialItemComboType.TntTnt
                ? new Color(1f, 1f, 1f, 0.9f)
                : new Color(1f, 1f, 1f, 0.84f);
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

        private readonly struct ActiveTntAreaVisual
        {
            public ActiveTntAreaVisual(SpriteRenderer renderer, Vector3 initialScale, Color baseColor)
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
