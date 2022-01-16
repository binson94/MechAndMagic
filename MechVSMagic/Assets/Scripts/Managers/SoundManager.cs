using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioSource BGM;
    [SerializeField] AudioSource SFX;

    [SerializeField] AudioClip[] bgms;
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

    public void PlayBGM(int idx)
    {
        BGM.clip = bgms[idx];
        BGM.Play();
    }

    public void PlaySFX(int idx)
    {
        SFX.clip = sfxs[idx];
        SFX.Play();
    }
}
