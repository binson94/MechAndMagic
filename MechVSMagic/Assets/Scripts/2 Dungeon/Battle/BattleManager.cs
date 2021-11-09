using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum BattleState { Start, Calc, AllieTurnStart, AllieSkillSelected, AllieTargetSelected, EnemyTurn, Win, Lose }

//1. 버튼과 enemy 1:1 매칭, 버튼 위치 고정
public class BattleManager : MonoBehaviour
{
    [Header("Characters")]
    #region CharList
    //전투 중인 모든 캐릭터
    List<Character> allCharList = new List<Character>();
    //아군만 저장
    List<Character> allieList = new List<Character>();
    //적군만 저장
    List<Character> monList = new List<Character>();
    #endregion

    [Header("Enemy Spawn")]
    #region Spawn
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] Transform[] enemyPos;
    RoomInfo roomInfo;
    #endregion

    [Header("Caster")]
    //캐릭터들의 TP 최대치, 전투 시작 시 계산
    [SerializeField] int[] TP = new int[6];
    //캐릭터들의 현재 TP
    [SerializeField] int[] nowTP = new int[6];

    //TP를 통해 선정된 현재 턴 실행자
    Character currCaster;

    [SerializeField] GameObject point;
    //TP가 동일할 때, 속도와 공속 기준으로 순서대로 queue에 저장
    List<Character> casterQueue = new List<Character>();
    public BattleState state;

    [Header("UI")]
    //아군 타겟 선택 관련
    [SerializeField] GameObject startBtn;         //최초 전투 시작 버튼

    [SerializeField] int skillidx;
    [SerializeField] GameObject skillSeclectUI;   //스킬 선택 UI, 내 턴에 활성화
    [SerializeField] GameObject[] skillBtns;      //각각 스킬 선택 버튼, 스킬 수만큼 활성화

    [SerializeField] GameObject targetSelectUI;   //타겟 선택 UI, 스킬 선택 시 활성화
    [SerializeField] GameObject[] targetBtns;     //각각 타겟 선택 버튼, 타겟 수만큼 활성화

    [SerializeField] GameObject winUI;
    [SerializeField] GameObject loseUI;


    #region Start
    //던전 풀에 따른 적 캐릭터 생성
    void Start()
    {
        roomInfo = new RoomInfo(PlayerPrefs.GetInt(string.Concat("Room", GameManager.instance.slotNumber), 1));

        Character tmp;
        for (int i = 0; i < roomInfo.monsterCount; i++)
        {
            tmp = Instantiate(enemyPrefabs[roomInfo.monsterIdx[i]], enemyPos[i].position, Quaternion.identity).GetComponent<Character>();
            monList.Add(tmp.GetComponent<Character>());
            allCharList.Add(tmp);
        }
    }

    //전투 시작 시 1번만 호출, 아군, 적군 정보 불러오기, 버프 및 디버프 설정, TP값 초기화
    public void BattleStart()
    {
        state = BattleState.Start;
        startBtn.SetActive(false);

        Character tmp;

        //아군 캐릭터 정보 불러오기
        GameObject[] allies = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < allies.Length; i++)
        {
            tmp = allies[i].GetComponent<Character>();
            allieList.Add(tmp);
            allCharList.Add(tmp);
        }

        foreach (Character c in allCharList)
            c.OnBattleStart();


        //버프 및 디버프 설정, 현재 미구현

        //TP 값 초기화
        for (int i = 0; i < allCharList.Count; i++)
        {
            nowTP[i] = 0;
            TP[i] = 75 - allCharList[i].buffStat[(int)StatName.SPD];
        }

        StartCoroutine(FirstCalc());
    }

    //전투 시작 시 잠시 대기 후 TP 계산 시작
    IEnumerator FirstCalc()
    {
        yield return new WaitForSeconds(1f);
        SelectNextCaster();
    }
    #endregion

    #region Calcuation
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
                for (int i = 0; i < allCharList.Count; i++)
                {
                    if (allCharList[i].isActiveAndEnabled)
                        nowTP[i]++;
                    if (nowTP[i] >= TP[i])
                        charged.Add(allCharList[i]);
                }
                yield return new WaitForSeconds(0.02f);
            }

            //TP 최대치 도달 유닛이 둘 이상인 경우
            if (charged.Count > 1)
            {
                Shuffle(charged);
                charged.Sort(delegate (Character a, Character b)
                {
                    if (a.buffStat[(int)StatName.SPD] < b.buffStat[(int)StatName.SPD])
                        return 1;
                    else if (a.buffStat[(int)StatName.SPD] > b.buffStat[(int)StatName.SPD])
                        return -1;
                    else return 0;
                });     //속도 기준 정렬

                int pivot = 0;
                int i;

                //속도가 같은 경우, 레벨 기준 정렬
                for (i = 1; i < charged.Count; i++)                  
                {
                    if (charged[pivot].buffStat[(int)StatName.SPD] > charged[i].buffStat[(int)StatName.SPD])
                    {
                        LVLSort(charged, pivot, i);
                        pivot = i;
                    }
                }
                LVLSort(charged, pivot, i);
            }

            //TP가 최대에 도달한 모든 캐릭터를 attackQueue에 저장, 다음 선정 시 TP 계산을 실시하지 않고 attackQueue에서 선정
            casterQueue = charged;

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

    void Shuffle<T>(List<T> list)
    {
        int idx = list.Count - 1;

        while(idx > 0)
        {
            int rand = Random.Range(0, idx + 1);
            T val = list[idx];
            list[idx] = list[rand];
            list[rand] = val;
            idx--;
        }
    }

    //다음 턴 행동 대상이 선정되었을 때 호출
    void TurnAct()
    {
        point.transform.position = currCaster.gameObject.transform.position + new Vector3(0, 1, 0);
        point.SetActive(true);
        

        if (allieList.Contains(currCaster))
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
    #endregion

    #region AllieTurn
    //아군 턴인 경우, 선택 UI 보이기, 캐릭터 스킬 수에 따라 버튼 활성화, AP 초기화
    void AllieTurn()
    {
        if (state != BattleState.AllieTurnStart)
            return;
        
        currCaster.OnTurnStart();
        
        for (int i = 0; i < 5; i++)
            skillBtns[i].SetActive(currCaster.activeSkills[i] > 0);

        skillSeclectUI.SetActive(true);
    }

    //스킬 선택 버튼, 타겟 선택 창 활성화
    public void AllieSkillBtn(int idx)
    {
        Skill skill = SkillManager.instance.GetSkillData(currCaster.classIdx, currCaster.activeSkills[idx]);
        Debug.Log(skill.skillName);

        if (currCaster.buffStat[(int)StatName.currAP] < skill.apCost)
        {
            Debug.Log("not enough AP");
            return;
        }
        else if (currCaster.cooldowns[idx] > 0)
        {
            Debug.Log("Cooldown");
            return;
        }

        state = BattleState.AllieSkillSelected;
        
        for (int i = 0; i < roomInfo.monsterCount; i++)
            targetBtns[i].SetActive(monList[i].isActiveAndEnabled);

        //랜덤 타겟, 전체 타겟 등 타겟 선택이 필요 없는 경우 예외 처리
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
        state = BattleState.AllieTargetSelected;
        currCaster.CastSkill(monList[idx], skillidx);

        if (monList[idx].buffStat[(int)StatName.currHP] <= 0)
            monList[idx].gameObject.SetActive(false);
        

        targetSelectUI.SetActive(false);

        bool isWin = !monList.Any(n => n.isActiveAndEnabled);

        if (isWin)
            Win();
    }

    public void AllieTurnEndBtn()
    {
        targetSelectUI.SetActive(false);
        skillSeclectUI.SetActive(false);

        nowTP[allCharList.IndexOf(currCaster)] = 0;

        StartCoroutine(AllieTurnEnd());
    }

    IEnumerator AllieTurnEnd()
    {
        point.SetActive(false);

        yield return new WaitForSeconds(1f);

        SelectNextCaster();
    }
    #endregion

    #region EnemyTurn
    //적 턴, 아군 캐릭터 대상으로 정해진 스킬 시전
    void EnemyTurn()
    {
        if (state != BattleState.EnemyTurn)
            return; 
        
        Character target;

        //무작위로 대상 선택
        target = allieList[Random.Range(0, allieList.Count)].GetComponent<Character>();

        if (target == null)
            return;

        currCaster.CastSkill(target, 0);

        if (target.buffStat[(int)StatName.currHP] <= 0)
        {
            allieList.Remove(target);

            target.gameObject.SetActive(false);
        }

        if (allieList.Count == 0)
        {
            state = BattleState.Lose;
            Lose();
        }
        else
        {
            nowTP[allCharList.IndexOf(currCaster)] = 0;
            StartCoroutine(EnemyTurnEnd());
        }
    }

    IEnumerator EnemyTurnEnd()
    {
        yield return new WaitForSeconds(1f);

        point.SetActive(false);
        SelectNextCaster();
    }
    #endregion

    #region BattleEnd
    //승리, 보상 획득, 탐험 계속 진행
    void Win()
    {
        skillSeclectUI.SetActive(false);
        targetSelectUI.SetActive(false);
        Debug.Log("win");
        winUI.SetActive(true);
    }

    public void Btn_BackToMap()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("2_0 Dungeon");
    }

    //패배, 현재까지의 보상만 가진 채 마을로 귀환
    void Lose()
    {
        skillSeclectUI.SetActive(false);
        targetSelectUI.SetActive(false);
        Debug.Log("Lose");
        loseUI.SetActive(false);
    }

    public void Btn_BackToTown()
    {
        Debug.Log("Town Scene Load");
    }
    #endregion
}
