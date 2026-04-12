using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    [CreateAssetMenu(fileName = "GameAudioBank", menuName = "DreamBlastClone/Audio Bank")]
    public sealed class GameAudioBank : ScriptableObject
    {
        [Header("Music")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip gameplayMusic;

        [Header("UI")]
        [SerializeField] private AudioClip menuButtonClick;
        [SerializeField] private AudioClip popupOpen;
        [SerializeField] private AudioClip popupClose;

        [Header("Gameplay")]
        [SerializeField] private AudioClip cubeBlast;
        [SerializeField] private AudioClip rocketActivation;
        [SerializeField] private AudioClip tntActivation;
        [SerializeField] private AudioClip goalCompletion;

        [Header("Obstacles")]
        [SerializeField] private AudioClip vaseHit;
        [SerializeField] private AudioClip vaseDestroy;
        [SerializeField] private AudioClip stoneDestroy;
        [SerializeField] private AudioClip chaliceDoorHit;
        [SerializeField] private AudioClip chaliceDoorBreak;
        [SerializeField] private AudioClip chaliceCollect;

        [Header("State")]
        [SerializeField] private AudioClip winSting;
        [SerializeField] private AudioClip loseSting;

        public AudioClip GetClip(GameMusicCue cue)
        {
            return cue switch
            {
                GameMusicCue.MainMenu => mainMenuMusic,
                GameMusicCue.Gameplay => gameplayMusic,
                _ => null
            };
        }

        public AudioClip GetClip(GameSfxCue cue)
        {
            return cue switch
            {
                GameSfxCue.MenuButtonClick => menuButtonClick,
                GameSfxCue.PopupOpen => popupOpen,
                GameSfxCue.PopupClose => popupClose,
                GameSfxCue.CubeBlast => cubeBlast,
                GameSfxCue.RocketActivation => rocketActivation,
                GameSfxCue.TntActivation => tntActivation,
                GameSfxCue.VaseHit => vaseHit,
                GameSfxCue.VaseDestroy => vaseDestroy,
                GameSfxCue.StoneDestroy => stoneDestroy,
                GameSfxCue.ChaliceDoorHit => chaliceDoorHit,
                GameSfxCue.ChaliceDoorBreak => chaliceDoorBreak,
                GameSfxCue.ChaliceCollect => chaliceCollect,
                GameSfxCue.GoalCompletion => goalCompletion,
                GameSfxCue.WinSting => winSting,
                GameSfxCue.LoseSting => loseSting,
                _ => null
            };
        }
    }
}
