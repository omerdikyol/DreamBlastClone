using System.Collections.Generic;
using DreamBlastClone.Controllers;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class MoveHintController : MonoBehaviour
    {
        [SerializeField] private LevelSessionHost sessionHost;
        [SerializeField] private BoardInputSessionBridge inputBridge;
        [SerializeField] private BoardView boardView;
        [SerializeField] private int hintMoveCount = 3;
        [SerializeField] private float delayBetweenHintsSeconds = 2.5f;
        [SerializeField] private float hintVisibleSeconds = 3f;

        private readonly MoveHintResolver moveHintResolver = new MoveHintResolver();
        private readonly List<MoveHintTargetEffectView> activeTargetEffects = new List<MoveHintTargetEffectView>();
        private readonly List<MoveHintSuggestion> queuedSuggestions = new List<MoveHintSuggestion>();
        private float phaseElapsedSeconds;
        private int currentSuggestionIndex;
        private HintPhase phase = HintPhase.Waiting;
        private bool isSubscribedToInputBridge;

        public void Configure(LevelSessionHost targetSessionHost, BoardInputSessionBridge targetInputBridge)
        {
            if (targetSessionHost != null)
            {
                sessionHost = targetSessionHost;
            }

            if (targetInputBridge != null && inputBridge != targetInputBridge)
            {
                UnsubscribeFromInputBridge();
                inputBridge = targetInputBridge;
            }

            if (boardView == null && inputBridge != null)
            {
                boardView = inputBridge.BoardView;
            }

            SubscribeToInputBridge();
        }

        private void OnEnable()
        {
            SubscribeToInputBridge();
        }

        private void OnDisable()
        {
            UnsubscribeFromInputBridge();
            ResetIdleState();
        }

        private void Update()
        {
            if (!TryResolveDependencies(out var session))
            {
                ResetIdleState();
                return;
            }

            SubscribeToInputBridge();

            if (!CanShowHint(session))
            {
                ResetIdleState();
                return;
            }

            phaseElapsedSeconds += Time.unscaledDeltaTime;

            switch (phase)
            {
                case HintPhase.Waiting:
                    if (phaseElapsedSeconds < delayBetweenHintsSeconds)
                    {
                        return;
                    }

                    EnsureSuggestions(session);
                    if (queuedSuggestions.Count == 0)
                    {
                        phaseElapsedSeconds = 0f;
                        return;
                    }

                    ShowTargetEffects(queuedSuggestions[currentSuggestionIndex]);
                    phase = HintPhase.Showing;
                    phaseElapsedSeconds = 0f;
                    return;
                case HintPhase.Showing:
                    if (phaseElapsedSeconds < hintVisibleSeconds)
                    {
                        return;
                    }

                    ClearTargetEffects();
                    if (queuedSuggestions.Count > 0)
                    {
                        currentSuggestionIndex = (currentSuggestionIndex + 1) % queuedSuggestions.Count;
                    }

                    phase = HintPhase.Waiting;
                    phaseElapsedSeconds = 0f;
                    return;
            }
        }

        private void HandleTapProcessed(LevelSessionTapResult result)
        {
            ResetIdleState();
        }

        private void HandleScreenTapDetected()
        {
            ResetIdleState();
        }

        private bool TryResolveDependencies(out LevelSession session)
        {
            session = null;

            sessionHost ??= GetComponent<LevelSessionHost>();
            inputBridge ??= GetComponent<BoardInputSessionBridge>();
            boardView ??= inputBridge != null ? inputBridge.BoardView : FindFirstObjectByType<BoardView>();

            session = sessionHost?.Session;
            return session is not null && inputBridge is not null && boardView is not null;
        }

        private bool CanShowHint(LevelSession session)
        {
            return session.CurrentLevelState == LevelState.Continue
                && inputBridge is not null
                && !inputBridge.IsInputSuppressed
                && !inputBridge.IsBoardBusy
                && boardView is not null;
        }

        private void ResetIdleState()
        {
            phaseElapsedSeconds = 0f;
            currentSuggestionIndex = 0;
            phase = HintPhase.Waiting;
            queuedSuggestions.Clear();
            ClearTargetEffects();
        }

        private void SubscribeToInputBridge()
        {
            if (isSubscribedToInputBridge || inputBridge == null)
            {
                return;
            }

            inputBridge.TapProcessed += HandleTapProcessed;
            inputBridge.ScreenTapDetected += HandleScreenTapDetected;
            isSubscribedToInputBridge = true;
        }

        private void UnsubscribeFromInputBridge()
        {
            if (!isSubscribedToInputBridge || inputBridge == null)
            {
                return;
            }

            inputBridge.TapProcessed -= HandleTapProcessed;
            inputBridge.ScreenTapDetected -= HandleScreenTapDetected;
            isSubscribedToInputBridge = false;
        }

        private void ShowTargetEffects(MoveHintSuggestion suggestion)
        {
            ClearTargetEffects();

            for (var index = 0; index < suggestion.HighlightCoordinates.Count; index++)
            {
                if (!boardView.TryGetItemVisual(suggestion.HighlightCoordinates[index], out var itemVisual)
                    || itemVisual == null)
                {
                    continue;
                }

                var effect = itemVisual.GetComponent<MoveHintTargetEffectView>();
                if (effect == null)
                {
                    effect = itemVisual.AddComponent<MoveHintTargetEffectView>();
                }

                effect.Play();
                activeTargetEffects.Add(effect);
            }
        }

        private void ClearTargetEffects()
        {
            for (var index = activeTargetEffects.Count - 1; index >= 0; index--)
            {
                var effect = activeTargetEffects[index];
                if (effect != null)
                {
                    effect.Stop();
                }
            }

            activeTargetEffects.Clear();
        }

        private void EnsureSuggestions(LevelSession session)
        {
            if (queuedSuggestions.Count > 0)
            {
                return;
            }

            var suggestions = moveHintResolver.ResolveTopMoves(
                session.Board,
                session.Goals,
                Mathf.Max(1, hintMoveCount));

            queuedSuggestions.Clear();
            for (var index = 0; index < suggestions.Count; index++)
            {
                if (suggestions[index].IsValid)
                {
                    queuedSuggestions.Add(suggestions[index]);
                }
            }

            currentSuggestionIndex = 0;
        }

        private enum HintPhase
        {
            Waiting,
            Showing
        }
    }
}
