using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//스텟 index - 10가지
public enum Stat { HP, AP, ATK, DEF, ACC, DOG, CRC, CRB, PEN, SPD };

public class Character : MonoBehaviour
{
    [Header("Stats")]
    //레벨
    public int LVL;
    //캐릭터 기본 스텟, 레벨만 따름
    public int[] basicStat = new int[10];
    //던전 입장 시 스텟 - 아이템 및 영구 적용 버프
    public int[] dungeonStat = new int[10];
    public int currHP;
    public int currAP;

    [Header("Skills")]
    public Skill[] activeSkills = new Skill[5];
    public Skill[] passiveSkills = new Skill[3];

    [Header("Buffs")]
    //영구 적용 버프, 버프 해제 먹지 않음, 던전 입장 시 계산
    public List<Buff> eternalBuffList;

    //순간 적용 버프, 버프 해제 먹음, 
    public List<Buff> limitedBuffList;

    //던전 입장 시 호출 - 영구 적용 버프/디버프 적용 및 
    public void OnDungeonEnter()
    {
        //eternalBuffList에 영구 적용 버프 추가

        //dungeonStat Update : (basicStat + 
    }

    //랜덤 타겟 or 전체 타겟 스킬
    public virtual void CastSkill(int idx)
    {

    }

    //타겟 지정 스킬
    public virtual void CastSkill(Character target, int idx)
    {
        if (activeSkills[idx] == null)
        {
            Debug.LogError("skill is null");
            return;
        }

        for (int i = 0; activeSkills[idx].skillEffectType[i] != 0; i++)
        {
            switch(activeSkills[idx].skillEffectType[i])
            {
                case 5:
                    int dmg = dungeonStat[(int)Stat.ATK];//+ itemStat * itemRate + itemAdd ) * buffRate + buffAdd
                    target.currHP -= dmg;
                    Debug.Log(string.Concat(name, " cast skill, skill type : ", 5, ", ", target.name, " get damage ", dmg));
                    break;
                default:
                    break;
            }
        }

        currAP -= activeSkills[idx].skillAPCost;
    }
}
