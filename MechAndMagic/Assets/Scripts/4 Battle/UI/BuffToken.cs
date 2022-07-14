using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuffToken : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] Image buffIconImage;
    [SerializeField] Image buffBG;
    [SerializeField] Text turnTxt;

    [SerializeField] RectTransform rect;

    PopUpManager pm;
    
    string buffExplain;

    public void SetImage(PopUpManager pm, Buff buff, bool isBuff)
    {
        this.pm = pm;
        buffIconImage.sprite = SpriteGetter.instance.GetBuffIcon((Obj)buff.objectIdx[0]);
        buffBG.sprite = SpriteGetter.instance.GetBuffBG(isBuff);

        buffExplain = $"{buff.name}({buff.duration}턴, ";
        buffExplain += buff.isDispel ? "해제 가능)" : "해제 불가)";

        for(int i = 0;i < buff.objectIdx.Length;i++)
            AddEffectExplain(buff, i, isBuff);
            
        turnTxt.text = $"{buff.duration}";
    }
    void AddEffectExplain(Buff buff, int effectIdx, bool isBuff)
    {
        switch((Obj)buff.objectIdx[effectIdx])
        {
            case Obj.기절:
                buffExplain += "\n행동할 수 없음";
                break;
            case Obj.출혈:
                buffExplain += "\n방어력 무시 지속 피해";
                break;
            case Obj.화상:
                buffExplain += "\n높은 지속 피해";
                break;
            case Obj.순환:
                buffExplain += "\n지속적인 회복";
                break;
            case Obj.저주:
                buffExplain += "\n방어력 무시 지속 피해";
                break;
            case Obj.중독:
                buffExplain += "\n체력 회복 불가";
                break;
            case Obj.보호막:
                buffExplain += "\n피해 흡수";
                break;
            case Obj.임플란트봄:
                buffExplain += "\n사망 시 폭발";
                break;
            case Obj.맹독:
                buffExplain += "\n낮은 방어력 무시 지속 피해, 크리티컬 가능";
                break;
            case Obj.악령빙의:
                buffExplain += "높은 방어력 무시 지속 피해";
                break;
            default:
                buffExplain += $"\n{(Obj)buff.objectIdx[effectIdx]} ";
                buffExplain += buff.isMulti[effectIdx] ? $"{buff.buffRate[effectIdx] * 100}%" : $"{buff.buffRate[effectIdx]}";
                buffExplain += isBuff ? " 증가" : " 감소";
                break;
        }
    }

    public void OnPointerDown(PointerEventData point)
    {
        pm.ShowPopUp(buffExplain, rect, Color.white);
    }

    public void OnPointerUp(PointerEventData point)
    {
        pm.HidePopUp();
    }
}
