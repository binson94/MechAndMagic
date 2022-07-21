using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoryManager : MonoBehaviour
{
    [SerializeField] Text storyNameTxt;
    [SerializeField] Text storyTxt;

    private void Start() {
        int storyIdx = GameManager.instance.slotData.storyIdx;
        if (storyIdx % 5 == 1) SoundManager.instance.PlayBGM(BGMList.Intro);
        else if (storyIdx % 5 == 4) SoundManager.instance.PlayBGM(BGMList.End);

        storyNameTxt.text = GameManager.instance.slotData.region == 10 ? "기계 " : "마법 ";
        if(storyIdx % 5 == 1)
            storyNameTxt.text =  $"{storyNameTxt.text} 인트로";
        else if(storyIdx % 5 == 4)
            storyNameTxt.text = $"{storyNameTxt.text} 엔딩";
        else
            storyNameTxt.text = $"{storyNameTxt.text} {storyIdx % 5 - 1}챕터";
        
        storyTxt.text = Resources.Load<TextAsset>($"Storys/{storyIdx}").text;
        GameManager.instance.slotData.chapter = storyIdx % 5;
    }

    public void Btn_GoToTown()
    {
        SoundManager.instance.PlaySFX(22);
        GameManager.instance.SwitchSceneData(SceneKind.Town);
        GameManager.instance.LoadScene(SceneKind.Town);
    }
}
