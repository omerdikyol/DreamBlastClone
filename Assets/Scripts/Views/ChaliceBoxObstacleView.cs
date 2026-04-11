using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class ChaliceBoxObstacleView : MonoBehaviour
    {
        private static readonly ChaliceSlotLayout[] SlotLayouts =
        {
            new ChaliceSlotLayout(new Vector2(-0.28f, -0.22f), 0.31f, 0),
            new ChaliceSlotLayout(new Vector2(-0.14f, -0.22f), 0.31f, 1),
            new ChaliceSlotLayout(new Vector2(0.00f, -0.22f), 0.31f, 2),
            new ChaliceSlotLayout(new Vector2(0.14f, -0.22f), 0.31f, 1),
            new ChaliceSlotLayout(new Vector2(0.28f, -0.22f), 0.31f, 0),
            new ChaliceSlotLayout(new Vector2(-0.28f, 0.18f), 0.34f, 3),
            new ChaliceSlotLayout(new Vector2(-0.14f, 0.18f), 0.34f, 4),
            new ChaliceSlotLayout(new Vector2(0.00f, 0.18f), 0.34f, 5),
            new ChaliceSlotLayout(new Vector2(0.14f, 0.18f), 0.34f, 4),
            new ChaliceSlotLayout(new Vector2(0.28f, 0.18f), 0.34f, 3)
        };

        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer doorsRenderer;
        [SerializeField] private SpriteRenderer chaliceRenderer;

        private readonly List<SpriteRenderer> slotRenderers = new List<SpriteRenderer>(SlotLayouts.Length);

        public void SetAppearance(
            bool isDoorPhase,
            IReadOnlyList<bool> visibleSlotMask,
            bool startTweenPresentation = true,
            IReadOnlyList<int> removedSlotIndices = null,
            float phaseOffset = 0f)
        {
            var background = backgroundRenderer != null ? backgroundRenderer : transform.Find("Bg")?.GetComponent<SpriteRenderer>();
            var doors = doorsRenderer != null ? doorsRenderer : transform.Find("Doors")?.GetComponent<SpriteRenderer>();
            var chaliceTemplate = chaliceRenderer != null ? chaliceRenderer : transform.Find("Chalice")?.GetComponent<SpriteRenderer>();

            if (background == null || doors == null || chaliceTemplate == null)
            {
                throw new InvalidOperationException("ChaliceBoxObstacleView requires Bg, Doors, and Chalice SpriteRenderers.");
            }

            if (visibleSlotMask is null)
            {
                throw new ArgumentNullException(nameof(visibleSlotMask));
            }

            if (visibleSlotMask.Count != SlotLayouts.Length)
            {
                throw new InvalidOperationException($"ChaliceBoxObstacleView requires exactly {SlotLayouts.Length} visible-slot entries.");
            }

            background.enabled = true;
            background.color = Color.white;
            background.sortingOrder = 0;
            doors.color = Color.white;
            doors.sortingOrder = 1;
            chaliceTemplate.enabled = false;
            chaliceTemplate.color = Color.white;
            chaliceTemplate.sortingOrder = 2;

            EnsureSlotRenderers(background, chaliceTemplate);

            if (isDoorPhase)
            {
                doors.enabled = true;
                SetSlotVisibility(visibleSlotMask: null);
                ApplyTweenPresentation(background, doors, isDoorPhase, startTweenPresentation, removedSlotIndices, phaseOffset);
                return;
            }

            doors.enabled = false;
            SetSlotVisibility(visibleSlotMask);
            ApplyTweenPresentation(background, doors, isDoorPhase, startTweenPresentation, removedSlotIndices, phaseOffset);
        }

        private void EnsureSlotRenderers(SpriteRenderer backgroundRenderer, SpriteRenderer chaliceTemplate)
        {
            if (slotRenderers.Count > 0)
            {
                for (var index = 0; index < slotRenderers.Count; index++)
                {
                    slotRenderers[index].sortingOrder = backgroundRenderer.sortingOrder + 2 + SlotLayouts[index].SortingOrder;
                }
                return;
            }

            if (chaliceTemplate.sprite is null)
            {
                throw new InvalidOperationException("ChaliceBoxObstacleView requires the Chalice SpriteRenderer to have a sprite assigned.");
            }

            var slotRoot = transform.Find("ChaliceSlots");
            if (slotRoot is null)
            {
                slotRoot = new GameObject("ChaliceSlots").transform;
                slotRoot.SetParent(transform, false);
            }

            for (var index = 0; index < SlotLayouts.Length; index++)
            {
                var slot = new GameObject($"ChaliceSlot_{index}");
                slot.transform.SetParent(slotRoot, false);
                slot.transform.localPosition = new Vector3(SlotLayouts[index].LocalPosition.x, SlotLayouts[index].LocalPosition.y, 0f);
                slot.transform.localScale = Vector3.one * SlotLayouts[index].Scale;

                var renderer = slot.AddComponent<SpriteRenderer>();
                renderer.sprite = chaliceTemplate.sprite;
                renderer.color = Color.white;
                renderer.sortingLayerID = chaliceTemplate.sortingLayerID;
                renderer.sortingOrder = backgroundRenderer.sortingOrder + 2 + SlotLayouts[index].SortingOrder;
                renderer.enabled = false;
                slotRenderers.Add(renderer);
            }
        }

        private void SetSlotVisibility(IReadOnlyList<bool> visibleSlotMask)
        {
            for (var index = 0; index < slotRenderers.Count; index++)
            {
                var renderer = slotRenderers[index];
                renderer.enabled = visibleSlotMask != null && visibleSlotMask[index];
                renderer.color = Color.white;
            }
        }

        private void ApplyTweenPresentation(
            SpriteRenderer background,
            SpriteRenderer doors,
            bool isDoorPhase,
            bool startTweenPresentation,
            IReadOnlyList<int> removedSlotIndices,
            float phaseOffset)
        {
            if (!TryGetComponent<ChaliceBoxTweenPresentationView>(out var tweenPresentation))
            {
                return;
            }

            tweenPresentation.PlayState(
                background,
                doors,
                slotRenderers,
                isDoorPhase,
                startTweenPresentation,
                removedSlotIndices ?? Array.Empty<int>(),
                phaseOffset);
        }

        private readonly struct ChaliceSlotLayout
        {
            public ChaliceSlotLayout(Vector2 localPosition, float scale, int sortingOrder)
            {
                LocalPosition = localPosition;
                Scale = scale;
                SortingOrder = sortingOrder;
            }

            public Vector2 LocalPosition { get; }

            public float Scale { get; }

            public int SortingOrder { get; }
        }
    }
}
