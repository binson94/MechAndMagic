using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogManager : MonoBehaviour
{
    public static LogManager instance;

    [SerializeField] ScrollRect logScroll;
    [SerializeField] ScrollRect expandLogScroll;

    [SerializeField] RectTransform logContent;
    [SerializeField] RectTransform expandLogContent;
    [SerializeField] GameObject logPrefab;

    bool isExpand = false;

    [SerializeField] GameObject expandPanel;
    [SerializeField] GameObject normalPanel;

    private void Awake()
    {
        instance = this;
    }

    public void AddLog(string str)
    {
        Log token = GetToken(logContent);
        token.Set(str);

        token = GetToken(expandLogContent);
        token.Set(str);

        StartCoroutine(AddScroll());
    }
    IEnumerator AddScroll()
    {
        yield return null;

        logScroll.verticalNormalizedPosition = 0;
        expandLogScroll.verticalNormalizedPosition = 0;

    }

    Log GetToken(RectTransform parent)
    {
        Log token = Instantiate(logPrefab).GetComponent<Log>();
        token.transform.SetParent(parent);

        RectTransform newRect = token.transform as RectTransform;
        RectTransform prefabRect = logPrefab.GetComponent<RectTransform>();
        newRect.anchoredPosition = prefabRect.anchoredPosition;
        newRect.anchorMax = prefabRect.anchorMax;
        newRect.anchorMin = prefabRect.anchorMin;
        newRect.localRotation = prefabRect.localRotation;
        newRect.localScale = prefabRect.localScale;
        newRect.pivot = prefabRect.pivot;
        newRect.sizeDelta = prefabRect.sizeDelta;

        return token;
    }

    public void Btn_ExpandToggle()
    {
        isExpand = !isExpand;
        expandPanel.SetActive(isExpand);
        normalPanel.SetActive(!isExpand);
    }
}
