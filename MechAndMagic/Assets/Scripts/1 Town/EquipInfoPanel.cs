using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

///<summary> 숙소, 대장간에서 장비 정보 표현하는 UI Set 클래스 </summary>
public class EquipInfoPanel : MonoBehaviour
{
    ///<summary> 아이템 정보 표기 텍스트들
    ///<para> 0 name, 1 lvl, 2 rarity, 3 stat, 4 common stat </para> </summary>
    [Tooltip("0 name, 1 lvl, 2 rarity, 3 stat, 4 common stat")] [SerializeField] Text[] itemTxts;

    ///<summary> 아이템 등급 나타내는 테두리 이미지 </summary>
    [SerializeField] Image gridImage;
    ///<summary> 아이템 아이콘 이미지 </summary>
    [SerializeField] Image iconImage;

    ///<summary> 등급 표기 색깔, 일반 -> 전설 오름차순 </summar>
    readonly Color[] rareColor = new Color[5]{  new Color(148f / 255, 148f / 255, 148f / 255 ,1),
                                                new Color(124f / 255, 209f / 255, 232f / 255 ,1),
                                                new Color(1, 205f / 255, 95f / 255 ,1),
                                                new Color(142f / 255, 71f / 255, 221f / 255 ,1),
                                                new Color(232f / 255, 52f / 255, 52f / 255 ,1)};

    public void InfoUpdate(Equipment e)
    {
        if (e != null)
        {
            itemTxts[0].text = e.ebp.name;
            itemTxts[1].text = $"Lv.{e.ebp.reqlvl}";
            
            switch(e.ebp.rarity)
            {
                case Rarity.Common:
                    itemTxts[2].text = "일반";
                    break;
                case Rarity.Uncommon:
                    itemTxts[2].text = "고급";
                    break;
                case Rarity.Rare:
                    itemTxts[2].text = "고유";
                    break;
                case Rarity.Unique:
                    itemTxts[2].text = "고유";
                    break;
                case Rarity.Legendary:
                    itemTxts[2].text = "전설";
                    break;
            }
            itemTxts[2].color = rareColor[e.ebp.rarity - Rarity.Common];
            itemTxts[3].text = $"{e.mainStat}\t+{e.mainStatValue}\n";
            if(e.subStat != Obj.None)
                itemTxts[3].text += $"{e.subStat}\t+{e.subStatValue}";

            itemTxts[4].text = string.Empty;
            for(int i = 0;i < e.commonStatValue.Count;i++)
                itemTxts[4].text += $"{e.commonStatValue[i].Key}\t+{e.commonStatValue[i].Value}\n";
            
            gridImage.sprite = SpriteGetter.instance.GetGrid(e.ebp.rarity);
            iconImage.sprite = SpriteGetter.instance.GetEquipIcon(e);
            gridImage.gameObject.SetActive(true); iconImage.gameObject.SetActive(true);
        }
        else
        {
            foreach (Text t in itemTxts) t.text = string.Empty;
            gridImage.gameObject.SetActive(false); iconImage.gameObject.SetActive(false);
        }
    }

    public void InfoUpdate(Skillbook skillbook)
    {
        foreach(Text t in itemTxts) t.text = string.Empty;
        Skill skill = SkillManager.GetSkill(GameManager.instance.slotData.slotClass, skillbook.idx);
        if (skill != null)
        {
            itemTxts[0].text = $"교본 : {skill.name}";
            gridImage.sprite = SpriteGetter.instance.GetGrid((Rarity)(skill.reqLvl / 2 + 1));
            iconImage.sprite = SpriteGetter.instance.GetSkillIcon(skill.icon);
            gridImage.gameObject.SetActive(true); iconImage.gameObject.SetActive(true);
        }
        else
        {
            gridImage.gameObject.SetActive(false); iconImage.gameObject.SetActive(false);
        }
    }

    public void InfoUpdate(EquipBluePrint ebp)
    {
        foreach(Text t in itemTxts) t.text = string.Empty;
        if (ebp != null)
        {
            itemTxts[0].text = $"제작법 : {ebp.name}";
            gridImage.sprite = SpriteGetter.instance.GetGrid(ebp.rarity);
            iconImage.sprite = SpriteGetter.instance.GetRecipeIcon();
            gridImage.gameObject.SetActive(true); iconImage.gameObject.SetActive(true);
        }
        else
        {
            gridImage.gameObject.SetActive(false); iconImage.gameObject.SetActive(false);
        }
    }
}
