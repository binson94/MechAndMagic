using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomImage : MonoBehaviour
{
    Room room;
    public RectTransform rectTransform;

    [SerializeField] Image bgImage;
    [SerializeField] Image roomImage;

    DungeonManager dungeonMgr;

    const float randomRange = 50;

    public void Init(Room r, DungeonManager dmgr)
    {
        room = r;
        dungeonMgr = dmgr;

        rectTransform = GetComponent<RectTransform>();
        GetComponent<Button>().onClick.AddListener(Btn_Select);
        LoadSprite();
    }

    void LoadSprite()
    {
        bgImage.sprite = dungeonMgr.roomBGSprites[Random.Range(0, 3)];

        if(room.isOpen)
            roomImage.sprite = dungeonMgr.roomSprites[(int)room.type];
        else
            roomImage.sprite = dungeonMgr.roomSprites[5];
    }

    public void SetPosition(Vector3 vec)
    {
        //vec += new Vector3(Random.Range(-randomRange, randomRange), Random.Range(-randomRange, randomRange), 0);
        rectTransform.anchoredPosition = vec;
    }

    void Btn_Select() => dungeonMgr.Btn_RoomSelect(room.floor, room.roomNumber);
    
}
