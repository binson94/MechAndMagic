using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RoomType
{
    Empty, Monster, Positive, Neutral, Negative, Quest, Boss
}

///<summary> 방 정보 저장 클래스 </summary>
public class Room
{
    ///<summary> 방의 층, 시작 지점이 0층 </summary>
    public int floor;
    ///<summary> 층에서 왼쪽에서 몇 번째인지, 0부터 시작 </summary>
    public int roomNumber;
    ///<summary> 연결된 다음 층 방들의 roomNumber들 </summary>
    public List<int> next = new List<int>();
    ///<summary> 연결된 이전 층 방들의 roomNumber들, 미연결 방 검사 시 이용 </summary>
    public List<int> prev = new List<int>();

    ///<summary> 방 종류 </summary>
    public RoomType type;
    ///<summary> 몬스터일 경우 방 idx, 이벤트일 경우 이벤트 idx </summary>
    public int roomEventIdx;
    ///<summary> 공개 방 여부, Quest는 반드시 숨김 </summary>
    public bool isOpen;

    ///<summary> 방끼리 연결 </summary>
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
    ///<summary> 연결 제거 </summary>
    public void RemoveLink(Room r)
    {
        if (r != null)
        {
            next.RemoveAll(n => n.Equals(r.roomNumber));
            r.prev.RemoveAll(n => n.Equals(roomNumber));
        }
    }
}

///<summary> 던전 정보 저장하는 클래스 </summary>
public class Dungeon
{
    static List<EventPool> eventPools = new List<EventPool>();
    static Dungeon()
    {
        LitJson.JsonData json = LitJson.JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Dungeons/Event").text);
        for(int i = 0;i < json.Count;i++)
            eventPools.Add(new EventPool((int)json[i]["idx"], (RoomType)(int)json[i]["event"], (int)json[i]["region"]));
    }

    ///<summary> 던전 idx </summary>
    public int idx;
    ///<summary> 던전 이름 </summary>
    public string dungeonName;

    ///<summary> 던전의 층 갯수 </summary>
    public int floorCount;
    ///<summary> 각 층의 방 갯수 </summary>
    public int[] roomCount;

    ///<summary> 0층 : 시작 방만 존재, 최고층 방(rooms[floorCount - 1]) : 보스 방만 존재 </summary>
    public List<Room> rooms = new List<Room>();

    DungeonBluePrint dbp = null;

    #region DungeonMaking
    //던전 생성
    ///<summary> 데이터 로드를 위한 빈 생성자 </summary>
    public Dungeon() {}
    ///<summary> 새로운 던전 생성 </summary>
    public Dungeon(int dungeonIdx)
    {
        dbp = new DungeonBluePrint(dungeonIdx);
        idx = dungeonIdx;
        dungeonName = dbp.name;

        MakeRoom();
        MakePath();
        CheckAloneNode();
        CheckCross();
    }

    ///<summary> 던전 방 생성 </summary>
    private void MakeRoom()
    {
        //층 수 선정 (시작 방 포함해서 + 1)
        floorCount = Random.Range(dbp.floorMinMax[0] + 1, dbp.floorMinMax[1] + 2);
        roomCount = new int[floorCount];

        //시작 층(0층)에는 1개만 존재
        roomCount[0] = 1;
        rooms.Add(NewRoom(0, 0));

        for (int i = 1; i < floorCount - 1; i++)
        {
            roomCount[i] = Random.Range(dbp.roomMinMax[0], dbp.roomMinMax[1] + 1);

            for (int j = 0; j < roomCount[i]; j++)
                rooms.Add(NewRoom(i, j));
        }

        //끝 층에는 1개만 존재(보스방)
        roomCount[floorCount - 1] = 1;
        rooms.Add(NewRoom(-1, 0));
    }
   
    ///<summary> 새로운 방 생성 </summary>
    ///<param name="f"> 방 층 수, 보스방은 -1 </param>
    ///<param name="roomNb"> 방 roomNumber </param>
    private Room NewRoom(int f, int roomNb)
    {
        //보스방 처리
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
        //그 외 방 확률에 따른 설정
        else
        {
            float rand = Random.Range(0, 1f);

            int roomType;
            float roomPivot = dbp.roomKindChances[0];
            for (roomType = 0; rand > roomPivot && roomType < 5; roomPivot += dbp.roomKindChances[++roomType]) ;
            r.type = (RoomType)roomType;
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
                    var events = from token in eventPools
                                 where token.eventType == r.type && (token.region == 0 || token.region == GameManager.instance.slotData.region)
                                 select token.idx;
                    r.roomEventIdx = events.Skip(Random.Range(0, events.Count())).First();
                    break;
                }
            case RoomType.Quest:
                {
                    r.roomEventIdx = GetNewOutbreak();
                    break;
                }
            case RoomType.Boss:
                {
                    r.roomEventIdx = dbp.bossRoomIdx;
                    break;
                }
        }

