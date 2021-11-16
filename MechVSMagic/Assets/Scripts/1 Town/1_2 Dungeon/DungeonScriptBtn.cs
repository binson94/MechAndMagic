using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonScriptBtn : MonoBehaviour
{
    public int idx;
    [SerializeField] Text text;
    Button btn;
    DungeonButtonManager mgr;

    public void SetData(int idx, string str, DungeonButtonManager m)
    {
        mgr = m;
        this.idx = idx;
        text.text = str;

        btn = GetComponent<Button>();
        btn.onClick.AddListener(Btn_Script);
    }

    public void Btn_Script()
    {
        mgr.Btn_Script(idx);
    }
}
