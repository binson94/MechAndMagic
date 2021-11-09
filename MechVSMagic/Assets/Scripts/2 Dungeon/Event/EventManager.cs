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

    private void Start()
    {
        eventInfo = new EventInfo(PlayerPrefs.GetInt(string.Concat("Room", GameManager.instance.slotNumber)));
        eventText.text = eventInfo.script;
    }

    public void Btn_BackToMap()
    {
        SceneManager.LoadScene("2_0 Dungeon");
    }
}
