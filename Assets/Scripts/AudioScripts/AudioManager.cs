// Assets/Scripts/AudioScripts/AudioManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SolMechs.Audio
{
    /// <summary>
    /// Global audio controller (singleton, persistent).
    /// - Single AudioMixer with exposed params: "MusicVolume" and "SFXVolume"
    /// - BGM via dedicated AudioSource
    /// - SFX via small pooled AudioSources (+ fallback OneShot)
    /// - Universal toggle (AudioEnabled) controls: Music (Pause/Play) + SFX bus (mute/unmute)
    /// - WebGL-safe unlock helpers: PrimeOnUserInteraction / ForceResumeIfPossible
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixer (single asset with groups Master/Music/SFX)")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";

        [Header("Optional mixer routing")]
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        [Header("BGM")]
        [SerializeField] private AudioSource musicSource;          // dedicated music source
        [SerializeField] private List<AudioClip> bgmClips = new(); // loopable tracks
        [SerializeField] private int defaultBgmIndex = 0;

        [Header("SFX")]
        [SerializeField] private SFXLibrary sfxLibrary;            // ScriptableObject mapping SFXType->Clip
        [SerializeField, Min(1)] private int sfxPoolSize = 8;
        [SerializeField] private AudioSource sfxPrefab;            // plain AudioSource (no clip)
        private readonly Queue<AudioSource> _sfxPool = new();
        private AudioSource _fallbackOneShot;

        // ================== Persistence keys ==================
        private const string KEY_AUDIO_ENABLED = "sm_audio_enabled"; // universal on/off
        private const string KEY_MUSIC_VOL_DB = "sm_music_vol_db";
        private const string KEY_SFX_VOL_DB = "sm_sfx_vol_db";

        // ================== Defaults (dB) =====================
        private const float DEFAULT_MUSIC_DB = -12f;
        private const float DEFAULT_SFX_DB = -10f;

        // ================== State =============================
        /// <summary>Universal audio state (music + sfx).</summary>
        public bool AudioEnabled { get; private set; } = true;

        /// <summary>Kept for API compatibility with UI that calls ToggleMusic().</summary>
        public bool MusicEnabled => AudioEnabled;

        // Cached SFX bus level to restore after mute
        private float _cachedSfxDbBeforeMute = DEFAULT_SFX_DB;

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Route sources
            if (musicSource && musicGroup) musicSource.outputAudioMixerGroup = musicGroup;

            // Build SFX pool
            if (sfxPrefab != null)
            {
                for (int i = 0; i < sfxPoolSize; i++)
                {
                    var src = Instantiate(sfxPrefab, transform);
                    src.playOnAwake = false;
                    if (sfxGroup) src.outputAudioMixerGroup = sfxGroup;
                    _sfxPool.Enqueue(src);
                }
            }

            // Fallback one-shot (always available)
            _fallbackOneShot = gameObject.AddComponent<AudioSource>();
            _fallbackOneShot.playOnAwake = false;
            if (sfxGroup) _fallbackOneShot.outputAudioMixerGroup = sfxGroup;

            // Load persisted volumes or defaults
            float musicDb = PlayerPrefs.HasKey(KEY_MUSIC_VOL_DB) ? PlayerPrefs.GetFloat(KEY_MUSIC_VOL_DB) : DEFAULT_MUSIC_DB;
            float sfxDb = PlayerPrefs.HasKey(KEY_SFX_VOL_DB) ? PlayerPrefs.GetFloat(KEY_SFX_VOL_DB) : DEFAULT_SFX_DB;

            SetMusicVolumeDb(musicDb, save: false);
            SetSFXVolumeDb(sfxDb, save: false);
            _cachedSfxDbBeforeMute = sfxDb;

            // Universal enabled state
            AudioEnabled = PlayerPrefs.GetInt(KEY_AUDIO_ENABLED, 1) == 1;

            // Apply initial state
            ApplyAudioEnableState();

            // Arm music if available
            if (bgmClips.Count > 0)
            {
                int idx = Mathf.Clamp(defaultBgmIndex, 0, bgmClips.Count - 1);
                PlayMusic(bgmClips[idx], loop: true);
                ForceResumeIfPossible(); // ensures playing when enabled
            }
        }

        // ================= BGM =================

        /// <summary>Starts playing a music clip (if AudioEnabled).</summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (!clip || !musicSource) return;
            musicSource.clip = clip;
            musicSource.loop = loop;
            if (AudioEnabled) musicSource.Play();
        }

        /// <summary>
        /// TEMP: Keeps legacy UI working. Delegates to universal toggle.
        /// Replace with a Music-only behavior in the future if desired.
        /// </summary>
        public void ToggleMusic() => ToggleAudio();

        // ================= Master (universal) =================

        /// <summary>Universal toggle (music pause/play + SFX bus mute/unmute).</summary>
        public void ToggleAudio() => SetAudioEnabled(!AudioEnabled);

        /// <summary>Sets universal audio state (music + sfx).</summary>
        public void SetAudioEnabled(bool enabled)
        {
            if (AudioEnabled == enabled) return;
            AudioEnabled = enabled;
            PlayerPrefs.SetInt(KEY_AUDIO_ENABLED, AudioEnabled ? 1 : 0);

            // MUSIC: use Pause/Play (do not mute bus to avoid sticky states)
            if (musicSource)
            {
                if (AudioEnabled)
                {
                    if (musicSource.clip && !musicSource.isPlaying)
                    {
                        AudioListener.pause = false; // unlock for WebGL/browsers
                        musicSource.Play();
                    }
                }
                else
                {
                    if (musicSource.isPlaying) musicSource.Pause();
                }
            }

            // SFX: hard-mute/unmute bus
            if (mixer && !string.IsNullOrEmpty(sfxVolumeParam))
            {
                // Update cache only if current value is NOT already muted
                if (mixer.GetFloat(sfxVolumeParam, out var currSfx) && currSfx > -79.9f)
                    _cachedSfxDbBeforeMute = currSfx;

                if (AudioEnabled) mixer.SetFloat(sfxVolumeParam, _cachedSfxDbBeforeMute);
                else mixer.SetFloat(sfxVolumeParam, -80f);
            }

            PlayerPrefs.Save();
        }

        /// <summary>Apply current universal state to music source.</summary>
        private void ApplyAudioEnableState()
        {
            if (!musicSource) return;

            if (AudioEnabled)
            {
                if (!musicSource.isPlaying && musicSource.clip) musicSource.Play();
            }
            else
            {
                if (musicSource.isPlaying) musicSource.Pause();
            }
        }

        // ================= Volume controls =================

        /// <summary>Sets Music bus dB and persists.</summary>
        public void SetMusicVolumeDb(float db, bool save = true)
        {
            if (mixer && !string.IsNullOrEmpty(musicVolumeParam))
                mixer.SetFloat(musicVolumeParam, db);

            if (save)
            {
                PlayerPrefs.SetFloat(KEY_MUSIC_VOL_DB, db);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Sets SFX bus dB and persists.</summary>
        public void SetSFXVolumeDb(float db, bool save = true)
        {
            if (mixer && !string.IsNullOrEmpty(sfxVolumeParam))
                mixer.SetFloat(sfxVolumeParam, db);

            _cachedSfxDbBeforeMute = db;

            if (save)
            {
                PlayerPrefs.SetFloat(KEY_SFX_VOL_DB, db);
                PlayerPrefs.Save();
            }
        }

        // ================= WebGL / Autoplay helpers =================

        /// <summary>
        /// Ensure music actually resumes if it should be playing (after user interaction).
        /// </summary>
        public void ForceResumeIfPossible()
        {
            if (!AudioEnabled || !musicSource || !musicSource.clip) return;
            if (!musicSource.isPlaying)
            {
                AudioListener.pause = false;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Call this from any UI click to unlock audio contexts on WebGL/browsers.
        /// </summary>
        public void PrimeOnUserInteraction()
        {
            AudioListener.pause = false;
            ForceResumeIfPossible();
        }

        // ================= SFX =================

        /// <summary>Plays a SFX by key (respects universal mute).</summary>
        public void PlaySFX(SFXType type, float pitch = 1f)
        {
            if (!AudioEnabled) return;

            var clip = sfxLibrary ? sfxLibrary.GetClip(type) : null;
            if (!clip) return;

            if (_sfxPool.Count > 0)
            {
                var src = _sfxPool.Dequeue();
                src.pitch = pitch;
                src.clip = clip;
                src.Play();
                StartCoroutine(ReturnToPoolAfterPlay(src));
                return;
            }

            // Fallback when pool is busy/absent
            _fallbackOneShot.pitch = pitch;
            _fallbackOneShot.PlayOneShot(clip);
        }

        private System.Collections.IEnumerator ReturnToPoolAfterPlay(AudioSource src)
        {
            // Using scaled time is fine; SFX are short. If you pause timeScale=0, consider WaitUntil(!isPlaying) with unscaled time.
            yield return new WaitWhile(() => src.isPlaying);
            src.clip = null;
            _sfxPool.Enqueue(src);
        }

        // ============ Optional helpers: linear <-> dB ============

        public static float LinearToDb(float linear)
        {
            return Mathf.Approximately(linear, 0f) ? -80f : 20f * Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f));
        }

        public static float DbToLinear(float db)
        {
            return Mathf.Pow(10f, db / 20f);
        }
    }
}
