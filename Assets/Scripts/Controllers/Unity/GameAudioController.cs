using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public class GameAudioController : MonoBehaviour
    {
        private const string DefaultBankResourcePath = "GameAudioBank";

        [SerializeField] private GameAudioBank audioBank;
        [SerializeField] private int sfxVoicePoolSize = 6;
        [SerializeField] private float musicVolume = 0.42f;
        [SerializeField] private float sfxVolume = 1f;

        private readonly List<SfxVoiceState> sfxVoices = new List<SfxVoiceState>();
        private readonly Dictionary<GameSfxCue, float> nextAllowedPlayTimes = new Dictionary<GameSfxCue, float>();
        private readonly List<PendingSfxRequest> pendingSfxRequests = new List<PendingSfxRequest>();
        private AudioSource musicSource;
        private bool isInitialized;
        private float musicVolumeMultiplier = 1f;

        public GameMusicCue? CurrentMusicCue { get; private set; }

        protected virtual void Awake()
        {
            EnsureInitialized();
        }

        protected virtual void Update()
        {
            ProcessPendingSfxRequests();
        }

        public virtual void PlayMusic(GameMusicCue cue, bool restartIfSame = false)
        {
            EnsureInitialized();
            var clip = ResolveBank()?.GetClip(cue);

            if (clip == null || musicSource == null)
            {
                return;
            }

            if (!restartIfSame && CurrentMusicCue == cue && musicSource.clip == clip && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.loop = true;
            ApplyMusicVolume();
            musicSource.playOnAwake = false;
            musicSource.Stop();
            musicSource.Play();
            CurrentMusicCue = cue;
        }

        public virtual void StopMusic()
        {
            if (musicSource == null)
            {
                return;
            }

            musicSource.Stop();
            CurrentMusicCue = null;
        }

        public virtual void SetMusicVolumeMultiplier(float multiplier)
        {
            musicVolumeMultiplier = Mathf.Clamp01(multiplier);
            ApplyMusicVolume();
        }

        public virtual bool PlaySfx(GameSfxCue cue)
        {
            return PlaySfx(cue, 1f, bypassCooldown: false, bypassVoiceLimit: false);
        }

        public virtual bool PlaySfx(GameSfxCue cue, float volumeMultiplier, bool bypassCooldown, bool bypassVoiceLimit)
        {
            EnsureInitialized();
            RefreshVoiceStates(GetNow());

            var clip = ResolveBank()?.GetClip(cue);
            if (clip == null)
            {
                return false;
            }

            var policy = GetPolicy(cue);
            var now = GetNow();
            if (!bypassCooldown
                && nextAllowedPlayTimes.TryGetValue(cue, out var nextAllowedTime)
                && now < nextAllowedTime)
            {
                return false;
            }

            if (!bypassVoiceLimit && CountBusyVoices(cue) >= policy.MaxVoices)
            {
                return false;
            }

            var voice = GetAvailableSfxSource();
            if (voice == null || voice.Source == null)
            {
                return false;
            }

            voice.Source.clip = clip;
            voice.Source.loop = false;
            voice.Source.volume = Mathf.Clamp01(sfxVolume * GetVolumeMultiplier(cue) * Mathf.Max(0f, volumeMultiplier));
            voice.Source.playOnAwake = false;
            voice.Source.Play();

            var busyDuration = Mathf.Max(clip.length, 0.05f);
            voice.Cue = cue;
            voice.BusyUntil = now + busyDuration;
            if (!bypassCooldown)
            {
                nextAllowedPlayTimes[cue] = now + policy.CooldownSeconds;
            }

            return true;
        }

        public virtual void PlaySfxDelayed(GameSfxCue cue, float delaySeconds, float volumeMultiplier, bool bypassCooldown, bool bypassVoiceLimit)
        {
            EnsureInitialized();
            pendingSfxRequests.Add(new PendingSfxRequest(
                cue,
                GetNow() + Mathf.Max(0f, delaySeconds),
                Mathf.Max(0f, volumeMultiplier),
                bypassCooldown,
                bypassVoiceLimit));
        }

        public int CountActiveVoices(GameSfxCue cue)
        {
            RefreshVoiceStates(GetNow());
            return CountBusyVoices(cue);
        }

        private void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            musicSource = GetOrCreateSource("MusicAudioSource");
            if (musicSource != null)
            {
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                ApplyMusicVolume();
            }

            var targetVoiceCount = Mathf.Max(1, sfxVoicePoolSize);
            while (sfxVoices.Count < targetVoiceCount)
            {
                sfxVoices.Add(new SfxVoiceState(GetOrCreateSource($"SfxAudioSource_{sfxVoices.Count + 1}")));
            }

            isInitialized = true;
        }

        private GameAudioBank ResolveBank()
        {
            if (audioBank == null)
            {
                audioBank = Resources.Load<GameAudioBank>(DefaultBankResourcePath);
            }

            return audioBank;
        }

        private AudioSource GetOrCreateSource(string sourceName)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            var audioSource = sourceObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            return audioSource;
        }

        private SfxVoiceState GetAvailableSfxSource()
        {
            var now = GetNow();
            RefreshVoiceStates(now);

            for (var index = 0; index < sfxVoices.Count; index++)
            {
                if (!sfxVoices[index].IsBusy)
                {
                    return sfxVoices[index];
                }
            }

            return null;
        }

        private void RefreshVoiceStates(float now)
        {
            for (var index = 0; index < sfxVoices.Count; index++)
            {
                var voice = sfxVoices[index];
                if (!voice.IsBusy)
                {
                    continue;
                }

                if (now >= voice.BusyUntil)
                {
                    voice.Clear();
                }
            }
        }

        private int CountBusyVoices(GameSfxCue cue)
        {
            var count = 0;

            for (var index = 0; index < sfxVoices.Count; index++)
            {
                var voice = sfxVoices[index];
                if (voice.IsBusy && voice.Cue == cue)
                {
                    count++;
                }
            }

            return count;
        }

        private static SfxCuePolicy GetPolicy(GameSfxCue cue)
        {
            return cue switch
            {
                GameSfxCue.CubeBlast => new SfxCuePolicy(0.06f, 2),
                GameSfxCue.ChaliceCollect => new SfxCuePolicy(0.08f, 2),
                GameSfxCue.VaseHit => new SfxCuePolicy(0.08f, 1),
                GameSfxCue.RocketActivation => new SfxCuePolicy(0.10f, 1),
                GameSfxCue.TntActivation => new SfxCuePolicy(0.12f, 1),
                _ => new SfxCuePolicy(0f, 1)
            };
        }

        private static float GetVolumeMultiplier(GameSfxCue cue)
        {
            return cue switch
            {
                GameSfxCue.RocketActivation => 1.55f,
                _ => 1f
            };
        }

        private static float GetNow()
        {
            return Time.unscaledTime;
        }

        private void ProcessPendingSfxRequests()
        {
            if (pendingSfxRequests.Count == 0)
            {
                return;
            }

            var now = GetNow();
            for (var index = pendingSfxRequests.Count - 1; index >= 0; index--)
            {
                var request = pendingSfxRequests[index];
                if (now < request.PlayAtTime)
                {
                    continue;
                }

                PlaySfx(
                    request.Cue,
                    request.VolumeMultiplier,
                    request.BypassCooldown,
                    request.BypassVoiceLimit);
                pendingSfxRequests.RemoveAt(index);
            }
        }

        private void ApplyMusicVolume()
        {
            if (musicSource == null)
            {
                return;
            }

            musicSource.volume = Mathf.Clamp01(musicVolume) * musicVolumeMultiplier;
        }

        private sealed class SfxVoiceState
        {
            public SfxVoiceState(AudioSource source)
            {
                Source = source;
                Clear();
            }

            public AudioSource Source { get; }

            public GameSfxCue Cue { get; set; }

            public float BusyUntil { get; set; }

            public bool IsBusy => BusyUntil > 0f;

            public void Clear()
            {
                Cue = default;
                BusyUntil = 0f;
            }
        }

        private readonly struct SfxCuePolicy
        {
            public SfxCuePolicy(float cooldownSeconds, int maxVoices)
            {
                CooldownSeconds = Mathf.Max(0f, cooldownSeconds);
                MaxVoices = Math.Max(1, maxVoices);
            }

            public float CooldownSeconds { get; }

            public int MaxVoices { get; }
        }

        private readonly struct PendingSfxRequest
        {
            public PendingSfxRequest(
                GameSfxCue cue,
                float playAtTime,
                float volumeMultiplier,
                bool bypassCooldown,
                bool bypassVoiceLimit)
            {
                Cue = cue;
                PlayAtTime = playAtTime;
                VolumeMultiplier = volumeMultiplier;
                BypassCooldown = bypassCooldown;
                BypassVoiceLimit = bypassVoiceLimit;
            }

            public GameSfxCue Cue { get; }

            public float PlayAtTime { get; }

            public float VolumeMultiplier { get; }

            public bool BypassCooldown { get; }

            public bool BypassVoiceLimit { get; }
        }
    }
}
