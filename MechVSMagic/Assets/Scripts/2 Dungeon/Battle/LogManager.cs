using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogManager : MonoBehaviour
{
    public static LogManager instance;

    [SerializeField] Transform logContent;
    [SerializeField] Transform expandLogContent;
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
        Log tmp = Instantiate(logPrefab).GetComponent<Log>();
        tmp.Set(str);

        tmp.transform.SetParent(logContent);

        tmp = Instantiate(logPrefab).GetComponent<Log>();
        tmp.Set(str);
        tmp.transform.SetParent(expandLogContent);
    }

    public void Btn_ExpandToggle()
    {
        isExpand = !isExpand;
        expandPanel.SetActive(isExpand);
        normalPanel.SetActive(!isExpand);
    }
}
