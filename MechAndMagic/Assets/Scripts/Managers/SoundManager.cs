using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum BGM
{
    Title = 1, Intro, Town1, Town2, Battle1, Battle2, Battle3, Battle4, Boss
}

public class SoundManager : MonoBehaviour
{
    static GameObject container;
    static SoundManager _instance = null;
    public static SoundManager instance
    {
        get
        {
            if (_instance == null)
            {
                container = new GameObject();
                container.name = "SoundManager";
                _instance = container.AddComponent(typeof(SoundManager)) as SoundManager;

                MakeAudioSource();
                LoadResources();
                _instance.LoadOption();

                container.transform.SetParent(GameManager.instance.transform);
            }

            return _instance;
        }
    }

    static AudioMixer mixer;
    static AudioSource BGM;
    static AudioSource SFX;

    static AudioClip[] mechBgms = new AudioClip[13];
    static AudioClip[] magicBgms = new AudioClip[13];
    static AudioClip[] sfxs = new AudioClip[24];

    ///<summary> BGM, SFX Audio Source 생성 </summary>
    static void MakeAudioSource()
    {
        mixer = Resources.Load<AudioMixer>("Sounds/AudioMixer");

        GameObject tmp = new GameObject();
        tmp.name = "BGM";
        BGM = tmp.AddComponent(typeof(AudioSource)) as AudioSource;
        BGM.playOnAwake = false;
        BGM.loop = true;
        BGM.outputAudioMixerGroup = mixer.FindMatchingGroups("BGM")[0];
        tmp.transform.SetParent(container.transform);

        tmp = new GameObject();
        tmp.name = "SFX";
        SFX = tmp.AddComponent(typeof(AudioSource)) as AudioSource;
        SFX.playOnAwake = false;
        SFX.loop = false;
        SFX.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX")[0];
        tmp.transform.SetParent(container.transform);
    }
    ///<summary> BGM, SFX 파일 불러옴 </summary>
    static void LoadResources()
    {
        mechBgms[1] = magicBgms[1] = Resources.Load<AudioClip>("Sounds/BGM/1_BGM");
        for(int i = 2;i <= 12;i++)
        {
            mechBgms[i] = Resources.Load<AudioClip>($"Sounds/BGM/{i}_BGM_Mech");
            magicBgms[i] = Resources.Load<AudioClip>($"Sounds/BGM/{i}_BGM_Magic");
        }

        for(int i = 1;i <= 23;i++)
            Resources.Load<AudioClip>($"Sounds/SFX/{i}_SFX");
    }

    public Option option;

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
        if (GameManager.slotData == null)
            tmp = mechBgms[(int)idx];
        else
            tmp = GameManager.slotData.slotClass < 5 ? mechBgms[(int)idx] : magicBgms[(int)idx];

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
