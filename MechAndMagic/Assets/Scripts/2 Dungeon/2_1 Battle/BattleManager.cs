using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public enum BattleState { Start, Calc, AllieTurnStart, AllieSkillSelected, AllieTargetSelected, EnemyTurn, Win, Lose }



//1. ��ư�� enemy 1:1 ��Ī, ��ư ��ġ ����
public class BattleManager : MonoBehaviour
{
    #region Variables
    BattleState state;

    #region CharList
    //���� ���� ��� ĳ����
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
    //ĳ���͵��� TP �ִ�ġ, ���� ���� �� ���
    [SerializeField] TPSlider tpBars;
    Dictionary<Unit, int[]> charTP = new Dictionary<Unit, int[]>();

    //TP�� ���� ������ ���� �� ������
    Unit currCaster;

    //TP�� ������ ��, �ӵ��� ���� �������� ������� queue�� ����
    List<Unit> casterQueue = new List<Unit>();
    List<int> targetIdxs = new List<int>();
    #endregion

    #region UI
    [Header("UI")]
    //�Ʊ� Ÿ�� ���� ����
    [SerializeField] GameObject startBtn;         //���� ���� ���� ��ư
    [SerializeField] GameObject turnEndBtn;

    [Header("Unit Status")]
    [SerializeField] Status[] unitStatus;
    [SerializeField] APBar apBar;
    [SerializeField] Text[] statusTxts;

    int skillIdx;
    [Header("Skill Panel")]
    [SerializeField] Sprite[] skillIcons;
    [SerializeField] GameObject skillBtnPanel;    //��ų ���� UI, �� �Ͽ� Ȱ��ȭ
    [SerializeField] SkillButton[] skillBtns;      //���� ��ų ���� ��ư, ��ų ����ŭ Ȱ��ȭ
    [SerializeField] GameObject skillTxtPanel;
    [SerializeField] Text[] skillTxts;

    bool isBoth = false;
    int isMinus = 0;
    [SerializeField] GameObject skillChoosePanel; //���� ������ ��ų ����

    [SerializeField] GameObject targetBtnPanel;   //Ÿ�� ���� UI, ��ų ���� �� Ȱ��ȭ
    [SerializeField] GameObject[] targetBtns;     //���� Ÿ�� ���� ��ư, Ÿ�� ����ŭ Ȱ��ȭ

    [Header("End Panel")]
    [SerializeField] GameObject winUI;
    [SerializeField] GameObject bossWinUI;
    [SerializeField] GameObject loseUI;
    #endregion
    #endregion Variables

    #region Function_Start
    //BGM ���, ���� �� ĳ���� ����
    public void OnStart()
    {
        //
        GameManager.sound.PlayBGM(BGM.Battle1);
        if (GameManager.slotData.dungeonState.currRoomEvent > 100)
            roomInfo = new RoomInfo(1);
        else
            roomInfo = new RoomInfo(GameManager.slotData.dungeonState.currRoomEvent);

        Spawn();

        SkillBtnInit();
        SkillBtnUpdate();

        void Spawn()
        {
            int i;

            //�Ʊ� ĳ���� ����
            Character c = Instantiate(alliePrefabs[GameManager.slotData.slotClass], alliePos.position, Quaternion.identity).GetComponent<Character>();
            for (i = 0; i < c.activeIdxs.Length; i++)
                c.activeIdxs[i] = GameManager.slotData.activeSkills[i];
            for (i = 0; i < c.passiveIdxs.Length; i++)
                c.passiveIdxs[i] = GameManager.slotData.passiveSkills[i];

            allCharList.Add(c);

            //�� ����
            if (GameManager.slotData.dungeonState.golemHP >= 0)
            {
                Golem g = Instantiate(alliePrefabs[11], alliePos.position + new Vector3(1, 0, 0), Quaternion.identity).GetComponent<Golem>();
                g.GolemInit(allCharList[0].GetComponent<MadScientist>());
                allCharList.Add(g);
            }
            else
                allCharList.Add(DummyUnit);

            //���� Ǯ�� ���� �� ĳ���� ����
            for (i = 0; i < roomInfo.monsterCount; i++)
            {
                Monster mon = Instantiate(enemyPrefabs[roomInfo.monsterIdx[i]], alliePos.position, Quaternion.identity).GetComponent<Monster>();
                allCharList.Add(mon);
            }
            for (; i < 3; i++) { allCharList.Add(DummyUnit); unitStatus[i + 1].gameObject.SetActive(false); }
        }
    }

