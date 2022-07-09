using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum BGMList
{
    Title, Intro, Town1 = 3, Town2 = 5, Battle1 = 7, Battle2 = 9, Battle3 = 11, Battle4 = 13,
    Boss1 = 15, Boss2 = 17, Boss3 = 19, End = 21
}

public class SoundManager : MonoBehaviour
{
    static SoundManager _instance = null;
    public static SoundManager instance {get {return _instance;}}

    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioSource BGM;
    [SerializeField] AudioSource SFX;

    [SerializeField] List<AudioClip> bgms = new List<AudioClip>();
    //List<AudioClip> sfxs = new List<AudioClip>();
    [SerializeField] List<AudioClip> sfxs = new List<AudioClip>();

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

    public Task<IList<AudioClip>> LoadBGM() => Addressables.LoadAssetsAsync<AudioClip>("BGM", (result) => {bgms.Add(result);}).Task;
    public Task<IList<AudioClip>> LoadSFX() => Addressables.LoadAssetsAsync<AudioClip>("SFX", (result) => {sfxs.Add(result);}).Task;
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


    public void PlayBGM(BGMList idx)
    {
        int pos = (int)idx;
        if(GameManager.instance.slotData != null)
            pos += GameManager.instance.slotData.region == 10 ? 1 : 0;
        AudioClip clip = bgms[pos];

        if(BGM.clip != clip)
        {
            BGM.clip = clip;
            BGM.Play();
        }
    }
    public void PlaySFX(int idx)
    {
        SFX.PlayOneShot(sfxs[Mathf.Max(0, idx - 1)]);
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
