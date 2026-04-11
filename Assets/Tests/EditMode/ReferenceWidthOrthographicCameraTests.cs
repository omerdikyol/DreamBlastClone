using DreamBlastClone.Controllers.Unity;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class ReferenceWidthOrthographicCameraTests
    {
        [Test]
        public void ResolveOrthographicSizeKeepsReferenceSizeAtReferenceAspect()
        {
            var size = ReferenceWidthOrthographicCamera.ResolveOrthographicSize(5f, new Vector2(1080f, 1920f), 1080f / 1920f);

            Assert.That(size, Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void ResolveOrthographicSizeExpandsForNarrowerAspectToPreserveWidth()
        {
            var size = ReferenceWidthOrthographicCamera.ResolveOrthographicSize(5f, new Vector2(1080f, 1920f), 9f / 19.5f);

            Assert.That(size, Is.GreaterThan(5f));
            Assert.That(size, Is.EqualTo(6.09375f).Within(0.0001f));
        }

        [Test]
        public void ResolveOrthographicSizeDoesNotShrinkOnWiderAspect()
        {
            var size = ReferenceWidthOrthographicCamera.ResolveOrthographicSize(5f, new Vector2(1080f, 1920f), 3f / 4f);

            Assert.That(size, Is.EqualTo(5f).Within(0.0001f));
        }
    }
}
