using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillInfoPanel : MonoBehaviour
{
    [SerializeField] Text equipName;

    public void InfoUpdate(Skill s)
    {
        if (s != null)
            equipName.text = s.name;
    }
}
