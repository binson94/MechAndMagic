using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BuffOrder
{
    public Unit caster;
    public int idx;

    static BuffOrder defaultOrder = new BuffOrder(null);
    public static BuffOrder Default { get { return defaultOrder; } }

    public BuffOrder(Unit u, int o = -1)
    {
        caster = u;
        idx = o;
    }
    public bool Equal(BuffOrder order)
    {
        if (caster == order.caster)
            return idx == order.idx;
        else
            return false;
    }
}

///<summary> 버프, 디버프 관리 클래스, 캐릭터 별 4개 존재 </summary>
public class BuffSlot
{
    public List<Buff> buffs = new List<Buff>();

    public int Count { get => buffs.Count; }

    ///<summary> 새로운 버프 추가 </summary>
    public void Add(Buff b)
    {
        var tmp = from x in buffs where x.type == b.type && x.name == b.name && x.duration == b.duration && x.isDispel == b.isDispel && x.order.Equal(b.order) select x;
        if (tmp.Count() > 0)
            tmp.First().Add(b);
        else
            buffs.Add(b);
    }
    ///<summary> 무작위로 count만큼 제거(isDispel true인 것만) </summary>
    public int Remove(int count)
    {
        if (Count <= 0)
            return 0;

        List<Buff> b = (from x in buffs where x.type != BuffType.AP && x.isDispel select x).ToList();

        if (b.Count <= count)
        {
            count = b.Count;
            foreach (Buff x in b)
            {
                buffs.Remove(x);
                LogManager.instance.AddLog($"{x.name}(이)가 제거됐습니다.");
            }
        }
        else
        {
            while (b.Count > count)
                b.RemoveAt(Random.Range(0, b.Count));

            foreach (Buff x in b)
            {
                buffs.Remove(x);
                LogManager.instance.AddLog($"{x.name}(이)가 제거됐습니다.");
            }
        }

        return count;
    }
    ///<summary> 스킬 시전 시 AP 소비량 획득 </summary>
    public void GetAPCost(ref float add, ref float mul, int category, bool isBuff)
    {
        foreach (Buff b in buffs)
            if (b.type == BuffType.AP)
                for (int i = 0; i < b.count; i++)
                    if (b.objectIdx[i] == 0 || b.objectIdx[i] == category)
                    {
                        if(isBuff)
                        {
                            if (b.isMulti[i])
                                mul += b.buffRate[i];
                            else
                                add += b.buffRate[i];
                        }
                        else
                        {
                            if (b.isMulti[i])
                                mul -= b.buffRate[i];
                            else
                                add -= b.buffRate[i];
                        }
                    }
    }
    ///<summary> 스텟 업데이트 시 스텟 버프량 계산 </summary>
    public void GetBuffRate(ref float[] add, ref float[] mul, bool isBuff)
    {
        for (int i = 0; i < buffs.Count; i++)
        {
            Buff b = buffs[i];
            if (b.type != BuffType.Stat)
                continue;

            for (int j = 0; j < b.count; j++)
            {
                if (b.objectIdx[j] <= 0 || 13 <= b.objectIdx[j])
                    continue;

                //Debug.Log($"{b.name}, {b.objectIdx[j]}, {b.buffRate[j]}, {b.isMulti[j]}");

                if(isBuff)
                {
                    if (b.isMulti[j])
                        mul[b.objectIdx[j]] += b.buffRate[j];
                    else
                        add[b.objectIdx[j]] += b.buffRate[j];
                }
                else
                {
                    if (b.isMulti[j])
                        mul[b.objectIdx[j]] -= b.buffRate[j];
                    else
                        add[b.objectIdx[j]] -= b.buffRate[j];
                }
            }

            //다음 적중 시도만 강화일 경우, buff 제거
            if (b.duration == -2)
            {
                buffs.RemoveAt(i);
                i--;
            }
        }
    }

    public void Clear() => buffs.Clear();

    ///<summary> 턴 시작 시, 버프 지속시간 업데이트 </summary>
    public void TurnUpdate()
    {
        for (int i = 0; i < buffs.Count; i++)
        {
            if (buffs[i].duration == 99)
                continue;
            buffs[i].duration--;

            if (buffs[i].duration <= 0)
                buffs.RemoveAt(i--);
        }
    }
}

public class Buff
{
    ///<summary> 버프 종류, 0 표시용, 1 스텟, 2 AP </summary>
    public BuffType type;

    ///<summary> 같은 스킬에서 걸린 버프 하나로 통합하기 위한 구분자
    ///<para> 시전자와 identifier 숫자로 구성 </para> </summary>
    public BuffOrder order;
    ///<summary> 버프 이름 </summary>
    public string name;


    ///<summary> 효과 갯수 </summary>
    public int count;
    ///<summary> 효과 대상 (stat - Obj Idx / AP - skill Category) </summary>
    public int[] objectIdx;
    ///<summary> 효과 수치 </summary>
    public float[] buffRate;
    ///<summary> 곱연산 여부 </summary>
    public bool[] isMulti;

    ///<summary> 버프 지속 시간 </summary>
    public int duration;
    ///<summary> stat - 디스펠 여부 / AP - true : turn 지속, false : 횟수 지속 </summary>
    public bool isDispel;
    ///<summary> 버프 표시 여부 </summary>
    public bool isVisible;

    public Buff(BuffType t, BuffOrder order, string _name, int obj, float stat, float rate, 
        int mul, int du, int dispel = 0, int visible = 0)
    { 
        type = t;
        this.order = order;
        name = _name;

        count = 1;
        objectIdx = new int[1];
        objectIdx[0] = obj;
        buffRate = new float[1];
        buffRate[0] = stat * rate;

        isMulti = new bool[1];
        isMulti[0] = mul == 1;

        duration = du == 0 ? 99 : du;
        isDispel = dispel == 1;
        isVisible = visible == 1;
    }
    public void Add(Buff b)
    {
        float[] tmp = new float[count];
        for (int i = 0; i < count; i++) tmp[i] = objectIdx[i];
        objectIdx = new int[count + 1];
        for (int i = 0; i < count; i++) objectIdx[i] = (int)tmp[i];
        objectIdx[count] = b.objectIdx[0];

        for (int i = 0; i < count; i++) tmp[i] = buffRate[i];
        buffRate = new float[count + 1];
        for (int i = 0; i < count; i++) buffRate[i] = tmp[i];
        buffRate[count] = b.buffRate[0];

        for (int i = 0; i < count; i++) tmp[i] = isMulti[i] ? 1 : 0;
        isMulti = new bool[count + 1];
        for (int i = 0; i < count; i++) isMulti[i] = tmp[i] == 1;
        isMulti[count++] = b.isMulti[0];
    }
}

///<summary> 44 빠르게 막기, 78 절대 방어 </summary>
public class GuardBuff
{
    ///<summary> 스킬 이름 </summary>
    public string name;

    ///<summary> 방어 횟수 </summary>
    public int shieldCount;

    ///<summary> 반격 여부 </summary>
    public bool isReturn;
    ///<summary> 방어력 상승 정도
    public float rate;

    public GuardBuff(string name = "", int buffCount = 0, bool ret = false, float rate = 0)
    {
        this.name = name;
        shieldCount = buffCount;
        isReturn = ret;
        this.rate = rate;
    }
}