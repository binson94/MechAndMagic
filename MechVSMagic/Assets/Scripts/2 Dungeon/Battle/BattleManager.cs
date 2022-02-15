using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public enum BattleState { Start, Calc, AllieTurnStart, AllieSkillSelected, AllieTargetSelected, EnemyTurn, Win, Lose }

public class CharacterState
{
    public int currHP;

    public int golemHP;
    public int druidRevive;
    public List<Buff> eternalBuffList = new List<Buff>();
    public List<Buff> eternalDebuffList = new List<Buff>();

    public CharacterState()
    {
        currHP = -1;
        druidRevive = 0;
        golemHP = GameManager.slotData.slotClass == 4 ? 0 : -1;
    }
}

//1. 버튼과 enemy 1:1 매칭, 버튼 위치 고정
public class BattleManager : MonoBehaviour
{
    /* #region CharList */
    //전투 중인 모든 캐릭터
    List<Unit> allCharList = new List<Unit>();
    //아군만 저장
    List<Character> characterList = new List<Character>();
    CharacterState charState;
    //적군만 저장
    List<Monster> monsterList = new List<Monster>();
    /* #endregion */

    /* #region Spawn */
    [Header("Allie Spawn")]
    [SerializeField] GameObject[] alliePrefabs;
    [SerializeField] Transform alliePos;

