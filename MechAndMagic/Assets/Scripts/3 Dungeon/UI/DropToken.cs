using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropToken : MonoBehaviour
{

    ///<summary> 등급 표기 색깔, 일반 -> 전설 오름차순, 5는 흰색 </summar>
    static readonly Color[] rareColor = new Color[]{  new Color(148f / 255, 148f / 255, 148f / 255, 1),
                                                new Color(124f / 255, 209f / 255, 232f / 255 ,1),
                                                new Color(1, 205f / 255, 95f / 255 ,1),
                                                new Color(142f / 255, 71f / 255, 221f / 255 ,1),
                                                new Color(232f / 255, 52f / 255, 52f / 255 ,1),
                                                new Color(1, 1, 1, 1)};
    ///<summary> 드랍 정보 </summary>
    [SerializeField] Image[] dropIconImages;
    [SerializeField] Text[] dropCountTxts;
    int[] colorIdx;
    string[] scripts;

    PopUpManager pm;

    ///<summary> 드랍 정보 불러오기(드랍 타입, 드랍 인덱스, 드랍 갯수) </summary>
    public void Initialize(PopUpManager pm, List<Triplet<DropType, int, int>> drops)
    {
        this.pm = pm;
        scripts = new string[drops.Count];
        colorIdx = new int[drops.Count];
        int i;
        EquipBluePrint ebp;
        for(i = 0;i < drops.Count;i++)
        {
            dropCountTxts[i].text = $"{drops[i].third}";
            switch(drops[i].first)
            {
                case DropType.Material:
                    colorIdx[i] = 5;
                    dropIconImages[i].sprite = SpriteGetter.instance.GetResourceIcon(drops[i].second);
                    scripts[i] = ItemManager.GetResourceName(drops[i].second);
                    break;
                case DropType.Equip:
                    ebp = ItemManager.GetEBP(drops[i].second);
                    dropIconImages[i].sprite = SpriteGetter.instance.GetEquipIcon(ebp);
                    colorIdx[i] = ebp.rarity - Rarity.Common;
                    scripts[i] = ebp.name;
                    break;
                case DropType.Recipe:
                    ebp = ItemManager.GetEBP(drops[i].second);
                    dropIconImages[i].sprite = SpriteGetter.instance.GetRecipeIcon();
                    colorIdx[i] = ebp.rarity - Rarity.Common;

                    if(GameManager.Instance.slotData.region == 10)
                        scripts[i] = $"설계도 : {ebp.name}";
                    else
                        scripts[i] = $"비법서 : {ebp.name}";
                    break;
                case DropType.Skillbook:
                    Skill s = SkillManager.GetSkill(GameManager.SlotClass, drops[i].second);
                    dropIconImages[i].sprite = SpriteGetter.instance.GetSkillIcon(s.icon);
                    colorIdx[i] = 5;
                    if(GameManager.Instance.slotData.region == 10)
                        scripts[i] = $"교본 : {s.name}";
                    else
                        scripts[i] = $"마법서 : {s.name}";
                    break;
            }
        }

        for (; i < dropCountTxts.Length; i++)
        {
            dropCountTxts[i].text = string.Empty;
            dropIconImages[i].gameObject.SetActive(false);
        }
    }

    public void ShowPopUp(int idx, RectTransform rect) => pm.ShowPopUp(scripts[idx], rect, rareColor[colorIdx[idx]]);
    
    public void HidePopUp() => pm.HidePopUp();
}
