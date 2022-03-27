using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    [SerializeField] Image skillIcon;
    [SerializeField] Text skillTxt;
    [SerializeField] Text apTxt;
    [SerializeField] GameObject highlight;

    public void Init(Skill s, Sprite sp)
    {
        skillTxt.text = s.name;
        skillIcon.sprite = sp;
        APUpdate(s.apCost);
    }

    public void APUpdate(int val)
    {
        apTxt.text = string.Concat("<color=#ed2929> ", val, " </color> AP");
    }

    public void Highlight(bool isHigh)
    {
        highlight.SetActive(isHigh);
    }
}
