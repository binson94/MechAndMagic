using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public enum RoomType
{
    Empty, Monster, Positive, Neutral, Negative, Quest, Boss
}

public class Room
{
    public int floor;
    //층에서 왼쪽에서 몇 번째인지
    public int roomNumber;
    public List<int> next = new List<int>();
    public List<int> prev = new List<int>();
    public RoomType type;
    public bool isOpen;

    public void LinkNext(Room r)
    {
        next.Add(r.roomNumber);
        r.prev.Add(roomNumber);
    }

    public void RemoveLink(Room r)
    {
        next.Remove(r.roomNumber);
        r.prev.Remove(roomNumber);
    }
}

[System.Serializable]
public class Dungeon
{
    public string dungeonName;

    //0층 : 시작 방만 존재, 최고층 : 보스 방만 존재
    public List<List<Room>> rooms = new List<List<Room>>();

    public void DungeonInstantiate(DungeonBluePrint dbp)
    {
        //층 수 선정 (시작 방 포함해서 + 1)
        int floor = Random.Range(dbp.floorMinMax[0] + 1, dbp.floorMinMax[1] + 2);
        for (int i = 0; i < floor; i++)
            rooms.Add(new List<Room>());


        rooms[0].Add(GetRoom(0, 0));
        for (int i = 1; i < floor - 1; i++)
        {
            int room = Random.Range(dbp.roomMinMax[0], dbp.roomMinMax[1] + 1);

            for (int j = 0; j < room; j++)
                rooms[i].Add(GetRoom(i, j, dbp.openChance, dbp.roomKindChances));
        }
        rooms[floor - 1].Add(GetRoom(-1, 0));
    }

    //prob : empty, monster, pos, neu, neg, quest 순서 확률
    Room GetRoom(int f,int roomNb, float openProb = 0, params float[] prob)
    {
        Room r = new Room
        {
            floor = f,
            roomNumber = roomNb
        };

        //시작 방은 항상 빈 방
        if (f == 0)
            r.type = RoomType.Empty;
        else if (f == -1)
            r.type = RoomType.Boss;
        else
        {
            float rand = Random.Range(0, 1f);

            int roomI;
            float roomPivot = prob[0];
            for (roomI = 0; rand > roomPivot; roomPivot += prob[++roomI]) ;
            r.type = (RoomType)roomI;
        }

        //공개 이벤트 설정
        if (2 <= (int)r.type && (int)r.type <= 5)
        {
            float open = Random.Range(0, 1f);
            if (open < openProb)
                r.isOpen = true;
            else
                r.isOpen = false;
        }
        else
            r.isOpen = true;

        return r;
    }

    public void DebugShow()
    {
        for(int i =0;i<rooms.Count;i++)
        {
            for (int j = 0; j < rooms[i].Count; j++)
                Debug.Log(string.Concat("floor : ", i,", idx : ", rooms[i][j].roomNumber, ", type : ", rooms[i][j].type, ", open : ", rooms[i][j].isOpen));
        }
    }
}
