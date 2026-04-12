using UnityEngine;
using UnityEngine.UI;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class UIButtonClickSoundPlayer : MonoBehaviour
    {
        [SerializeField] private Button targetButton;
        [SerializeField] private GameAudioController audioController;

        private void OnEnable()
        {
            if (!TryResolveDependencies())
            {
                return;
            }

            targetButton.onClick.AddListener(HandleButtonClicked);
        }

        private void OnDisable()
        {
            if (targetButton != null)
            {
                targetButton.onClick.RemoveListener(HandleButtonClicked);
            }
        }

        private bool TryResolveDependencies()
        {
            targetButton ??= GetComponent<Button>();
            audioController ??= GetComponentInParent<GameAudioController>() ?? FindFirstObjectByType<GameAudioController>();
            return targetButton != null && audioController != null;
        }

        private void HandleButtonClicked()
        {
            audioController?.PlaySfx(GameSfxCue.MenuButtonClick);
        }
    }
}
