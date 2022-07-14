using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

///<summary> 전투 제어 클래스 </summary>
public class BattleManager : MonoBehaviour
{
    enum BattleState { Start, Calc, AllieTurnStart, AllieSkillSelected, AllieTargetSelected, EnemyTurn, Win, Lose }

    #region Variables
    BattleState state;

    ///<summary> 전투 중인 모든 캐릭터 리스트
    ///<para> 0 플레이어, 1 골렘, 정령, 2 ~ 4 적, 없으면 dummy 연결 </para> </summary>
    [SerializeField] Unit[] allChars = new Unit[5];
    [SerializeField] Animator fadeAnimator;


    ///<summary> 플레이어 프리팹 
    ///<para> 1 ~ 8 캐릭터, 9 골렘, 10 정령 </para> </summary>
    [Header("Spawn")]
    [SerializeField] GameObject[] alliePrefabs;
    ///<summary> 적 프리팹 </summary>
    [SerializeField] GameObject enemyPrefab;
    ///<summary> 더미 유닛 </summary>
    [SerializeField] Unit dummyUnit;
    ///<summary> 몬스터 및 보상 정보 json 불러옴 </summary>
    RoomInfo roomInfo;


    ///<summary> 캐릭터, TP 현재, 최대치 저장 </summary>
    [Header("Caster")]
    Dictionary<Unit, int[]> charTP = new Dictionary<Unit, int[]>();
    ///<summary> TP를 통해 선정된 현재 턴 실행자 </summary>
    Unit currCaster;
    ///<summary> TP가 동일할 때, 속도와 공속 기준으로 순서대로 queue에 저장 </summary>
    List<Unit> casterQueue = new List<Unit>();
    ///<summary> 플레이어 스킬 시전 시 선택한 타겟들 idx 저장 </summary>
    List<int> targetIdxs = new List<int>();


    ///<summary> 턴 종료 버튼 </summary>
    [Header("UI")]
    [SerializeField] GameObject turnEndBtn;


    ///<summary> 체력 및 이름 표시 UI, 0 플레이어, 1 ~ 3 적 </summary>
    [Header("Unit Status")]
    [SerializeField] Status[] unitStatus;
    ///<summary> 적 일러스트 </summary>
    [SerializeField] Image[] enemyIilusts;
    ///<summary> 캐릭터들의 TP 표시 </summary>
    [SerializeField] TPSlider tpBars;
    ///<summary> 플레이어 AP 표시 UI </summary>
    [SerializeField] APBar apBar;
    ///<summary> Show Player Stat Txt </summary>
    [SerializeField] Text[] statusTxts;
    ///<summary> 포션 버튼 </summary>
    [SerializeField] Image[] potionBtns;
    public BuffToken buffTokenPrefab;
    public RectTransform poolParent;
    public Queue<BuffToken> buffTokenPool = new Queue<BuffToken>();

    List<AsyncOperationHandle<Sprite>> enemyIilustHandles = new List<AsyncOperationHandle<Sprite>>();


    ///<summary> 스킬 선택 UI, 내 턴에 활성화 </summary>
    [Header("Skill Panel")]
    [SerializeField] GameObject skillBtnPanel;
    ///<summary> 현재 선택한 스킬 버튼 Idx(0 ~ 5) </summary>
    int selectSkillBtnIdx;
    ///<summary> 각각 스킬 선택 버튼, 스킬 수만큼 활성화 </summary>
    [SerializeField] SkillButton[] skillBtns;
    ///<summary> 스킬 설명, 0 이름, 1 긍정, 2 부정 </summary>
    [SerializeField] Text[] skillTxts;

    ///<summary> 비전 마스터 동시 시전 여부 </summary>
    bool isBoth = false;
    ///<summary> 비전 마스터 스킬 선택 0 양, 1 음 </summary>
    int isMinus = 0;
    ///<summary> 비전 마스터 스킬 선택 UI </summary>
    [SerializeField] GameObject skillChoosePanel;

    ///<summary> 타겟 선택 UI, 스킬 선택 시 활성화 </summary>
    [SerializeField] GameObject targetBtnPanel;
    ///<summary> 각각 타겟 선택 버튼, 타겟 수만큼 활성화 </summary>
    [SerializeField] GameObject[] targetBtns;


    ///<summary> 승리 시 활성화 </summary>
    [Header("End Panel")]
    [SerializeField] GameObject winPanel;
    ///<summary> 던전 종료 UI, 패배, 보스 승리 시 보여줌 </summary>
    [SerializeField] ReportPanel reportPanel;
    #endregion Variables

