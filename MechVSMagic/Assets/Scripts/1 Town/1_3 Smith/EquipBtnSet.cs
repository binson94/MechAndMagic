using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipBtnSet : MonoBehaviour
{
    SmithManager SM;
    int[] idxs = new int[4];
    [SerializeField] Button[] btns;
    [SerializeField] Text[] txts;

    public void Init(SmithManager s, List<int> idx)
    {
        SM = s;
        int i = 0;

        for (; i < idx.Count; i++)
        {
            idxs[i] = idx[i];
            txts[i].text = string.Concat("idx : ", idx[i]);
            btns[i].gameObject.SetActive(true);
        }

        for (; i < 4; i++)
            btns[i].gameObject.SetActive(false);
    }

    public void Btn_Equip(int idx)
    {
        SM.Btn_Equip(idxs[idx]);
    }
}
