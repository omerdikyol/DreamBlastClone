using System;
using DG.Tweening;
using DreamBlastClone.Core;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class CubeItemView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float eligibleTransitionDuration = 0.16f;
        [SerializeField] private float eligibleTransitionStartScaleMultiplier = 0.9f;
        [SerializeField] private float eligibleTransitionOvershootScaleMultiplier = 1.08f;
        [SerializeField] private Color rocketEligibleTransitionColor = new Color(1f, 0.96f, 0.78f, 1f);
        [SerializeField] private Color tntEligibleTransitionColor = new Color(1f, 0.82f, 0.62f, 1f);

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

        private Sequence appearanceTween;

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

        public void PlayVisualStateTransition(
            CubeVisualState state,
            BoardItemIdleLoopView idleLoop,
            float idlePhaseOffset,
            bool startIdleLoop)
        {
            StopAppearanceTween();

            if (!startIdleLoop)
            {
                return;
            }

            if (idleLoop != null)
            {
                idleLoop.StopAndReset();
            }

            if (!Application.isPlaying
                || state == CubeVisualState.Default
                || eligibleTransitionDuration <= 0f)
            {
                idleLoop?.PlayAtGlobalPhase(idlePhaseOffset);
                return;
            }

            var targetRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            if (targetRenderer == null)
            {
                throw new InvalidOperationException("CubeItemView requires a SpriteRenderer reference.");
            }

            PresentationTweenBootstrap.EnsureInitialized();

            var baseScale = transform.localScale;
            var startScale = baseScale * Mathf.Max(0.01f, eligibleTransitionStartScaleMultiplier);
            var overshootScale = baseScale * Mathf.Max(1f, eligibleTransitionOvershootScaleMultiplier);
            transform.localScale = startScale;
            targetRenderer.color = ResolveTransitionColor(state);

            appearanceTween = DOTween.Sequence()
                .Append(transform.DOScale(overshootScale, eligibleTransitionDuration * 0.42f).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(baseScale, eligibleTransitionDuration * 0.58f).SetEase(Ease.OutBack))
                .Join(DOTween.To(
                        () => targetRenderer.color,
                        value => targetRenderer.color = value,
                        Color.white,
                        eligibleTransitionDuration)
                    .SetEase(Ease.OutQuad))
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: false)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() =>
                {
                    appearanceTween = null;
                    idleLoop?.PlayAtGlobalPhase(idlePhaseOffset);
                });
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

        private Color ResolveTransitionColor(CubeVisualState state)
        {
            return state switch
            {
                CubeVisualState.RocketEligible => rocketEligibleTransitionColor,
                CubeVisualState.TntEligible => tntEligibleTransitionColor,
                _ => Color.white
            };
        }

        private void OnDisable()
        {
            StopAppearanceTween();
        }

        private void OnDestroy()
        {
            StopAppearanceTween();
        }

        private void StopAppearanceTween()
        {
            if (appearanceTween == null)
            {
                return;
            }

            appearanceTween.Kill();
            appearanceTween = null;
        }
    }
}
