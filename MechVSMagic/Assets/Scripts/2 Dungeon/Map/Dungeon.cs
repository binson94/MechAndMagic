using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RoomType
{
    Empty, Monster, Positive, Neutral, Negative, Quest, Boss
}

[System.Serializable]
public class Room
{
    public int floor;
    //층에서 왼쪽에서 몇 번째인지
    public int roomNumber;
    public List<int> next = new List<int>();
    public List<int> prev = new List<int>();
    public RoomType type;
    //몬스터일 경우-> 방 인덱스, 이벤트일 경우 -> 이벤트 인덱스
    public int roomEventIdx;
    public bool isOpen;

    public void MakeLink(Room r)
    {
        if (r != null)
            if (!next.Contains(r.roomNumber))
            {
                next.Add(r.roomNumber);
                r.prev.Add(roomNumber);
                next.Sort();
                prev.Sort();
            }
    }
    public void RemoveLink(Room r)
    {
        if (r != null)
        {
            next.RemoveAll(n => n.Equals(r.roomNumber));
            r.prev.RemoveAll(n => n.Equals(roomNumber));
        }
    }
}

[System.Serializable]
public class Dungeon
{
    public int idx;
    public string dungeonName;

    //층의 갯수
    public int floorCount;
    //각 층의 방 갯수
    public int[] roomCount;

    //0층 : 시작 방만 존재, 최고층 방(rooms[floorCount - 1]) : 보스 방만 존재
    public List<Room> rooms = new List<Room>();

    DungeonBluePrint dbp;
    #region DungeonMaking
    //던전 생성
    public void DungeonInstantiate(DungeonBluePrint dbp)
    {
        this.dbp = dbp;
        MakeRoom();
        MakePath();
        CheckAloneNode();
        CheckCross();
    }

    //방 생성
    private void MakeRoom()
    {
        //층 수 선정 (시작 방 포함해서 + 1)
        floorCount = Random.Range(dbp.floorMinMax[0] + 1, dbp.floorMinMax[1] + 2);
        roomCount = new int[floorCount];

        roomCount[0] = 1;
        rooms.Add(NewRoom(0, 0));

        for (int i = 1; i < floorCount - 1; i++)
        {
            roomCount[i] = Random.Range(dbp.roomMinMax[0], dbp.roomMinMax[1] + 1);

            for (int j = 0; j < roomCount[i]; j++)
                rooms.Add(NewRoom(i, j));
        }

        roomCount[floorCount - 1] = 1;
        rooms.Add(NewRoom(-1, 0));
    }
    
    //prob : empty, monster, pos, neu, neg, quest 순서 확률
    private Room NewRoom(int f, int roomNb)
    {
        Room r = new Room
        {
            floor = f < 0 ? floorCount - 1 : f,
            roomNumber = roomNb
        };

        //시작 방 설정
        if (f == 0)
        {
            r.type = RoomType.Empty;
            r.prev.Add(-1);
        }
        //보스 방 설정
        else if (f == -1)
        {
            r.type = RoomType.Boss;
            r.next.Add(-1);
        }
        else
        {
            float rand = Random.Range(0, 1f);

            int roomI;
            float roomPivot = dbp.roomKindChances[0];
            for (roomI = 0; rand > roomPivot && roomI < 5; roomPivot += dbp.roomKindChances[++roomI]) ;
            r.type = (RoomType)roomI;
        }

        //몬스터 방 && 이벤트 설정
        switch (r.type)
        {
            case RoomType.Monster:
                {
                    float rand = Random.Range(0, 1f);
                    int monIdx;
                    float monProb = dbp.monRoomChance[0];
                    for (monIdx = 0; rand > monProb && monIdx < dbp.monRoomCount - 1; monProb += dbp.monRoomChance[++monIdx]) ;
                    r.roomEventIdx = dbp.monRoomIdx[monIdx];
                    break;
                }
            case RoomType.Positive:
            case RoomType.Neutral:
            case RoomType.Negative:
                {

                    int[] pivots = (from num in dbp.eventIdx
                                    where (r.type == RoomType.Positive && num <= 10) || (r.type == RoomType.Neutral && (10 < num && num <= 20))
                                    || (r.type == RoomType.Negative && (20 < num && num <= 30))
                                    select num).ToArray();
                    r.roomEventIdx = pivots.Skip(Random.Range(0, pivots.Length)).First();
                    break;
                }
            case RoomType.Quest:
                {
                    r.roomEventIdx = dbp.questIdx[Random.Range(0, dbp.questCount)];
                    break;
                }
            case RoomType.Boss:
                {
                    r.roomEventIdx = dbp.bossRoomIdx;
                    break;
                }
        }

        //공개 이벤트 설정
        if (2 <= (int)r.type && (int)r.type <= 4)
        {
            float open = Random.Range(0, 1f);
            if (open < dbp.openChance)
                r.isOpen = true;
            else
                r.isOpen = false;
        }
        else if (r.type == RoomType.Quest)
            r.isOpen = false;
        else
            r.isOpen = true;

        return r;
    }

