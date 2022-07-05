using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APBar : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Text apTxt;
    [SerializeField] RectTransform[] pivots;
    [SerializeField] GameObject barPrefab;
    [SerializeField] Transform barParent;
    const float length = 164;
    const float intervalLength = 3;

    Queue<GameObject> pool = new Queue<GameObject>();
    List<GameObject> barImages = new List<GameObject>();

    public void SetValue(int currAP, int maxAP)
    {
        Reset();

        int interval = maxAP - 1;
        float length = (APBar.length - intervalLength * interval) / maxAP;
        float pos = pivots[0].anchoredPosition.x + length / 2;

        for(int i = 0;i < currAP;i++)
        {
            GameObject go = NewBarToken();
            go.transform.SetParent(barParent);

            go.GetComponent<RectTransform>().sizeDelta = new Vector2(length, 22);
            go.transform.localPosition = new Vector3(pos, -0.5f, 0);
            pos += length + intervalLength;

            go.SetActive(true);
            barImages.Add(go);
        }

        apTxt.text = $"{currAP}/{maxAP}";
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
