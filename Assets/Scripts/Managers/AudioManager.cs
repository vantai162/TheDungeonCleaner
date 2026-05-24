using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource[] sfx;
    [SerializeField] private AudioSource[] bgm;

    private int bgmIndex;
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
        
        InvokeRepeating(nameof(PlayMusicIfNeeded), 0, 2);
    }

    public void PlayMusicIfNeeded()
    {
        if (bgm[bgmIndex].isPlaying == false)
            PlayRandomBGM();
    }
    
    public void PlayRandomBGM()
    {
        bgmIndex = Random.Range(0, bgm.Length-1);
        PlayBGM(bgmIndex);
    }
    
    public void PlayBGM(int bgmToPlay)
    {
        for (int i = 0; i < bgm.Length; i++)
        {
            bgm[i].Stop();
        }
        
        bgmIndex = bgmToPlay;
        bgm[bgmToPlay].Play();
    }

    public void PauseBGM() => bgm[bgmIndex].Pause();

    public void PlaySFX(int sfxToPlay, bool randomPitch = true)
    {
        if(sfxToPlay >= sfx.Length)
            return;
        if (randomPitch)
            sfx[sfxToPlay].pitch = Random.Range(.9f, 1.1f);
        
        sfx[sfxToPlay].Play();
    }
    
    // Not use but left here for future reference
    public void StopSFX(int sfxToStop) => sfx[sfxToStop].Stop();
}
