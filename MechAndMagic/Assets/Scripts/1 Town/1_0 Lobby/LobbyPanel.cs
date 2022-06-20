using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPanel : MonoBehaviour, ITownPanel
{
    [Header("Player Info")]
    ///<summary> 클래스 텍스트 </summary>
    [SerializeField] Text classTxt;
    [SerializeField] Text lvlTxt;

    [Header("Equipment Info")]
    [Tooltip("0 lv1 ~ 4 lv9")]
    ///<summary> 장비 정보 프레임 스프라이트들
    ///<para> 0 lv1, 1 lv3, 2 lv5, 3 lv7, 4 lv9</para>
    ///</summary>
    [SerializeField] Sprite[] equipFrameSprites;
    [Tooltip("장비 아이콘")]
    ///<summary> 장비 아이콘 스프라이트들 </summary>
    [SerializeField] Sprite[] equipIconSprites;
    ///<summary> 장착 중인 장비 정보 보여줄 이미지 </summary>
    [SerializeField] EquipInfoImage[] equipInfos;

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

        LoadPlayerInfo();
        LoadItemInfo();
        LoadQuestInfo();
    }

    ///<summary> 플레이어 레벨 및 직업 표시 </summary>
    void LoadPlayerInfo()
    {
        switch(GameManager.instance.slotData.slotClass)
        {
            case 1:
                classTxt.text = "암드파이터";
                break;
            case 2:
                classTxt.text = "메탈나이트";
                break;
            case 3:
                classTxt.text = "블래스터";
                break;
            case 4:
                classTxt.text = "매드 사이언티스트";
                break;
            case 5:
                classTxt.text = "엘리멘탈 컨트롤러";
                break;
            case 6:
                classTxt.text = "드루이드";
                break;
            case 7:
                classTxt.text = "비전 마스터";
                break;
            case 8:
                classTxt.text = "매지컬 로그";
                break;
        }
        lvlTxt.text = $"Lv.{GameManager.instance.slotData.lvl}";
    }
    ///<summary> 현재 장착한 장비 정보 불러오기, 장비 정보 창에 적용 </summary>
    void LoadItemInfo()
    {
        for(int i = 0;i < 7;i++)
        {
            Equipment e = GameManager.instance.slotData.itemData.equipmentSlots[i + 1];
            if(e == null)
                equipInfos[i].SetImage(equipFrameSprites[0], null, 0);
            else
                equipInfos[i].SetImage(equipFrameSprites[(int)e.ebp.rarity - 1], equipIconSprites[0], e.ebp.reqlvl);
            
        }
    }
    ///<summary> 현재 진행 중인 퀘스트 정보 불러오기, 퀘스트 리스트 판넬에 적용 </summary>
    void LoadQuestInfo()
    {
         KeyValuePair<QuestBlueprint, int>[] currQuest = QuestManager.GetProceedingQuestData();

        for (int i = 0; i < 3; i++)
            if (currQuest[i].Key != null)
                questInfos[i].SetQuestProceed(currQuest[i]);
            else
                questInfos[i].gameObject.SetActive(false);
    }
}