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

public enum Obj { None, currHP, HP, currAP, AP, 공격력, DEF, ACC, DOG, CRC, CRB, PEN, SPD, 
    Stun, GetDmg, GiveDmg, LossPer, CurrPer, BuffCnt, DebuffCnt, MaxHP, Bleed, Burn, Cannon,
    Cycle, Curse, Posion, Shield, Bomb, Venom, Ghost, APCost };

public enum BuffType
{
    None, Stat, AP
}