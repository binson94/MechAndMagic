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
        outbreakIdx = PlayerPrefs.GetInt(string.Concat("Room", GameManager.currSlot));

        outbreakTxt.text = QuestSlot.GetQuestScript(true, outbreakIdx);

        QuestDataManager.NewQuest(true, outbreakIdx);
    }

    public void Btn_Back()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("2_0 Dungeon");
    }
}
