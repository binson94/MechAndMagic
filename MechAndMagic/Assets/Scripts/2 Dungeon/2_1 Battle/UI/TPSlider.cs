using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TPSlider : MonoBehaviour
{
    [SerializeField] RectTransform[] handles;
    [SerializeField] RectTransform[] pos;
    float interval;
    float posY;

    private void Start() {
        interval = (pos[1].anchoredPosition.x - pos[0].anchoredPosition.x);
    }

    public void ActiveSet(List<Unit> units)
    {
        for(int i = 0;i < units.Count; i++) handles[i].gameObject.SetActive(units[i].isActiveAndEnabled);
    }

    public void SetValue(int handleIdx, float value)
    {
        handles[handleIdx].localPosition = new Vector3(pos[0].anchoredPosition.x + interval * value, -handles[handleIdx].sizeDelta.y / 2, 0);
    }
}
