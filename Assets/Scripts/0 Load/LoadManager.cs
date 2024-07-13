using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class LoadManager : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Text stateTxt;
    bool canStart = false;
    private async void Start() {
        Task bgmTask = SoundManager.Instance.LoadBGM();
        Task sfxTask = SoundManager.Instance.LoadSFX();
        await Task.WhenAll(bgmTask, sfxTask);

        SoundManager.Instance.OnLoadComplete();

        //#if UNITY_EDITOR
        //PlayerPrefs.SetString("Slot3", Resources.Load<TextAsset>("test").text);
        //#endif

        stateTxt.text = "터치하여 시작";
        stateTxt.GetComponent<Animator>().SetBool("loaded", true);
        canStart = true;
    }

    public void Btn_Start()
    {
        if(canStart) UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
}