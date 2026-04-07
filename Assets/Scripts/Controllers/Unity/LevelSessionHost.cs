using System;
using DreamBlastClone.Controllers;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class LevelSessionHost : MonoBehaviour
    {
        public LevelSession Session { get; private set; }

        public void SetSession(LevelSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }
    }
}
