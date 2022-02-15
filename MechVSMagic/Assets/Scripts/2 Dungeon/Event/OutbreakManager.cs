using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OutbreakManager : MonoBehaviour
{
    int[] outbreakIdx = new int[2];
    bool isSelect = false;

    [SerializeField] GameObject selectPanel;
    [SerializeField] Text[] outbreakTxt;
    [SerializeField] GameObject backBtn;

    private void Start()
    {
        GameManager.sound.PlayBGM(BGM.Battle1);

        outbreakIdx[0] = GameManager.slotData.dungeonRoom;
        outbreakIdx[1] = GameManager.slotData.outbreakSubRoom;

        outbreakTxt[0].text = QuestSlot.GetQuestScript(true, outbreakIdx[0]);
        outbreakTxt[1].text = QuestSlot.GetQuestScript(true, outbreakIdx[1]);
    }

    public void Btn_QuestSelect(int idx)
    {
        if (isSelect)
            return;
        isSelect = true;

        GameManager.SwitchSceneData(SceneKind.Dungeon);
        QuestDataManager.NewQuest(true, outbreakIdx[idx]);
        selectPanel.SetActive(false);
        backBtn.SetActive(true);
    }

    public void Btn_Back()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("2_0 Dungeon");
    }
}
