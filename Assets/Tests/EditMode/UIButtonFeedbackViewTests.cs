using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class UIButtonFeedbackViewTests
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
        public void PlayIdleStartsAndStopRestoresScale()
        {
            host = new GameObject("Button", typeof(RectTransform));
            var feedback = host.AddComponent<UIButtonFeedbackView>();
            host.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            feedback.PlayIdle();
            feedback.StopAndReset();

            Assert.That(feedback.IsPlaying, Is.False);
            Assert.That(host.transform.localScale, Is.EqualTo(new Vector3(1.1f, 1.1f, 1f)));
        }

        [Test]
        public void PointerDownAndUpUpdatePressedStateWithoutLeaking()
        {
            host = new GameObject("Button", typeof(RectTransform));
            var feedback = host.AddComponent<UIButtonFeedbackView>();

            feedback.OnPointerDown(null);
            Assert.That(feedback.IsPressed, Is.True);

            feedback.OnPointerUp(null);
            Assert.That(feedback.IsPressed, Is.False);

            feedback.StopAndReset();
            Assert.That(host.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void PointerExitClearsPressedState()
        {
            host = new GameObject("Button", typeof(RectTransform));
            var feedback = host.AddComponent<UIButtonFeedbackView>();

            feedback.OnPointerDown(null);
            feedback.OnPointerExit(null);

            Assert.That(feedback.IsPressed, Is.False);
        }
    }
}
