using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class MainSceneButtonIdleLoopViewTests
    {
        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host is not null)
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void PlayStartsPulseAndStopRestoresScale()
        {
            host = new GameObject("MainButton", typeof(RectTransform));
            var idleLoop = host.AddComponent<MainSceneButtonIdleLoopView>();
            host.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            idleLoop.Play();
            idleLoop.StopAndReset();

            Assert.That(idleLoop.IsPlaying, Is.False);
            Assert.That(host.transform.localScale, Is.EqualTo(new Vector3(1.2f, 1.2f, 1f)));
        }
    }
}
