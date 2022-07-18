using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomImage : MonoBehaviour
{
    Room room;
    ///<summary> 방 선택 버튼 </summary>
    [SerializeField] Image btnImage;
    ///<summary> 방 버튼 트랜스폼 </summary>
    public RectTransform rect
    {
        get => btnImage.rectTransform;
    }
    ///<summary> 방 종류 표시 이미지 </summary>
    [SerializeField] Image roomImage;

    DungeonManager dungeonMgr;

    const float randomRange = 50;

    public void Init(Room r, DungeonManager dmgr)
    {
        room = r;
        dungeonMgr = dmgr;
        LoadSprite();
    }

    void LoadSprite()
    {
        btnImage.sprite = dungeonMgr.roomBGSprites[Random.Range(0, 3)];

        if(room.isOpen)
            roomImage.sprite = dungeonMgr.roomSprites[(int)room.type];
        else
            roomImage.sprite = dungeonMgr.roomSprites[5];
    }

    public void SetPosition(Vector3 vec)
    {
        //vec += new Vector3(Random.Range(-randomRange, randomRange), Random.Range(-randomRange, randomRange), 0);
        rect.anchoredPosition = vec;
    }

    public void Btn_Select() 
    {
        SoundManager.instance.PlaySFX(22);
        dungeonMgr.Btn_RoomSelect(room.floor, room.roomNumber);
    }
}
