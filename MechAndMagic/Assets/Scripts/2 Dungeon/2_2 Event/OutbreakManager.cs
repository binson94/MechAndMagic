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

        outbreakIdx = GameManager.slotData.dungeonState.currRoomEvent;
        outbreakTxt.text = QuestSlot.GetQuestScript(true, outbreakIdx);

        AcceptOutbreakQuest();
    }
    void AcceptOutbreakQuest()
    {
        GameManager.SwitchSceneData(SceneKind.Dungeon);
        QuestManager.NewQuest(true, outbreakIdx);
    }

    public void Btn_Back()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("2_0 Dungeon");
    }
}
