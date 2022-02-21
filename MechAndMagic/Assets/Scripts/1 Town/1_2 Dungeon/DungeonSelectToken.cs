using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonSelectToken : MonoBehaviour
{
    int idx;

    //0 : name, 1 : script
    Text[] txts = new Text[2];
    Button[] btns = new Button[2];
    DungeonPanel mgr;

    public void SetData(int idx, GameObject scriptBtn, DungeonPanel m)
    {
        mgr = m;
        this.idx = idx;
        
        txts[0] = transform.GetChild(0).GetComponent<Text>();
        txts[1] = scriptBtn.transform.GetChild(0).GetComponent<Text>();

        txts[0].text = string.Concat("던전 ", idx);
        txts[1].text = string.Concat("설명 ", idx);

        btns[0] = GetComponent<Button>();
        btns[1] = scriptBtn.GetComponent<Button>();

        btns[0].onClick.AddListener(Btn_Name);
        btns[1].onClick.AddListener(Btn_Script);
    }

    void Btn_Name()
    {
        mgr.Btn_Name(idx);
    }

    void Btn_Script()
    {
        mgr.Btn_Script(idx);
    }
}
