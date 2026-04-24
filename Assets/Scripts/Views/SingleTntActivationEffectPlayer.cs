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
        [SerializeField] private float blastStartDelay = 1.2f;
        // How long TNT blocks settle — just the initial impact pop.
        // Lingering particles keep animating during settle via Advance().
        [SerializeField] private float settleBlockingDuration = 0.06f;
        [SerializeField] private float effectZ = -0.18f;
        [SerializeField] private float pulseScaleMultiplier = 1.35f;
        [SerializeField] private Sprite tntBurstSprite;
        [SerializeField] private Sprite tntDebrisSprite;

        private readonly List<ActiveVisual> activeVisuals = new List<ActiveVisual>();
        private float elapsed;

        public bool IsPlaying => activeVisuals.Count > 0;

        public float Duration => Mathf.Max(0f, blastStartDelay) + duration;

        public float BlastStartDelay => Mathf.Max(0f, blastStartDelay);

        public float SettleBlockingDuration => Mathf.Min(settleBlockingDuration, duration);

        public Sprite TntBurstSprite => tntBurstSprite;

        public Sprite TntDebrisSprite => tntDebrisSprite;

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

            if (tntBurstSprite is null || tntDebrisSprite is null)
            {
                throw new InvalidOperationException("Single TNT effect requires burst and debris sprites to be assigned.");
            }

            Stop();
            elapsed = 0f;

            var root = effectRoot is not null ? effectRoot : transform;
            var areaCenter = GetImpactCenter(boardView, descriptor.MinX, descriptor.MinY, descriptor.MaxX, descriptor.MaxY);
            var areaDiameter = GetImpactDiameter(boardView, descriptor.MinX, descriptor.MinY, descriptor.MaxX, descriptor.MaxY);
            CreatePulse(root, areaCenter, areaDiameter);
            CreateExplosionCore(root, areaCenter, areaDiameter);
            ApplyCurrentState();
            return activeVisuals.Count > 0;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed = Mathf.Min(Duration, elapsed + Mathf.Max(0f, deltaTime));
            ApplyCurrentState();

            if (elapsed >= Duration)
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

        private void CreatePulse(Transform root, Vector3 areaCenter, float areaDiameter)
        {
            var pulseRoot = CreateRoot(root, "SingleTntPulse");
            var renderer = CreateSpriteRenderer(pulseRoot, tntBurstSprite, new Color(1f, 1f, 1f, 0.95f), 11);
            pulseRoot.transform.position = areaCenter;
            var startScale = GetSpriteScale(tntBurstSprite, areaDiameter * 0.45f);
            var endScale = GetSpriteScale(tntBurstSprite, areaDiameter * pulseScaleMultiplier);
            pulseRoot.transform.localScale = startScale;
            activeVisuals.Add(new ActiveVisual(
                pulseRoot,
                renderer,
                startPosition: areaCenter,
                endPosition: areaCenter,
                startScale,
                endScale,
                startRotation: 0f,
                endRotation: 10f,
                renderer.color));
        }

        private void CreateExplosionCore(Transform root, Vector3 areaCenter, float areaDiameter)
        {
            var explosionRoot = CreateRoot(root, "SingleTntExplosionCore");
            var renderer = CreateSpriteRenderer(explosionRoot, tntDebrisSprite, new Color(1f, 1f, 1f, 0.92f), 10);
            var startScale = GetSpriteScale(tntDebrisSprite, areaDiameter * 0.58f);
            var endScale = GetSpriteScale(tntDebrisSprite, areaDiameter * 1.08f);
            var startRotation = -12f;
            var endRotation = 18f;

            explosionRoot.transform.position = areaCenter;
            explosionRoot.transform.localScale = startScale;
            explosionRoot.transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
            activeVisuals.Add(new ActiveVisual(
                explosionRoot,
                renderer,
                areaCenter,
                areaCenter,
                startScale,
                endScale,
                startRotation,
                endRotation,
                renderer.color));
        }

        private void ApplyCurrentState()
        {
            var localElapsed = elapsed - Mathf.Max(0f, blastStartDelay);
            var progress = duration > 0f ? Mathf.Clamp01(localElapsed / duration) : 1f;
            var easedProgress = EaseOutCubic(progress);
            var isWaitingForBlast = localElapsed < 0f;

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
                color.a *= isWaitingForBlast ? 0f : 1f - easedProgress;
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

        private Vector3 GetWorldPosition(BoardView boardView, BoardCoordinate coordinate, float zOffset)
        {
            var worldPosition = boardView.GetCellCenterWorld(coordinate);
            worldPosition.z = boardView.transform.position.z + zOffset;
            return worldPosition;
        }

        private Vector3 GetImpactCenter(BoardView boardView, int minX, int minY, int maxX, int maxY)
        {
            var bottomLeft = GetWorldPosition(boardView, new BoardCoordinate(minX, minY), effectZ + 0.02f);
            var topRight = GetWorldPosition(boardView, new BoardCoordinate(maxX, maxY), effectZ + 0.02f);
            return (bottomLeft + topRight) * 0.5f;
        }

        private static float GetImpactDiameter(BoardView boardView, int minX, int minY, int maxX, int maxY)
        {
            var width = (maxX - minX + 1) * boardView.CellSize;
            var height = (maxY - minY + 1) * boardView.CellSize;
            return Mathf.Max(width, height);
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