    #region Function_Start
    ///<summary> BGM 재생, 몬스터 및 캐릭터 생성 </summary>
    private void Start()
    {
        //BGM 재생
        SoundManager.instance.PlayBGM(BGMList.Battle1);
        //방 몬스터 및 보상 정보 로드
        roomInfo = new RoomInfo(GameManager.instance.slotData.dungeonData.currRoomEvent);
        //캐릭터 및 몬스터 생성
        Spawn();

        //캐릭터 스킬 정보 로드
        SkillBtnInit();
        SkillAPCostUpdate();

        foreach (Unit c in allChars)
            c.OnBattleStart(this);

        LoadDungeonBuff();
        LoadHPData();

        SetStatusTxt();
        StatusUpdate();
    }
    void Spawn()
    {
        int i;

        //아군 캐릭터 생성
        Character c = Instantiate(alliePrefabs[GameManager.instance.slotData.slotClass]).GetComponent<Character>();
        c.name = GameManager.instance.slotData.className;

        for (i = 0; i < c.activeIdxs.Length; i++)
            c.activeIdxs[i] = GameManager.instance.slotData.activeSkills[i];
        for (i = 0; i < c.passiveIdxs.Length; i++)
            c.passiveIdxs[i] = GameManager.instance.slotData.passiveSkills[i];

        allChars[0] = c;


        //골렘 생성
        if (GameManager.instance.slotData.dungeonData.golemHP >= 0)
        {
            Golem g = Instantiate(alliePrefabs[9]).GetComponent<Golem>();
            g.GolemInit(allChars[0].GetComponent<MadScientist>());
            allChars[1] = g;
        }
        else
            allChars[1] = dummyUnit;


        //던전 풀에 따른 적 캐릭터 생성
        for (i = 0; i < roomInfo.monsterCount; i++)
        {
            Monster mon = Instantiate(enemyPrefab).GetComponent<Monster>();
            mon.monsterIdx = roomInfo.monsterIdx[i];
            allChars[i + 2] = mon;
        }
        LoadEnemyIilust();

        for (; i < 3; i++) { allChars[i + 2] = dummyUnit; unitStatus[i + 1].gameObject.SetActive(false); enemyIilusts[i].gameObject.SetActive(false); }
    }

    async void LoadEnemyIilust()
    {
        List<Task> tasks = new List<Task>();

        for(int i = 0;i < roomInfo.monsterCount;i++)
        {
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>($"Mon{allChars[i + 2].GetComponent<Monster>().monsterIdx.ToString("D3")}");
            enemyIilustHandles.Add(handle);
            tasks.Add(handle.Task);
        }

        await Task.WhenAll(tasks);

        for(int i = 0;i < roomInfo.monsterCount;i++)
            enemyIilusts[i].sprite = enemyIilustHandles[i].Result;
        fadeAnimator.SetBool("Fade", true);
    }

