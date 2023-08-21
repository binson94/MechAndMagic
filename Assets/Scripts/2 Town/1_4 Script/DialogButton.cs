using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogButton : MonoBehaviour 
{
    [SerializeField] Text btnTxt;
    [SerializeField] Image questIcon;

    public void Set(KeyValuePair<DialogData, QuestState> dialog, Sprite quest)
    {
        btnTxt.text = dialog.Key.name;
        questIcon.gameObject.SetActive(dialog.Key.kind == 1);
        questIcon.sprite = quest;
    }
}