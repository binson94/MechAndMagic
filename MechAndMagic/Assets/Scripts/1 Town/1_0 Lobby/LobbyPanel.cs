using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPanel : MonoBehaviour, ITownPanel
{
    [Header("NPC")]
    [SerializeField] TownManager TM;
    int[] npcIdx = new int[2];
    [SerializeField] Sprite[] npcSprites;
    [SerializeField] Image[] npcImages;

    [Header("Quest Info")]
    ///<summary> 퀘스트 정보 판넬 </summary>
    [SerializeField] GameObject questListPanel;
    ///<summary> 퀘스트 리스트 창 여는 버튼 </summary>
    [SerializeField] GameObject questListBtn;
    ///<summary> 각 원소가 퀘스트 1개 정보 전담 </summary>
    [SerializeField] QuestPanel[] questInfos;
    
    public void ResetAllState()
    {
        questListPanel.SetActive(false);
        questListBtn.SetActive(true);

        LoadQuestInfo();

        int npcStart = 0;
        if (GameManager.instance.slotData.chapter >= 3)
            npcStart += 2;
        if (GameManager.instance.slotData.slotClass >= 5)
            npcStart += 4;
        npcIdx[0] = npcStart; npcIdx[1] = npcStart + 1;

        for(int i = 0;i < 2;i++)
            npcImages[i].sprite = npcSprites[npcIdx[i]];
    }
    ///<summary> 현재 진행 중인 퀘스트 정보 불러오기, 퀘스트 리스트 판넬에 적용 </summary>
    void LoadQuestInfo()
    {
         KeyValuePair<QuestBlueprint, int>[] currQuest = QuestManager.GetProceedingQuestData();

        for (int i = 0; i < 3; i++)
            if (currQuest[i].Key != null)
                {questInfos[i].SetQuestProceed(currQuest[i]); questInfos[i].gameObject.SetActive(true);}
            else
                questInfos[i].gameObject.SetActive(false);
    }

    ///<summary> NPC 선택 시 Script Panel로 전환 </summary>
    public void Btn_SelectNPC(int isRight) => TM.LoadDialog(npcIdx[isRight]);
}