using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APBar : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Text apTxt;
    [SerializeField] RectTransform[] pivots;
    const float intervalLength = 3;

    ///<summary> AP 오브젝트 프리팹 </summary>
    [SerializeField] GameObject barPrefab;
    ///<summary> AP 오브젝트 부모 오브젝트 </summary>
    [SerializeField] Transform barParent;
    ///<summary> AP 오브젝트 풀 </summary>
    Queue<GameObject> pool = new Queue<GameObject>();
    ///<summary> AP 오브젝트 리스트 </summary>
    List<GameObject> barImages = new List<GameObject>();

    public void SetValue(int currAP, int maxAP)
    {
        Reset();

        int interval = maxAP - 1;
        float length = (pivots[1].anchoredPosition.x - pivots[0].anchoredPosition.x - intervalLength * interval) / maxAP;
        float pos = pivots[0].anchoredPosition.x;

        for(int i = 0;i < currAP;i++)
        {
            GameObject go = NewBarToken();
            RectTransform rect = go.GetComponent<RectTransform>();

            rect.sizeDelta = new Vector2(length, 22);
            rect.anchoredPosition = new Vector2(pos, -0.5f);
            pos += length + intervalLength;

            go.SetActive(true);
            barImages.Add(go);
        }

        apTxt.text = $"{Mathf.Max(0, currAP)}/{Mathf.Max(0, maxAP)}";
    }

    void Reset()
    {
        foreach(GameObject token in barImages)
        {
            token.SetActive(false);
            pool.Enqueue(token);
        }
       
        barImages.Clear();
    }
    GameObject NewBarToken()
    {
        GameObject token;
        if(pool.Count > 0)
        {
            token = pool.Dequeue();
            token.transform.SetParent(barParent);
        }
        else
        {
            token = Instantiate(barPrefab);
            token.transform.SetParent(barParent);

            //해상도에 맞게 사이즈 조절
            RectTransform newRect = token.transform as RectTransform;
            RectTransform prefabRect = barPrefab.GetComponent<RectTransform>();
            newRect.anchoredPosition = prefabRect.anchoredPosition;
            newRect.anchorMax = prefabRect.anchorMax;
            newRect.anchorMin = prefabRect.anchorMin;
            newRect.localRotation = prefabRect.localRotation;
            newRect.localScale = prefabRect.localScale; ;
            newRect.pivot = prefabRect.pivot;
            newRect.sizeDelta = prefabRect.sizeDelta;
        }

        return token;
    }
}
