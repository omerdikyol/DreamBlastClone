using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DreamBlastClone.Views
{
    public sealed class MoveHintOutlineView : MonoBehaviour
    {
        [SerializeField] private Color outlineColor = new Color(1f, 0.97f, 0.84f, 1f);
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.96f, 1f);
        [SerializeField] private float baseAlpha = 0.42f;
        [SerializeField] private float glowAlpha = 0.18f;
        [SerializeField] private float highlightAlpha = 0.72f;
        [SerializeField] private float lineWidth = 0.022f;
        [SerializeField] private float glowWidthMultiplier = 1.7f;
        [SerializeField] private float highlightWidthMultiplier = 0.42f;
        [SerializeField] private float alphaPulseAmplitude = 0.028f;
        [SerializeField] private float pulseFrequency = 0.9f;
        [SerializeField] private float zOffset = -0.2f;
        [SerializeField] private int sortingOrder = 20;

        private static Material runtimeLineMaterial;
        private readonly MoveHintOutlineSegmentBuilder segmentBuilder = new MoveHintOutlineSegmentBuilder();
        private readonly List<SegmentRenderers> activeSegments = new List<SegmentRenderers>();
        private float pulseTimeSeconds;

        public bool IsShowing => activeSegments.Count > 0;

        private void Update()
        {
            if (!IsShowing)
            {
                return;
            }

            pulseTimeSeconds += Time.unscaledDeltaTime;
            ApplyPulse();
        }

        public void Show(BoardView boardView, IReadOnlyList<DreamBlastClone.Core.BoardCoordinate> highlightedCoordinates)
        {
            if (boardView is null)
            {
                throw new ArgumentNullException(nameof(boardView));
            }

            if (highlightedCoordinates is null)
            {
                throw new ArgumentNullException(nameof(highlightedCoordinates));
            }

            Hide();

            if (highlightedCoordinates.Count == 0)
            {
                return;
            }

            transform.SetParent(boardView.transform, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            var segments = segmentBuilder.Build(highlightedCoordinates);
            for (var index = 0; index < segments.Count; index++)
            {
                var segment = segments[index];
                var segmentRoot = new GameObject($"MoveHintSegment_{index}");
                segmentRoot.transform.SetParent(transform, worldPositionStays: false);

                var glowRenderer = CreateLineRenderer(segmentRoot.transform, "Glow", sortingOrder);
                var coreRenderer = CreateLineRenderer(segmentRoot.transform, "Core", sortingOrder + 1);
                var highlightRenderer = CreateLineRenderer(segmentRoot.transform, "Highlight", sortingOrder + 2);
                var start = boardView.GetGridVertexLocal(segment.StartX, segment.StartY, zOffset);
                var end = boardView.GetGridVertexLocal(segment.EndX, segment.EndY, zOffset);
                SetSegmentPositions(glowRenderer, start, end);
                SetSegmentPositions(coreRenderer, start, end);
                SetSegmentPositions(highlightRenderer, start, end);
                activeSegments.Add(new SegmentRenderers(segmentRoot, glowRenderer, coreRenderer, highlightRenderer));
            }

            pulseTimeSeconds = 0f;
            ApplyPulse();
        }

        public void Hide()
        {
            for (var index = activeSegments.Count - 1; index >= 0; index--)
            {
                var segment = activeSegments[index];
                if (segment.Root == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(segment.Root);
                }
                else
                {
                    DestroyImmediate(segment.Root);
                }
            }

            activeSegments.Clear();
        }

        private void OnDisable()
        {
            Hide();
        }

        private void OnDestroy()
        {
            Hide();
        }

        private LineRenderer CreateLineRenderer(Transform parent, string name, int rendererSortingOrder)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, worldPositionStays: false);
            var lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = false;
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.numCapVertices = 8;
            lineRenderer.numCornerVertices = 8;
            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.material = ResolveRuntimeLineMaterial();
            lineRenderer.sortingOrder = rendererSortingOrder;
            lineRenderer.positionCount = 2;
            return lineRenderer;
        }

        private static void SetSegmentPositions(LineRenderer lineRenderer, Vector3 start, Vector3 end)
        {
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        private void ApplyPulse()
        {
            var pulse = pulseFrequency > 0f
                ? Mathf.Sin(pulseTimeSeconds * pulseFrequency * Mathf.PI * 2f)
                : 0f;
            var coreAlpha = Mathf.Clamp01(baseAlpha * (1f + pulse * alphaPulseAmplitude));
            var glowPulse = Mathf.Sin((pulseTimeSeconds + 0.14f) * pulseFrequency * Mathf.PI * 2f);
            var resolvedGlowAlpha = Mathf.Clamp01(glowAlpha * (1f + glowPulse * alphaPulseAmplitude * 1.2f));
            var coreColor = outlineColor;
            coreColor.a = coreAlpha;
            var glowColor = outlineColor;
            glowColor.a = resolvedGlowAlpha;
            var coreWidth = Mathf.Max(0.005f, lineWidth);
            var glowWidth = Mathf.Max(coreWidth, lineWidth * glowWidthMultiplier);

            for (var index = 0; index < activeSegments.Count; index++)
            {
                var segment = activeSegments[index];
                if (segment.Glow == null || segment.Core == null || segment.Highlight == null)
                {
                    continue;
                }

                segment.Glow.startWidth = glowWidth;
                segment.Glow.endWidth = glowWidth;
                segment.Glow.colorGradient = BuildGradient(glowColor, 0.22f, 1f);

                segment.Core.startWidth = coreWidth;
                segment.Core.endWidth = coreWidth;
                segment.Core.colorGradient = BuildGradient(coreColor, 0.5f, 1f);

                var highlightResolvedColor = highlightColor;
                highlightResolvedColor.a = Mathf.Clamp01(highlightAlpha * (1f + glowPulse * alphaPulseAmplitude * 1.4f));
                var highlightWidth = Mathf.Max(0.0035f, coreWidth * highlightWidthMultiplier);
                segment.Highlight.startWidth = highlightWidth;
                segment.Highlight.endWidth = highlightWidth;
                segment.Highlight.colorGradient = BuildGradient(highlightResolvedColor, 0.15f, 0.95f);
            }
        }

        private static Gradient BuildGradient(Color color, float edgeAlphaMultiplier, float centerAlphaMultiplier)
        {
            var edgeColor = color;
            edgeColor.a *= edgeAlphaMultiplier;
            var centerColor = color;
            centerColor.a *= centerAlphaMultiplier;

            return new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(edgeColor, 0f),
                    new GradientColorKey(centerColor, 0.5f),
                    new GradientColorKey(edgeColor, 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(edgeColor.a, 0f),
                    new GradientAlphaKey(centerColor.a, 0.5f),
                    new GradientAlphaKey(edgeColor.a, 1f)
                }
            };
        }

        private static Material ResolveRuntimeLineMaterial()
        {
            if (runtimeLineMaterial != null)
            {
                return runtimeLineMaterial;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException("MoveHintOutlineView requires the Sprites/Default shader.");
            }

            runtimeLineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return runtimeLineMaterial;
        }

        private readonly struct SegmentRenderers
        {
            public SegmentRenderers(GameObject root, LineRenderer glow, LineRenderer core, LineRenderer highlight)
            {
                Root = root;
                Glow = glow;
                Core = core;
                Highlight = highlight;
            }

            public GameObject Root { get; }

            public LineRenderer Glow { get; }

            public LineRenderer Core { get; }

            public LineRenderer Highlight { get; }
        }
    }
}