    //���� ���� �� 1���� ȣ��, �Ʊ�, ���� ���� �ҷ�����, ���� �� ����� ����, TP�� �ʱ�ȭ
    public void BattleStart()
    {
        state = BattleState.Start;
        startBtn.SetActive(false);

        //���� �̺�Ʈ�� ���� ����, ����� ó��
        foreach (DungeonBuff b in GameManager.slotData.dungeonState.dungeonBuffs)
            allCharList[0].turnBuffs.Add(new Buff(BuffType.Stat, allCharList[0].LVL, new BuffOrder(), b.name, b.objIdx, 1, (float)b.rate, 1, 99, 0, 1));
        foreach (DungeonBuff b in GameManager.slotData.dungeonState.dungeonDebuffs)
            allCharList[0].turnDebuffs.Add(new Buff(BuffType.Stat, allCharList[0].LVL, new BuffOrder(), b.name, b.objIdx, 1, (float)b.rate, 1, 99, 0, 1));

        foreach (Unit c in allCharList)
            c.OnBattleStart(this);

        //ĳ���� ���� ü�� �ҷ�����
        if (GameManager.slotData.dungeonState.currHP > 0)
            allCharList[0].buffStat[(int)Obj.currHP] = GameManager.slotData.dungeonState.currHP;
        else
            allCharList[0].buffStat[(int)Obj.currHP] = allCharList[0].buffStat[(int)Obj.HP];

        //����̵� - ��Ȱ ���� �ҷ�����
        if (allCharList[0].classIdx == 6)
            allCharList[0].GetComponent<Druid>().revive = GameManager.slotData.dungeonState.druidRevive;
        //�ŵ� ���̾�Ƽ��Ʈ - �� ü�� �ҷ�����
        if (GameManager.slotData.dungeonState.golemHP == 0)
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
    //�ӵ��� ������ ��, TP �ִ밪 ������Ʈ
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
    //���� �� ������ Ž��
    public void SelectNextCaster()
    {
        if (state == BattleState.Calc)
            return;

        StartCoroutine(TPCalculate());
    }
    //TP ���
    IEnumerator TPCalculate()
    {
        state = BattleState.Calc;

        //TP�� �� ĳ���Ͱ� �̹� �ִ� ���
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

        //TP ���
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

        //TP �ִ�ġ ���� ������ �� �̻��� ���
        if (charged.Count > 1)
        {
            Shuffle(charged);
            //�ӵ� - ���� ���� ����
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

        //TP�� �ִ뿡 ������ ��� ĳ���͸� Queue�� ����
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

    //���� �� �ൿ ����� �����Ǿ��� �� ȣ��
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
            if (GameManager.slotData.activeSkills[i] > 0)
            {
                skillBtns[i].gameObject.SetActive(true);
                skillBtns[i].Init(SkillManager.GetSkill(GameManager.slotData.slotClass, GameManager.slotData.activeSkills[i]), skillIcons[0]);
            }
            else
                skillBtns[i].gameObject.SetActive(false);
        }
    }
    void SkillBtnUpdate()
    {
        for (int i = 0; i < skillBtns.Length; i++)
        {
            if (GameManager.slotData.activeSkills[i] > 0)
            {
                Skill s = SkillManager.GetSkill(GameManager.slotData.slotClass, GameManager.slotData.activeSkills[i]);
                skillBtns[i].APUpdate(allCharList[0].GetSkillCost(s));
            }
        }
    }
    #endregion

    #region Function_AllieTurn
    //�Ʊ� ���� ���, ���� UI ���̱�, ĳ���� ��ų ���� ���� ��ư Ȱ��ȭ, AP �ʱ�ȭ
    void AllieTurnStart()
    {
        if (state != BattleState.AllieTurnStart) return;

        //����, �� - �˾Ƽ� �ൿ �� �� ����
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
    //��ų ���� ��ư, Ÿ�� ���� â Ȱ��ȭ
    public void Btn_SkillSelect(int idx)
    {
        if (state != BattleState.AllieTurnStart)
            return;

        Skill skill = SkillManager.GetSkill(currCaster.classIdx, GameManager.slotData.activeSkills[idx]);

        if (skillIdx == idx)
        {
            skillTxtPanel.SetActive(false);
            //���� ������ ���� ��ų
            if (skill.category == 1023)
            {
                //�� �� ����
                if (currCaster.GetComponent<VisionMaster>().skillState > 1)
                {
                    isBoth = true;
                    state = BattleState.AllieSkillSelected;

                    Skill minusS = SkillManager.GetSkill(currCaster.classIdx, GameManager.slotData.activeSkills[idx] + 1);

                    //Ÿ�� ����
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
                    //Ÿ�� �̼���
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
                //�ϳ��� ���� ����
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
            //�� �� ��ų
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

                //Ÿ�� ���� ��ų
                if (skill.targetSelect == 1)
                {
                    targetIdxs.Clear();
                    state = BattleState.AllieSkillSelected;

                    for (int i = 0; i < 3; i++)
                        targetBtns[i].SetActive(allCharList[i + 2].isActiveAndEnabled);

                    //���� Ÿ��, ��ü Ÿ�� �� Ÿ�� ������ �ʿ� ���� ��� ���� ó��
                    skillBtnPanel.SetActive(false);
                    targetBtnPanel.SetActive(true);

                    skillIdx = idx;
                }
                //Ÿ�� �̼��� ��ų
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
            skillTxts[1].text = "���� ����"; skillTxts[2].text= "���� ����";
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
    //���� ������ ��ų ���� ��ư
    public void Btn_SkillChoose(int isMinus)
    {
        Skill skill = SkillManager.GetSkill(currCaster.classIdx, GameManager.slotData.activeSkills[skillIdx] + isMinus);

        currCaster.GetComponent<VisionMaster>().skillState = isMinus;
        this.isMinus = isMinus;

        Debug.Log(skill.name);

        string castLog = currCaster.CanCastSkill(skillIdx);

        if (castLog != "")
        {
            LogManager.instance.AddLog(castLog);
            return;
        }

        //Ÿ�� ���� ��ų
        if (skill.targetSelect == 1)
        {
            targetIdxs.Clear();
            state = BattleState.AllieSkillSelected;

            for (int i = 0; i < 3; i++)
                targetBtns[i].SetActive(allCharList[i + 2].isActiveAndEnabled);

            //���� Ÿ��, ��ü Ÿ�� �� Ÿ�� ������ �ʿ� ���� ��� ���� ó��
            skillChoosePanel.SetActive(false);
            targetBtnPanel.SetActive(true);
        }
        //Ÿ�� �̼��� ��ų
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
    //��ų ���� ��� ��ư, ��ų ���� �� ���·� ���ư�
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
    //Ÿ�� ���� ��ư, ��ų ����
    public void Btn_TargetSelect(int idx)
    {
        if (targetIdxs.Contains(idx))
            targetIdxs.Remove(idx);
        else
            targetIdxs.Add(idx);

        Skill s = SkillManager.GetSkill(currCaster.classIdx, GameManager.slotData.activeSkills[skillIdx] + isMinus);

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
        if (GameManager.slotData.dungeonState.potionUse[idx])
            LogManager.instance.AddLog("�̹� ����߽��ϴ�.");
        else
        {
            //��Ȱ�� ����
            int potionIdx = GameManager.slotData.potionSlot[idx] == 13 ? GameManager.slotData.potionSlot[(idx + 1) % 2] : GameManager.slotData.potionSlot[idx];

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
    //�� ��, �Ʊ� ĳ���� ������� ������ ��ų ����
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
    //�¸�, ���� ȹ��, Ž�� ��� ����
    void Win()
    {
        skillBtnPanel.SetActive(false);
        targetBtnPanel.SetActive(false);

        for (int i = 0; i < roomInfo.ItemCount; i++)
            ItemManager.ItemDrop(roomInfo.ItemIdx[i], roomInfo.ItemChance[i]);

        GameManager.slotData.dungeonState.currHP = allCharList[0].buffStat[(int)Obj.currHP];

        if (allCharList[0].classIdx == 4 && allCharList[1].isActiveAndEnabled)
            GameManager.slotData.dungeonState.golemHP = allCharList[1].buffStat[(int)Obj.currHP];
        else
            GameManager.slotData.dungeonState.golemHP = -1;

        if (allCharList[0].classIdx == 6)
            GameManager.slotData.dungeonState.druidRevive = allCharList[0].GetComponent<Druid>().revive;

        LogManager.instance.AddLog("�¸�");

        if (GameManager.slotData.dungeonState.currRoomEvent > 100)
        {
            string drops = "��� ���\n";
            bossWinUI.SetActive(true);
            foreach (Triplet<DropType, int, int> token in GameManager.slotData.dungeonState.dropList)
                drops = string.Concat(drops, token.first, " ", token.second, " ", token.third, "\n");

            Debug.Log(drops);
        }
        else
            winUI.SetActive(true);
    }
    public void Btn_BackToMap()
    {
        GameManager.SwitchSceneData(SceneKind.Dungeon);
        GameManager.UpdateBuff();
        QuestManager.QuestUpdate(QuestType.Battle, 0, 1);
        UnityEngine.SceneManagement.SceneManager.LoadScene("2_0 Dungeon");
    }

    //�й�, ��������� ���� ���� ä ������ ��ȯ
    void Lose()
    {
        skillBtnPanel.SetActive(false);
        targetBtnPanel.SetActive(false);
        LogManager.instance.AddLog("Lose");
        loseUI.SetActive(false);
    }

    public void Btn_BackToTown()
    {
        GameManager.GetExp(roomInfo.roomExp);
        GameManager.RemoveDungeonData();
        GameManager.SwitchSceneData(SceneKind.Town);
        UnityEngine.SceneManagement.SceneManager.LoadScene("1 Town");
    }
    #endregion

    #region Function_CharSkills
    public void ReduceTP(List<Unit> targets, int amt)
    {
        foreach (Unit u in targets)
            charTP[u][0] = Mathf.Max(0, charTP[u][0] - amt);
    }

    //�ŵ� ���̾�Ƽ��Ʈ
    public bool HasGolem() => allCharList[1].isActiveAndEnabled;
    public void GolemControl(KeyValuePair<int, List<Unit>> token)
    {
        if (HasGolem())
            allCharList[1].GetComponent<Golem>().AddControl(token);
    }

    //������Ż ��Ʈ�ѷ�
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

    //����
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
            //�Ʊ� �� ���� 1��ü
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
            //�Ʊ� �� ��ü
            case 3:
                return AllList(0);
            //���� �� ���� 1��ü
            case 4:
                return RandomList(1, 1);
            //���� �� ���� 2��ü
            case 5:
                return RandomList(1, 2);
            //���� �� ��ü
            case 6:
                return AllList(1);
            //�Ǿ� �̱��� ���� 1��ü
            case 7:
                return RandomList(2, 1);
            //�Ǿ� �̱��� ���� 2��ü
            case 8:
                return RandomList(2, 2);
            //�Ǿ� �̱��� ���� 3��ü
            case 9:
                return RandomList(2, 3);
            //�Ǿ� �̱��� ���� 4��ü
            case 10:
                return RandomList(2, 4);
            //�Ǿ� �̱��� ��ü
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
