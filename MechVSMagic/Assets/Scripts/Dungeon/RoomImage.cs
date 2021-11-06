using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomImage : MonoBehaviour
{
    Room room;
    public RectTransform rect;
    [SerializeField] Text roomText;
    [SerializeField] Image roomImage;

    DungeonManager dungeonMgr;

    public void Init(Room r, DungeonManager dmgr)
    {
        room = r;
        dungeonMgr = dmgr;

        rect = GetComponent<RectTransform>();
        GetComponent<Button>().onClick.AddListener(Btn_Select);
        rect.transform.position = new Vector3(75, 75, 0) + Vector3.right * room.roomNumber * 200 + Vector3.up * room.floor * 300;
    }

    public void SetPosition(Vector3 vec)
    {
        rect.transform.position = vec;
    }

    void Btn_Select()
    {
        Debug.Log(string.Concat("(", room.floor, ", ", room.roomNumber, ")"));
        dungeonMgr.Btn_RoomSelect(room.floor, room.roomNumber);
    }
}
