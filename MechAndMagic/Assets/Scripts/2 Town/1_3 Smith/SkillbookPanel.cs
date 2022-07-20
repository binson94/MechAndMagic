using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillbookPanel : MonoBehaviour, ITownPanel
{
    [SerializeField] SmithPanel SP;
    [SerializeField] Image[] resourceImages;
    [SerializeField] Text[] resourceTxts;
    [SerializeField] Text[] disassembleTxts;
    [SerializeField] Text reqSkillTxt;

    public void ResetAllState()
    {
        List<Triplet<int, int ,int>> resources = ItemManager.GetRequireResources(SP.SelectedSkillbook.Value);

        int i;
        for(i = 0;i < resources.Count;i++)
        {
            resourceImages[i].sprite = SpriteGetter.instance.GetResourceIcon(resources[i].first);
            resourceImages[i].gameObject.SetActive(true);
            resourceTxts[i].text = $"({resources[i].second} / {resources[i].third})";
            if(resources[i].second < resources[i].third)
                resourceTxts[i].text = $"<color=#f93f3d>{resourceTxts[i].text}</color>";

            disassembleTxts[i].text = $"+{resources[i].third / 2}";
        }
        for(;i < 2;i++)
        {
            resourceImages[i].gameObject.SetActive(false);
            resourceTxts[i].text = string.Empty;
            disassembleTxts[i].text = string.Empty;
        }

        Skill skill = SkillManager.GetSkill(GameManager.instance.slotData.slotClass, SP.SelectedSkillbook.Value.idx);
        reqSkillTxt.text = string.Empty;
        for(i =0;i<3;i++)
            if (skill.reqskills[i] != 0 && GameManager.instance.slotData.itemData.learnedSkills.Contains(skill.reqskills[i]))
                reqSkillTxt.text = $"{reqSkillTxt.text}{SkillManager.GetSkill(GameManager.instance.slotData.slotClass, skill.reqskills[i]).name}\n";
            else if(skill.reqskills[i] != 0)
                reqSkillTxt.text = $"{reqSkillTxt.text}<color=#ed2929>{SkillManager.GetSkill(GameManager.instance.slotData.slotClass, skill.reqskills[i]).name}</color>\n";

        if(reqSkillTxt.text == string.Empty) reqSkillTxt.text = "없음";

    }
   
    public void Btn_SkillLearn()
    {
        if(GameManager.instance.slotData.itemData.IsLearned(SP.SelectedSkillbook.Value.idx))
        {
            Debug.Log("이미 학습했습니다.");
            return;
        }
        else if(!ItemManager.CanSkillLearn(SkillManager.GetSkill(GameManager.instance.slotData.slotClass, SP.SelectedSkillbook.Value.idx)))
            return;
        

        ItemManager.SkillLearn(SP.SelectedSkillbook);
        SP.ResetSelectInfo();
    }
    public void Btn_SkillbookDisassemble()
    {
        ItemManager.DisassembleSkillBook(SP.SelectedSkillbook);
        SP.ResetSelectInfo();
    }
}
