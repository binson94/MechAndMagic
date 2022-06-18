using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public enum BattleState { Start, Calc, AllieTurnStart, AllieSkillSelected, AllieTargetSelected, EnemyTurn, Win, Lose }



//1. 버튼과 enemy 1:1 매칭, 버튼 위치 고정
public class BattleManager : MonoBehaviour
{
    #region Variables
    BattleState state;

    #region CharList
    //전투 중인 모든 캐릭터
    [SerializeField] List<Unit> allCharList = new List<Unit>();
    #endregion

    #region Spawn
    [Header("Spawn")]
    [SerializeField] GameObject[] alliePrefabs;
    [SerializeField] Transform alliePos;
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] Unit DummyUnit;
    RoomInfo roomInfo;
    #endregion

    #region Caster
    [Header("Caster")]
    //캐릭터들의 TP 최대치, 전투 시작 시 계산
    [SerializeField] TPSlider tpBars;
    Dictionary<Unit, int[]> charTP = new Dictionary<Unit, int[]>();

    //TP를 통해 선정된 현재 턴 실행자
    Unit currCaster;

    //TP가 동일할 때, 속도와 공속 기준으로 순서대로 queue에 저장
    List<Unit> casterQueue = new List<Unit>();
    List<int> targetIdxs = new List<int>();
    #endregion

    #region UI
    [Header("UI")]
    //아군 타겟 선택 관련
    [SerializeField] GameObject startBtn;         //최초 전투 시작 버튼
    [SerializeField] GameObject turnEndBtn;

    [Header("Unit Status")]
    [SerializeField] Status[] unitStatus;
    [SerializeField] APBar apBar;
    [SerializeField] Text[] statusTxts;

    int skillIdx;
    [Header("Skill Panel")]
    [SerializeField] Sprite[] skillIcons;
    [SerializeField] GameObject skillBtnPanel;    //스킬 선택 UI, 내 턴에 활성화
    [SerializeField] SkillButton[] skillBtns;      //각각 스킬 선택 버튼, 스킬 수만큼 활성화
    [SerializeField] GameObject skillTxtPanel;
    [SerializeField] Text[] skillTxts;

    bool isBoth = false;
    int isMinus = 0;
    [SerializeField] GameObject skillChoosePanel; //비전 마스터 스킬 선택

    [SerializeField] GameObject targetBtnPanel;   //타겟 선택 UI, 스킬 선택 시 활성화
    [SerializeField] GameObject[] targetBtns;     //각각 타겟 선택 버튼, 타겟 수만큼 활성화

    [Header("End Panel")]
    [SerializeField] GameObject winUI;
    [SerializeField] GameObject bossWinUI;
    [SerializeField] GameObject loseUI;
    #endregion
    #endregion Variables

    #region Function_Start
    //BGM 재생, 몬스터 및 캐릭터 생성
    public void OnStart()
    {
        Debug.Log("aa" + GameManager.instance.slotData.dungeonData.currRoomEvent);

        //
        SoundManager.instance.PlayBGM(BGM.Battle1);
        if (GameManager.instance.slotData.dungeonData.currRoomEvent > 100)
            roomInfo = new RoomInfo(1);
        else
            roomInfo = new RoomInfo(GameManager.instance.slotData.dungeonData.currRoomEvent);

        Spawn();

        SkillBtnInit();
        SkillBtnUpdate();

        void Spawn()
        {
            int i;

            //아군 캐릭터 생성
            Character c = Instantiate(alliePrefabs[GameManager.instance.slotData.slotClass], alliePos.position, Quaternion.identity).GetComponent<Character>();
            for (i = 0; i < c.activeIdxs.Length; i++)
                c.activeIdxs[i] = GameManager.instance.slotData.activeSkills[i];
            for (i = 0; i < c.passiveIdxs.Length; i++)
                c.passiveIdxs[i] = GameManager.instance.slotData.passiveSkills[i];

            allCharList.Add(c);

            //골렘 생성
            if (GameManager.instance.slotData.dungeonData.golemHP >= 0)
            {
                Golem g = Instantiate(alliePrefabs[11], alliePos.position + new Vector3(1, 0, 0), Quaternion.identity).GetComponent<Golem>();
                g.GolemInit(allCharList[0].GetComponent<MadScientist>());
                allCharList.Add(g);
            }
            else
                allCharList.Add(DummyUnit);

            //던전 풀에 따른 적 캐릭터 생성
            for (i = 0; i < roomInfo.monsterCount; i++)
            {
                Monster mon = Instantiate(enemyPrefabs[roomInfo.monsterIdx[i]], alliePos.position, Quaternion.identity).GetComponent<Monster>();
                allCharList.Add(mon);
            }
            for (; i < 3; i++) { allCharList.Add(DummyUnit); unitStatus[i + 1].gameObject.SetActive(false); }
        }
    }

    //전투 시작 시 1번만 호출, 아군, 적군 정보 불러오기, 버프 및 디버프 설정, TP값 초기화
    public void BattleStart()
    {
        state = BattleState.Start;
        startBtn.SetActive(false);

        //던전 이벤트로 생긴 버프, 디버프 처리
        foreach (DungeonBuff b in GameManager.instance.slotData.dungeonData.dungeonBuffs)
            allCharList[0].turnBuffs.Add(new Buff(BuffType.Stat, allCharList[0].LVL, new BuffOrder(), b.name, b.objIdx, 1, (float)b.rate, 1, 99, 0, 1));
        foreach (DungeonBuff b in GameManager.instance.slotData.dungeonData.dungeonDebuffs)
            allCharList[0].turnDebuffs.Add(new Buff(BuffType.Stat, allCharList[0].LVL, new BuffOrder(), b.name, b.objIdx, 1, (float)b.rate, 1, 99, 0, 1));

        foreach (Unit c in allCharList)
            c.OnBattleStart(this);

        //캐릭터 현재 체력 불러오기
        if (GameManager.instance.slotData.dungeonData.currHP > 0)
            allCharList[0].buffStat[(int)Obj.currHP] = GameManager.instance.slotData.dungeonData.currHP;
        else
            allCharList[0].buffStat[(int)Obj.currHP] = allCharList[0].buffStat[(int)Obj.HP];

        //드루이드 - 부활 여부 불러오기
        if (allCharList[0].classIdx == 6)
            allCharList[0].GetComponent<Druid>().revive = GameManager.instance.slotData.dungeonData.druidRevive;
        //매드 사이언티스트 - 골렘 체력 불러오기
        if (GameManager.instance.slotData.dungeonData.golemHP == 0)
            allCharList[1].buffStat[(int)Obj.currHP] = allCharList[1].buffStat[(int)Obj.HP];

        foreach (Unit u in allCharList)
            if (u.isActiveAndEnabled)
                charTP.Add(u, new int[2] { 0, 0 });

        for (int i = 0, j = 0; i < allCharList.Count; i++)
        {
            if (allCharList[i] == DummyUnit || allCharList[i].classIdx > 10)
                continue;
            unitStatus[j++].SetName(allCharList[i]);
        }

        TPMaxUpdate();
        StatusUpdate();
        SelectNextCaster();
    }
    #endregion Function_Start

    #region Function_TP
    //속도가 변했을 때, TP 최대값 업데이트
    void TPMaxUpdate()
    {
        foreach (Unit u in allCharList)
            if (u.isActiveAndEnabled)
                charTP[u][1] = 75 - u.buffStat[(int)Obj.SPD];

        tpBars.ActiveSet(allCharList);
        TPImageUpdate();
    }
    void TPImageUpdate()
    {
        for (int i = 0; i < allCharList.Count; i++)
            if (allCharList[i].isActiveAndEnabled)
                tpBars.SetValue(i, (float)charTP[allCharList[i]][0] / charTP[allCharList[i]][1]);
    }
    //다음 턴 시전자 탐색
    public void SelectNextCaster()
    {
        if (state == BattleState.Calc)
            return;

        StartCoroutine(TPCalculate());
    }
    //TP 계산
    IEnumerator TPCalculate()
    {
        state = BattleState.Calc;

        //TP가 찬 캐릭터가 이미 있는 경우
        if (casterQueue.Count > 0)
        {
            Unit u;
            do { u = casterQueue[0]; casterQueue.RemoveAt(0); } while (!u.isActiveAndEnabled && casterQueue.Count > 0);

            if (u.isActiveAndEnabled)
            {
                currCaster = u;
                TurnAct();
                yield break;
            }
        }

        List<Unit> charged = new List<Unit>();

        //TP 상승
        while (charged.Count == 0)
        {
            foreach (Unit u in allCharList)
            {
                if (u.isActiveAndEnabled)
                {
                    charTP[u][0]++;
                    if (charTP[u][0] >= charTP[u][1])
                        charged.Add(u);
                }
            }

            TPImageUpdate();
            yield return new WaitForSeconds(0.02f);
        }

        //TP 최대치 도달 유닛이 둘 이상인 경우
        if (charged.Count > 1)
        {
            Shuffle(charged);
            //속도 - 레벨 기준 정렬
            charged.Sort(delegate (Unit a, Unit b)
            {
                if (a.buffStat[(int)Obj.SPD] < b.buffStat[(int)Obj.SPD])
                    return 1;
                else if (a.buffStat[(int)Obj.SPD] > b.buffStat[(int)Obj.SPD])
                    return -1;
                else if (a.LVL > b.LVL)
                    return 1;
                else if (a.LVL < b.LVL)
                    return -1;
                else return 0;
            });
        }

        //TP가 최대에 도달한 모든 캐릭터를 Queue에 저장
        casterQueue = charged;
        currCaster = casterQueue[0]; casterQueue.RemoveAt(0);
        TurnAct();

        yield break;

        void Shuffle<T>(List<T> list)
        {
            int idx = list.Count - 1;

            while (idx > 0)
            {
                int rand = Random.Range(0, idx + 1);
                T val = list[idx];
                list[idx] = list[rand];
                list[rand] = val;
                idx--;
            }
        }
    }

    //다음 턴 행동 대상이 선정되었을 때 호출
    void TurnAct()
    {
        if (allCharList.IndexOf(currCaster) < 2)
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
    #endregion Function_TP

    #region Update
    void StatusUpdate()
    {
        for (int i = 0, j = 0; i < allCharList.Count; i++)
        {
            if (allCharList[i].classIdx > 10 || allCharList[i] == DummyUnit)
                continue;
            unitStatus[j++].UpdateValue(allCharList[i]);
        }
        StatTxtUpdate();

        void StatTxtUpdate()
        {
            for (int i = 0; i < 8; i++)
                statusTxts[i].text = allCharList[0].buffStat[i + 5].ToString();

            statusTxts[5].text = string.Concat(statusTxts[5].text, "%");
            statusTxts[6].text = string.Concat(statusTxts[6].text, "%");
        }
    }
    void SkillBtnInit()
    {
        for (int i = 0; i < skillBtns.Length; i++)
        {
            if (GameManager.instance.slotData.activeSkills[i] > 0)
            {
                skillBtns[i].gameObject.SetActive(true);
                skillBtns[i].Init(SkillManager.GetSkill(GameManager.instance.slotData.slotClass, GameManager.instance.slotData.activeSkills[i]), skillIcons[0]);
            }
            else
                skillBtns[i].gameObject.SetActive(false);
        }
    }
    void SkillBtnUpdate()
    {
        for (int i = 0; i < skillBtns.Length; i++)
        {
            if (GameManager.instance.slotData.activeSkills[i] > 0)
            {
                Skill s = SkillManager.GetSkill(GameManager.instance.slotData.slotClass, GameManager.instance.slotData.activeSkills[i]);
                skillBtns[i].APUpdate(allCharList[0].GetSkillCost(s));
            }
        }
    }
    #endregion

    #region Function_AllieTurn
    //아군 턴인 경우, 선택 UI 보이기, 캐릭터 스킬 수에 따라 버튼 활성화, AP 초기화
    void AllieTurnStart()
    {
        if (state != BattleState.AllieTurnStart) return;

        //정령, 골렘 - 알아서 행동 후 턴 종료
        if (currCaster.classIdx == 11 || currCaster.classIdx == 12 || currCaster.IsStun())
        {
            currCaster.OnTurnStart();

            if (IsWin())
                Win();
            else
                Btn_TurnEnd();
        }
        else
        {
            turnEndBtn.SetActive(true);
            currCaster.OnTurnStart();
            apBar.SetValue(currCaster.buffStat[(int)Obj.currAP], currCaster.buffStat[(int)Obj.AP]);

            Btn_SkillCancel();

            if (IsWin())
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

        Skill skill = SkillManager.GetSkill(currCaster.classIdx, GameManager.instance.slotData.activeSkills[idx]);

        if (skillIdx == idx)
        {
            skillTxtPanel.SetActive(false);
            //비전 마스터 선택 스킬
            if (skill.category == 1023)
            {
                //둘 다 시전
                if (currCaster.GetComponent<VisionMaster>().skillState > 1)
                {
                    isBoth = true;
                    state = BattleState.AllieSkillSelected;

                    Skill minusS = SkillManager.GetSkill(currCaster.classIdx, GameManager.instance.slotData.activeSkills[idx] + 1);

                    //타겟 선택
                    if (skill.targetSelect == 1 || minusS.targetSelect == 1)
                    {
                        isMinus = skill.targetSelect == 1 ? 0 : 1;

                        targetIdxs.Clear();
                        state = BattleState.AllieSkillSelected;

                        for (int i = 0; i < 3; i++)
                            targetBtns[i].SetActive(allCharList[i + 2].isActiveAndEnabled);


                        skillBtnPanel.SetActive(false);
                        targetBtnPanel.SetActive(true);

                        skillIdx = idx;
                    }
                    //타겟 미선택
                    else
                    {
                        state = BattleState.AllieTargetSelected;
                        currCaster.GetComponent<VisionMaster>().ActiveSkill_Both(idx, new List<Unit>());

                        apBar.SetValue(currCaster.buffStat[(int)Obj.currAP], currCaster.buffStat[(int)Obj.AP]);

                        StatusUpdate();
                        Btn_SkillCancel();

                        if (IsWin())
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
                    skillIdx = idx;
                }
            }
            //그 외 스킬
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

                    for (int i = 0; i < 3; i++)
                        targetBtns[i].SetActive(allCharList[i + 2].isActiveAndEnabled);

                    //랜덤 타겟, 전체 타겟 등 타겟 선택이 필요 없는 경우 예외 처리
                    skillBtnPanel.SetActive(false);
                    targetBtnPanel.SetActive(true);

                    skillIdx = idx;
                }
                //타겟 미선택 스킬
                else
                {
                    state = BattleState.AllieTargetSelected;
                    currCaster.ActiveSkill(idx, new List<Unit>());

                    targetBtnPanel.SetActive(false);
                    skillBtnPanel.SetActive(true);
                    state = BattleState.AllieTurnStart;

                    apBar.SetValue(currCaster.buffStat[(int)Obj.currAP], currCaster.buffStat[(int)Obj.AP]);

                    StatusUpdate();

                    if (IsWin())
                        Win();
                }
            }
        }
        else
        {
            skillTxts[0].text = skill.name;
            skillTxts[1].text = "긍정 설명"; skillTxts[2].text= "부정 설명";
            skillTxtPanel.SetActive(true);
            skillIdx = idx;
            for (int i = 0; i < skillBtns.Length; i++)
                skillBtns[i].Highlight(i == skillIdx);
        }

        bool IsUniqueCondition()
        {
            if (109 <= skill.idx && skill.idx <= 111)
            {
                Elemental e = allCharList[1].GetComponent<Elemental>();
                if (!allCharList[1].isActiveAndEnabled || e == null || e.type != skill.category || e.isUpgraded)
                {
                    LogManager.instance.AddLog("must summon elemental before upgrade");
                    return true;
                }
            }
            if (skill.idx == 121)
            {
                Elemental e = allCharList[1].GetComponent<Elemental>();
                if (!allCharList[1].isActiveAndEnabled || e == null || !e.isUpgraded)
                {
                    LogManager.instance.AddLog("need upgraded elemental");
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
        Skill skill = SkillManager.GetSkill(currCaster.classIdx, GameManager.instance.slotData.activeSkills[skillIdx] + isMinus);

        currCaster.GetComponent<VisionMaster>().skillState = isMinus;
        this.isMinus = isMinus;

        Debug.Log(skill.name);

        string castLog = currCaster.CanCastSkill(skillIdx);

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

            for (int i = 0; i < 3; i++)
                targetBtns[i].SetActive(allCharList[i + 2].isActiveAndEnabled);

            //랜덤 타겟, 전체 타겟 등 타겟 선택이 필요 없는 경우 예외 처리
            skillChoosePanel.SetActive(false);
            targetBtnPanel.SetActive(true);
        }
        //타겟 미선택 스킬
        else
        {
            state = BattleState.AllieTargetSelected;
            currCaster.ActiveSkill(skillIdx, new List<Unit>());

            apBar.SetValue(currCaster.buffStat[(int)Obj.currAP], currCaster.buffStat[(int)Obj.AP]);

            StatusUpdate();
            Btn_SkillCancel();

            if (IsWin())
                Win();
        }
    }
    //스킬 선택 취소 버튼, 스킬 선택 전 상태로 돌아감
    public void Btn_SkillCancel()
    {
        skillIdx = -1;
        foreach (SkillButton s in skillBtns)
            s.Highlight(false);

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

        Skill s = SkillManager.GetSkill(currCaster.classIdx, GameManager.instance.slotData.activeSkills[skillIdx] + isMinus);

        int count = 0;
        for (int i = 2; i < 5; i++) if (allCharList[i].isActiveAndEnabled) count++;

        if (targetIdxs.Count == s.targetCount || targetIdxs.Count == count)
        {
            List<Unit> selects = new List<Unit>();
            foreach (int i in targetIdxs)
                selects.Add(allCharList[i]);

            state = BattleState.AllieTargetSelected;
            if (isBoth)
                currCaster.GetComponent<VisionMaster>().ActiveSkill_Both(skillIdx, selects);
            else
                currCaster.ActiveSkill(skillIdx, selects);

            isBoth = false;
            isMinus = 0;

            apBar.SetValue(currCaster.buffStat[(int)Obj.currAP], currCaster.buffStat[(int)Obj.AP]);

            StatusUpdate();
            Btn_SkillCancel();
            
            if (IsWin())
                Win();
        }
    }
    public void Btn_UsePotion(int idx)
    {
        if (GameManager.instance.slotData.dungeonData.potionUse[idx])
            LogManager.instance.AddLog("이미 사용했습니다.");
        else
        {
            //재활용 포션
            int potionIdx = GameManager.instance.slotData.potionSlot[idx] == 13 ? GameManager.instance.slotData.potionSlot[(idx + 1) % 2] : GameManager.instance.slotData.potionSlot[idx];

            string potionLog = allCharList[0].GetComponent<Character>().CanUsePotion(potionIdx);

            if (potionLog != "")
                LogManager.instance.AddLog(potionLog);
            else
                allCharList[0].GetComponent<Character>().UsePotion(potionIdx);

            StatusUpdate();
        }
    }
    public void Btn_TurnEnd()
    {
        if (state != BattleState.AllieTurnStart) return;

        targetBtnPanel.SetActive(false);
        skillBtnPanel.SetActive(true);
        turnEndBtn.SetActive(false);

        currCaster.OnTurnEnd();

        if (currCaster.classIdx == 4 && currCaster.GetComponent<MadScientist>().turnCount == 7)
        {
            charTP[currCaster][0] = charTP[currCaster][1];
            currCaster.GetComponent<MadScientist>().turnCount = 0;
        }
        else
            charTP[currCaster][0] = 0;
        TPImageUpdate();
        StatusUpdate();
        StartCoroutine(AllieTurnEnd());
    }
    IEnumerator AllieTurnEnd()
    {
        yield return new WaitForSeconds(1f);

        SelectNextCaster();
    }
    #endregion

    #region Function_EnemyTurn
    //적 턴, 아군 캐릭터 대상으로 정해진 스킬 시전
    void EnemyTurn()
    {
        if (state != BattleState.EnemyTurn)
            return;
        Monster caster = currCaster.GetComponent<Monster>();
        caster.OnTurnStart();

        if (IsLose())
        {
            state = BattleState.Lose;
            Lose();
        }
        else
        {
            currCaster.OnTurnEnd();
            charTP[currCaster][0] = 0;
            TPImageUpdate();
            StatusUpdate();
            StartCoroutine(EnemyTurnEnd());
        }
    }
    IEnumerator EnemyTurnEnd()
    {
        yield return new WaitForSeconds(1f);

        SelectNextCaster();
    }
    #endregion

    #region Function_BattleEnd
    bool IsWin()
    {
        foreach (Unit u in allCharList) if (u.buffStat[(int)Obj.currHP] <= 0 || u.classIdx == 0) u.gameObject.SetActive(false);

        for (int i = 2; i < 5; i++) if (allCharList[i].isActiveAndEnabled) return false;
        return true;
    }
    bool IsLose()
    {
        foreach (Unit u in allCharList) if (u.buffStat[(int)Obj.currHP] <= 0 || u.classIdx == 0) u.gameObject.SetActive(false);

        for (int i = 0; i < 2; i++) if (allCharList[i].isActiveAndEnabled) return false;
        return true;
    }
    //승리, 보상 획득, 탐험 계속 진행
    void Win()
    {
        skillBtnPanel.SetActive(false);
        targetBtnPanel.SetActive(false);

        for (int i = 0; i < roomInfo.ItemCount; i++)
            ItemManager.ItemDrop(roomInfo.ItemIdx[i], roomInfo.ItemChance[i]);

        GameManager.instance.slotData.dungeonData.currHP = allCharList[0].buffStat[(int)Obj.currHP];

        if (allCharList[0].classIdx == 4 && allCharList[1].isActiveAndEnabled)
            GameManager.instance.slotData.dungeonData.golemHP = allCharList[1].buffStat[(int)Obj.currHP];
        else
            GameManager.instance.slotData.dungeonData.golemHP = -1;

        if (allCharList[0].classIdx == 6)
            GameManager.instance.slotData.dungeonData.druidRevive = allCharList[0].GetComponent<Druid>().revive;

        LogManager.instance.AddLog("승리");
        //체력 유지 퀘스트 업데이트
        QuestManager.DiehardUpdate((float)allCharList[0].buffStat[(int)Obj.currHP] / allCharList[0].buffStat[(int)Obj.HP]);

        if (GameManager.instance.slotData.dungeonData.currRoomEvent > 100)
        {
            string drops = "드랍 목록\n";
            bossWinUI.SetActive(true);
            foreach (Triplet<DropType, int, int> token in GameManager.instance.slotData.dungeonData.dropList)
                drops = string.Concat(drops, token.first, " ", token.second, " ", token.third, "\n");

            Debug.Log(drops);
        }
        else
            winUI.SetActive(true);
    }
    public void Btn_BackToMap()
    {
        GameManager.instance.SwitchSceneData(SceneKind.Dungeon);
        GameManager.instance.UpdateDungeonBuff();
        QuestManager.QuestUpdate(QuestType.Battle, 0, 1);
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
        GameManager.instance.GetExp(roomInfo.roomExp);
        GameManager.instance.RemoveDungeonData();
        GameManager.instance.SwitchSceneData(SceneKind.Town);
        UnityEngine.SceneManagement.SceneManager.LoadScene("1 Town");
    }
    #endregion

    #region Function_CharSkills
    public void ReduceTP(List<Unit> targets, int amt)
    {
        foreach (Unit u in targets)
            charTP[u][0] = Mathf.Max(0, charTP[u][0] - amt);
    }

    //매드 사이언티스트
    public bool HasGolem() => allCharList[1].isActiveAndEnabled;
    public void GolemControl(KeyValuePair<int, List<Unit>> token)
    {
        if (HasGolem())
            allCharList[1].GetComponent<Golem>().AddControl(token);
    }

    //엘리멘탈 컨트롤러
    public void SummonElemental(ElementalController caster, int type)
    {
        Unit tmp = allCharList[1];
        charTP.Remove(tmp);
        allCharList.Remove(tmp);
        casterQueue.Remove(tmp);
        if (tmp != DummyUnit)
            Destroy(tmp.gameObject);


        Elemental e = Instantiate(alliePrefabs[10], alliePos.position + new Vector3(1, 0, 0), Quaternion.identity).GetComponent<Elemental>();
        e.Summon(this, caster, type);

        charTP.Add(e, new int[2] { 0, 0 });
        allCharList.Insert(1, e);

        TPMaxUpdate();
    }
    public void UpgradeElemental(ElementalController caster, int type)
    {
        Unit tmp = allCharList[1];
        charTP.Remove(tmp);
        allCharList.Remove(tmp);
        casterQueue.Remove(tmp);
        if (tmp != DummyUnit)
            Destroy(tmp.gameObject);

        Elemental e = Instantiate(alliePrefabs[10], alliePos.position + new Vector3(1, 0, 0), Quaternion.identity).GetComponent<Elemental>();
        e.Summon(this, caster, type, true);

        charTP.Add(e, new int[2] { 0, 0 });
        allCharList.Insert(1, e);

        TPMaxUpdate();
    }
    public int SacrificeElemental(ElementalController caster, Skill skill)
    {
        int type = -1;
        if (allCharList[1].GetComponent<Elemental>())
        {
            Unit tmp = allCharList[1];
            type = tmp.GetComponent<Elemental>().type;

            charTP.Remove(tmp);
            allCharList.Remove(tmp);
            casterQueue.Remove(tmp);
            Destroy(tmp.gameObject);

            allCharList.Insert(1, DummyUnit);
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
        List<Monster> mons = new List<Monster>();
        for (int i = 2; i < 5; i++) if (allCharList[i].isActiveAndEnabled) mons.Add(allCharList[i].GetComponent<Monster>());
        var ene = from x in mons where x.monsterIdx == 10 || x.monsterIdx == 11 select x;

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
    #endregion Function_CharSkills

    public List<Unit> GetEffectTarget(int idx)
    {
        List<Unit> tmp = new List<Unit>();
        switch (idx)
        {
            //아군 측 랜덤 1개체
            case 2:
                if (HasUpgradedElemental())
                {
                    tmp.Add(allCharList[1]);
                    return tmp;
                }
                else if (IsMadSpecialCondition())
                {
                    tmp.Add(allCharList[0]);
                    return tmp;
                }
                else
                    return RandomList(0, 1);
            //아군 측 전체
            case 3:
                return AllList(0);
            //적군 측 랜덤 1개체
            case 4:
                return RandomList(1, 1);
            //적군 측 랜덤 2개체
            case 5:
                return RandomList(1, 2);
            //적군 측 전체
            case 6:
                return AllList(1);
            //피아 미구분 랜덤 1개체
            case 7:
                return RandomList(2, 1);
            //피아 미구분 랜덤 2개체
            case 8:
                return RandomList(2, 2);
            //피아 미구분 랜덤 3개체
            case 9:
                return RandomList(2, 3);
            //피아 미구분 랜덤 4개체
            case 10:
                return RandomList(2, 4);
            //피아 미구분 전체
            case 11:
                return AllList(2);
            case 13:
                tmp.Add(allCharList[0]);
                return tmp;
            default:
                return tmp;
        }

        bool HasUpgradedElemental()
        {
            Elemental e = allCharList[1].GetComponent<Elemental>();
            if (e == null)
                return false;
            return e.isUpgraded && allCharList[1].isActiveAndEnabled;
        }
        bool IsMadSpecialCondition()
        {
            if (allCharList[0].classIdx != 4)
                return false;
            return allCharList[0].GetComponent<MadScientist>().isMagnetic;
        }
        List<Unit> RandomList(int type, int count)
        {
            List<Unit> baseList = new List<Unit>();
            switch (type)
            {
                case 0:
                    for (int i = 0; i < 2; i++) if (allCharList[i].isActiveAndEnabled) baseList.Add(allCharList[i]);
                    break;
                case 1:
                    for (int i = 2; i < 5; i++) if (allCharList[i].isActiveAndEnabled) baseList.Add(allCharList[i]);
                    break;
                case 2:
                    for (int i = 0; i < 5; i++) if (allCharList[i].isActiveAndEnabled) baseList.Add(allCharList[i]);
                    break;
            }

            for (int i = 0; i < baseList.Count; i++)
            Debug.Log(baseList[i].name);

            if (baseList.Count <= count)
                return baseList;

            for(int i = 0;i < count;i++)
            {
                int idx = Random.Range(0, baseList.Count);
                tmp.Add(baseList[idx]);
                baseList.RemoveAt(idx);
            }

            return tmp;
        }
        List<Unit> AllList(int type)
        {
            List<Unit> baseList = new List<Unit>();
            switch (type)
            {
                case 0:
                    for (int i = 0; i < 2; i++) if (allCharList[i].isActiveAndEnabled) baseList.Add(allCharList[i]);
                    break;
                case 1:
                    for (int i = 2; i < 5; i++) if (allCharList[i].isActiveAndEnabled) baseList.Add(allCharList[i]);
                    break;
                case 2:
                    for (int i = 0; i < 5; i++) if (allCharList[i].isActiveAndEnabled) baseList.Add(allCharList[i]);
                    break;
            }

            return baseList;
        }
    }
}
