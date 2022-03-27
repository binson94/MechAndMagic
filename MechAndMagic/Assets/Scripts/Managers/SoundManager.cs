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

    public Option option;

    private void Awake() 
    {
        if(PlayerPrefs.HasKey("Option"))
            option = LitJson.JsonMapper.ToObject<Option>(PlayerPrefs.GetString("Option"));
        else
            option = new Option();
    }

    public void BGMSet(float val)
    {
        option.bgm = (double)val;
        mixer.SetFloat("BGM", Mathf.Log10(val) * 20);
        SaveOption();
    }
    public void SFXSet(float val)
    {
        option.sfx = (double)val;
        mixer.SetFloat("SFX", Mathf.Log10(val) * 20);
        SaveOption();
    }
    public void TxtSet(float val)
    {
        option.txtSpd = Mathf.RoundToInt(val);
        SaveOption();
    }
    void SaveOption() => PlayerPrefs.SetString("Option", LitJson.JsonMapper.ToJson(option));


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
        SFX.PlayOneShot(sfxs[idx]);
    }
}

public class Option
{
    public double bgm;
    public double sfx;
    public int txtSpd;

    public Option()
    {
        bgm = 1;
        sfx = 1;
        txtSpd = 1;
    }
}
