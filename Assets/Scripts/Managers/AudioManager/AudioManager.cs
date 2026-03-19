using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private AudioSource[] bgm;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private string sfxMixerParam = "sfx";
    [SerializeField] private string bgmMixerParam = "bgm";
    [SerializeField] private float sliderMultiplier = 25f;

    [SerializeField] private bool playBgm;
    [SerializeField] private int bgmIndex;

    private void Awake()
    {
        instance = this;

        // BGM should keep playing even when AudioListener is paused
        for (int i = 0; i < bgm.Length; i++)
        {
            bgm[i].ignoreListenerPause = true;
        }
    }

    private void Start()
    {
        StartCoroutine(ApplySavedAudioSettings());
        PlayBGM(3);
        RouteAllSFXSources();
        StartCoroutine(PeriodicSFXRoutingCo());
    }

    private IEnumerator ApplySavedAudioSettings()
    {
        // AudioMixer.SetFloat doesn't work on the first frame, wait one frame
        yield return null;

        if (audioMixer == null)
            yield break;

        float savedSfx = PlayerPrefs.GetFloat(sfxMixerParam, .7f);
        float savedBgm = PlayerPrefs.GetFloat(bgmMixerParam, .7f);

        float sfxDb = Mathf.Log10(Mathf.Max(savedSfx, 0.0001f)) * sliderMultiplier;
        float bgmDb = Mathf.Log10(Mathf.Max(savedBgm, 0.0001f)) * sliderMultiplier;

        audioMixer.SetFloat(sfxMixerParam, sfxDb);
        audioMixer.SetFloat(bgmMixerParam, bgmDb);
    }

    private void Update()
    {
        if (playBgm == false && BgmIsPlaying())
            StopAllBGM();


        if (playBgm && bgm[bgmIndex].isPlaying == false)
            PlayRandomBGM();
    }

    public void PlaySFX(AudioSource sfx, bool randomPitch = false, float minPitch = .85f, float maxPitch = 1.1f)
    {
        if (sfx == null)
            return;

        // Ensure this SFX goes through the SFX mixer group
        if (sfxMixerGroup != null && sfx.outputAudioMixerGroup != sfxMixerGroup)
            sfx.outputAudioMixerGroup = sfxMixerGroup;

        float pitch = Random.Range(minPitch, maxPitch);

        sfx.pitch = pitch;
        sfx.Play();
    }

    public void SFXDelayAndFade(AudioSource source, bool play, float taretVolume, float delay = 0, float fadeDuratuin = 1)
    {
        StartCoroutine(SFXDelayAndFadeCo(source, play, taretVolume, delay, fadeDuratuin));
    }

    public void PlayBGM(int index)
    {
        StopAllBGM();

        bgmIndex = index;
        bgm[index].Play();
    }

    public void StopAllBGM()
    {
        for (int i = 0; i < bgm.Length; i++)
        {
            bgm[i].Stop();
        }
    }

    [ContextMenu("Play random music")]
    public void PlayRandomBGM()
    {
        StopAllBGM();
        bgmIndex = Random.Range(0, bgm.Length);
        PlayBGM(bgmIndex);
    }

    private bool BgmIsPlaying()
    {
        for (int i = 0; i < bgm.Length; i++)
        {
            if (bgm[i].isPlaying)
                return true;
        }

        return false;
    }

    private IEnumerator SFXDelayAndFadeCo(AudioSource source,bool play, float targetVolume, float delay = 0, float fadeDuration = 1)
    {
        yield return new WaitForSeconds(delay);

        float startVolume = play ? 0 : source.volume;
        float endVolume = play ? targetVolume : 0;
        float elapsed = 0;

        if (play)
        {
            source.volume = 0;
            source.Play();
        }

        //Fade in/out over the duration
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume,endVolume, elapsed/ fadeDuration);
            yield return null;
        }

        source.volume = endVolume;

        if (play == false)
            source.Stop();
    }
    public void SetSFXPause(bool paused)
    {
        AudioListener.pause = paused;
    }

    /// <summary>
    /// Routes all AudioSources in the scene (except BGM) to the SFX mixer group.
    /// </summary>
    public void RouteAllSFXSources()
    {
        if (sfxMixerGroup == null)
            return;

        HashSet<AudioSource> bgmSet = new HashSet<AudioSource>(bgm);
        AudioSource[] allSources = FindObjectsOfType<AudioSource>(true);

        foreach (AudioSource source in allSources)
        {
            if (bgmSet.Contains(source))
                continue;

            if (source.outputAudioMixerGroup == null)
                source.outputAudioMixerGroup = sfxMixerGroup;
        }
    }

    /// <summary>
    /// Periodically routes new AudioSources (from spawned enemies etc.) to SFX group.
    /// </summary>
    private IEnumerator PeriodicSFXRoutingCo()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            RouteAllSFXSources();
        }
    }
}
