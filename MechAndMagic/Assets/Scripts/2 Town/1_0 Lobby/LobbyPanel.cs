using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class LobbyPanel : MonoBehaviour, ITownPanel
{

    ///<summary> 마을 매니저, NPC 정보 얻기, 대화로 넘어가기 </summary>
    [Header("NPC")]
    [SerializeField] TownManager TM;
    ///<summary> 현재 마을에 표시 중인 NPC idx </summary>
    int[] npcIdx = new int[2];
    ///<summary> NPC 일러스트 </summary>
    [SerializeField] Image[] npcImages;
    ///<summary> npc 전체 스프라이트 </summary>
    [SerializeField] Sprite[] npcSprites;
    ///<summary> 퀘스트 정보 표시 스프라이트 </summary>
    [SerializeField] Image[] npcQuestIconImages;
    ///<summary> 퀘스트 정보 표시 이미지 </summary>
    [SerializeField] Sprite[] npcQuestIconSprites;


    ///<summary> 퀘스트 정보 판넬 </summary>
    [Header("Quest Info")]
    [SerializeField] GameObject questListPanel;
    ///<summary> 퀘스트 리스트 창 여는 버튼 </summary>
    [SerializeField] GameObject questListBtn;
    ///<summary> 각 원소가 퀘스트 1개 정보 전담 </summary>
    [SerializeField] QuestInfoToken[] questInfos;

    public void ResetAllState()
    {
        questListPanel.SetActive(false);
        questListBtn.SetActive(true);
        LoadQuestInfo();

        //npc Idx 및 일러스트 설정
        int npcStart = 0;
        if (GameManager.instance.slotData.chapter >= 3)
            npcStart += 2;
        if (GameManager.instance.slotData.slotClass >= 5)
            npcStart += 4;
        npcIdx[0] = npcStart; npcIdx[1] = npcStart + 1;
        for (int i = 0; i < 2; i++)
            npcImages[i].sprite = npcSprites[npcIdx[i]];

        //npc 퀘스트 아이콘 불러오기
        LoadNPCQuestIcon();

        //기계 리플레이서 21번 퀘스트 이후 숨김
        if (QuestManager.GetClearedQuest().Contains(21) || QuestManager.GetCurrQuest().Any(x => x.idx == 21))
        {
            npcImages[1].gameObject.SetActive(false);
            npcQuestIconImages[1].gameObject.SetActive(false);
        }


    }
    ///<summary> 현재 진행 중인 퀘스트 정보 불러오기, 퀘스트 리스트 판넬에 적용 </summary>
    void LoadQuestInfo()
    {
        KeyValuePair<QuestBlueprint, int>[] currQuest = QuestManager.GetProceedingQuestData();

        for (int i = 0; i < 3; i++)
            questInfos[i].SetQuestProceed(currQuest[i]);
    }
    ///<summary> NPC 퀘스트 아이콘 불러오기 </summary>
    void LoadNPCQuestIcon()
    {
        NPC[] npcs = new NPC[2] { TM.GetNPCData(npcIdx[0]), TM.GetNPCData(npcIdx[1]) };

        List<QuestProceed> proceedingQuestList = QuestManager.GetCurrQuest();
        List<int> clearedQuestList = QuestManager.GetClearedQuest();
        List<int> stateList = new List<int>();

        for (int i = 0; i < 2; i++)
        {
            stateList.Clear();
            for (int j = 0; j < npcs[i].count && stateList.Count < 4; j++)
            {
                DialogData dialog = npcs[i].dialogs[j];

                //현재 3개 이상 퀘스트 수행 중일 시, 새로운 퀘스트 관련 대화 표시 안함
                if (proceedingQuestList.Count >= 3 && dialog.kind == 1 &&
                    !proceedingQuestList.Any(x => x.idx == dialog.linkedQuest))
                    continue;

                //관련 퀘스트 수행 중 여부 알아냄
                int questState;
                if (dialog.linkedQuest == 0)
                    questState = -1;
                else
                {
                    questState = 0;
                    foreach (QuestProceed qp in proceedingQuestList)
                        if (qp.idx == dialog.linkedQuest)
                        {
                            questState = (int)qp.state;
                            break;
                        }
                }

                //1. 퀘스트 관련 대화가 아니거나 퀘스트 3개 이상 수행 중 아님
                //2. 선행 퀘스트 클리어함
                //3. 숨김 퀘스트 클리어 안함
                // 위 조건 모두 만족 시 대화에 추가
                if (IsAdd(npcs[i], j))
                    stateList.Add(questState);
            }

            int state = -1;
            for (int j = 0; j < stateList.Count; j++)
            {
                if (stateList[j] == (int)QuestState.CanClear)
                {
                    state = (int)QuestState.CanClear;
                    break;
                }
                else if (stateList[j] == (int)QuestState.NotReceive)
                    state = (int)QuestState.NotReceive;
                else if (state < 0 && stateList[j] == (int)QuestState.Proceeding)
                    state = (int)QuestState.Proceeding;
            }
            switch (state)
            {
                case 0:
                case 1:
                case 2:
                    npcQuestIconImages[i].sprite = npcQuestIconSprites[state];
                    npcQuestIconImages[i].gameObject.SetActive(true);
                    break;
                default:
                    npcQuestIconImages[i].gameObject.SetActive(false);
                    break;

            }
        }

        //선행 퀘스트, 관련 퀘스트, 레벨 조건 검사
        bool IsAdd(NPC npc, int idx)
        {
            int reqQuest = npc.dialogs[idx].reqQuest;
            int linkedQuest = npc.dialogs[idx].linkedQuest;
            //선행 퀘스트 클리어, 관련 퀘스트 클리어 안함, 레벨 넘김
            return (clearedQuestList.Contains(reqQuest) &&
                    (linkedQuest == 0 || !clearedQuestList.Contains(linkedQuest)) &&
                    GameManager.instance.slotData.lvl >= npc.dialogs[idx].lvl);
        }

    }

    ///<summary> NPC 선택 시 Script Panel로 전환 </summary>
    public void Btn_SelectNPC(int isRight) => TM.LoadDialog(npcIdx[isRight]);
}