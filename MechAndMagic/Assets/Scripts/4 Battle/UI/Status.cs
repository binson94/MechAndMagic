using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Status : MonoBehaviour
{
    ///<summary> 캐릭터 이름 표기 텍스트 </summary>
    [SerializeField] Text nameTxt;
    ///<summary> 캐릭터 레벨 표기 텍스트 </summary>
    [SerializeField] Text lvlTxt;

    ///<summary> 캐릭터 체력 표기 슬라이더 </summary>
    [SerializeField] Slider hpBar;
    ///<summary> 캐릭터 보호막 표기 슬라이더 </summary>
    [SerializeField] Slider shieldBar;
    ///<summary> 캐릭터 체력 표기 텍스트 </summary>
    [SerializeField] Text hpTxt;
    ///<summary> 특수 자원 텍스트(블래스터, 드루이드) </summary>
    [SerializeField] Text specialResourceTxt;

    [SerializeField] BattleManager BM;
    [SerializeField] RectTransform buffTokenParent;
    [SerializeField] PopUpManager PM;
    List<BuffToken> buffTokens = new List<BuffToken>();

    ///<summary> 전투 중 호출, 체력 슬라이더 설정, 버프 설정 </summary>
    public void StatusUpdate(Unit u)
    {
        nameTxt.text = u.name;
        lvlTxt.text = $"{u.LVL}";

        int curr = Mathf.Max(0, u.buffStat[(int)Obj.currHP]);
        hpBar.value = (float)curr / u.buffStat[(int)Obj.체력];
        hpTxt.text = $"{curr}";

        shieldBar.value = (float)u.shieldAmount / u.buffStat[(int)Obj.체력];
        if(u.shieldAmount > 0)
            hpTxt.text = $"{hpTxt.text}+<color=#F9DC3C>{u.shieldAmount}</color>";

        Blaster b;
        Druid d;
        if((b = u as Blaster) != null && b.currHeat > 0)
            specialResourceTxt.text = $"열기 : {b.currHeat}";
        else if((d = u as Druid) != null && d.currVitality > 0)
            specialResourceTxt.text = $"생명력 : {d.currVitality}";
        else if(specialResourceTxt != null)
            specialResourceTxt.text = string.Empty;


        if(curr == 0)
        {
            foreach(BuffToken token in buffTokens)
            {
                token.gameObject.SetActive(false);
                token.transform.SetParent(BM.poolParent);
                BM.buffTokenPool.Enqueue(token);
            }
            buffTokens.Clear();
        }
        else
            BuffImageUpdate(u);
    }

    void BuffImageUpdate(Unit u)
    {
        foreach(BuffToken token in buffTokens)
        {
            token.gameObject.SetActive(false);
            token.transform.SetParent(BM.poolParent);
            BM.buffTokenPool.Enqueue(token);
        }
        buffTokens.Clear();

        var buffs = from x in u.turnBuffs.buffs where x.isVisible select x;

        foreach(Buff b in buffs)
        {
            BuffToken token = GameManager.GetToken(BM.buffTokenPool, buffTokenParent, BM.buffTokenPrefab);
            buffTokens.Add(token);
            token.Initialize(PM, b, true);
            token.gameObject.SetActive(true);
        }

        MetalKnight mk;
        if((mk = u as MetalKnight) != null)
        {
            foreach(GuardBuff b in mk.guardBuffList)
            {
                BuffToken token = GameManager.GetToken(BM.buffTokenPool, buffTokenParent, BM.buffTokenPrefab);
                buffTokens.Add(token);
                token.Initialize(PM, b);
                token.gameObject.SetActive(true);
            }
        }

        var debuffs = from x in u.turnDebuffs.buffs where x.isVisible select x;
        foreach(Buff b in debuffs)
        {
            BuffToken token = GameManager.GetToken(BM.buffTokenPool, buffTokenParent, BM.buffTokenPrefab);
            buffTokens.Add(token);
            token.Initialize(PM, b, false);
            token.gameObject.SetActive(true);
        }
    }
}
