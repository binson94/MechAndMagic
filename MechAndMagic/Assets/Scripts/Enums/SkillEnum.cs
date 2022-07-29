///<summary> 스킬 학습 상태 </summary>
public enum SkillState
{
    CantLearn, CanLearn, Learned, Equip
}

public enum EffectType
{
    //액티브 - 스킬 시전 시 계산
    Damage = 1, Heal,
    Active_Buff, Active_Debuff,

    Passive_HasSkillBuff, Passive_HasSkillDebuff,

    Passive_CastBuff, Passive_CastDebuff,
    Passive_EternalBuff, Passive_EternalDebuff,
    Passive_CritHitBuff, Passive_CritHitDebuff,
    Passive_APBuff,
    Active_RemoveBuff, Active_RemoveDebuff,
    DoNothing, CharSpecial1, CharSpecial2, CharSpecial3
}

public enum Obj { None, currHP, 체력, currAP, 행동력, 공격력, 방어력, 명중률, 회피율, 치명타율, 치명타피해, 방어력무시, 속도, 
    기절, GetDmg, GiveDmg, LossPer, CurrPer, BuffCnt, DebuffCnt, MaxHP, 출혈, 화상, Cannon,
    순환, 저주, 중독, 보호막, 임플란트봄, 맹독, 악령빙의, APCost,
    쿨링히트, Magnetic, Immune, Niddle, 맹독부여, 상처 };

public enum BuffType
{
    None, Stat, AP
}