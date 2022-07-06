using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> 모든 캐릭터 TP 값 표시 슬라이더 </summary>
public class TPSlider : MonoBehaviour
{
    [SerializeField] RectTransform[] handles;
    ///<summary> 좌 우 끝 점 위치 </summary>
    [SerializeField] RectTransform[] pos;
    float interval;
    float posY;

    private void Start() {
        interval = (pos[1].anchoredPosition.x - pos[0].anchoredPosition.x);
    }

    public void ActiveSet(Unit[] units)
    {
        for(int i = 0;i < units.Length; i++) handles[i].gameObject.SetActive(units[i].isActiveAndEnabled);
    }

    public void SetValue(int handleIdx, float value)
    {
        handles[handleIdx].localPosition = new Vector3(pos[0].anchoredPosition.x + interval * value, -handles[handleIdx].sizeDelta.y / 2, 0);
    }
}
