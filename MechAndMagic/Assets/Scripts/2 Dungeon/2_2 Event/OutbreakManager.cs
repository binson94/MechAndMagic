using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OutbreakManager : MonoBehaviour
{
    int outbreakIdx;
    [SerializeField] Text outbreakTxt;

    private void Start()
    {
        SoundManager.instance.PlayBGM(BGM.Battle1);

        outbreakIdx = GameManager.instance.slotData.dungeonData.currRoomEvent;
        outbreakTxt.text = QuestManager.GetQuestScript(true, outbreakIdx);

        AcceptOutbreakQuest();
    }
    void AcceptOutbreakQuest()
    {
        GameManager.instance.SwitchSceneData(SceneKind.Dungeon);
        QuestManager.AcceptQuest(true, outbreakIdx);
    }

    public void Btn_Back()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("2_0 Dungeon");
    }
}
