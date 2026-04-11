using DG.Tweening;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public static class PresentationTweenBootstrap
    {
        private static bool isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            DOTween.Init(recycleAllByDefault: false, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(500, 100);
            isInitialized = true;
        }
    }
}
