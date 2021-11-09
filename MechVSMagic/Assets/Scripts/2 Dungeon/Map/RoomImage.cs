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
        SetTxt();
    }

    private void SetTxt()
    {
        if (room.isOpen)
        {
            roomText.text = string.Concat(room.type, "\n", room.roomEventIdx);
        }
        else
            roomText.text = "비공개";
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