    void LoadDungeonBuff()
    {
        //던전 이벤트로 생긴 버프, 디버프 처리
        foreach (DungeonBuff b in GameManager.instance.slotData.dungeonData.dungeonBuffs)
            allChars[0].turnBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, b.name, b.objIdx, 1, (float)b.rate, 1, 99, 0, 1));
        foreach (DungeonBuff b in GameManager.instance.slotData.dungeonData.dungeonDebuffs)
            allChars[0].turnDebuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, b.name, b.objIdx, 1, (float)b.rate, 1, 99, 0, 1));
    }
    void LoadHPData()
    {
        //캐릭터 현재 체력 불러오기 - 던전 최초 시작 시 -1로 설정되어 있음 -> 음수인 경우 최대 체력 로드
        if (GameManager.instance.slotData.dungeonData.currHP > 0)
            allChars[0].buffStat[(int)Obj.currHP] = GameManager.instance.slotData.dungeonData.currHP;
        else
            allChars[0].buffStat[(int)Obj.currHP] = allChars[0].buffStat[(int)Obj.체력];

        //드루이드 - 부활 여부 불러오기
        if (allChars[0].classIdx == 6)
            allChars[0].GetComponent<Druid>().revive = GameManager.instance.slotData.dungeonData.druidRevive;
        //매드 사이언티스트 - 골렘 체력 불러오기
        if (GameManager.instance.slotData.dungeonData.golemHP > 0)
            allChars[1].buffStat[(int)Obj.currHP] = GameManager.instance.slotData.dungeonData.golemHP;
        else if (GameManager.instance.slotData.dungeonData.golemHP == 0)
            allChars[1].buffStat[(int)Obj.currHP] = allChars[1].buffStat[(int)Obj.체력];
    }
    void SetStatusTxt()
    {
        for (int i = 0, j = 0; i < allChars.Length; i++)
        {
            if (allChars[i] == dummyUnit || i == 1)
                continue;
            unitStatus[j++].StatusInit(allChars[i]);
        }

        for(int i = 0;i < 2;i++)
        {
            potionBtns[i].sprite = SpriteGetter.instance.GetPotionIcon(GameManager.instance.slotData.potionSlot[i]);
            potionBtns[i].gameObject.SetActive(GameManager.instance.slotData.potionSlot[i] > 0);
        }
    }
    ///<summary> 전투 시작 시 1번만 호출, 아군, 적군 정보 불러오기, 버프 및 디버프 설정, TP값 초기화 </summary>
    public void BattleStart()
    {
        state = BattleState.Start;

        foreach (Unit u in allChars)
            if (u.isActiveAndEnabled)
                charTP.Add(u, new int[2] { 0, 0 });

        TPMaxUpdate();
        SelectNextCaster();
    }
    ///<summary> 액티브 스킬 수만큼 스킬 버튼 활성화 </summary>
    void SkillBtnInit()
    {
        for (int i = 0; i < skillBtns.Length; i++)
        {
            if (GameManager.instance.slotData.activeSkills[i] > 0)
            {
                skillBtns[i].gameObject.SetActive(true);
                skillBtns[i].Init(SkillManager.GetSkill(GameManager.instance.slotData.slotClass, GameManager.instance.slotData.activeSkills[i]));
            }
            else
                skillBtns[i].gameObject.SetActive(false);
        }
    }
    #endregion Function_Start

    #region Function_TP
    ///<summary> 속도 값 변했을 때, TP 최대 값 변화 </summary>
    void TPMaxUpdate()
    {
        foreach (Unit u in allChars)
            if (u.isActiveAndEnabled)
                charTP[u][1] = Mathf.Max(25, 75 - u.buffStat[(int)Obj.속도]);

        tpBars.ActiveSet(allChars);
        TPImageUpdate();
    }
    ///<summary> TP 이미지 위치 재조정 </summary>
    void TPImageUpdate()
    {
        for (int i = 0; i < allChars.Length; i++)
            if (allChars[i].isActiveAndEnabled)
                tpBars.SetValue(i, (float)charTP[allChars[i]][0] / charTP[allChars[i]][1]);
    }
    
    ///<summary> 다음 턴 시전자 탐색 </summary>
    public void SelectNextCaster()
    {
        if (state == BattleState.Calc)
            return;

        StartCoroutine(TPCalculate());
    }
    ///<summary> TP 계산 </summary>
    IEnumerator TPCalculate()
    {
        state = BattleState.Calc;

        //TP가 찬 캐릭터가 이미 있는 경우
        if (casterQueue.Count > 0)
        {
            //생존 여부 확인 후 그 캐릭터 턴 시작
            Unit u;
            do { u = casterQueue[0]; casterQueue.RemoveAt(0); } while (!u.isActiveAndEnabled && casterQueue.Count > 0);

            if (u.isActiveAndEnabled)
            {
                currCaster = u;
                TurnAct();
                yield break;
            }
        }

        //TP가 가득 찬 캐릭터들
        List<Unit> charged = new List<Unit>();

        //TP 상승
        while (charged.Count == 0)
        {
            foreach (Unit u in allChars)
            {
                if (u.isActiveAndEnabled)
                {
                    charTP[u][0]++;
                    if (charTP[u][0] >= charTP[u][1])
                        charged.Add(u);
                }
            }

            TPImageUpdate();
            yield return new WaitForSeconds(0.01f);
        }

        //TP 최대치 도달 유닛이 둘 이상인 경우
        if (charged.Count > 1)
        {
            Shuffle(charged);
            //속도 - 레벨 기준 정렬
            charged.Sort(delegate (Unit a, Unit b)
            {
                if (a.buffStat[(int)Obj.속도] < b.buffStat[(int)Obj.속도])
                    return 1;
                else if (a.buffStat[(int)Obj.속도] > b.buffStat[(int)Obj.속도])
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
    ///<summary> 다음 턴 행동 대상이 선정되었을 때 호출 </summary>
    void TurnAct()
    {
        if (currCaster.classIdx == 10)
        {
            state = BattleState.EnemyTurn;
            EnemyTurn();
        }
        else
        {
            state = BattleState.AllieTurnStart;
            AllieTurnStart();
        }
    }
    #endregion Function_TP

    #region Update
    ///<summary> 플레이어 스텟 텍스트 및 적 체력 최신화 </summary>
    void StatusUpdate()
    {
        //체력 최신화
        for (int i = 0, j = 0; i < allChars.Length; i++)
        {
            if (allChars[i] == dummyUnit || i == 1)
                continue;
            unitStatus[j++].StatusUpdate(allChars[i]);
        }
        //스텟 텍스트 최신화
        StatTxtUpdate();

        void StatTxtUpdate()
        {
            for (int i = 0; i < 8; i++)
                statusTxts[i].text = allChars[0].buffStat[i + 5].ToString();

            statusTxts[5].text = $"{statusTxts[5].text}%";
            statusTxts[6].text = $"{statusTxts[6].text}%";
        }
    }
    ///<summary> 스킬 AP 소모량 표시 최신화 </summary>
    void SkillAPCostUpdate()
    {
        for (int i = 0; i < skillBtns.Length; i++)
        {
            if (GameManager.instance.slotData.activeSkills[i] > 0)
            {
                Skill s = SkillManager.GetSkill(GameManager.instance.slotData.slotClass, GameManager.instance.slotData.activeSkills[i]);
                skillBtns[i].APUpdate(allChars[0].GetSkillCost(s));
            }
        }
    }
    #endregion

    #region Function_AllieTurn
    ///<summary> 아군 턴인 경우, 선택 UI 보이기, 캐릭터 스킬 수에 따라 버튼 활성화, AP 초기화 </summary>
    void AllieTurnStart()
    {
        if (state != BattleState.AllieTurnStart) return;

        currCaster.OnTurnStart();
        StatusUpdate();

        //정령, 골렘, 기절 중 - 알아서 행동 후 턴 종료
        if (currCaster.classIdx == 11 || currCaster.classIdx == 12 || currCaster.IsStun())
        {
            if (IsWin())
                Win();
            else
                Btn_TurnEnd();
        }
        else
        {
            turnEndBtn.SetActive(true);
            apBar.SetValue(currCaster.buffStat[(int)Obj.currAP], currCaster.buffStat[(int)Obj.행동력]);

            Btn_SkillCancel();

            if (IsWin())
                Win();
        }

    }
    ///<summary> 스킬 선택 버튼, 타겟 선택 창 활성화 </summary>
    public void Btn_SkillSelect(int buttonIdx)
    {
        if (state != BattleState.AllieTurnStart)
            return;

        Skill skill = SkillManager.GetSkill(currCaster.classIdx, GameManager.instance.slotData.activeSkills[buttonIdx]);

        if (selectSkillBtnIdx == buttonIdx)
        {
            foreach(Text txt in skillTxts) txt.text = string.Empty;
            
            //비전 마스터 선택 스킬
            if (skill.category == 1023)
            {
                //둘 다 시전
                if (currCaster.GetComponent<VisionMaster>().skillState > 1)
                {
                    isBoth = true;
                    state = BattleState.AllieSkillSelected;

                    Skill minusS = SkillManager.GetSkill(currCaster.classIdx, GameManager.instance.slotData.activeSkills[buttonIdx] + 1);

                    //타겟 선택
                    if (skill.targetSelect == 1 || minusS.targetSelect == 1)
                    {
                        isMinus = skill.targetSelect == 1 ? 0 : 1;

                        targetIdxs.Clear();
                        state = BattleState.AllieSkillSelected;

                        for (int i = 0; i < 3; i++)
                            targetBtns[i].SetActive(allChars[i + 2].isActiveAndEnabled);


                        skillBtnPanel.SetActive(false);
                        targetBtnPanel.SetActive(true);
                    }
                    //타겟 미선택
                    else
                    {
                        state = BattleState.AllieTargetSelected;
                        currCaster.GetComponent<VisionMaster>().ActiveSkill_Both(buttonIdx, new List<Unit>());

                        apBar.SetValue(currCaster.buffStat[(int)Obj.currAP], currCaster.buffStat[(int)Obj.행동력]);

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
                    selectSkillBtnIdx = buttonIdx;
                }
            }
            //그 외 스킬
            else
            {
                string castLog = currCaster.CanCastSkill(buttonIdx);

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
                        targetBtns[i].SetActive(allChars[i + 2].isActiveAndEnabled);

                    skillBtnPanel.SetActive(false);
                    targetBtnPanel.SetActive(true);

                    selectSkillBtnIdx = buttonIdx;
                }
                //타겟 미선택 스킬
                else
                {
                    state = BattleState.AllieTargetSelected;
                    currCaster.ActiveSkill(buttonIdx, new List<Unit>());
                    apBar.SetValue(currCaster.buffStat[(int)Obj.currAP], currCaster.buffStat[(int)Obj.행동력]);

                    StatusUpdate();

                    Btn_SkillCancel();

                    if (IsWin())
                        Win();
                }
            }
        }
        else
        {
            skillTxts[0].text = skill.name;
            skillTxts[1].text = skill.posScript; skillTxts[2].text= skill.negScript;
            selectSkillBtnIdx = buttonIdx;

            for (int i = 0; i < skillBtns.Length; i++)
                skillBtns[i].Highlight(i == selectSkillBtnIdx);
        }

        bool IsUniqueCondition()
        {
            if (109 <= skill.idx && skill.idx <= 111)
            {
                Elemental e = allChars[1].GetComponent<Elemental>();
                if (!allChars[1].isActiveAndEnabled || e == null || e.type != skill.category || e.isUpgraded)
                {
                    LogManager.instance.AddLog("정령을 소환한 후 사용해야 합니다.");
                    return true;
                }
            }
            if (skill.idx == 121)
            {
                Elemental e = allChars[1].GetComponent<Elemental>();
                if (!allChars[1].isActiveAndEnabled || e == null || !e.isUpgraded)
                {
                    LogManager.instance.AddLog("강화된 정령이 필요합니다.");
                    return true;
                }
            }
            else if (currCaster.classIdx == 6 && skill.effectType[0] == 39 && currCaster.GetComponent<Druid>().currVitality < skill.effectRate[0])
            {
                LogManager.instance.AddLog("생명력이 부족합니다.");
                return true;
            }

            return false;
        }
    }
    ///<summary> 비전 마스터 스킬 선택 버튼 </summary>
    ///<param name="isMinus"> 0 양, 1 음 </param>
    public void Btn_SkillChoose(int isMinus)
    {
        Skill skill = SkillManager.GetSkill(currCaster.classIdx, GameManager.instance.slotData.activeSkills[selectSkillBtnIdx] + isMinus);

        currCaster.GetComponent<VisionMaster>().skillState = isMinus;
        this.isMinus = isMinus;

        string castLog = currCaster.CanCastSkill(selectSkillBtnIdx);

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
                targetBtns[i].SetActive(allChars[i + 2].isActiveAndEnabled);

            //랜덤 타겟, 전체 타겟 등 타겟 선택이 필요 없는 경우 예외 처리
            skillChoosePanel.SetActive(false);
            targetBtnPanel.SetActive(true);
        }
        //타겟 미선택 스킬
        else
        {
            state = BattleState.AllieTargetSelected;
            currCaster.ActiveSkill(selectSkillBtnIdx, new List<Unit>());

            apBar.SetValue(currCaster.buffStat[(int)Obj.currAP], currCaster.buffStat[(int)Obj.행동력]);

            StatusUpdate();
            Btn_SkillCancel();

            if (IsWin())
                Win();
        }
    }
    ///<summary> 스킬 선택 취소 버튼, 스킬 선택 전 상태로 돌아감 </summary>
    public void Btn_SkillCancel()
    {
        selectSkillBtnIdx = -1;
        foreach (SkillButton s in skillBtns)
            s.Highlight(false);

        foreach(Text t in skillTxts) t.text = string.Empty;

        isBoth = false;
        isMinus = 0;
        targetIdxs.Clear();
        state = BattleState.AllieTurnStart;
        targetBtnPanel.SetActive(false);
        skillChoosePanel.SetActive(false);
        skillBtnPanel.SetActive(true);
    }
    ///<summary> 타겟 선택 버튼, 스킬 시전 </summary>
    public void Btn_TargetSelect(int idx)
    {
        if (targetIdxs.Contains(idx))
            targetIdxs.Remove(idx);
        else
            targetIdxs.Add(idx);

        Skill s = SkillManager.GetSkill(currCaster.classIdx, GameManager.instance.slotData.activeSkills[selectSkillBtnIdx] + isMinus);

        int activeCount = 0;
        for (int i = 2; i < 5; i++) if (allChars[i].isActiveAndEnabled) activeCount++;

        if (targetIdxs.Count == s.targetCount || targetIdxs.Count == activeCount)
        {
            List<Unit> selects = new List<Unit>();
            foreach (int i in targetIdxs)
                selects.Add(allChars[i]);

            state = BattleState.AllieTargetSelected;
            if (isBoth)
                currCaster.GetComponent<VisionMaster>().ActiveSkill_Both(selectSkillBtnIdx, selects);
            else
                currCaster.ActiveSkill(selectSkillBtnIdx, selects);

            isBoth = false;
            isMinus = 0;

            apBar.SetValue(currCaster.buffStat[(int)Obj.currAP], currCaster.buffStat[(int)Obj.행동력]);

            StatusUpdate();
            Btn_SkillCancel();
            
            if (IsWin())
                Win();
        }
    }
    ///<summary> 포션 사용 버튼 </summary>
    public void Btn_UsePotion(int slotIdx)
    {
        if(state != BattleState.AllieTurnStart) return;

        if(GameManager.instance.slotData.potionSlot[slotIdx] == 0)
            LogManager.instance.AddLog("착용한 포션이 없습니다.");
        else if (GameManager.instance.slotData.dungeonData.potionUse[slotIdx])
            LogManager.instance.AddLog("이미 사용했습니다.");
        else if(GameManager.instance.slotData.potionSlot[slotIdx] == 4 && !GameManager.instance.slotData.dungeonData.potionUse[(slotIdx + 1) % 2])
            LogManager.instance.AddLog("다른 포션을 아직 사용할 수 있습니다.");
        else
        {
            switch(GameManager.instance.slotData.potionSlot[slotIdx])
            {
                //행동력 포션 - 행동력 최대로 회복
                case 1:
                    if(allChars[0].buffStat[(int)Obj.currAP] >= allChars[0].buffStat[(int)Obj.행동력])
                    {
                        LogManager.instance.AddLog("행동력이 최대치입니다.");
                        return;
                    }
                    allChars[0].GetAPHeal(allChars[0].buffStat[(int)Obj.행동력]);
                    apBar.SetValue(currCaster.buffStat[(int)Obj.currAP], currCaster.buffStat[(int)Obj.행동력]);
                    break;
                //정화 포션 - 모든 디버프 제거
                case 2:
                    if (!allChars[0].turnDebuffs.buffs.Any(x => x.isDispel))
                    {
                        LogManager.instance.AddLog("해제 가능한 디버프가 없습니다.");
                        return;
                    }
                    allChars[0].RemoveDebuff(allChars[0].turnDebuffs.Count);
                    break;
                //회복 포션 - 체력 최대로 회복
                case 3:
                    if(allChars[0].buffStat[(int)Obj.currHP] >= allChars[0].buffStat[(int)Obj.체력])
                    {
                        LogManager.instance.AddLog("체력이 최대치입니다.");
                        return;
                    }
                    allChars[0].GetHeal(allChars[0].buffStat[(int)Obj.체력]);
                    break;
                //재활용 포션 - 다른 포션 재사용 가능
                case 4:
                    GameManager.instance.slotData.dungeonData.potionUse[(slotIdx + 1) % 2] = false;
                    break;
            }

            GameManager.instance.slotData.dungeonData.potionUse[slotIdx] = true;
            
            StatusUpdate();
        }
    }
    ///<summary> 턴 종료 버튼 </summary>
    public void Btn_TurnEnd()
    {
        if (state != BattleState.AllieTurnStart) return;

        Btn_SkillCancel();
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
        yield return new WaitForSeconds(0.5f);

        SelectNextCaster();
    }
    #endregion

    #region Function_EnemyTurn
    ///<summary> 적 턴, 아군 캐릭터 대상으로 정해진 스킬 시전 </summary>
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
        yield return new WaitForSeconds(0.5f);

        SelectNextCaster();
    }
    #endregion

    #region Function_BattleEnd
    bool IsWin()
    {
        for(int i = 2;i < 5;i ++) 
        if (allChars[i].buffStat[(int)Obj.currHP] <= 0 || allChars[i].classIdx == 0) 
            {
                allChars[i].gameObject.SetActive(false);
                enemyIilusts[i - 2].gameObject.SetActive(false);
            }

        for (int i = 2; i < 5; i++) if (allChars[i].isActiveAndEnabled) return false;
        return true;
    }
    bool IsLose()
    {
        foreach (Unit u in allChars) if (u.buffStat[(int)Obj.currHP] <= 0 || u.classIdx == 0) u.gameObject.SetActive(false);

        for (int i = 0; i < 2; i++) if (allChars[i].isActiveAndEnabled) return false;
        return true;
    }
    ///<summary> 승리, 보상 획득, 탐험 계속 진행 </summary>
    void Win()
    {
        state = BattleState.Win;
        skillBtnPanel.SetActive(false);
        targetBtnPanel.SetActive(false);
        turnEndBtn.SetActive(false);

        //보상 획득
        for (int i = 0; i < roomInfo.ItemCount; i++)
            ItemManager.ItemDrop(roomInfo.ItemIdx[i], roomInfo.ItemChance[i]);
        GameManager.instance.GetExp(roomInfo.roomExp);
        QuestManager.QuestUpdate(QuestType.Battle, 0, 1);
        //체력 유지 퀘스트 업데이트
        QuestManager.DiehardUpdate((float)allChars[0].buffStat[(int)Obj.currHP] / allChars[0].buffStat[(int)Obj.체력]);

        //체력 상태 저장
        GameManager.instance.slotData.dungeonData.currHP = allChars[0].buffStat[(int)Obj.currHP];
        //골렘 체력 저장
        if (allChars[0].classIdx == 4 && allChars[1].isActiveAndEnabled)
            GameManager.instance.slotData.dungeonData.golemHP = allChars[1].buffStat[(int)Obj.currHP];
        else
            GameManager.instance.slotData.dungeonData.golemHP = -1;
        //드루이드 부활 여부 저장
        if (allChars[0].classIdx == 6)
            GameManager.instance.slotData.dungeonData.druidRevive = allChars[0].GetComponent<Druid>().revive;

        LogManager.instance.AddLog("승리했습니다.");

        //보스방 승리 -> 보고서 보이기
        if (GameManager.instance.slotData.dungeonData.currPos[0] == GameManager.instance.slotData.dungeonData.currDungeon.floorCount - 1)
        {
            if (GameManager.instance.slotData.questData.outbreakProceed.type == QuestType.Diehard_Over || GameManager.instance.slotData.questData.outbreakProceed.type == QuestType.Diehard_Under) 
                GameManager.instance.slotData.questData.outbreakProceed.state = QuestState.CanClear;

            QuestManager.QuestUpdate(QuestType.Dungeon, GameManager.instance.slotData.dungeonIdx, 1);

            reportPanel.LoadData();
            reportPanel.gameObject.SetActive(true);
            GameManager.instance.RemoveDungeonData();
            GameManager.instance.SwitchSceneData(SceneKind.Town);
        }
        else
        {
            GameManager.instance.SwitchSceneData(SceneKind.Dungeon);
            GameManager.instance.UpdateDungeonBuff();
            winPanel.SetActive(true);
        }
    }
    ///<summary> 패배, 현재까지의 보상만 가진 채 마을로 귀환 </summary>
    void Lose()
    {
        state = BattleState.Lose;
        skillBtnPanel.SetActive(false);
        targetBtnPanel.SetActive(false);
        turnEndBtn.SetActive(false);
        LogManager.instance.AddLog("패배하였습니다.");

        reportPanel.LoadData();
        reportPanel.gameObject.SetActive(true);
        GameManager.instance.RemoveDungeonData();
        GameManager.instance.SwitchSceneData(SceneKind.Town);
    }
    ///<summary> 방 승리 -> 던전으로 돌아가기 </summary>
    public void Btn_BackToMap()
    { 
        for(int i = 0;i < enemyIilustHandles.Count;i++)
        {
            enemyIilusts[i].sprite = null;
            Addressables.Release(enemyIilustHandles[i]);
        }
        GameManager.instance.LoadScene(SceneKind.Dungeon);
    }
    public void Btn_BackToTown()
    {
        for(int i = 0;i < enemyIilustHandles.Count;i++)
        {
            enemyIilusts[i].sprite = null;
            Addressables.Release(enemyIilustHandles[i]);
        }
        GameManager.instance.LoadScene(SceneKind.Town);
    }
    
    #endregion

    #region Function_CharSkills
    ///<summary> TP 감소시키는 특수 스킬 </summary>
    public void ReduceTP(List<Unit> targets, int amt)
    {
        foreach (Unit u in targets)
            charTP[u][0] = Mathf.Max(0, charTP[u][0] - amt);
    }

    ///<summary> 매드 사이언티스트 골렘 생존 여부 </summary>
    public bool HasGolem() => allChars[1].isActiveAndEnabled;
    ///<summary> 매드 사이언티스트 골렘 조종 스킬 </summary>
    public void GolemControl(KeyValuePair<int, List<Unit>> token)
    {
        if (HasGolem())
            allChars[1].GetComponent<Golem>().AddControl(token);
    }

    ///<summary> 엘리멘탈 컨트롤러 정령 소환 </summary>
    public void SummonElemental(ElementalController caster, int type)
    {
        Unit tmp = allChars[1];
        charTP.Remove(tmp);
        casterQueue.Remove(tmp);
        if (tmp != dummyUnit)
            Destroy(tmp.gameObject);


        Elemental e = Instantiate(alliePrefabs[10]).GetComponent<Elemental>();
        e.Summon(this, caster, type);

        charTP.Add(e, new int[2] { 0, 0 });
        allChars[1] = e;

        TPMaxUpdate();
    }
    ///<summary> 엘리멘탈 컨트롤러 상위 정령 소환 </summary>
    public void UpgradeElemental(ElementalController caster, int type)
    {
        Unit tmp = allChars[1];
        charTP.Remove(tmp);
        casterQueue.Remove(tmp);
        if (tmp != dummyUnit)
            Destroy(tmp.gameObject);

        Elemental e = Instantiate(alliePrefabs[10]).GetComponent<Elemental>();
        e.Summon(this, caster, type, true);

        charTP.Add(e, new int[2] { 0, 0 });
        allChars[1] = e;

        TPMaxUpdate();
    }
    ///<summary> 엘리멘탈 컨트롤러 정령 희생 스킬 </summary>
    public int SacrificeElemental(ElementalController caster, Skill skill)
    {
        int type = -1;
        if (allChars[1].GetComponent<Elemental>())
        {
            Unit tmp = allChars[1];
            type = tmp.GetComponent<Elemental>().type;

            charTP.Remove(tmp);
            casterQueue.Remove(tmp);
            Destroy(tmp.gameObject);

            allChars[1] = dummyUnit;
        }

        return type;
    }
    ///<summary> 바람 정령 희생 - 맞은 적 TP 0으로 </summary>
    public void Sacrifice_TP(List<Unit> targets)
    {
        foreach (Unit u in targets)
            charTP[u][0] = 0;
        TPImageUpdate();
    }

    ///<summary> 포병 장전 </summary>
    public bool ReloadBullet()
    {
        List<Monster> mons = new List<Monster>();
        for (int i = 2; i < 5; i++) if (allChars[i].isActiveAndEnabled) mons.Add(allChars[i].GetComponent<Monster>());
        var ene = from x in mons where x.monsterIdx == 10 || x.monsterIdx == 11 select x;

        if (ene.Count() <= 0)
            return false;

        Monster m = ene.First();
        charTP.Remove(m);
        casterQueue.Remove(m);
        m.gameObject.SetActive(false);

        return true;
    }
    ///<summary> 돈키호테 - 무작위 적 1기 TP 최대로 </summary>
    public void Quixote()
    {
        Unit m = GetEffectTarget(4)[0];
        charTP[m][0] = charTP[m][1];
    }
    #endregion Function_CharSkills

    ///<summary> 효과 대상 반환 </summary>
    public List<Unit> GetEffectTarget(int targetIdx)
    {
        List<Unit> tmp = new List<Unit>();
        switch (targetIdx)
        {
            //아군 측 랜덤 1개체
            case 2:
                if (HasUpgradedElemental())
                {
                    tmp.Add(allChars[1]);
                    return tmp;
                }
                else if (IsMadSpecialCondition())
                {
                    tmp.Add(allChars[0]);
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
                tmp.Add(allChars[0]);
                return tmp;
            default:
                return tmp;
        }

        bool HasUpgradedElemental()
        {
            Elemental e = allChars[1].GetComponent<Elemental>();
            if (e == null)
                return false;
            return e.isUpgraded && allChars[1].isActiveAndEnabled;
        }
        bool IsMadSpecialCondition() => allChars[0].classIdx == 4 && allChars[0].GetComponent<MadScientist>().isMagnetic;
        List<Unit> RandomList(int type, int count)
        {
            List<Unit> baseList = new List<Unit>();
            switch (type)
            {
                case 0:
                    for (int i = 0; i < 2; i++) if (allChars[i].isActiveAndEnabled) baseList.Add(allChars[i]);
                    break;
                case 1:
                    for (int i = 2; i < 5; i++) if (allChars[i].isActiveAndEnabled) baseList.Add(allChars[i]);
                    break;
                case 2:
                    for (int i = 0; i < 5; i++) if (allChars[i].isActiveAndEnabled) baseList.Add(allChars[i]);
                    break;
            }

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
                    for (int i = 0; i < 2; i++) if (allChars[i].isActiveAndEnabled) baseList.Add(allChars[i]);
                    break;
                case 1:
                    for (int i = 2; i < 5; i++) if (allChars[i].isActiveAndEnabled) baseList.Add(allChars[i]);
                    break;
                case 2:
                    for (int i = 0; i < 5; i++) if (allChars[i].isActiveAndEnabled) baseList.Add(allChars[i]);
                    break;
            }

            return baseList;
        }
    }
}
