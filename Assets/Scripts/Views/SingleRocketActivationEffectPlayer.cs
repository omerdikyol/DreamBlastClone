using System;
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
            return true;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed = Mathf.Min(activeDuration, elapsed + Mathf.Max(0f, deltaTime));
            ApplyCurrentPositions();

            if (elapsed >= activeDuration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            DestroyRenderer(ref negativePartRenderer);
            DestroyRenderer(ref positivePartRenderer);
            activeBoardView = null;
            activeDescriptor = null;
            originWorldPosition = default;
            negativeEndWorldPosition = default;
            positiveEndWorldPosition = default;
            negativeTravelDuration = 0f;
            positiveTravelDuration = 0f;
            activeDuration = 0f;
            elapsed = 0f;
        }

        private void ApplyCurrentPositions()
        {
            var negativeProgress = negativeTravelDuration > 0f ? Mathf.Clamp01(elapsed / negativeTravelDuration) : 1f;
            var positiveProgress = positiveTravelDuration > 0f ? Mathf.Clamp01(elapsed / positiveTravelDuration) : 1f;
            negativePartRenderer.transform.position = Vector3.Lerp(originWorldPosition, negativeEndWorldPosition, negativeProgress);
            positivePartRenderer.transform.position = Vector3.Lerp(originWorldPosition, positiveEndWorldPosition, positiveProgress);
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

        private Vector3 GetSpriteScale(Sprite sprite)
        {
            var bounds = sprite.bounds.size;
            var safeWidth = bounds.x > 0f ? bounds.x : 1f;
            var safeHeight = bounds.y > 0f ? bounds.y : 1f;
            return new Vector3(activeBoardView.CellSize / safeWidth, activeBoardView.CellSize / safeHeight, 1f);
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
    }
}
