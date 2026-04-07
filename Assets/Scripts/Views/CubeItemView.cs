using System;
using DreamBlastClone.Core;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class CubeItemView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Default")]
        [SerializeField] private Sprite redDefaultSprite;
        [SerializeField] private Sprite greenDefaultSprite;
        [SerializeField] private Sprite blueDefaultSprite;
        [SerializeField] private Sprite yellowDefaultSprite;

        [Header("Rocket Eligible")]
        [SerializeField] private Sprite redRocketSprite;
        [SerializeField] private Sprite greenRocketSprite;
        [SerializeField] private Sprite blueRocketSprite;
        [SerializeField] private Sprite yellowRocketSprite;

        [Header("TNT Eligible")]
        [SerializeField] private Sprite redTntSprite;
        [SerializeField] private Sprite greenTntSprite;
        [SerializeField] private Sprite blueTntSprite;
        [SerializeField] private Sprite yellowTntSprite;

        public void SetAppearance(CubeColor color, CubeVisualState state)
        {
            var targetRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();

            if (targetRenderer == null)
            {
                throw new InvalidOperationException("CubeItemView requires a SpriteRenderer reference.");
            }

            var sprite = ResolveSprite(color, state);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"CubeItemView requires a sprite for color '{color}' in visual state '{state}'.");
            }

            targetRenderer.sprite = sprite;
            targetRenderer.color = Color.white;
        }

        private Sprite ResolveSprite(CubeColor color, CubeVisualState state)
        {
            return (color, state) switch
            {
                (CubeColor.Red, CubeVisualState.Default) => redDefaultSprite,
                (CubeColor.Green, CubeVisualState.Default) => greenDefaultSprite,
                (CubeColor.Blue, CubeVisualState.Default) => blueDefaultSprite,
                (CubeColor.Yellow, CubeVisualState.Default) => yellowDefaultSprite,
                (CubeColor.Red, CubeVisualState.RocketEligible) => redRocketSprite,
                (CubeColor.Green, CubeVisualState.RocketEligible) => greenRocketSprite,
                (CubeColor.Blue, CubeVisualState.RocketEligible) => blueRocketSprite,
                (CubeColor.Yellow, CubeVisualState.RocketEligible) => yellowRocketSprite,
                (CubeColor.Red, CubeVisualState.TntEligible) => redTntSprite,
                (CubeColor.Green, CubeVisualState.TntEligible) => greenTntSprite,
                (CubeColor.Blue, CubeVisualState.TntEligible) => blueTntSprite,
                (CubeColor.Yellow, CubeVisualState.TntEligible) => yellowTntSprite,
                _ => null
            };
        }
    }
}
