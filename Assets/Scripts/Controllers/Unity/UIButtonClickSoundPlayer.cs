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

        private void HandleButtonClicked()
        {
            ResolveAudioController();
            audioController?.PlaySfx(GameSfxCue.MenuButtonClick);
        }
    }
}