    //경로 생성
    private void MakePath()
    {
        //처음 방은 모든 다음 방과 연결
        for (int j = 0; j < roomCount[1]; j++)
            rooms[0].MakeLink(GetRoom(1, j));

        List<int> nextRoomIdx = new List<int>();

        for (int i = 1; i < floorCount - 1; i++)
        {
            for (int j = 0; j < roomCount[i]; j++)
            {
                //1개 ~ 3개
                int nextRoomCount = Random.Range(1, 4);
                for (int k = -1; k < 2; k++) nextRoomIdx.Add(k);
                Shuffle(nextRoomIdx);

                for (int k = 0; k < nextRoomCount; k++)
                    GetRoom(i, j).MakeLink(GetBoundedRoom(i + 1, j + nextRoomIdx[k]));

                nextRoomIdx.Clear();
            }
        }

        //막 전 층은 모두 보스 방과 연결
        for (int j = 0; j < roomCount[floorCount - 2]; j++)
            GetRoom(floorCount - 2, j).MakeLink(GetRoom(floorCount - 1, 0));

        void Shuffle(List<int> list)
        {
            int n = list.Count;

            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                int v = list[k];
                list[k] = list[n];
                list[n] = v;
            }
        }
    }

    //연결되지 않은 방 찾아서 연결
    private void CheckAloneNode()
    {
        for (int i = 2; i < floorCount - 1; i++)
        {
            for (int j = 0; j < roomCount[i]; j++)
            {
                if (GetRoom(i, j).prev.Count <= 0)
                    GetBoundedRoom(i - 1, j + Random.Range(-1, 2)).MakeLink(GetRoom(i, j));
            }
        }
    }

    //교차 제거
    private void CheckCross()
    {
        for (int i = 1; i < floorCount - 1; i++)
        {
            for (int j = 0; j < roomCount[i] - 1; j++)
            {
                Room left = GetRoom(i, j);
                Room right = GetRoom(i, j + 1);

                if (!left.next.Any(n => n == j + 1))
                    continue;
                if (!right.next.Any(n => n == j))
                    continue;

                left.MakeLink(GetRoom(i + 1, j));
                right.MakeLink(GetRoom(i + 1, j + 1));

                float rnd = Random.Range(0, 1f);
                if(rnd < 0.33f)
                {
                    left.RemoveLink(GetRoom(i + 1, j + 1));
                    right.RemoveLink(GetRoom(i + 1, j));
                }
                else if(rnd < 0.66f)
                    left.RemoveLink(GetRoom(i + 1, j + 1));
                else
                    right.RemoveLink(GetRoom(i + 1, j));
            }
        }
    }
    #endregion DungeonMaking

    public void QuestDetermined(int[] pos)
    {
        float probMax = dbp.roomKindChances[(int)RoomType.Positive] 
            + dbp.roomKindChances[(int)RoomType.Neutral] + dbp.roomKindChances[(int)RoomType.Negative];

        foreach (Room r in rooms)
        {
            if (r.type == RoomType.Quest && (r != GetRoom(pos[0], pos[1]))) 
            {
                float rand = Random.Range(0, probMax);
                int i;
                for (i = 2; rand < dbp.roomKindChances[i] && i < 4; i++) ;
                r.type = (RoomType)i;
                
                int[] pivots = (from num in dbp.eventIdx
                                where (r.type == RoomType.Positive && num <= 10) || (r.type == RoomType.Neutral && (10 < num && num <= 20))
                                || (r.type == RoomType.Negative && (20 < num && num <= 30))
                                select num).ToArray();
                r.roomEventIdx = pivots.Skip(Random.Range(0, pivots.Length)).First();
                break;
            }
        }
    }

    public void DebugShow()
    {
        for (int i = 0; i < floorCount; i++)
        {
            Debug.Log(string.Concat(i, " floor, ", roomCount[i], " rooms"));
            for (int j = 0; j < roomCount[i]; j++) 
            {
                Debug.Log(string.Concat("(", i, ", ", j, "), type : ", GetRoom(i,j).type, ", open : ", GetRoom(i, j).isOpen));
            }
        }
    }

    //j가 배열 밖으로 벗어나도, 보정해서 반환
    private Room GetBoundedRoom(int i, int j)
    {
        if (i < 0 || i >= floorCount)
            return null;

        j = Mathf.Max(0, Mathf.Min(roomCount[i] - 1, j));

        return GetRoom(i, j);
    }
    //1차원 배열로 변경함에 따라, 편의성 함수
    public Room GetRoom(int i, int j)
    {
        for (int k = 0; k < i; k++)
            j += roomCount[k];
        return rooms[j];
    }
}
