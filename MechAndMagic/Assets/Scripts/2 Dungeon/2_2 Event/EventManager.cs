using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EventManager : MonoBehaviour
{
    int eventIdx;
    [SerializeField] Text eventText;
    EventInfo eventInfo;

    [SerializeField] GameObject[] selectBtns;
    [SerializeField] GameObject backBtn;

    private void Start()
    {
        GameManager.sound.PlayBGM(BGM.Battle1);
        eventInfo = new EventInfo(GameManager.slotData.dungeonRoom);
        eventText.text = eventInfo.script;

        EventEffect();
    }

    void EventEffect()
    {
        for(int i =0;i<eventInfo.typeCount;i++)
        {
            switch (eventInfo.type[i])
            {
                case 1:
                    {
                        Debug.Log("Item Get");
                        break;
                    }
                case 2:
                    {
                        Debug.Log("Buff / Debuff");
                        break;
                    }
                case 3:
                    {
                        Debug.Log("remove Buff / Debuff");
                        break;
                    }
                case 4:
                    {
                        Debug.Log("change item");
                        foreach (GameObject g in selectBtns) g.SetActive(true);
                        backBtn.SetActive(false);
                        break;
                    }
                case 5:
                    {
                        Debug.Log("give item to prevent from disadvantage");
                        foreach (GameObject g in selectBtns) g.SetActive(true);
                        backBtn.SetActive(false);
                        break;
                    }
                case 6:
                    {
                        Debug.Log("must give item");
                        break;
                    }
            }
        }
    }

    public void Btn_Yes()
    {
        Debug.Log("Yes");
        
        foreach (GameObject g in selectBtns) g.SetActive(false);
        backBtn.SetActive(true);
    }

    public void Btn_No()
    {
        Debug.Log("No");

        foreach (GameObject g in selectBtns) g.SetActive(false);
        backBtn.SetActive(true);
    }

    public void Btn_BackToMap()
    {
        GameManager.SwitchSceneData(SceneKind.Dungeon);
        SceneManager.LoadScene("2_0 Dungeon");
    }
}
