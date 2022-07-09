using UnityEngine;
using UnityEngine.UI;

public class RepairPanel : MonoBehaviour 
{
    
    ///<summary> 스텟 표시 텍스트
    ///<para> 0 lvl, 1 exp  req, 2 ~ 11 stat </para> </summary>
    [Header("Stat")]
    [Tooltip(" 0 lvl, 1 exp / req, 2 ~ 11 stat")]
    [SerializeField] Text[] statTxts;
    ///<summary> 경험치 표시 슬라이더 </summary>
    [SerializeField] Slider expSlider;


    ///<summary> 장비 정보 표시 UI Set </summary>
    [Header("Equip")]
    [SerializeField] EquipInfoPanel equipPanel;

    [Header("Skill")]
    ///<summary> 스킬 정보 표시 텍스트들
    ///<para> 0 script, 1 pos, 2 neg </para> </summary>
    [SerializeField] Text[] skillScriptTxts;
    ///<summary> 스킬 아이콘 스프라이트 </summary>
    [SerializeField] Sprite[] skillIconSprites;
    ///<summary> 현재 장착 중인 스킬 정보 표시 </summary>
    [SerializeField] SkillBtnToken[] skillBtns;

    [Header("Drop")]
    [SerializeField] Text dropTxt;


    private void Start() {
        LoadStatInfo();
        LoadEquipInfo();
        LoadSkillInfo();
        LoadDropInfo();
    }

    public void ResetAllState()
    {
        foreach(Text t in skillScriptTxts) t.text = string.Empty;
        equipPanel.InfoUpdate(null as Equipment);
    }

    public void Btn_Equip(int part)
    {
        equipPanel.InfoUpdate(GameManager.instance.slotData.itemData.equipmentSlots[part]);
    }
    public void Btn_Skill(int skillSlotIdx)
    {
        Skill s = SkillManager.GetSlotSkill(skillSlotIdx);
        skillScriptTxts[0].text = s.script;
        skillScriptTxts[1].text = s.posScript;
        skillScriptTxts[2].text = s.negScript;
    }
    

    void LoadStatInfo()
    {
        statTxts[0].text = GameManager.instance.slotData.lvl.ToString();
        statTxts[1].text = $"{GameManager.instance.slotData.exp} / {GameManager.reqExp[GameManager.instance.slotData.lvl]}";
        expSlider.value = GameManager.instance.slotData.exp / (float)GameManager.reqExp[GameManager.instance.slotData.lvl];

        int i, j;
        for (i = j = 2; i < 13; i++, j++)
        {
            if (i == 3) i++;
            statTxts[j].text = GameManager.instance.slotData.itemStats[i].ToString();
        }

        statTxts[8].text = $"{statTxts[8].text}%";
        statTxts[9].text = $"{statTxts[9].text}%";
    }
    void LoadEquipInfo()
    {
        Debug.Log("load equip");
    }

    void LoadSkillInfo()
    {
        for(int i = 0;i < skillBtns.Length;i++)
            skillBtns[i].Init(i);
    }

    void LoadDropInfo()
    {
        dropTxt.text = "드랍 목록\n";
        foreach (Triplet<DropType, int, int> token in GameManager.instance.slotData.dungeonData.dropList)
            dropTxt.text = string.Concat(dropTxt.text, token.first, " ", token.second, " ", token.third, "\n");
    }
}