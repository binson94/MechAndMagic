using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

///<summary> 4개의 장비 버튼을 묶어 한개의 클래스에서 관리 </summary>
public class EquipBtnToken : MonoBehaviour
{
    SmithPanel SM;
    BedItemPanel BM;
    [SerializeField] Button[] btns;
    [SerializeField] Image[] icons;

    #region TokenData
    ///<summary> 0: equip, 1 : blueprint, 2 : skillbook </summary>
    int tokenKind;
    int[] equipPosIdxs = new int[4];
    Equipment[] equipments = new Equipment[4];
    EquipBluePrint[] ebps = new EquipBluePrint[4];
    Skillbook[] sbooks = new Skillbook[4];
    #endregion TokenData

    public void Init(SmithPanel s, List<KeyValuePair<int, Equipment>> p)
    {
        SM = s;
        BM = null;
        int i = 0;
        tokenKind = 0;

        for (; i < p.Count; i++)
        {
            equipPosIdxs[i] = p[i].Key;
            equipments[i] = p[i].Value;
            icons[i].sprite = SpriteGetter.instance.GetEquipIcon(p[i].Value);
            btns[i].image.sprite = SpriteGetter.instance.GetGrid(p[i].Value.ebp.rarity);
            icons[i].gameObject.SetActive(true);
        }

        for (; i < 4; i++)
            icons[i].gameObject.SetActive(false);
    }
    public void Init(SmithPanel s, List<KeyValuePair<int, EquipBluePrint>> p)
    {
        SM = s;
        BM = null;
        int i = 0;
        tokenKind = 1;

        for (; i < p.Count; i++)
        {
            equipPosIdxs[i] = p[i].Key;
            ebps[i] = p[i].Value;
            icons[i].sprite = SpriteGetter.instance.GetRecipeIcon();
            btns[i].image.sprite = SpriteGetter.instance.GetGrid(p[i].Value.rarity);
            icons[i].gameObject.SetActive(true);
        }

        for (; i < 4; i++)
            icons[i].gameObject.SetActive(false);
    }
    public void Init(SmithPanel s, List<KeyValuePair<int, Skillbook>> p)
    {
        SM = s;
        BM = null;
        int i = 0;
        tokenKind = 2;

        for (; i < p.Count; i++)
        {
            Skill skill = SkillManager.GetSkill(GameManager.instance.slotData.slotClass, p[i].Value.idx);

            equipPosIdxs[i] = p[i].Key;
            sbooks[i] = p[i].Value;
            icons[i].sprite = SpriteGetter.instance.GetSkillIcon(skill.icon);
            btns[i].image.sprite = SpriteGetter.instance.GetGrid((Rarity)(skill.reqLvl / 2 + 1));
            icons[i].gameObject.SetActive(true);
        }

        for (; i < 4; i++)
            icons[i].gameObject.SetActive(false);
    }

    public void Init(BedItemPanel b, List<KeyValuePair<int, Equipment>> p)
    {
        BM = b;
        SM = null;
        int i = 0;

        for (; i < p.Count; i++)
        {
            equipPosIdxs[i] = p[i].Key;
            equipments[i] = p[i].Value;
            icons[i].sprite = SpriteGetter.instance.GetEquipIcon(p[i].Value);
            btns[i].image.sprite = SpriteGetter.instance.GetGrid(p[i].Value.ebp.rarity);
            icons[i].gameObject.SetActive(true);
        }

        for (; i < 4; i++)
            icons[i].gameObject.SetActive(false);
    }

    public void Btn_Equip(int idx)
    {
        if (SM != null)
        {
            if (tokenKind == 0)
                SM.Btn_EquipToken(new KeyValuePair<int, Equipment>(equipPosIdxs[idx], equipments[idx]));
            else if (tokenKind == 1)
                SM.Btn_RecipeToken(ebps[idx]);
            else if(tokenKind == 2)
                SM.Btn_SkillbookToken(sbooks[idx]);
            else
                SM.Btn_FusionToken(new KeyValuePair<int, Equipment>(equipPosIdxs[idx], equipments[idx]));
        }
        else
            BM.Btn_EquipToken(new KeyValuePair<int, Equipment>(equipPosIdxs[idx], equipments[idx]));
    }
}
