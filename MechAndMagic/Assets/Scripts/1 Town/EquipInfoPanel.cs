using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipInfoPanel : MonoBehaviour
{
    [SerializeField] Text equipName;

    public void InfoUpdate(Equipment e)
    {
        if (e != null)
            equipName.text = string.Concat(e.star, " ", e.ebp.name);
    }

    public void InfoUpdate(Skillbook s)
    {
        if (s != null)
            equipName.text = string.Concat("교본 : ", SkillManager.GetSkill(GameManager.slotData.slotClass, s.idx).name);
    }

    public void InfoUpdate(EquipBluePrint ebp)
    {
        if (ebp != null)
            equipName.text = ebp.name;
    }
}
