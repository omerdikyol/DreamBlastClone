using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class BoardItemIdleLoopViewTests
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
        public void PlayStartsSingleIdleLoop()
        {
            var idleLoop = CreateIdleLoop();

            idleLoop.Play();
            idleLoop.Play();

            Assert.That(idleLoop.IsPlaying, Is.True);
        }

        [Test]
        public void PlayAtGlobalPhaseDoesNotWaitAtBasePose()
        {
            var idleLoop = CreateIdleLoop();

            idleLoop.PlayAtGlobalPhase(0.5f);

            Assert.That(idleLoop.IsPlaying, Is.True);
            Assert.That(host.transform.localScale, Is.Not.EqualTo(Vector3.one));
        }

        [Test]
        public void StopAndResetRestoresCapturedTransform()
        {
            var idleLoop = CreateIdleLoop();
            host.transform.localPosition = new Vector3(1f, 2f, 3f);
            host.transform.localScale = new Vector3(0.5f, 0.75f, 1f);
            host.transform.localEulerAngles = new Vector3(0f, 0f, 12f);

            idleLoop.Play();
            idleLoop.StopAndReset();

            Assert.That(idleLoop.IsPlaying, Is.False);
            Assert.That(host.transform.localPosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(host.transform.localScale, Is.EqualTo(new Vector3(0.5f, 0.75f, 1f)));
            Assert.That(host.transform.localEulerAngles.z, Is.EqualTo(12f).Within(0.001f));
        }

        private BoardItemIdleLoopView CreateIdleLoop()
        {
            host = new GameObject("IdleItem");
            return host.AddComponent<BoardItemIdleLoopView>();
        }
    }
}
