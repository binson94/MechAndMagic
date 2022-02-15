using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum BGM
{
    Title, Intro, Town1, Town2, Battle1, Battle2, Battle3, Battle4, Boss
}

public class SoundManager : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioSource BGM;
    [SerializeField] AudioSource SFX;

    [SerializeField] AudioClip[] mechBgms;
    [SerializeField] AudioClip[] magicBgms;
    [SerializeField] AudioClip[] sfxs;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BGMSet(float val)
    {
        mixer.SetFloat("BGM", Mathf.Log10(val) * 20);
        PlayerPrefs.SetFloat("BGM", val);
    }

    public void SFXSet(float val)
    {
        mixer.SetFloat("SFX", Mathf.Log10(val) * 20);
        PlayerPrefs.SetFloat("SFX", val);
    }

    public void PlayBGM(BGM idx)
    {
        AudioClip tmp;
        if (GameManager.slotData == null)
            tmp = mechBgms[(int)idx];
        else
            tmp = GameManager.slotData.slotClass < 5 ? mechBgms[(int)idx] : magicBgms[(int)idx];

        if(BGM.clip != tmp)
        {
            BGM.clip = tmp;
            BGM.Play();
        }
    }

    public void PlaySFX(int idx)
    {
        SFX.clip = sfxs[idx];
        SFX.Play();
    }
}
