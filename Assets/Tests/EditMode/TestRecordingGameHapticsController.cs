using System.Collections.Generic;
using DreamBlastClone.Controllers.Unity;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class TestRecordingGameHapticsController : GameHapticsController
    {
        public List<GameHapticCue> PlayedCues { get; } = new List<GameHapticCue>();

        public override bool Play(GameHapticCue cue)
        {
            PlayedCues.Add(cue);
            return true;
        }
    }
}
