using UnityEngine;

/// <summary>Creates and plays the game's lightweight cartoon sound effects.</summary>
[RequireComponent(typeof(AudioSource))]
public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string SoundEnabledKey = "SoundEnabled";
    private AudioSource source;
    private AudioClip scoreClip;
    private AudioClip lifeClip;
    private AudioClip hitClip;
    private AudioClip gameOverClip;
    private AudioClip shieldClip;

    public bool SoundEnabled { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = .8f;

        SoundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) != 0;
        scoreClip = CreateMelody("Score Chime", new[] { 880f, 1175f }, .09f);
        lifeClip = CreateMelody("Life Up", new[] { 523f, 659f, 784f }, .13f);
        shieldClip = CreateMelody("Shield Sparkle", new[] { 784f, 988f, 1318f }, .1f);
        hitClip = CreateImpact("Obstacle Hit", 150f, .22f);
        gameOverClip = CreateMelody("Game Over", new[] { 392f, 330f, 262f }, .24f);
    }

    public void ToggleSound()
    {
        SetSoundEnabled(!SoundEnabled);
    }

    public void SetSoundEnabled(bool enabled)
    {
        SoundEnabled = enabled;
        PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void PlayScore() => Play(scoreClip, .55f);
    public void PlayLifeUp() => Play(lifeClip, .75f);
    public void PlayShield() => Play(shieldClip, .65f);
    public void PlayHit() => Play(hitClip, .7f);
    public void PlayGameOver() => Play(gameOverClip, .8f);

    private void Play(AudioClip clip, float volume)
    {
        if (SoundEnabled && source != null && clip != null)
            source.PlayOneShot(clip, volume);
    }

    private static AudioClip CreateMelody(string clipName, float[] notes, float noteDuration)
    {
        const int sampleRate = 44100;
        int samplesPerNote = Mathf.CeilToInt(sampleRate * noteDuration);
        int totalSamples = samplesPerNote * notes.Length;
        float[] samples = new float[totalSamples];

        for (int noteIndex = 0; noteIndex < notes.Length; noteIndex++)
        {
            int start = noteIndex * samplesPerNote;
            for (int i = 0; i < samplesPerNote; i++)
            {
                float t = i / (float)sampleRate;
                float progress = i / (float)samplesPerNote;
                float envelope = Mathf.Pow(1f - progress, 2.2f) * Mathf.Min(1f, i / 180f);
                float fundamental = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t);
                float overtone = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * 2f * t) * .22f;
                samples[start + i] = (fundamental + overtone) * envelope * .35f;
            }
        }

        return MakeClip(clipName, samples, sampleRate);
    }

    private static AudioClip CreateImpact(string clipName, float frequency, float duration)
    {
        const int sampleRate = 44100;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[count];
        uint noiseState = 0xA341316Cu;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float progress = i / (float)count;
            noiseState ^= noiseState << 13;
            noiseState ^= noiseState >> 17;
            noiseState ^= noiseState << 5;
            float noise = ((noiseState & 0xFFFF) / 32767.5f) - 1f;
            float envelope = Mathf.Pow(1f - progress, 3f) * Mathf.Min(1f, i / 100f);
            float thump = Mathf.Sin(2f * Mathf.PI * (frequency - 60f * progress) * t);
            samples[i] = (thump * .65f + noise * .35f) * envelope * .45f;
        }

        return MakeClip(clipName, samples, sampleRate);
    }

    private static AudioClip MakeClip(string clipName, float[] samples, int sampleRate)
    {
        AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
