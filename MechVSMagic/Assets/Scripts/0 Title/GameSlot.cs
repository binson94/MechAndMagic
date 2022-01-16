using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSlot : MonoBehaviour
{
    [SerializeField] GameObject deleteBtn;
    [SerializeField] GameObject loadBtn;
    [SerializeField] Text loadStr;
    [SerializeField] GameObject newBtn;

    public void SlotUpdate(bool hasData, string name = "")
    {
        deleteBtn.SetActive(hasData);
        loadBtn.SetActive(hasData);
        loadStr.text = name;

        newBtn.SetActive(!hasData);
    }
}
