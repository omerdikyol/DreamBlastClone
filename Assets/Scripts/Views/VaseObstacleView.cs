using System;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class VaseObstacleView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite undamagedSprite;
        [SerializeField] private Sprite damagedSprite;

        public void SetAppearance(int remainingDurability)
        {
            var targetRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();

            if (targetRenderer == null)
            {
                throw new InvalidOperationException("VaseObstacleView requires a SpriteRenderer reference.");
            }

            var sprite = remainingDurability <= 1 ? damagedSprite : undamagedSprite;
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"VaseObstacleView requires a sprite for remaining durability '{remainingDurability}'.");
            }

            targetRenderer.sprite = sprite;
            targetRenderer.color = Color.white;
        }
    }
}
