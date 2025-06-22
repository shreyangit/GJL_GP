using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class AnimationAudioClip
{
    [Header("Audio Settings")]
    public string animationName;
    public AudioClip audioClip;

    [Header("Playback Settings")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool loop = false;
    public bool randomizePitch = false;
    [Range(0f, 0.5f)] public float pitchVariation = 0.1f;

    [Header("Timing")]
    public float delayBeforePlay = 0f;
    public bool interruptible = true;
}

public class AnimationAudioController : MonoBehaviour
{
    [Header("Character Type")]
    public string characterType = "Player"; // Player, Normal, Brightness, Dynamite

    [Header("Audio Sources")]
    public AudioSource primaryAudioSource;
    public AudioSource secondaryAudioSource; // For overlapping sounds

    [Header("Animation Audio Clips")]
    public List<AnimationAudioClip> animationAudios = new List<AnimationAudioClip>();

    [Header("Debug Settings")]
    public bool showDebugLogs = true;
    public bool showAudioVisualizer = false;

    // Private variables
    private Animator animator;
    private Dictionary<string, AnimationAudioClip> audioClipsDictionary;
    private Coroutine currentAudioCoroutine;
    private string currentPlayingAnimation = "";
    private bool isInitialized = false;



    void Awake()
    {
        InitializeAudioSources();
        CreateAudioDictionary();
    }

    void Start()
    {
        InitializeAnimator();
        SetupDefaultAudioClips();
        isInitialized = true;

        if (showDebugLogs)
            Debug.Log($"✅ {characterType} AnimationAudioController initialized with {animationAudios.Count} audio clips");
    }

    void InitializeAudioSources()
    {
        // Setup primary audio source
        if (primaryAudioSource == null)
        {
            primaryAudioSource = GetComponent<AudioSource>();
            if (primaryAudioSource == null)
            {
                primaryAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        primaryAudioSource.playOnAwake = false;
        primaryAudioSource.spatialBlend = 0.8f; // 3D sound
        primaryAudioSource.maxDistance = 15f;
        primaryAudioSource.rolloffMode = AudioRolloffMode.Linear;

        // Setup secondary audio source for overlapping sounds
        if (secondaryAudioSource == null)
        {
            GameObject secondarySourceObj = new GameObject("Secondary Audio Source");
            secondarySourceObj.transform.SetParent(transform);
            secondarySourceObj.transform.localPosition = Vector3.zero;

            secondaryAudioSource = secondarySourceObj.AddComponent<AudioSource>();
            secondaryAudioSource.playOnAwake = false;
            secondaryAudioSource.spatialBlend = 0.8f;
            secondaryAudioSource.maxDistance = 15f;
            secondaryAudioSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    void InitializeAnimator()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"⚠️ No Animator found on {gameObject.name}! Animation audio won't work.");
        }
    }

    void CreateAudioDictionary()
    {
        audioClipsDictionary = new Dictionary<string, AnimationAudioClip>();

        foreach (AnimationAudioClip audioClip in animationAudios)
        {
            if (!string.IsNullOrEmpty(audioClip.animationName))
            {
                audioClipsDictionary[audioClip.animationName.ToLower()] = audioClip;
            }
        }
    }

    void SetupDefaultAudioClips()
    {
        // Add default animation entries based on character type
        switch (characterType.ToLower())
        {
            case "player":
                AddDefaultPlayerAudios();
                break;
            case "normal":
            case "brightness":
            case "dynamite":
                AddDefaultZombieAudios();
                break;
        }
    }

    void AddDefaultPlayerAudios()
    {
        string[] playerAnimations = {
            "Idle", "Walk", "Run", "Attack", "Shoot", "Reload",
            "TakeDamage", "Death", "Jump", "Dash"
        };

        AddDefaultAnimations(playerAnimations);
    }

    void AddDefaultZombieAudios()
    {
        string[] zombieAnimations = {
            "Idle", "Walk", "Attack", "TakeDamage", "Death",
            "Roar", "Bite", "Claw", "Explosion"
        };

        AddDefaultAnimations(zombieAnimations);
    }

    void AddDefaultAnimations(string[] animationNames)
    {
        foreach (string animName in animationNames)
        {
            if (!audioClipsDictionary.ContainsKey(animName.ToLower()))
            {
                AnimationAudioClip newAudioClip = new AnimationAudioClip
                {
                    animationName = animName,
                    audioClip = null,
                    volume = 0.8f,
                    pitch = 1f,
                    loop = false,
                    randomizePitch = false,
                    pitchVariation = 0.1f,
                    delayBeforePlay = 0f,
                    interruptible = true
                };

                animationAudios.Add(newAudioClip);
                audioClipsDictionary[animName.ToLower()] = newAudioClip;
            }
        }
    }

    // 🎵 PUBLIC METHODS - Called by Animation Events or Scripts

    public void PlayAnimationAudio(string animationName)
    {
        if (!isInitialized) return;

        string key = animationName.ToLower();

        if (audioClipsDictionary.ContainsKey(key))
        {
            AnimationAudioClip audioData = audioClipsDictionary[key];

            if (audioData.audioClip != null)
            {
                StartCoroutine(PlayAudioCoroutine(audioData, animationName));
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning($"⚠️ Audio clip for '{animationName}' is not assigned on {gameObject.name}");
            }
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"⚠️ Animation '{animationName}' not found in audio dictionary for {gameObject.name}");
        }
    }

    IEnumerator PlayAudioCoroutine(AnimationAudioClip audioData, string animationName)
    {
        // Wait for delay
        if (audioData.delayBeforePlay > 0)
        {
            yield return new WaitForSeconds(audioData.delayBeforePlay);
        }

        // Choose audio source
        AudioSource sourceToUse = primaryAudioSource;

        // Use secondary source if primary is busy and not interruptible
        if (primaryAudioSource.isPlaying && !audioData.interruptible)
        {
            sourceToUse = secondaryAudioSource;
        }

        // Stop current audio if interruptible
        if (audioData.interruptible && currentPlayingAnimation != animationName)
        {
            if (currentAudioCoroutine != null)
            {
                StopCoroutine(currentAudioCoroutine);
            }
            sourceToUse.Stop();
        }

        // Setup audio properties
        sourceToUse.clip = audioData.audioClip;
        sourceToUse.volume = audioData.volume;
        sourceToUse.loop = audioData.loop;

        // Apply pitch variation
        if (audioData.randomizePitch)
        {
            float randomPitch = audioData.pitch + Random.Range(-audioData.pitchVariation, audioData.pitchVariation);
            sourceToUse.pitch = Mathf.Clamp(randomPitch, 0.1f, 3f);
        }
        else
        {
            sourceToUse.pitch = audioData.pitch;
        }

        // Play audio
        sourceToUse.Play();
        currentPlayingAnimation = animationName;
        currentAudioCoroutine = StartCoroutine(AudioPlaybackTracker(sourceToUse, animationName));

        if (showDebugLogs)
        {
            Debug.Log($"🎵 Playing {characterType} audio: '{animationName}' (Volume: {audioData.volume}, Pitch: {sourceToUse.pitch})");
        }
    }

    IEnumerator AudioPlaybackTracker(AudioSource source, string animationName)
    {
        while (source.isPlaying)
        {
            yield return null;
        }

        if (currentPlayingAnimation == animationName)
        {
            currentPlayingAnimation = "";
        }
    }

    // 🎵 ANIMATION-SPECIFIC METHODS (call these from scripts or animation events)

    public void PlayIdleAudio() => PlayAnimationAudio("Idle");
    public void PlayWalkAudio() => PlayAnimationAudio("Walk");
    public void PlayRunAudio() => PlayAnimationAudio("Run");
    public void PlayAttackAudio() => PlayAnimationAudio("Attack");
    public void PlayShootAudio() => PlayAnimationAudio("Shoot");
    public void PlayReloadAudio() => PlayAnimationAudio("Reload");
    public void PlayTakeDamageAudio() => PlayAnimationAudio("TakeDamage");
    public void PlayDeathAudio() => PlayAnimationAudio("Death");
    public void PlayJumpAudio() => PlayAnimationAudio("Jump");
    public void PlayDashAudio() => PlayAnimationAudio("Dash");

    // Zombie-specific
    public void PlayRoarAudio() => PlayAnimationAudio("Roar");
    public void PlayBiteAudio() => PlayAnimationAudio("Bite");
    public void PlayClawAudio() => PlayAnimationAudio("Claw");
    public void PlayExplosionAudio() => PlayAnimationAudio("Explosion");

    // 🎵 UTILITY METHODS

    public void StopAllAudio()
    {
        if (currentAudioCoroutine != null)
        {
            StopCoroutine(currentAudioCoroutine);
        }

        primaryAudioSource.Stop();
        secondaryAudioSource.Stop();
        currentPlayingAnimation = "";

        if (showDebugLogs)
        {
            Debug.Log($"🔇 Stopped all audio for {characterType} - {gameObject.name}");
        }
    }

    public void SetGlobalVolume(float volume)
    {
        primaryAudioSource.volume *= volume;
        secondaryAudioSource.volume *= volume;
    }

    public bool IsPlayingAudio()
    {
        return primaryAudioSource.isPlaying || secondaryAudioSource.isPlaying;
    }

    public bool IsPlayingSpecificAudio(string animationName)
    {
        return currentPlayingAnimation.ToLower() == animationName.ToLower();
    }

    // 🎵 INTEGRATION WITH EXISTING SCRIPTS

    public void OnMovementStateChanged(bool isMoving, bool isRunning)
    {
        if (!isInitialized) return;

        if (isMoving)
        {
            if (isRunning)
            {
                PlayRunAudio();
            }
            else
            {
                PlayWalkAudio();
            }
        }
        else
        {
            if (!IsPlayingSpecificAudio("Attack") && !IsPlayingSpecificAudio("Shoot"))
            {
                PlayIdleAudio();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showAudioVisualizer) return;

        // Draw audio range
        Gizmos.color = IsPlayingAudio() ? Color.yellow : Color.gray;
        Gizmos.DrawWireSphere(transform.position, primaryAudioSource ? primaryAudioSource.maxDistance : 15f);

        // Draw audio status
        if (IsPlayingAudio())
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
        }
    }

    void OnDestroy()
    {
        StopAllAudio();
    }
}
