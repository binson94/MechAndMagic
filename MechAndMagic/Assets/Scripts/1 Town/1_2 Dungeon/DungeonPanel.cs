using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class DungeonPanel : MonoBehaviour, ITownPanel
{
    bool init = false;
    bool[] isOpen;
    List<DungeonSelectToken> nameBtns = new List<DungeonSelectToken>();
    List<GameObject> scriptBtns = new List<GameObject>();

    //챕터에 따라 contents 분할
    [SerializeField] RectTransform[] contents;
    [SerializeField] GameObject namePrefab;
    [SerializeField] GameObject scriptPrefab;

    private void Init()
    {
        init = true;
        for (int i = 0; i < 2; i++)
        {
            nameBtns.Add(Instantiate(namePrefab).GetComponent<DungeonSelectToken>());
            nameBtns[i].transform.SetParent(contents[0]);

            scriptBtns.Add(Instantiate(scriptPrefab));
            scriptBtns[i].transform.SetParent(contents[0]);

            nameBtns[i].SetData(i + 1, scriptBtns[i], this);
            scriptBtns[i].SetActive(false);
        }

        isOpen = new bool[nameBtns.Count];
        for (int i = 0; i < isOpen.Length; i++) isOpen[i] = false;
    }
    
    public void ResetAllState()
    {
        if(!init)
            Init();

        for(int i = 0;i < isOpen.Length;i++)
        {
            isOpen[i] = false;
            scriptBtns[i].SetActive(false);
        }
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
        GameManager.instance.SetNewDungeon(idx);
        QuestManager.RemoveOutbreak();
        GameManager.instance.SwitchSceneData(SceneKind.Dungeon);
        
        SceneManager.LoadScene("2_0 Dungeon");
    }
}
