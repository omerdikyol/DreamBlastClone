using System.Collections.Generic;
using DreamBlastClone.Controllers.Unity;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class TestRecordingGameAudioController : GameAudioController
    {
        public List<GameMusicCue> PlayedMusic { get; } = new List<GameMusicCue>();

        public List<GameSfxCue> PlayedSfx { get; } = new List<GameSfxCue>();

        public override void PlayMusic(GameMusicCue cue, bool restartIfSame = false)
        {
            PlayedMusic.Add(cue);
        }

        public override bool PlaySfx(GameSfxCue cue)
        {
            PlayedSfx.Add(cue);
            return true;
        }

        public override bool PlaySfx(GameSfxCue cue, float volumeMultiplier, bool bypassCooldown, bool bypassVoiceLimit)
        {
            PlayedSfx.Add(cue);
            return true;
        }

        public override void PlaySfxDelayed(GameSfxCue cue, float delaySeconds, float volumeMultiplier, bool bypassCooldown, bool bypassVoiceLimit)
        {
            PlayedSfx.Add(cue);
        }
    }
}
