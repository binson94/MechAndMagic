using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoryManager : MonoBehaviour
{
    [SerializeField] Text storyNameTxt;
    [SerializeField] Text storyTxt;

    private void Start() {
        int storyIdx = GameManager.Instance.slotData.storyIdx;
        if (storyIdx % 5 == 1) SoundManager.Instance.PlayBGM(BGMList.Intro);
        else if (storyIdx % 5 == 0) SoundManager.Instance.PlayBGM(BGMList.End);
        if(storyIdx % 5 == 1)
            storyNameTxt.text =  $"인트로";
        else if(storyIdx % 5 == 0)
            storyNameTxt.text = $"엔딩";
        else
            storyNameTxt.text = $"{storyIdx % 5 - 1}챕터";
        
        storyTxt.text = Resources.Load<TextAsset>($"Storys/{storyIdx}").text;

        if(storyIdx % 5 != 0)
            GameManager.Instance.slotData.chapter = storyIdx % 5;
    }

    public void Btn_GoToTown()
    {
        SoundManager.Instance.PlaySFX(22);
        GameManager.Instance.SwitchSceneData(SceneKind.Town);

        if(GameManager.Instance.slotData.storyIdx % 5 == 0)
            GameManager.Instance.LoadScene(SceneKind.Title);
        else
            GameManager.Instance.LoadScene(SceneKind.Town);
    }
}
