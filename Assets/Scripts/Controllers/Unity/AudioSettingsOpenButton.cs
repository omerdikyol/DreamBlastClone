using DreamBlastClone.Views;
using UnityEngine;
using UnityEngine.UI;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class AudioSettingsOpenButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private AudioSettingsPopupController popupController;

        private void OnEnable()
        {
            ResolveDependencies();

            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        private void HandleClicked()
        {
            ResolveDependencies();
            popupController?.TryOpen();
        }

        private void ResolveDependencies()
        {
            button ??= GetComponent<Button>();
            popupController ??= FindFirstObjectByType<AudioSettingsPopupController>();

            if (button == null)
            {
                return;
            }

            _ = button.GetComponent<UIButtonFeedbackView>() ?? button.gameObject.AddComponent<UIButtonFeedbackView>();
            _ = button.GetComponent<UIButtonClickSoundPlayer>() ?? button.gameObject.AddComponent<UIButtonClickSoundPlayer>();
        }
    }
}
