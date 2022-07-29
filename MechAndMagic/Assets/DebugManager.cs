using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public void Btn_DebugEarn()
    {
        for(int i = 1;i <= 15;i++) GameManager.Instance.slotData.itemData.basicMaterials[i] += 1000;

        Skill[] skills = SkillManager.GetSkillData(GameManager.Instance.slotData.slotClass);

        for (int i = 0; i < skills.Length; i++)
            GameManager.Instance.slotData.itemData.learnedSkills.Add(skills[i].idx);

        GameManager.Instance.slotData.lvl = 10;
        GameManager.Instance.slotData.exp = 0;
        GameManager.Instance.slotData.questData.clearedQuestList.Add(25);
        GameManager.Instance.slotData.questData.clearedQuestList.Add(54);
        
        ItemManager.Debug_Recipe();
        GameManager.Instance.SaveSlotData();
    }

    public void Btn_DebugLose()
    {
        for (int i = 1; i <= 15; i++) GameManager.Instance.slotData.itemData.basicMaterials[i] = 0;

        GameManager.Instance.slotData.itemData.skillbooks.Clear();
        GameManager.Instance.slotData.itemData.equipRecipes.Clear();
        GameManager.Instance.slotData.itemData.weapons.Clear();
        GameManager.Instance.slotData.itemData.armors.Clear();
        GameManager.Instance.slotData.itemData.accessories.Clear();
        for(int i = 0;i < GameManager.Instance.slotData.itemData.equipmentSlots.Length;i++)
            GameManager.Instance.slotData.itemData.equipmentSlots[i] = null;
        ItemManager.UnEquip(EquipPart.Weapon);

        GameManager.Instance.SaveSlotData();
    }
}
