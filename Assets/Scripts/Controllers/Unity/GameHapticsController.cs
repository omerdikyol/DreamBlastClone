using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public class GameHapticsController : MonoBehaviour
    {
        [SerializeField] private float lightCooldownSeconds = 0.06f;
        [SerializeField] private float mediumCooldownSeconds = 0.08f;
        [SerializeField] private float heavyCooldownSeconds = 0.12f;
        [SerializeField] private float strongerCueOverrideMinGapSeconds = 0.025f;

        private IHapticsBackend backend;
        private bool hasInitializedBackend;
        private bool hasPlayedCue;
        private GameHapticCue lastCue;
        private float lastCueTime;
        private float nextAllowedCueTime;

        protected virtual void Awake()
        {
            EnsureBackendInitialized();
        }

        public virtual bool Play(GameHapticCue cue)
        {
            if (!IsPlatformHapticsSupported())
            {
                return false;
            }

            var now = Time.unscaledTime;
            if (ShouldSuppress(cue, now))
            {
                return false;
            }

            if (!TryPlayPlatformHaptic(cue))
            {
                return false;
            }

            hasPlayedCue = true;
            lastCue = cue;
            lastCueTime = now;
            nextAllowedCueTime = now + GetCooldown(cue);
            return true;
        }

        protected virtual bool IsPlatformHapticsSupported()
        {
            EnsureBackendInitialized();
            return backend != null && backend.IsSupported;
        }

        protected virtual bool TryPlayPlatformHaptic(GameHapticCue cue)
        {
            EnsureBackendInitialized();
            return backend != null && backend.Play(cue);
        }

        private bool ShouldSuppress(GameHapticCue cue, float now)
        {
            if (!hasPlayedCue || now >= nextAllowedCueTime)
            {
                return false;
            }

            if (cue > lastCue)
            {
                var elapsed = now - lastCueTime;
                return elapsed > 0f && elapsed < Mathf.Max(0f, strongerCueOverrideMinGapSeconds);
            }

            return true;
        }

        private float GetCooldown(GameHapticCue cue)
        {
            return cue switch
            {
                GameHapticCue.Medium => Mathf.Max(0f, mediumCooldownSeconds),
                GameHapticCue.Heavy => Mathf.Max(0f, heavyCooldownSeconds),
                _ => Mathf.Max(0f, lightCooldownSeconds)
            };
        }

        private void EnsureBackendInitialized()
        {
            if (hasInitializedBackend)
            {
                return;
            }

            backend = CreateBackend();
            hasInitializedBackend = true;
        }

        private static IHapticsBackend CreateBackend()
        {
#if UNITY_EDITOR
            return new NoOpHapticsBackend();
#elif UNITY_IOS
            return new IosHapticsBackend();
#elif UNITY_ANDROID
            return new AndroidHapticsBackend();
#else
            return new NoOpHapticsBackend();
#endif
        }

        private interface IHapticsBackend
        {
            bool IsSupported { get; }

            bool Play(GameHapticCue cue);
        }

        private sealed class NoOpHapticsBackend : IHapticsBackend
        {
            public bool IsSupported => false;

            public bool Play(GameHapticCue cue)
            {
                return false;
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private sealed class AndroidHapticsBackend : IHapticsBackend
        {
            private readonly AndroidJavaObject vibrator;
            private readonly int sdkInt;

            public AndroidHapticsBackend()
            {
                try
                {
                    using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var version = new AndroidJavaClass("android.os.Build$VERSION");
                    var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    vibrator = activity?.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    sdkInt = version.GetStatic<int>("SDK_INT");
                }
                catch (Exception)
                {
                    vibrator = null;
                    sdkInt = 0;
                }
            }

            public bool IsSupported
            {
                get
                {
                    try
                    {
                        return vibrator != null && vibrator.Call<bool>("hasVibrator");
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            public bool Play(GameHapticCue cue)
            {
                if (!IsSupported)
                {
                    return false;
                }

                try
                {
                    var durationMs = cue switch
                    {
                        GameHapticCue.Medium => 28L,
                        GameHapticCue.Heavy => 40L,
                        _ => 18L
                    };
                    var amplitude = cue switch
                    {
                        GameHapticCue.Medium => 150,
                        GameHapticCue.Heavy => 255,
                        _ => 70
                    };

                    if (sdkInt >= 26)
                    {
                        using var vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
                        using var effect = vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", durationMs, amplitude);
                        vibrator.Call("vibrate", effect);
                        return true;
                    }

                    vibrator.Call("vibrate", durationMs);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        private sealed class IosHapticsBackend : IHapticsBackend
        {
            public bool IsSupported => true;

            public bool Play(GameHapticCue cue)
            {
                try
                {
                    DreamBlastClone_TriggerImpactHaptic((int)cue);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        [DllImport("__Internal")]
        private static extern void DreamBlastClone_TriggerImpactHaptic(int style);
#endif
    }
}
