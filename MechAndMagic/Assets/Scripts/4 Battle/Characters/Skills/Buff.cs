using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BuffOrder
{
    public Unit caster;
    public int idx;

    public BuffOrder(Unit u = null, int o = -1)
    {
        caster = u;
        idx = o;
    }
    public bool Equal(BuffOrder order)
    {
        if (caster == null && order.caster != null || caster != null && order.caster == null)
            return false;

        if (caster == order.caster)
            return idx == order.idx;
        else
            return false;
    }
}

public class BuffSlot
{
    public List<Buff> buffs = new List<Buff>();

    public int Count { get => buffs.Count; }

    public void Add(Buff b)
    {
        var tmp = from x in buffs where x.type == b.type && x.name == b.name && x.order.Equal(b.order) select x;
        if (tmp.Count() > 0)
            tmp.First().Add(b);
        else
            buffs.Add(b);
    }
    public int Remove(int count)
    {
        if (Count <= 0)
            return 0;

        List<Buff> b = (from x in buffs where x.type != BuffType.AP && x.isDispel select x).ToList();

        if (b.Count <= count)
        {
            count = b.Count;
            foreach (Buff x in b)
                buffs.Remove(x);
        }
        else
        {
            while (b.Count > count)
                b.RemoveAt(Random.Range(0, b.Count));

            foreach (Buff x in b)
                buffs.Remove(x);
        }

        return count;
    }
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
    public void GetBuffRate(ref float[] add, ref float[] mul, bool isBuff)
    {
        for (int i = 0; i < buffs.Count; i++)
        {
            Buff b = buffs[i];
            if (b.type != BuffType.Stat)
                continue;

            for (int j = 0; j < b.count; j++)
            {
                if (b.objectIdx[j] <= 0 || 12 <= b.objectIdx[j])
                    continue;

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

[System.Serializable]
public class Buff
{
    public BuffType type;

    public int lvl;
    public BuffOrder order;    //같은 스킬에서 걸린 버프는 같은 orderIdx
    public string name;     //버프 이름 - 스킬 이름을 따름


    public int count;       //효과 갯수
    public int[] objectIdx; //stat - Obj Idx / AP - skill Category
    public float[] buffRate;//버프 정도
    public bool[] isMulti;  //true : 곱연산, false : 합연산

    public int duration;    //
    public bool isDispel;   //stat - 디스펠 여부 / AP - true : turn 지속, false : 횟수 지속
    public bool isVisible;

    public Buff(BuffType t, int lvl, BuffOrder order, string _name, int obj, float stat, float rate, 
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
        for (int i = 0; i < count; i++) buffRate[i] = (int)tmp[i];
        buffRate[count] = b.buffRate[0];

        for (int i = 0; i < count; i++) tmp[i] = isMulti[i] ? 1 : 0;
        isMulti = new bool[count + 1];
        for (int i = 0; i < count; i++) isMulti[i] = tmp[i] == 1;
        isMulti[count] = b.isMulti[0];
    }
}

public class GuardBuff
{
    public string name;

    //버프 횟수
    public int buffTime;

    //반격 여부
    public bool isReturn;

    public int objectIdx;
    public float rate;
    public bool isMulti;

    public GuardBuff(string name = "", int time = 0, bool ret = false, int obj = 0, float rate = 0, bool mul = false)
    {
        this.name = name;
        buffTime = time;
        isReturn = ret;
        objectIdx = obj;
        this.rate = rate;
        isMulti = mul;
    }
}