    [Header("Enemy Spawn")]
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] Transform[] enemyPos;
    RoomInfo roomInfo;
    /* #endregion */

    /* #region Caster */
    [Header("Caster")]
    //캐릭터들의 TP 최대치, 전투 시작 시 계산
    [SerializeField] Dictionary<Unit, int[]> charTP = new Dictionary<Unit, int[]>();
    [SerializeField] Slider[] TPBars;

    //TP를 통해 선정된 현재 턴 실행자
    Unit currCaster;

    [SerializeField] GameObject point;
    //TP가 동일할 때, 속도와 공속 기준으로 순서대로 queue에 저장
    List<Unit> casterQueue = new List<Unit>();
    [SerializeField] List<int> targetIdxs = new List<int>();
    /* #endregion */
    
    BattleState state;

    /* #region UI */
    [Header("UI")]
    //아군 타겟 선택 관련
    [SerializeField] GameObject startBtn;         //최초 전투 시작 버튼
    [SerializeField] GameObject turnEndBtn;

    int skillidx;
    [SerializeField] GameObject skillBtnPanel;    //스킬 선택 UI, 내 턴에 활성화
    [SerializeField] GameObject[] skillBtns;      //각각 스킬 선택 버튼, 스킬 수만큼 활성화

    bool isBoth = false;
    int isMinus = 0;
    [SerializeField] GameObject skillChoosePanel; //비전 마스터 스킬 선택

    [SerializeField] GameObject targetBtnPanel;   //타겟 선택 UI, 스킬 선택 시 활성화
    [SerializeField] GameObject[] targetBtns;     //각각 타겟 선택 버튼, 타겟 수만큼 활성화

    [SerializeField] GameObject winUI;
    [SerializeField] GameObject bossWinUI;
    [SerializeField] GameObject loseUI;
    /* #endregion */

    /* #region Function_Start */
    void Start()
    {
        GameManager.sound.PlayBGM(BGM.Battle1);
        if (GameManager.slotData.dungeonRoom > 100)
            roomInfo = new RoomInfo(1);
        else
            roomInfo = new RoomInfo(GameManager.slotData.dungeonRoom);

        Monster mon;
        //던전 풀에 따른 적 캐릭터 생성
        for (int i = 0; i < roomInfo.monsterCount; i++)
        {
            mon = Instantiate(enemyPrefabs[roomInfo.monsterIdx[i]], enemyPos[i].position, Quaternion.identity).GetComponent<Monster>();
            monsterList.Add(mon);
            allCharList.Add(mon);
        }

        //아군 캐릭터 생성
        Character c = Instantiate(alliePrefabs[GameManager.slotData.slotClass], alliePos.position, Quaternion.identity).GetComponent<Character>();
        for (int i = 0; i < c.activeIdxs.Length; i++)
            c.activeIdxs[i] = GameManager.slotData.activeSkills[i];
        for (int i = 0; i < c.passiveIdxs.Length; i++)
            c.passiveIdxs[i] = GameManager.slotData.passiveSkills[i];

        characterList.Add(c);
        allCharList.Add(c);

        if (PlayerPrefs.HasKey(string.Concat("CharState", GameManager.currSlot)))
            charState = LitJson.JsonMapper.ToObject<CharacterState>(PlayerPrefs.GetString(string.Concat("CharState", GameManager.currSlot)));
        else
            charState = new CharacterState();

        if(charState.golemHP >= 0)
        {
            c = Instantiate(alliePrefabs[11], alliePos.position + new Vector3(1, 0, 0), Quaternion.identity).GetComponent<Golem>();
            c.GetComponent<Golem>().GolemInit(characterList[0].GetComponent<MadScientist>());
            characterList.Add(c);
            allCharList.Add(c);
        }


        for (int i = 0; i < skillBtns.Length; i++)
            skillBtns[i].SetActive(GameManager.slotData.activeSkills[i] > 0);
        for (int i = 0; i < TPBars.Length; i++)
            TPBars[i].gameObject.SetActive(i < allCharList.Count && allCharList[i].isActiveAndEnabled);
    }

    //전투 시작 시 1번만 호출, 아군, 적군 정보 불러오기, 버프 및 디버프 설정, TP값 초기화
    public void BattleStart()
    {
        state = BattleState.Start;
        startBtn.SetActive(false);

        foreach (Unit c in allCharList)
            c.OnBattleStart(this);

        if (charState.currHP > 0)
            characterList[0].buffStat[(int)Obj.currHP] = charState.currHP;
        else
            characterList[0].buffStat[(int)Obj.currHP] = characterList[0].buffStat[(int)Obj.HP];
        if (characterList[0].classIdx == 6)
            characterList[0].GetComponent<Druid>().revive = charState.druidRevive;

        if (charState.golemHP == 0)
            characterList[1].buffStat[(int)Obj.currHP] = characterList[1].buffStat[(int)Obj.HP];

        foreach (Unit u in allCharList)
            charTP.Add(u, new int[2] { 0, 0 });

        TPMaxUpdate();
        
        StartCoroutine(FirstCalc());
    }

    //전투 시작 시 잠시 대기 후 TP 계산 시작
    IEnumerator FirstCalc()
    {
        yield return new WaitForSeconds(1f);
        SelectNextCaster();
    }
    /* #endregion */

    /* #region Function_TP */
    //속도가 변했을 때, TP 최대값 업데이트
    void TPMaxUpdate()
    {
        foreach (Unit u in allCharList)
            charTP[u][1] = 75 - u.buffStat[(int)Obj.SPD];
        TPImageUpdate();
    }
    void TPImageUpdate()
    {
        for (int i = 0; i < allCharList.Count; i++)
            TPBars[i].value = (float)charTP[allCharList[i]][0] / charTP[allCharList[i]][1];
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

        while (casterQueue.Count > 0 && !casterQueue[0].isActiveAndEnabled)
            casterQueue.RemoveAt(0);

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
            List<Unit> charged = new List<Unit>();

            //TP 상승
            while (charged.Count == 0)
            {
                foreach (Unit u in allCharList)
                {
                    if (u.isActiveAndEnabled)
                        charTP[u][0]++;
                    if (charTP[u][0] >= charTP[u][1])
                        charged.Add(u);
                }
                TPImageUpdate();
                yield return new WaitForSeconds(0.02f);
            }

            //TP 최대치 도달 유닛이 둘 이상인 경우
            if (charged.Count > 1)
            {
                Shuffle(charged);
                charged.Sort(delegate (Unit a, Unit b)
                {
                    if (a.buffStat[(int)Obj.SPD] < b.buffStat[(int)Obj.SPD])
                        return 1;
                    else if (a.buffStat[(int)Obj.SPD] > b.buffStat[(int)Obj.SPD])
                        return -1;
                    else return 0;
                });     //속도 기준 정렬

                int pivot = 0;
                int i;

                //속도가 같은 경우, 레벨 기준 정렬
                for (i = 1; i < charged.Count; i++)                  
                {
                    if (charged[pivot].buffStat[(int)Obj.SPD] > charged[i].buffStat[(int)Obj.SPD])
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
        void LVLSort(List<Unit> list, int s, int e)
        {
            for (int i = 0; i < e - 1; i++)
            {
                int tmp = i;
                for (int j = i + 1; j < e; j++)
                    if (list[tmp].LVL < list[j].LVL)
                        tmp = j;

                Unit c = list[tmp];
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

        if (characterList.Contains(currCaster))
        {
            state = BattleState.AllieTurnStart;
            AllieTurnStart();
        }
        else
        {
            state = BattleState.EnemyTurn;
            EnemyTurn();
        }
    }
    /* #endregion */

    /* #region Function_AllieTurn */
    //아군 턴인 경우, 선택 UI 보이기, 캐릭터 스킬 수에 따라 버튼 활성화, AP 초기화
    void AllieTurnStart()
    {
        if (state != BattleState.AllieTurnStart)
            return;

        if(currCaster.classIdx == 11)
        {
            currCaster.OnTurnStart();

            if (!monsterList.Any(x => x.isActiveAndEnabled))
                Win();
            else
                Btn_TurnEnd();
        }
        else
        {
            turnEndBtn.SetActive(true);
            currCaster.OnTurnStart();

            if (!monsterList.Any(x => x.isActiveAndEnabled))
                Win();
            else if (currCaster.IsStun())
                Btn_TurnEnd();
        }

    }

    //스킬 선택 버튼, 타겟 선택 창 활성화
    public void Btn_SkillSelect(int idx)
    {
        if (state != BattleState.AllieTurnStart)
            return;

        Skill skill = SkillManager.GetSkill(currCaster.classIdx, GameManager.slotData.activeSkills[idx]);

        //비전 마스터 선택 스킬
        if (skill.category == 1023)
        {
            //둘 다 시전
            if(currCaster.GetComponent<VisionMaster>().skillState > 1)
            {
                isBoth = true;
                state = BattleState.AllieSkillSelected;

                Skill minusS = SkillManager.GetSkill(currCaster.classIdx, GameManager.slotData.activeSkills[idx] + 1);

                if (skill.targetSelect == 1 || minusS.targetSelect == 1)
                {
                    isMinus = skill.targetSelect == 1 ? 0 : 1;

                    targetIdxs.Clear();
                    state = BattleState.AllieSkillSelected;

                    int i;
                    for (i = 0; i < roomInfo.monsterCount; i++)
                        targetBtns[i].SetActive(monsterList[i].isActiveAndEnabled);
                    for (; i < 3; i++)
                        targetBtns[i].SetActive(false);

                    //랜덤 타겟, 전체 타겟 등 타겟 선택이 필요 없는 경우 예외 처리
                    skillBtnPanel.SetActive(false);
                    targetBtnPanel.SetActive(true);

                    skillidx = idx;
                }
                else
                {
                    state = BattleState.AllieTargetSelected;
                    currCaster.GetComponent<VisionMaster>().ActiveSkill_Both(idx, new List<Unit>());

                    targetBtnPanel.SetActive(false);
                    skillBtnPanel.SetActive(true);
                    state = BattleState.AllieTurnStart;
                    isBoth = false;
                    isMinus = 0;

                    if (!monsterList.Any(n => n.isActiveAndEnabled))
                        Win();
                }
            }
            //하나만 선택 시전
            else
            {
                isBoth = false;
                isMinus = 0;
                state = BattleState.AllieSkillSelected;

                skillBtnPanel.SetActive(false);
                skillChoosePanel.SetActive(true);
                skillidx = idx;
            }
        }
        else
        {
            Debug.Log(skill.name);

            string castLog = currCaster.CanCastSkill(idx);

            if (castLog != "")
            {
                LogManager.instance.AddLog(castLog);
                return;
            }
            if (IsUniqueCondition())
                return;

            //타겟 선택 스킬
            if (skill.targetSelect == 1)
            {
                targetIdxs.Clear();
                state = BattleState.AllieSkillSelected;

                int i;
                for (i = 0; i < roomInfo.monsterCount; i++)
                    targetBtns[i].SetActive(monsterList[i].isActiveAndEnabled);
                for (; i < 3; i++)
                    targetBtns[i].SetActive(false);

                //랜덤 타겟, 전체 타겟 등 타겟 선택이 필요 없는 경우 예외 처리
                skillBtnPanel.SetActive(false);
                targetBtnPanel.SetActive(true);

                skillidx = idx;
            }
            //타겟 미선택 스킬
            else
            {
                state = BattleState.AllieTargetSelected;
                currCaster.ActiveSkill(idx, new List<Unit>());

                targetBtnPanel.SetActive(false);
                skillBtnPanel.SetActive(true);
                state = BattleState.AllieTurnStart;

                if (!monsterList.Any(n => n.isActiveAndEnabled))
                    Win();
            }
        }

        bool IsUniqueCondition()
        {
            if (109 <= skill.idx && skill.idx <= 111)
            {
                if (!characterList.Any(x => x.classIdx == 11) || !characterList[1].isActiveAndEnabled || characterList[1].GetComponent<Elemental>().type != skill.category || characterList[1].GetComponent<Elemental>().isUpgraded)
                {
                    LogManager.instance.AddLog("must summon elemental before upgrade");
                    return true;
                }
            }
            if (skill.idx == 121)
            {
                if (!characterList.Any(x => x.classIdx == 11) || !characterList[1].isActiveAndEnabled || !characterList[1].GetComponent<Elemental>().isUpgraded)
                {
                    LogManager.instance.AddLog("need upgraded elemantal");
                    return true;
                }
            }
            else if (currCaster.classIdx == 6 && skill.effectType[0] == 39 && currCaster.GetComponent<Druid>().currVitality < skill.effectRate[0])
            {
                LogManager.instance.AddLog("not enough vitality");
                return true;
            }

            return false;
        }
    }

    //비전 마스터 스킬 선택 버튼
    public void Btn_SkillChoose(int isMinus)
    {
        Skill skill = SkillManager.GetSkill(currCaster.classIdx, GameManager.slotData.activeSkills[skillidx] + isMinus);

        currCaster.GetComponent<VisionMaster>().skillState = isMinus;
        this.isMinus = isMinus;

        Debug.Log(skill.name);

        string castLog = currCaster.CanCastSkill(skillidx);

        if (castLog != "")
        {
            LogManager.instance.AddLog(castLog);
            return;
        }

        //타겟 선택 스킬
        if (skill.targetSelect == 1)
        {
            targetIdxs.Clear();
            state = BattleState.AllieSkillSelected;

            int i;
            for (i = 0; i < roomInfo.monsterCount; i++)
                targetBtns[i].SetActive(monsterList[i].isActiveAndEnabled);
            for (; i < 3; i++)
                targetBtns[i].SetActive(false);

            //랜덤 타겟, 전체 타겟 등 타겟 선택이 필요 없는 경우 예외 처리
            skillChoosePanel.SetActive(false);
            targetBtnPanel.SetActive(true);
        }
        //타겟 미선택 스킬
        else
        {
            state = BattleState.AllieTargetSelected;
            currCaster.ActiveSkill(skillidx, new List<Unit>());

            targetBtnPanel.SetActive(false);
            skillBtnPanel.SetActive(true);
            state = BattleState.AllieTurnStart;

            if (!monsterList.Any(n => n.isActiveAndEnabled))
                Win();
        }
    }

    //스킬 선택 취소 버튼, 스킬 선택 전 상태로 돌아감
    public void Btn_SkillCancel()
    {
        isBoth = false;
        isMinus = 0;
        targetIdxs.Clear();
        state = BattleState.AllieTurnStart;
        targetBtnPanel.SetActive(false);
        skillChoosePanel.SetActive(false);
        skillBtnPanel.SetActive(true);
    }

    //타겟 선택 버튼, 스킬 시전
    public void Btn_TargetSelect(int idx)
    {
        if (targetIdxs.Contains(idx))
            targetIdxs.Remove(idx);
        else
            targetIdxs.Add(idx);

        Skill s = SkillManager.GetSkill(currCaster.classIdx, GameManager.slotData.activeSkills[skillidx] + isMinus);

        if (targetIdxs.Count == s.targetCount || targetIdxs.Count == monsterList.Count(x => x.isActiveAndEnabled))
        {
            List<Unit> selects = new List<Unit>();
            foreach (int i in targetIdxs)
                selects.Add(monsterList[i]);

            state = BattleState.AllieTargetSelected;
            if (isBoth)
                currCaster.GetComponent<VisionMaster>().ActiveSkill_Both(skillidx, selects);
            else
                currCaster.ActiveSkill(skillidx, selects);

            isBoth = false;
            isMinus = 0;

            targetBtnPanel.SetActive(false);
            skillBtnPanel.SetActive(true);
            state = BattleState.AllieTurnStart;

            targetIdxs.Clear();

            if (!monsterList.Any(n => n.isActiveAndEnabled))
                Win();
        }
    }

    public void Btn_TurnEnd()
    {
        targetBtnPanel.SetActive(false);
        skillBtnPanel.SetActive(true);
        turnEndBtn.SetActive(false);

        currCaster.OnTurnEnd();

        if(currCaster.classIdx == 4 && currCaster.GetComponent<MadScientist>().turnCount == 7)
        {    
            charTP[currCaster][0] = charTP[currCaster][1];
            currCaster.GetComponent<MadScientist>().turnCount = 0;
        }
        else
            charTP[currCaster][0] = 0;
        TPImageUpdate();

        StartCoroutine(AllieTurnEnd());
    }

    IEnumerator AllieTurnEnd()
    {
        point.SetActive(false);

        yield return new WaitForSeconds(1f);

        SelectNextCaster();
    }
    /* #endregion */

    /* #region Function_EnemyTurn */
    //적 턴, 아군 캐릭터 대상으로 정해진 스킬 시전
    void EnemyTurn()
    {
        if (state != BattleState.EnemyTurn)
            return;
        Monster caster = currCaster.GetComponent<Monster>();
        caster.OnTurnStart();

        if(caster.IsStun())
        {
            charTP[currCaster][0] = 0;
            TPImageUpdate();
            StartCoroutine(EnemyTurnEnd());
            return;
        }

        if (!characterList.Any(x=>x.isActiveAndEnabled))
        {
            state = BattleState.Lose;
            Lose();
        }
        else
        {
            currCaster.OnTurnEnd();
            charTP[currCaster][0] = 0;
            TPImageUpdate();
            StartCoroutine(EnemyTurnEnd());
        }
    }

    IEnumerator EnemyTurnEnd()
    {
        yield return new WaitForSeconds(1f);

        point.SetActive(false);
        SelectNextCaster();
    }
    /* #endregion */

    /* #region Function_BattleEnd */
    //승리, 보상 획득, 탐험 계속 진행
    void Win()
    {
        skillBtnPanel.SetActive(false);
        targetBtnPanel.SetActive(false);

        for (int i = 0; i < roomInfo.ItemCount; i++)
            ItemManager.ItemDrop(characterList[0].classIdx, roomInfo.ItemIdx[i], roomInfo.ItemChance[i]);

        charState.currHP = characterList[0].buffStat[(int)Obj.currHP];

        if (characterList[0].classIdx == 4 && characterList.Count > 1 && characterList[1].isActiveAndEnabled)
            charState.golemHP = characterList[1].buffStat[(int)Obj.currHP];
        else
            charState.golemHP = -1;

        if (characterList[0].classIdx == 6)
            charState.druidRevive = characterList[0].GetComponent<Druid>().revive;
        PlayerPrefs.SetString(string.Concat("CharState", GameManager.currSlot), LitJson.JsonMapper.ToJson(charState));

        LogManager.instance.AddLog("win");

        if (GameManager.slotData.dungeonRoom > 100)
            bossWinUI.SetActive(true);
        else
            winUI.SetActive(true);
    }

    public void Btn_BackToMap()
    {
        GameManager.SwitchSceneData(SceneKind.Dungeon);
        UnityEngine.SceneManagement.SceneManager.LoadScene("2_0 Dungeon");
    }

    //패배, 현재까지의 보상만 가진 채 마을로 귀환
    void Lose()
    {
        skillBtnPanel.SetActive(false);
        targetBtnPanel.SetActive(false);
        LogManager.instance.AddLog("Lose");
        loseUI.SetActive(false);
    }

    public void Btn_BackToTown()
    {
        PlayerPrefs.DeleteKey(string.Concat("DungeonData", GameManager.currSlot));
        GameManager.SwitchSceneData(SceneKind.Town);
        UnityEngine.SceneManagement.SceneManager.LoadScene("1 Town");
    }
    /* #endregion */

    /* #region Function_CharSkills */
    public void ReduceTP(List<Unit> targets, int amt)
    {
        foreach (Unit u in targets)
            charTP[u][0] = Mathf.Max(0, charTP[u][0] - amt);
    }

    //매드 사이언티스트
    public bool HasGolem() => characterList.Count > 1 && characterList[1].isActiveAndEnabled;
    public void GolemControl(KeyValuePair<int, List<Unit>> token)
    {
        if (HasGolem())
            characterList[1].GetComponent<Golem>().AddControl(token);
    }

    //엘리멘탈 컨트롤러
    public void SummonElemental(ElementalController caster, int type)
    {
        if (characterList.Any(x => x.classIdx == 11))
        {
            Character tmp = characterList[1];
            charTP.Remove(tmp);
            allCharList.Remove(tmp);
            characterList.Remove(tmp);
            casterQueue.Remove(tmp);
            Destroy(tmp.gameObject);
        }

        Elemental e = Instantiate(alliePrefabs[10], alliePos.position + new Vector3(1, 0, 0), Quaternion.identity).GetComponent<Elemental>();
        e.Summon(this, caster, type);

        charTP.Add(e, new int[2] { 0, 0 });
        characterList.Add(e);
        allCharList.Add(e);

        for (int i = 0; i < TPBars.Length; i++)
            TPBars[i].gameObject.SetActive(i < allCharList.Count && allCharList[i].isActiveAndEnabled);
        TPMaxUpdate();
    }
    public void UpgradeElemental(ElementalController caster, int type)
    {
        if (characterList.Any(x => x.classIdx == 11))
        {
            Character tmp = characterList[1];
            charTP.Remove(tmp);
            allCharList.Remove(tmp);
            characterList.Remove(tmp);
            casterQueue.Remove(tmp);
            Destroy(tmp.gameObject);
        }

        Elemental e = Instantiate(alliePrefabs[10], alliePos.position + new Vector3(1, 0, 0), Quaternion.identity).GetComponent<Elemental>();
        e.Summon(this, caster, type, true);

        charTP.Add(e, new int[2] { 0, 0 });
        characterList.Add(e);
        allCharList.Add(e);

        for (int i = 0; i < TPBars.Length; i++)
            TPBars[i].gameObject.SetActive(i < allCharList.Count && allCharList[i].isActiveAndEnabled);
        TPMaxUpdate();
    }
    public int SacrificeElemental(ElementalController caster, Skill skill)
    {
        int type = -1;
        if (characterList.Any(x => x.classIdx == 11))
        {
            Character tmp = characterList[1];
            type = tmp.GetComponent<Elemental>().type;

            charTP.Remove(tmp);
            allCharList.Remove(tmp);
            characterList.Remove(tmp);
            casterQueue.Remove(tmp);
            Destroy(tmp.gameObject);
        }

        return type;
    }
    public void Sacrifice_TP(List<Unit> targets)
    {
        foreach (Unit u in targets)
            charTP[u][0] = 0;
        TPImageUpdate();
    }

    //몬스터
    public bool ReloadBullet()
    {
        var ene = from x in monsterList where x.monsterIdx == 10 || x.monsterIdx == 11 select x;

        if (ene.Count() <= 0)
            return false;

        Monster m = ene.First();
        charTP.Remove(m);
        casterQueue.Remove(m);
        m.gameObject.SetActive(false);

        return true;
    }
    public void Quixote()
    {
        Unit m = GetEffectTarget(4)[0];
        charTP[m][0] = charTP[m][1];
    }
    /* #endregion Function_CharSkills */

    public List<Unit> GetEffectTarget(int idx)
    {
        List<Unit> tmp = new List<Unit>();
        switch (idx)
        {
            //아군 측 랜덤 1개체
            case 2:
                if (HasUpgradedElemental())
                {
                    tmp.Add(characterList[1]);
                    return tmp;
                }
                else if(IsMadSpecialCondition())
                {
                    tmp.Add(characterList[0]);
                    return tmp;
                }
                else
                    return RandomList(characterList.ConvertAll(x => (Unit)x), 1);
            //아군 측 전체
            case 3:
                return (from x in characterList where x.isActiveAndEnabled select x).ToList().ConvertAll(x => (Unit)x);
            //적군 측 랜덤 1개체
            case 4:
                return RandomList(monsterList.ConvertAll(x => (Unit)x), 1);
            //적군 측 랜덤 2개체
            case 5:
                return RandomList(monsterList.ConvertAll(x => (Unit)x), 2);
            //적군 측 전체
            case 6:
                return (from x in monsterList where x.isActiveAndEnabled select x).ToList().ConvertAll(x => (Unit)x);
            //피아 미구분 랜덤 1개체
            case 7:
                return RandomList(allCharList, 1);
            //피아 미구분 랜덤 2개체
            case 8:
                return RandomList(allCharList, 2);
            //피아 미구분 랜덤 3개체
            case 9:
                return RandomList(allCharList, 3);
            //피아 미구분 랜덤 4개체
            case 10:
                return RandomList(allCharList, 4);
            //피아 미구분 전체
            case 11:
                return (from x in allCharList where x.isActiveAndEnabled select x).ToList();
            case 13:
                tmp.Add(characterList[0]);
                return tmp;
            default:
                return tmp;
        }

        bool HasUpgradedElemental()
        {
            if (characterList.Count < 2)
                return false;
            if (characterList[1].GetComponent<Elemental>() == null)
                return false;
            return characterList[1].GetComponent<Elemental>().isUpgraded && characterList[1].isActiveAndEnabled;
        }
        bool IsMadSpecialCondition()
        {
            if(characterList[0].classIdx != 4)
                return false;
            return characterList[0].GetComponent<MadScientist>().isMagnetic;
        }
        List<Unit> RandomList(List<Unit> baseList, int count)
        {
            baseList = (from x in baseList where x.isActiveAndEnabled select x).ToList();

            if (baseList.Count <= count)
                return baseList;

            List<int> random = new List<int>();
            for (int i = 0; i < baseList.Count; i++)
                random.Add(i);
            for (int i = random.Count - 1; i > 0; i++)
            {
                int rand = Random.Range(0, i);
                int t = random[i];
                random[i] = random[rand];
                random[rand] = t;
            }

            for (int i = 0; i < count; i++)
                tmp.Add(baseList[random[i]]);
            return tmp;
        }
    }
}
