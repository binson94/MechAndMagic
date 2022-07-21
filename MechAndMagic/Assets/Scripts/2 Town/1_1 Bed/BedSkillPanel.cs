using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BedSkillPanel : MonoBehaviour, ITownPanel
{
    [SerializeField] TownManager TM;

    [Header("Skill Button")]
    ///<summary> 스킬 버튼 토큰들 부모 오브젝트 </summary>
    [SerializeField] RectTransform skillTokenParent;
    ///<summary> pool에 존재하는 토큰들 부모 오브젝트 </summary>
    [SerializeField] RectTransform skillTokenPoolParent;
    ///<summary> 스킬 버튼 토큰 </summary>
    [SerializeField] SkillBtnToken skillTokenPrefab;
    List<SkillBtnToken> skillTokenList = new List<SkillBtnToken>();
    Queue<SkillBtnToken> skillTokenPool = new Queue<SkillBtnToken>();


    ///<summary> 0 active, 1 passive, 2 all </summary>
    int currType = 0;
    ///<summary> 0 didn't Learned, 1 Learned, 2 All </summary>
    int currLearned = 0;
    ///<summary> 0 all </summary>
    int currLvl = 0;

    [Header("Skill Selection")]
    ///<summary> 현재 선택한 장착 슬롯 정보 표시하는 UI Set </summary>
    [SerializeField] SkillInfoPanel skillSlotPanel;
    ///<summary> 현재 선택한 슬롯 idx(0 ~ 5 : active, 6 ~ 9 : passive, -1 : none) </summary>
    [SerializeField] int skillSlotIdx = -1;
    ///<summary> 장착하고자 선택한 스킬 정보 표시하는 UI Set </summary>
    [SerializeField] SkillInfoPanel selectedSkillPanel;
    ///<summary> 선택한 스킬 idx </summary>
    [SerializeField] int selectedSkillIdx = -1;
    ///<summary> 선택한 스킬 상태 </summary>
    SkillState selectedSkillState = SkillState.CantLearn;

    ///<summary> 액티브 패시브 frame sprite </summary>
    [SerializeField] Sprite[] skillFrameSprites;
    ///<summary> 스킬 아이콘 스프라이트 </summary>
    [SerializeField] Sprite[] skillIconSprites;

    [Header("Skill Manage")]
    [SerializeField] GameObject learnBtn;
    [SerializeField] GameObject equipBtn;
    [SerializeField] GameObject unequipBtn;
    [SerializeField] SkillSlot[] skillSlots;

    public void ResetAllState()
    {
        currType = 2;
        currLearned = 2;
        SkillTokenUpdate();

        skillSlotIdx = -1;
        selectedSkillIdx = -1;
        selectedSkillState = SkillState.CantLearn;
        CurrSkillSlotUpdate();
        SelectedSkillPanelUpdate();

        SlotImageUpdate();
    }

    #region Category
    public void Btn_SwitchType(int type)
    {
        currType = type;
        SkillTokenUpdate();
    }
    public void Btn_SwitchLearned(int learned)
    {
        currLearned = learned;
        SkillTokenUpdate();
    }
    public void Btn_SwitchLvl(int lv)
    {
        currLvl = lv;
        SkillTokenUpdate();
    }
    #endregion Category
    #region Token Update
    ///<summary> 카테고리에 맞는 스킬 버튼 토큰 생성 </summary>
    void SkillTokenUpdate()
    {
        SkillTokenReset();

        Skill[] s = SkillManager.GetSkillData(GameManager.instance.slotData.slotClass);

        //표시할 스킬 리스트
        List<Skill> skills = new List<Skill>();

        //표시할 스킬 리스트 제작
        for (int i = 0; i < s.Length; i++)
            //비전 마스터 음 스킬은 표시하지 않음
            if (GameManager.instance.slotData.slotClass == 7 && s[i].category == 1024)
                continue;
            else if ((currType == 2 || s[i].useType == currType) &&
                    (currLearned == 2 || !(GameManager.instance.slotData.itemData.IsLearned(s[i].idx) ^ (currLearned == 1))) &&
                    (currLvl == 0 || s[i].reqLvl == currLvl))
                skills.Add(s[i]);

        //스킬 버튼 토큰에 적용
        foreach (Skill skillToken in skills)
        {
            SkillBtnToken btnToken = GameManager.GetToken(skillTokenPool, skillTokenParent, skillTokenPrefab);
            
            skillTokenList.Add(btnToken);
            btnToken.Init(this, skillToken, GetSkillState(skillToken), skillFrameSprites[skillToken.useType]);
            btnToken.gameObject.SetActive(true);
        }
        KeyValuePair<SkillState, string> GetSkillState(Skill skill)
        {
            SkillState state;
            string expression = string.Empty;
            int learned = currLearned < 2 ? currLearned : (GameManager.instance.slotData.itemData.IsLearned(skill.idx) ? 1 : 0);

            //미학습 경우
            if (learned == 0)
            {
                state = SkillState.CanLearn;

                if (skill.reqLvl > GameManager.instance.slotData.lvl)
                {
                    state = SkillState.CantLearn;
                    expression = $"Lv.{skill.reqLvl} 필요";
                }
                else
                {
                    foreach (int skillIdx in skill.reqskills)
                    {
                        Skill s = SkillManager.GetSkill(GameManager.instance.slotData.slotClass, skillIdx);
                        if (s != null && !GameManager.instance.slotData.itemData.IsLearned(s.idx))
                        {
                            state = SkillState.CantLearn;
                            expression = $"{s.name} 학습 필요";
                            break;
                        }
                    }

                    if (!GameManager.instance.slotData.itemData.HasSkillBook(skill.idx))
                    {
                        state = SkillState.CantLearn;
                        expression = "스킬북 없음";
                    }
                }
            }
            else if (GameManager.instance.slotData.activeSkills.Contains(skill.idx) || GameManager.instance.slotData.passiveSkills.Contains(skill.idx))
                state = SkillState.Equip;
            else
                state = SkillState.Learned;

            return new KeyValuePair<SkillState, string>(state, expression);
        }
    }
    ///<summary> 현재 표시 중인 스킬 버튼 토큰들 pool로 옮김 </summary>
    void SkillTokenReset()
    {
        for (int i = 0; i < skillTokenList.Count; i++)
        {
            skillTokenList[i].gameObject.SetActive(false);
            skillTokenList[i].transform.SetParent(skillTokenPoolParent);
            skillTokenPool.Enqueue(skillTokenList[i]);
        }
        skillTokenList.Clear();
    }
    #endregion Token Update
    ///<summary> 스킬 슬롯 버튼 클릭 시 호출 </summary>
    public void Btn_SelectSkillSlot(int slotIdx)
    {
        if (skillSlotIdx == slotIdx)
            skillSlotIdx = -1;
        else
        {
            skillSlotIdx = slotIdx;
            
            Skill s = SkillManager.GetSkill(GameManager.instance.slotData.slotClass, selectedSkillIdx);
            //교차 선택 시 선택 정보 초기화
            if (s != null && (skillSlotIdx < 6 ^ s.useType == 0))
            {
                selectedSkillIdx = -1;
                selectedSkillState = SkillState.CantLearn;
                SelectedSkillPanelUpdate();
            }
        }

        equipBtn.SetActive(selectedSkillState == SkillState.Learned && skillSlotIdx >= 0);
        CurrSkillSlotUpdate();
    }
    ///<summary> 스킬 버튼 토큰 클릭 시 호출 </summary>
    public void Btn_SkillToken(int skillIdx, SkillState state)
    {
        //이미 장착 중인 스킬인 경우, 그 스킬 슬롯 선택
        for (int i = 0; i < 6; i++)
            if (GameManager.instance.slotData.activeSkills[i] == skillIdx)
            {
                Btn_SelectSkillSlot(i);
                return;
            }
        for (int i = 0; i < 4; i++)
            if (GameManager.instance.slotData.passiveSkills[i] == skillIdx)
            {
                Btn_SelectSkillSlot(i + 6);
                return;
            }

        //선택한 스킬 재선택 -> 선택 해제
        if (selectedSkillIdx == skillIdx)
        {
            selectedSkillIdx = -1;
            selectedSkillState = SkillState.CantLearn;
        }
        //스킬 선택
        else
        {
            selectedSkillIdx = skillIdx;
            selectedSkillState = state;

            Skill s = SkillManager.GetSkill(GameManager.instance.slotData.slotClass, selectedSkillIdx);
            
            //교차 선택 시 슬롯 선택 초기화
            if (s != null && (skillSlotIdx < 6 ^ s.useType == 0))
            {
                skillSlotIdx = -1;
                CurrSkillSlotUpdate();
            }
            //슬롯 선택 안 되어 있을 시 빈 슬롯 선택
            if (s != null && skillSlotIdx < 0)
            {
                for (int i = 0; i < 6 && s.useType == 0; i++)
                    if (GameManager.instance.slotData.activeSkills[i] == 0)
                    {
                        Btn_SelectSkillSlot(i);
                        break;
                    }
                for (int i = 0; i < 4 && s.useType == 1; i++)
                    if (GameManager.instance.slotData.passiveSkills[i] == 0)
                    {
                        Btn_SelectSkillSlot(i + 6);
                        break;
                    }
            }
        }

        SelectedSkillPanelUpdate();
    }

    ///<summary> 선택한 슬롯 정보 표시 </summary>
    void CurrSkillSlotUpdate()
    {
        //스킬 슬롯 선택 시
        if (skillSlotIdx >= 0)
        {
            //액티브, 패시브 정보 얻음
            int skillIdx = skillSlotIdx < 6 ? GameManager.instance.slotData.activeSkills[skillSlotIdx] : GameManager.instance.slotData.passiveSkills[skillSlotIdx - 6];

            skillSlotPanel.InfoUpdate(SkillManager.GetSkill(GameManager.instance.slotData.slotClass, skillIdx));
            skillSlotPanel.gameObject.SetActive(true);
            unequipBtn.SetActive(skillSlotIdx < 6 ? (GameManager.instance.slotData.activeSkills[skillSlotIdx] > 0) : (GameManager.instance.slotData.passiveSkills[skillSlotIdx - 6] > 0));
        }
        //스킬 슬롯 선택 취소 시
        else
        {
            skillSlotPanel.gameObject.SetActive(false);
            unequipBtn.SetActive(false);
        }
        SlotImageUpdate();
    }
    ///<summary> 선택한 스킬 정보 표시 </summary>
    void SelectedSkillPanelUpdate()
    {
        if (selectedSkillIdx > 0)
        {
            selectedSkillPanel.InfoUpdate(SkillManager.GetSkill(GameManager.instance.slotData.slotClass, selectedSkillIdx));
            selectedSkillPanel.gameObject.SetActive(true);
            
            learnBtn.SetActive(selectedSkillState == SkillState.CanLearn);
            equipBtn.SetActive(selectedSkillState == SkillState.Learned && skillSlotIdx >= 0);
        }
        else
            selectedSkillPanel.gameObject.SetActive(false);
    }
    ///<summary> 스킬 슬롯 이미지 업데이트 </summary>
    void SlotImageUpdate()
    {
        for (int i = 0; i < 10; i++)
        {
            Skill s = SkillManager.GetSkill(GameManager.instance.slotData.slotClass,
                                            i < 6 ? GameManager.instance.slotData.activeSkills[i] : GameManager.instance.slotData.passiveSkills[i - 6]);

            if (s.idx == 0)
                skillSlots[i].ImageUpdate(0, 0, skillSlotIdx == i);
            else
                skillSlots[i].ImageUpdate(s.icon, s.reqLvl, skillSlotIdx == i);
        }
    }


    public void Btn_SkillEquip()
    {
        //슬롯을 선택해야 장착 가능
        if (skillSlotIdx < 0) return;

        //액티브 장착
        if (SkillManager.GetSkill(GameManager.instance.slotData.slotClass, selectedSkillIdx).useType == 0)
            GameManager.instance.slotData.activeSkills[skillSlotIdx] = selectedSkillIdx;
        //패시브 장착
        else
            GameManager.instance.slotData.passiveSkills[skillSlotIdx - 6] = selectedSkillIdx;
        GameManager.instance.SaveSlotData();

        SlotImageUpdate();

        skillSlotIdx = -1;
        selectedSkillIdx = -1;
        selectedSkillState = SkillState.CantLearn;

        SkillTokenUpdate();
        CurrSkillSlotUpdate();
        SelectedSkillPanelUpdate();
    }
    public void Btn_SkillUnEquip()
    {
        if(skillSlotIdx < 0) return;

        //액티브 해제
        if (skillSlotIdx < 6)
            GameManager.instance.slotData.activeSkills[skillSlotIdx] = 0;
        //패시브 해제
        else
            GameManager.instance.slotData.passiveSkills[skillSlotIdx - 6] = 0;
        GameManager.instance.SaveSlotData();

        SkillTokenUpdate();
        SlotImageUpdate();
        CurrSkillSlotUpdate();
    }
    
    public void Btn_SkillLearn() => BedToSkillLearn(selectedSkillIdx);
    public void BedToSkillLearn(int skillIdx)
    {
        TM.BedToSkillLearn(skillIdx);
    }
}
