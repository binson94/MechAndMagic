using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonNameBtn : MonoBehaviour
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
        btn.onClick.AddListener(Btn_Name);
    }

    public void Btn_Name()
    {
        mgr.Btn_Name(idx);
    }
}
