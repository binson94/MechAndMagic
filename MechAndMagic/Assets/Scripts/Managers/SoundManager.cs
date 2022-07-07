using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum BGM
{
    Title = 1, Intro, Town1, Town2, Battle1, Battle2, Battle3, Battle4, Boss1, Boss2, Boss3, End
}

public class SoundManager : MonoBehaviour
{
    static SoundManager _instance = null;
    public static SoundManager instance {get {return _instance;}}

    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioSource BGM;
    [SerializeField] AudioSource SFX;

    [SerializeField] AudioClip[] mechBgms = new AudioClip[13];
    [SerializeField] AudioClip[] magicBgms = new AudioClip[13];
    [SerializeField] AudioClip[] sfxs = new AudioClip[24];

    public Option option;

    private void Awake() 
    {
        if(_instance == null)
        {
            _instance = this;
            LoadOption();
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
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
        option.txtSpd = 2 * (int)val;
        SaveOption();
    }

    void LoadOption()
    {
        if (PlayerPrefs.HasKey("Option"))
        {
            option = LitJson.JsonMapper.ToObject<Option>(PlayerPrefs.GetString("Option"));
            mixer.SetFloat("BGM", Mathf.Log10((float)option.bgm) * 20);
            mixer.SetFloat("SFX", Mathf.Log10((float)option.sfx) * 20);
        }
        else
            option = new Option();
    }
    void SaveOption() => PlayerPrefs.SetString("Option", LitJson.JsonMapper.ToJson(option));


    public void PlayBGM(BGM idx)
    {
        AudioClip tmp;
        if (GameManager.instance.slotData == null)
            tmp = mechBgms[(int)idx];
        else
            tmp = GameManager.instance.slotData.slotClass < 5 ? mechBgms[(int)idx] : magicBgms[(int)idx];

        if (BGM.clip != tmp)
        {
            BGM.clip = tmp;
            BGM.Play();
        }
    }
    public void PlaySFX(int idx)
    {
        SFX.PlayOneShot(sfxs[idx]);
    }
    public int GetTxtSpd() => option.txtSpd;
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
