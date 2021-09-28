using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState { Start, Calc, AllieTurnStart, AllieSkillSelected, AllieTargetSelected, EnemyTurn, Win, Lose }

public class BattleManager : MonoBehaviour
{
    //전투 중인 모든 캐릭터
    public List<Character> inbattleChar = new List<Character>();

    //아군, 적군 구분 저장
    public List<Character> inbattleAllie = new List<Character>();
    public List<Character> inbattleEnemy = new List<Character>();

    //캐릭터들의 TP 최대치, 전투 시작 시 계산
    public List<int> TP;
    //캐릭터들의 현재 TP
    public List<int> nowTP;

    //TP를 통해 선정된 현재 턴 실행자
    public Character currCaster;
    //TP가 동일할 때, 속도와 공속 기준으로 순서대로 queue에 저장
    public List<Character> casterQueue = new List<Character>();
    public BattleState state;

    public GameObject startBtn;
    public GameObject point;

    //아군 타겟 선택 관련
    public int skillidx;
    public GameObject skillUI;
    public GameObject targetSelectUI;
    public GameObject[] skillBtns;
    public GameObject[] targetBtns;


    //전투 시작 시 1번만 호출, 아군, 적군 정보 불러오기, 버프 및 디버프 설정, TP값 초기화
    public void BattleStart()
    {
        state = BattleState.Start;
        startBtn.SetActive(false);

        Character tmp;

        //아군 캐릭터 정보 불러오기
        GameObject[] alies = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < alies.Length; i++)
        {
            tmp = alies[i].GetComponent<Character>();
            inbattleAllie.Add(tmp);
            inbattleChar.Add(tmp);
        }

