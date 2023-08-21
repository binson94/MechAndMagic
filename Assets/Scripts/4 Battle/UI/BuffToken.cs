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

    public void Initialize(PopUpManager pm, Buff buff, bool isBuff)
    {
        this.pm = pm;
        if(buff.objectIdx[0] == (int)Obj.쿨링히트)
            buffIconImage.sprite = SpriteGetter.instance.GetBuffIcon(Obj.기절);
        else if(buff.objectIdx[0] == (int)Obj.Magnetic)
            buffIconImage.sprite = SpriteGetter.instance.GetBuffIcon(Obj.보호막);
        else if (buff.objectIdx[0] == (int)Obj.Immune)
            buffIconImage.sprite = SpriteGetter.instance.GetBuffIcon(Obj.보호막);
        else if(buff.objectIdx[0] == (int)Obj.Niddle)
            buffIconImage.sprite = SpriteGetter.instance.GetBuffIcon(Obj.방어력);
        else if(buff.objectIdx[0] == (int)Obj.맹독부여)
            buffIconImage.sprite = SpriteGetter.instance.GetBuffIcon(Obj.맹독);
        else if (buff.objectIdx[0] == (int)Obj.상처)
            buffIconImage.sprite = SpriteGetter.instance.GetBuffIcon(Obj.출혈);
        else if (buff.type == BuffType.AP)
            buffIconImage.sprite = SpriteGetter.instance.GetBuffIcon(Obj.행동력);
        else
            buffIconImage.sprite = SpriteGetter.instance.GetBuffIcon((Obj)Mathf.Min(30, buff.objectIdx[0]));

        buffBG.sprite = SpriteGetter.instance.GetBuffBG(isBuff);

        buffExplain = $"{buff.name}({buff.duration}턴, ";
        buffExplain += buff.isDispel ? "해제 가능)" : "해제 불가)";

        for(int i = 0;i < buff.objectIdx.Length;i++)
            AddEffectExplain(buff, i, isBuff);
            
        turnTxt.text = $"{buff.duration}";
    }
    public void Initialize(PopUpManager pm, GuardBuff buff)
    {
        this.pm = pm;
        buffIconImage.sprite = SpriteGetter.instance.GetBuffIcon(Obj.방어력);
        buffBG.sprite = SpriteGetter.instance.GetBuffBG(true);

        buffExplain = $"{buff.name}";
        buffExplain += $"\n방어력 {buff.rate * 100}% 증가";
    }
    public void Initialize(PopUpManager pm, DungeonBuff buff, bool isBuff)
    {
        this.pm = pm;
        buffIconImage.sprite = SpriteGetter.instance.GetBuffIcon((Obj)Mathf.Min(31, buff.objIdx));
        buffBG.sprite = SpriteGetter.instance.GetBuffBG(isBuff);

        buffExplain = $"{buff.name}({buff.count}회 전투 지속)\n";
        buffExplain += $"{(Obj)buff.objIdx} {(int)(buff.rate * 100)}% ";
        buffExplain += isBuff ? "증가" : "감소";
    }
    void AddEffectExplain(Buff buff, int effectIdx, bool isBuff)
    {
        if (buff.type == BuffType.AP)
        {
            buffExplain += $"\n{GetCategoryName(buff.objectIdx[effectIdx])} 스킬 행동력 소모량 ";
            buffExplain += buff.isMulti[effectIdx] ? $"{buff.buffRate[effectIdx] * 100}%" : $"{buff.buffRate[effectIdx]}";
            buffExplain += isBuff ? " 감소" : " 증가";
        }
        else
            switch ((Obj)buff.objectIdx[effectIdx])
            {
                case Obj.currHP:
                    buffExplain += "\n지속적인 회복";
                    break;
                case Obj.기절:
                    buffExplain += "\n행동할 수 없음";
                    break;
                case Obj.출혈:
                    buffExplain += "\n방어력 무시 지속 피해";
                    break;
                case Obj.화상:
                    buffExplain += "\n높은 지속 피해";
                    break;
                case Obj.Cannon:
                    buffExplain += "\n포탄을 장전하였습니다.";
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
                    buffExplain += "\n높은 방어력 무시 지속 피해";
                    break;
                case Obj.쿨링히트:
                    buffExplain += "\n공격할 수 없음";
                    break;
                case Obj.Magnetic:
                    buffExplain += "\n대상으로 우선 지정됨";
                    break;
                case Obj.Immune:
                    buffExplain += "\n피해 면역";
                    break;
                case Obj.Niddle:
                    buffExplain += "\n피해 반사";
                    break;
                case Obj.맹독부여:
                    buffExplain += $"\n다음 {(int)buff.buffRate[effectIdx]}회 공격이 맹독 부여";
                    break;
                case Obj.상처:
                    buffExplain += "\n공격 대상의 치명타율 증가";
                    break;
                case Obj.치명타율:
                case Obj.치명타피해:
                case Obj.방어력무시:
                    buffExplain += $"\n{(Obj)buff.objectIdx[effectIdx]} ";
                    buffExplain += buff.isMulti[effectIdx] ? $"{buff.buffRate[effectIdx] * 100}%" : $"{buff.buffRate[effectIdx]}%p";
                    buffExplain += isBuff ? " 증가" : " 감소";
                    break;
                default:
                    buffExplain += $"\n{(Obj)buff.objectIdx[effectIdx]} ";
                    buffExplain += buff.isMulti[effectIdx] ? $"{buff.buffRate[effectIdx] * 100}%" : $"{buff.buffRate[effectIdx]}";
                    buffExplain += isBuff ? " 증가" : " 감소";
                    break;
            }
    }
    string GetCategoryName(int category)
    {
        switch(category)
        {
            case 0:
                return "모든";
            case 1000:
                return "주먹 계열";
            case 1001:
                return "발 계열";
            case 1007:
                return "불 계열";
            case 1008:
                return "물 계열";
            case 1009:
                return "바람 계열";
            case 1011:
                return "다중 원소 계열";
            default:
                return string.Empty;
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