        //공개 이벤트 설정
        //이벤트 - 확률에 따른 설정
        if (RoomType.Positive <= r.type && r.type <= RoomType.Negative)
        {
            float open = Random.Range(0, 1f);
            if (open < dbp.openChance)
                r.isOpen = true;
            else
                r.isOpen = false;
        }
        //돌발 퀘 - 항상 비공개
        else if (r.type == RoomType.Quest)
            r.isOpen = false;
        //몬스터 및 빈 방 - 항상 공개
        else
            r.isOpen = true;

        return r;
    }
    
    ///<summary> 방 사이 경로 생성 </summary>
    private void MakePath()
    {
        //처음 방은 모든 다음 방과 연결
        for (int j = 0; j < roomCount[1]; j++)
            rooms[0].MakeLink(GetRoom(1, j));

        List<int> nextRoomIdx = new List<int>();

        //1층 ~ 보스 전전층
        for (int i = 1; i < floorCount - 2; i++)
        {
            for (int j = 0; j < roomCount[i]; j++)
            {
                //1개 ~ 3개
                int nextRoomCount = Random.Range(1, 4);
                for (int dir = -1; dir <= 1; dir++) nextRoomIdx.Add(dir);
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
    ///<summary> 연결되지 않은 방 찾아서 연결 </summary>
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
    ///<summary> 교차 제거 </summary>
    private void CheckCross()
    {
        for (int i = 1; i < floorCount - 2; i++)
        {
            for (int j = 0; j < roomCount[i] - 1; j++)
            {
                Room left = GetRoom(i, j);
                Room right = GetRoom(i, j + 1);

                if (!left.next.Contains(j + 1))
                    continue;
                if (!right.next.Contains(j))
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
    ///<summary> 돌발 퀘스트 받을 시, 다른 돌발 퀘스트 방 이벤트로 변경 </summary>
    public void QuestDetermined(int[] pos)
    {
        //중간에 로드한 경우, dbp가 null임
        if(dbp == null)
            dbp = new DungeonBluePrint(idx);

        float probMax = dbp.roomKindChances[(int)RoomType.Positive] 
            + dbp.roomKindChances[(int)RoomType.Neutral] + dbp.roomKindChances[(int)RoomType.Negative];

        foreach (Room r in rooms)
        {
            if (r.type == RoomType.Quest && !(r.floor == pos[0] && r.roomNumber == pos[1])) 
            {
                float rand = Random.Range(0, probMax);
                int i;
                for (i = 2; rand < dbp.roomKindChances[i] && i < 4; i++) ;
                r.type = (RoomType)i;
                
                var events = from token in eventPools
                                 where token.eventType == r.type && (token.region == 0 || token.region == GameManager.instance.slotData.region)
                                 select token.idx;
                r.roomEventIdx = events.Skip(Random.Range(0, events.Count())).First();
            }
        }
    }
    public int GetNewOutbreak()
    {
        if(dbp == null) dbp = new DungeonBluePrint(idx);
        return dbp.questIdx[Random.Range(0, dbp.questCount)];
    }

    ///<summary> j가 배열 밖으로 벗어나도, 보정해서 반환 </summary>
    private Room GetBoundedRoom(int i, int j)
    {
        if (i < 0 || i >= floorCount)
            return null;

        j = Mathf.Max(0, Mathf.Min(roomCount[i] - 1, j));

        return GetRoom(i, j);
    }
    ///<summary> 1차원 배열로 변경함에 따라, 편의성 함수 </summary>
    public Room GetRoom(int i, int j)
    {
        for (int k = 0; k < i; k++)
            j += roomCount[k];
        return rooms[j];
    }

    struct EventPool
    {
        public int idx;
        public RoomType eventType;
        public int region;

        public EventPool(int idx, RoomType type, int region)
        {
            this.idx = idx;
            eventType = type;
            this.region = region;
        }
    }
}