        //적군 캐릭터 정보 불러오기
        GameObject[] enemys = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemys.Length; i++)
        {
            tmp = enemys[i].GetComponent<Character>();
            inbattleEnemy.Add(tmp);
            inbattleChar.Add(tmp);
        }
        inbattleEnemy.Sort(delegate (Character a, Character b) {
            if (a.transform.position.x > b.transform.position.x)
                return 1;
            else if (a.transform.position.x < b.transform.position.x)
                return -1;
            else
                return 0;
        });
       

        //버프 및 디버프 설정, 현재 미구현

        //TP 값 초기화
        for (int i = 0; i < inbattleChar.Count; i++)    //TP 값 초기화
        {
            nowTP.Add(0);
            TP.Add(75 - inbattleChar[i].SPD);
        }

        StartCoroutine(FirstCalc());
    }

    //전투 시작 시 잠시 대기 후 TP 계산 시작
    IEnumerator FirstCalc()
    {
        yield return new WaitForSeconds(1f);
        SelectNextCaster();
    }

    //다음 턴 시전자 탐색
    public void SelectNextCaster()
    {
        if (state == BattleState.Calc)
            return;

        StartCoroutine(TPCalcuate());
    }

    //TP 계산
    IEnumerator TPCalcuate()
    {
        state = BattleState.Calc;

        //TP가 찬 캐릭터가 이미 있는 경우
        if (casterQueue.Count > 0)
        {
            currCaster = casterQueue[0];
            casterQueue.RemoveAt(0);
            TurnAct();

            yield break;
        }
        else
        {
            List<Character> charged = new List<Character>();

            //TP 상승
            while (charged.Count == 0)
            {
                for (int i = 0; i < inbattleChar.Count; i++)
                {
                    if (inbattleChar[i].isActiveAndEnabled)
                        nowTP[i]++;
                    if (nowTP[i] >= TP[i])
                        charged.Add(inbattleChar[i]);
                }
                yield return new WaitForSeconds(0.02f);
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

            //TP가 최대에 도달한 모든 캐릭터를 attackQueue에 저장, 다음 선정 시 TP 계산을 실시하지 않고 attackQueue에서 선정
            while (charged.Count > 0)
            {
                casterQueue.Add(charged[0]);
                charged.RemoveAt(0);
            }

            currCaster = casterQueue[0];
            casterQueue.RemoveAt(0);
            TurnAct();

            yield break;
        }

        //레벨 순 정렬
        void LVLSort(List<Character> list, int s, int e)
        {
            for (int i = 0; i < e - 1; i++)
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

    //다음 턴 행동 대상이 선정되었을 때 호출
    void TurnAct()
    {
        point.transform.position = currCaster.gameObject.transform.position + new Vector3(0, 1, 0);
        point.SetActive(true);

        //플레이어면 적을 대상, 적이면 플레이어를 대상으로 선택, 현재는 플레이어도 랜덤으로 선택(차후 수정)
        if (inbattleAllie.Contains(currCaster))
        {
            state = BattleState.AllieTurnStart;
            AllieTurn();
        }
        else
        {
            state = BattleState.EnemyTurn;
            EnemyTurn();
        }
    }

    //아군 턴인 경우, 선택 UI 보이기, 캐릭터 스킬 수에 따라 버튼 활성화, AP 초기화
    void AllieTurn()
    {
        if (state != BattleState.AllieTurnStart)
            return;

        //AP 회복
        currCaster.currAP = currCaster.AP;
        skillUI.SetActive(true);
        //스킬 수 만큼 스킬 버튼 활성화, 현재 미구현
    }

    //스킬 선택 버튼, 타겟 선택 창 활성화
    public void AllieSkillBtn(int idx)
    {
        if(currCaster.currAP < 2)
        {
            Debug.Log("not enough AP");
            return;
        }

        state = BattleState.AllieSkillSelected;

        int i;
        for (i = 0; i < inbattleEnemy.Count; i++)
            targetBtns[i].SetActive(true);
        for (; i < 3; i++)
            targetBtns[i].SetActive(false);

        targetSelectUI.SetActive(true);

        skillidx = idx;
    }

    //스킬 선택 취소 버튼, 스킬 선택 전 상태로 돌아감
    public void AllieSkillCancelBtn()
    {
        state = BattleState.AllieTurnStart;
        targetSelectUI.SetActive(false);
    }

    //타겟 선택 버튼, 스킬 시전
    public void AllieTargetBtn(int idx)
    {
        bool isDie;

        state = BattleState.AllieTargetSelected;
        isDie = currCaster.CastSkill(inbattleEnemy[idx], skillidx);

        if(isDie)
        {
            inbattleEnemy[idx].gameObject.SetActive(false);
            inbattleEnemy.RemoveAt(idx);
        }

        targetSelectUI.SetActive(false);

        if (inbattleEnemy.Count == 0)
            Win();
    }

    public void AllieTurnEndBtn()
    {
        targetSelectUI.SetActive(false);
        skillUI.SetActive(false);

        nowTP[inbattleChar.IndexOf(currCaster)] = 0;

        StartCoroutine(AllieTurnEnd());
    }

    IEnumerator AllieTurnEnd()
    {
        point.SetActive(false);

        yield return new WaitForSeconds(1f);

        SelectNextCaster();
    }

    //적 턴, 아군 캐릭터 대상으로 정해진 스킬 시전
    void EnemyTurn()
    {
        if (state != BattleState.EnemyTurn)
            return; 

        bool isDie;
        Character target;

        //무작위로 대상 선택
        target = inbattleAllie[Random.Range(0, inbattleAllie.Count)].GetComponent<Character>();

        if (target == null)
            return;

        isDie = currCaster.Attack(target);

        if (isDie)
        {
            inbattleAllie.Remove(target);

            target.gameObject.SetActive(false);
        }

        if (inbattleAllie.Count == 0)
        {
            state = BattleState.Lose;
            Lose();
        }
        else
        {
            nowTP[inbattleChar.IndexOf(currCaster)] = 0;
            StartCoroutine(EnemyTurnEnd());
        }
    }

    IEnumerator EnemyTurnEnd()
    {
        yield return new WaitForSeconds(1f);

        point.SetActive(false);
        SelectNextCaster();
    }

    //승리, 보상 획득, 탐험 계속 진행
    void Win()
    {
        Debug.Log("win");
    }

    //패배, 현재까지의 보상만 가진 채 마을로 귀환
    void Lose()
    {
        Debug.Log("Lose");
    }
}
