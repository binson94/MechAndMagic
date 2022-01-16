using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class DungeonButtonManager : MonoBehaviour
{
    bool[] isOpen;
    List<GameObject> nameBtns = new List<GameObject>();
    List<GameObject> scriptBtns = new List<GameObject>();

    //챕터에 따라 contents 분할
    [SerializeField] RectTransform[] contents;
    [SerializeField] GameObject namePrefab;
    [SerializeField] GameObject scriptPrefab;

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        for (int i = 0; i < 2; i++)
        {
            nameBtns.Add(Instantiate(namePrefab));
            nameBtns[i].GetComponent<DungeonNameBtn>().SetData(i+1, string.Concat("던전 ", i+1), this);
            nameBtns[i].transform.SetParent(contents[0]);

            scriptBtns.Add(Instantiate(scriptPrefab));
            scriptBtns[i].GetComponent<DungeonScriptBtn>().SetData(i+1, string.Concat("설명 ", i+1), this);
            scriptBtns[i].transform.SetParent(contents[0]);
            scriptBtns[i].SetActive(false);
        }

        isOpen = new bool[nameBtns.Count];
        for (int i = 0; i < isOpen.Length; i++) isOpen[i] = false;
    }

    public void Btn_Name(int idx)
    {
        idx--;
        for (int i = 0; i < isOpen.Length; i++)
            if (i != idx)
            {
                isOpen[i] = false;
                scriptBtns[i].SetActive(false);
            }

        isOpen[idx] = !isOpen[idx];
        scriptBtns[idx].SetActive(isOpen[idx]);
    }

    public void Btn_Script(int idx)
    {
        GameManager.slotData.dungeonIdx = idx;
        GameManager.SaveSlotData();

        PlayerPrefs.DeleteKey(string.Concat("DungeonData", GameManager.currSlot));
        PlayerPrefs.DeleteKey(string.Concat("CharState", GameManager.currSlot));
        QuestDataManager.RemoveOutbreak();
        GameManager.SwitchSceneData(SceneKind.Dungeon);
        SceneManager.LoadScene("2_0 Dungeon");
    }
}
