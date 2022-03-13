using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipBtnSet : MonoBehaviour
{
    

    SmithPanel SM;
    BedItemPanel BM;
    [SerializeField] Button[] btns;
    [SerializeField] Text[] txts;

    #region Token Data
    int[] idxs = new int[4];

    //0: equip, 1 : blueprint, 2 : skillbook
    int tokenKind;
    Equipment[] equipments = new Equipment[4];
    EquipBluePrint[] ebps = new EquipBluePrint[4];
    Skillbook[] sbooks = new Skillbook[4];
    #endregion

    public void Init(SmithPanel s, List<KeyValuePair<int, Equipment>> p)
    {
        SM = s;
        BM = null;
        int i = 0;
        tokenKind = 0;

        for (; i < p.Count; i++)
        {
            idxs[i] = p[i].Key;
            equipments[i] = p[i].Value;
            txts[i].text = string.Concat("idx : ", idxs[i]);
            btns[i].gameObject.SetActive(true);
        }

        for (; i < 4; i++)
            btns[i].gameObject.SetActive(false);
    }
    public void Init(SmithPanel s, List<KeyValuePair<int, EquipBluePrint>> p)
    {
        SM = s;
        BM = null;
        int i = 0;
        tokenKind = 1;

        for (; i < p.Count; i++)
        {
            idxs[i] = p[i].Key;
            ebps[i] = p[i].Value;
            txts[i].text = string.Concat("idx : ", idxs[i]);
            btns[i].gameObject.SetActive(true);
        }

        for (; i < 4; i++)
            btns[i].gameObject.SetActive(false);
    }
    public void Init(SmithPanel s, List<KeyValuePair<int, Skillbook>> p)
    {
        SM = s;
        BM = null;
        int i = 0;
        tokenKind = 2;

        for (; i < p.Count; i++)
        {
            idxs[i] = p[i].Key;
            sbooks[i] = p[i].Value;
            txts[i].text = string.Concat("idx : ", idxs[i]);
            btns[i].gameObject.SetActive(true);
        }

        for (; i < 4; i++)
            btns[i].gameObject.SetActive(false);
    }

    public void Init(BedItemPanel b, List<KeyValuePair<int, Equipment>> p)
    {
        BM = b;
        SM = null;
        int i = 0;

        for (; i < p.Count; i++)
        {
            idxs[i] = p[i].Key;
            equipments[i] = p[i].Value;
            txts[i].text = string.Concat("idx : ", idxs[i]);
            btns[i].gameObject.SetActive(true);
        }

        for (; i < 4; i++)
            btns[i].gameObject.SetActive(false);
    }

    public void Btn_Equip(int idx)
    {
        if (SM != null)
        {
            if (tokenKind == 0)
                SM.Btn_EquipToken(new KeyValuePair<int, Equipment>(idxs[idx], equipments[idx]));
            else if (tokenKind == 1)
                SM.Btn_EBPToken(ebps[idx]);
            else
                SM.Btn_SkillbookToken(sbooks[idx]);
        }
        else
            BM.Btn_EquipToken(new KeyValuePair<int, Equipment>(idxs[idx], equipments[idx]));
    }
}
