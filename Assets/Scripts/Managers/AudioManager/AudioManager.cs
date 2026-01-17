using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private AudioSource[] bgm;

    [SerializeField] private bool playBgm;
    [SerializeField] private int bgmIndex;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        PlayBGM(3);
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
}
