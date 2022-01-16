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
        outbreakIdx = GameManager.slotData.dungeonRoom;

        outbreakTxt.text = QuestSlot.GetQuestScript(true, outbreakIdx);

        QuestDataManager.NewQuest(true, outbreakIdx);
    }

    public void Btn_Back()
    {
        GameManager.SwitchSceneData(SceneKind.Dungeon);
        UnityEngine.SceneManagement.SceneManager.LoadScene("2_0 Dungeon");
    }
}
