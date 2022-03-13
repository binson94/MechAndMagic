using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APBar : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Text apTxt;
    [SerializeField] RectTransform[] pivots;
    [SerializeField] GameObject barPrefab;
    [SerializeField] Transform barParent;
    float length = 164;
    float intervalLength = 3;

    Queue<GameObject> pool = new Queue<GameObject>();
    List<GameObject> barImages = new List<GameObject>();

    public void SetValue(int currAP, int maxAP)
    {
        Reset();

        int interval = maxAP - 1;
        float length = (this.length - intervalLength * interval) / maxAP;
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

        apTxt.text = string.Concat(currAP, "/", maxAP);
    }

    void Reset()
    {
        while(barImages.Count > 0)
        {
            barImages[0].SetActive(false);
            pool.Enqueue(barImages[0]);
            barImages.RemoveAt(0);
        }
    }
    GameObject NewBarToken()
    {
        if (pool.Count > 0) return pool.Dequeue();
        else return Instantiate(barPrefab);
    }
}
