using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomConnectManager : MonoBehaviour
{
    RectTransform canvas;
    [SerializeField] GameObject connectionPrefab;
    List<GameObject> connections = new List<GameObject>();

    private void Start()
    {
        canvas = transform.parent.GetComponent<RectTransform>();
    }

    public void AddConnect(params RectTransform[] rect)
    {
        GameObject connect = Instantiate(connectionPrefab);
        RectTransform rectTransform = connect.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(10, Vector2.Distance(rect[0].position, rect[1].position));
        rectTransform.rotation = Quaternion.Euler(new Vector3(0, 0, Vector3.SignedAngle(Vector3.up,rect[1].position - rect[0].position, Vector3.forward)));
        rectTransform.position = (rect[0].position + rect[1].position) / 2;
        connect.transform.SetParent(transform);
    }
}
