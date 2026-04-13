using UnityEngine;
using UnityEngine.UI;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class UIButtonClickSoundPlayer : MonoBehaviour
    {
        [SerializeField] private Button targetButton;
        [SerializeField] private GameAudioController audioController;
        [SerializeField] private GameHapticsController hapticsController;

        private void OnEnable()
        {
            targetButton ??= GetComponent<Button>();
            if (targetButton == null)
            {
                return;
            }

            targetButton.onClick.RemoveListener(HandleButtonClicked);
            targetButton.onClick.AddListener(HandleButtonClicked);
        }

        private void OnDisable()
        {
            if (targetButton != null)
            {
                targetButton.onClick.RemoveListener(HandleButtonClicked);
            }
        }

        private void ResolveAudioController()
        {
            audioController ??= GetComponentInParent<GameAudioController>() ?? FindFirstObjectByType<GameAudioController>();
        }

        private void ResolveHapticsController()
        {
            hapticsController ??=
                audioController != null
                    ? audioController.GetComponent<GameHapticsController>() ?? audioController.gameObject.AddComponent<GameHapticsController>()
                    : GetComponentInParent<GameHapticsController>() ?? FindFirstObjectByType<GameHapticsController>() ?? gameObject.AddComponent<GameHapticsController>();
        }

        private void HandleButtonClicked()
        {
            ResolveAudioController();
            ResolveHapticsController();
            audioController?.PlaySfx(GameSfxCue.MenuButtonClick);
            hapticsController?.Play(GameHapticCue.Light);
        }
    }
}
