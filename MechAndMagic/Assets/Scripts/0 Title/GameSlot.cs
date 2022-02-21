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

    public void SlotUpdate(SlotData slot)
    {
        deleteBtn.SetActive(slot != null);
        loadBtn.SetActive(slot != null);

        if (slot != null)
            loadStr.text = string.Concat("class : ", slot.slotClass);
        newBtn.SetActive(slot == null);
    }
}
