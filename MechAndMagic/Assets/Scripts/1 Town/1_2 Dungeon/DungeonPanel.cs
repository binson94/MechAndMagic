using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LitJson;

public class DungeonPanel : MonoBehaviour, ITownPanel
{
    #region PlayerInfo
    ///<summary> 클래스 텍스트 </summary>
    [SerializeField] Text classTxt;
    [SerializeField] Text lvlTxt;

    [Tooltip("0 lv1 ~ 4 lv9")]
    ///<summary> 장비 정보 프레임 스프라이트들
    ///<para> 0 lv1, 1 lv3, 2 lv5, 3 lv7, 4 lv9</para>
    ///</summary>
    [SerializeField] Sprite[] equipFrameSprites;
    [Tooltip("장비 아이콘")]
    ///<summary> 장비 아이콘 스프라이트들 </summary>
    [SerializeField] Sprite[] equipIconSprites;
    ///<summary> 장착 중인 장비 정보 보여줄 이미지 </summary>
    [SerializeField] EquipInfoImage[] equipInfos;
    #endregion PlayerInfo

    #region DungeonInfo
    bool[] isOpen;
    List<DungeonSelectToken> dungeonBtnTokens = new List<DungeonSelectToken>();
    List<DungeonScriptToken> dungeonScriptTokens = new List<DungeonScriptToken>();

    ///<summary> 모든 토큰의 부모(스크롤뷰) </summary>
    [SerializeField] RectTransform tokenParent;
    ///<summary> pool에 존재하는 토큰들의 부모 </summary>
    [SerializeField] RectTransform poolParent;

    [SerializeField] GameObject namePrefab;
    List<DungeonSelectToken> dungeonBtnPool = new List<DungeonSelectToken>();
    [SerializeField] GameObject scriptPrefab;
    List<DungeonScriptToken> dungeonScriptPool = new List<DungeonScriptToken>();

    JsonData json = null;
    #endregion DungeonInfo

    public void ResetAllState()
    {
        LoadPlayerInfo();
        LoadItemInfo();
        LoadChapter(GameManager.instance.slotData.chapter);
    }


    ///<summary> 플레이어 레벨 및 직업 표시 </summary>
    void LoadPlayerInfo()
    {
        switch(GameManager.instance.slotData.slotClass)
        {
            case 1:
                classTxt.text = "암드파이터";
                break;
            case 2:
                classTxt.text = "메탈나이트";
                break;
            case 3:
                classTxt.text = "블래스터";
                break;
            case 4:
                classTxt.text = "매드 사이언티스트";
                break;
            case 5:
                classTxt.text = "엘리멘탈 컨트롤러";
                break;
            case 6:
                classTxt.text = "드루이드";
                break;
            case 7:
                classTxt.text = "비전 마스터";
                break;
            case 8:
                classTxt.text = "매지컬 로그";
                break;
        }
        lvlTxt.text = $"Lv.{GameManager.instance.slotData.lvl}";
    }
    ///<summary> 현재 장착한 장비 정보 불러오기, 장비 정보 창에 적용 </summary>
    void LoadItemInfo()
    {
        for(int i = 0;i < 7;i++)
        {
            Equipment e = GameManager.instance.slotData.itemData.equipmentSlots[i + 1];
            if(e == null)
                equipInfos[i].SetImage(equipFrameSprites[0], null, 0);
            else
                equipInfos[i].SetImage(equipFrameSprites[e.ebp.reqlvl / 2], equipIconSprites[0], e.ebp.reqlvl);
            
        }
    }


    ///<summary> 현재 보여주는 던전 앤트리들 모두 pool에 넣음 </summary>
    void DeleteAllEntry()
    {
        while(dungeonBtnTokens.Count >= 1)
        {
            dungeonBtnTokens[0].gameObject.SetActive(false);
            dungeonBtnPool.Add(dungeonBtnTokens[0]);
            dungeonBtnTokens[0].transform.SetParent(poolParent);
            dungeonBtnTokens.RemoveAt(0);

            dungeonScriptTokens[0].gameObject.SetActive(false);
            dungeonScriptPool.Add(dungeonScriptTokens[0]);
            dungeonScriptTokens[0].transform.SetParent(poolParent);
            dungeonScriptTokens.RemoveAt(0);
        }
    }
    ///<summary> 선택한 챕터 불러오기 </summary>
    public void LoadChapter(int chapter)
    {
        if(json == null) json = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Dungeons/Dungeon").text);

        DeleteAllEntry();

        for (int i = 0; i < json.Count; i++)
        {
            if((int)json[i]["chapter"] != chapter)
                continue;

            //이름 토큰
            dungeonBtnTokens.Insert(0, GetToken(namePrefab, dungeonBtnPool));
            dungeonBtnTokens[0].transform.SetParent(tokenParent);
            dungeonBtnTokens[0].SetData(i, json, this);

            //설명 토큰
            dungeonScriptTokens.Insert(0, GetToken(scriptPrefab, dungeonScriptPool));
            dungeonScriptTokens[0].transform.SetParent(tokenParent);
            dungeonScriptTokens[0].SetData(json[i]["aboutScript"].ToString(), json[i]["rewardScript"].ToString());
            dungeonScriptTokens[0].gameObject.SetActive(false);
        }

        isOpen = new bool[dungeonBtnTokens.Count];
        for (int i = 0; i < isOpen.Length; i++) isOpen[i] = false;

        //pool 사용한 token 생성
        T GetToken<T>(GameObject prefab, List<T> pool) where T : MonoBehaviour
        {
            T token;
            if(pool.Count >= 1)
            {
                token = pool[0];
                pool.RemoveAt(0);
                token.gameObject.SetActive(true);
                return token;
            }
            else
                return Instantiate(prefab).GetComponent<T>();
        }
    }

    ///<summary> 던전 선택 버튼 누를 시, 설명 토글 </summary>
    public void Btn_SelectDungeon(int dungeonIdx)
    {
        for (int i = 0; i < isOpen.Length; i++)
            //내가 선택한 던전 -> 토글
            if (dungeonBtnTokens[i].dungeonIdx == dungeonIdx)
            {
                isOpen[i] = !isOpen[i];
                dungeonBtnTokens[i].ToggleStartBtn(isOpen[i]);
                dungeonScriptTokens[i].gameObject.SetActive(isOpen[i]);
            }
            //그외 던전 -> 닫기
            else
            {
                isOpen[i] = false;
                dungeonBtnTokens[i].ToggleStartBtn(false);
                dungeonScriptTokens[i].gameObject.SetActive(false);
            }
    }
    
    ///<summary> 던전 시작 버튼 누를 시, 새로운 던전 생성 및 씬 전환 </summary>
    public void Btn_StartDungeon(int dungeonIdx)
    {
        GameManager.instance.SetNewDungeon(dungeonIdx);
        QuestManager.RemoveOutbreak();
        GameManager.instance.SwitchSceneData(SceneKind.Dungeon);
        
        SceneManager.LoadScene("2_0 Dungeon");
    }
}
