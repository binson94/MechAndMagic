using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    static public BattleManager instance;
    public List<Character> inbattleChar = new List<Character>();
    public List<int> TP;
    public List<int> nowTP;

    public Character nowAttacker;
    public List<Character> attackQueue = new List<Character>();


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    //전투 시작 시 1번만 호출
    public void BattleStart()
    {
        //모든 캐릭터를 inbattleChar에 할당, 현재 미구현

        for (int i = 0; i < inbattleChar.Count; i++)    //TP 값 초기화
        {
            nowTP.Add(0);
            TP.Add(75 - inbattleChar[i].SPD);
        }

        SelectNextAttack();
    }

    //다음 공격 시전 대상 탐색 시 호출
    public void SelectNextAttack()
    {
        if (attackQueue.Count > 0)
        {
            nowAttacker = attackQueue[0];
            attackQueue.RemoveAt(0);
            Attack();
        }
        else
        {
            List<Character> charged = new List<Character>();

            while (charged.Count == 0)
            {
                for (int i = 0; i < inbattleChar.Count; i++)
                {
                    nowTP[i]++;
                    if (nowTP[i] >= TP[i])
                        charged.Add(inbattleChar[i]);
                }
            }

            //TP 최대치 도달 유닛이 둘 이상인 경우
            if (charged.Count > 1)
            {
                charged.Sort(delegate (Character a, Character b)
                {
                    if (a.SPD < b.SPD)
                        return 1;
                    else if (a.SPD > b.SPD)
                        return -1;
                    else return 0;
                });     //속도 기준 정렬

                int pivot = 0;
                int i;


                for (i = 1; i < charged.Count; i++)                  //속도가 같은 경우, 레벨 기준 정렬
                {
                    if (charged[pivot].SPD > charged[i].SPD)
                    {
                        LVLSort(charged, pivot, i);
                        pivot = i;
                    }
                }
                LVLSort(charged, pivot, i);

            }

            while(charged.Count > 0)
            {
                attackQueue.Add(charged[0]);
                charged.RemoveAt(0);
            }

            nowAttacker = attackQueue[0];
            attackQueue.RemoveAt(0);
            Attack();
        }
    }

    void Attack()
    {
        Debug.Log(nowAttacker.name + " attack");
        nowTP[inbattleChar.IndexOf(nowAttacker)] = 0;
    }

    void LVLSort(List<Character> list, int s, int e)
    {
        for(int i = 0;i<e-1;i++)
        {
            int tmp = i;
            for (int j = i + 1; j < e; j++)
                if (list[tmp].LVL < list[j].LVL)
                    tmp = j;

            Character c = list[tmp];
            list[tmp] = list[i];
            list[i] = c;
        }
    }
}
