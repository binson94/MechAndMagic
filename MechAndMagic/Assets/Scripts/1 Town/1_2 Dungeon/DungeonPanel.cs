using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LitJson;

public class DungeonPanel : MonoBehaviour, ITownPanel
{
    #region DungeonInfo
    bool[] isOpen;
    List<DungeonSelectToken> dungeonBtnTokens = new List<DungeonSelectToken>();
    List<DungeonScriptToken> dungeonScriptTokens = new List<DungeonScriptToken>();

    ///<summary> 모든 토큰의 부모(스크롤뷰) </summary>
    [SerializeField] RectTransform tokenParent;
    ///<summary> pool에 존재하는 토큰들의 부모 </summary>
    [SerializeField] RectTransform poolParent;

    [SerializeField] Sprite[] dungeonFrameSprites;
    [SerializeField] Sprite[] dungeonIconSprites;

    [SerializeField] GameObject namePrefab;
    Queue<DungeonSelectToken> dungeonBtnPool = new Queue<DungeonSelectToken>();
    [SerializeField] GameObject scriptPrefab;
    Queue<DungeonScriptToken> dungeonScriptPool = new Queue<DungeonScriptToken>();

    JsonData json = null;
    #endregion DungeonInfo

    public void ResetAllState()
    {
        LoadChapter(GameManager.instance.slotData.chapter);
    }

    ///<summary> 현재 보여주는 던전 앤트리들 모두 pool에 넣음 </summary>
    void DeleteAllEntry()
    {
        for (int i = 0; i < dungeonBtnTokens.Count; i++)
        {
            dungeonBtnTokens[i].gameObject.SetActive(false);
            dungeonBtnPool.Enqueue(dungeonBtnTokens[i]);
            dungeonBtnTokens[i].transform.SetParent(poolParent);

            dungeonScriptTokens[i].gameObject.SetActive(false);
            dungeonScriptPool.Enqueue(dungeonScriptTokens[i]);
            dungeonScriptTokens[i].transform.SetParent(poolParent);
        }
        dungeonBtnTokens.Clear(); dungeonScriptTokens.Clear();
    }
    ///<summary> 선택한 챕터 불러오기 </summary>
    public void LoadChapter(int chapter)
    {
        if(json == null) json = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Dungeons/Dungeon").text);

        DeleteAllEntry();

        for (int i = 0; i < json.Count; i++)
        {
            if((int)json[i]["chapter"] != chapter || (int)json[i]["region"] != GameManager.instance.slotData.region)
                continue;

            //이름 토큰
            dungeonBtnTokens.Insert(0, GameManager.GetToken(dungeonBtnPool, tokenParent, namePrefab));
            dungeonBtnTokens[0].SetData(i, json, dungeonIconSprites[(int)json[i]["icon"] - 1],dungeonFrameSprites[(int)json[i]["main"]],  this);
            dungeonBtnTokens[0].gameObject.SetActive(true);

            //설명 토큰
            dungeonScriptTokens.Insert(0, GameManager.GetToken(dungeonScriptPool, tokenParent, scriptPrefab));
            dungeonScriptTokens[0].SetData(json[i]["aboutScript"].ToString(), json[i]["rewardScript"].ToString());
            dungeonScriptTokens[0].gameObject.SetActive(false);
        }

        isOpen = new bool[dungeonBtnTokens.Count];
        for (int i = 0; i < isOpen.Length; i++) isOpen[i] = false;
    }

    ///<summary> 던전 선택 버튼 누를 시, 설명 토글 </summary>
    public void Btn_SelectDungeon(int dungeonIdx)
    {
        for (int i = 0; i < isOpen.Length; i++)
            //내가 선택한 던전 -> 토글
            if (dungeonBtnTokens[i].jsonIdx == dungeonIdx)
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
        
        SceneManager.LoadScene((int)SceneKind.Dungeon);
    }
}
