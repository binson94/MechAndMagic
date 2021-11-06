using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.Linq;
using LitJson;

public class DungeonManager : MonoBehaviour
{
    public int dungeonIdx;
    Dungeon currDungeon;
    public int[] currPos = new int[2];

    [SerializeField] RoomConnectManager roomConnectMgr;
    [SerializeField] GameObject roomPrefab;

    [SerializeField] ScrollRect scroll;
    [SerializeField] GameObject scrollContent;

    List<List<RoomImage>> roomImages = new List<List<RoomImage>>();

    private void Start()
    {
        currDungeon = new Dungeon();

        LoadDungeon();
        MakeImage();
        SaveDungeon();

        currDungeon.DebugShow();
    }

    #region DungeonMaking
    public void SaveDungeon()
    {
        string dungeonData = JsonMapper.ToJson(currDungeon);
        Debug.Log(dungeonData);

        PlayerPrefs.SetString("Dungeon", dungeonData);
    }

    private void LoadDungeon()
    {
        if(PlayerPrefs.HasKey("Dungeon"))
        {
            currDungeon = JsonMapper.ToObject<Dungeon>(PlayerPrefs.GetString("Dungeon"));
            currPos[0] = PlayerPrefs.GetInt("PosX");
            currPos[1] = PlayerPrefs.GetInt("PosY");
        }
        else
        {
            currDungeon.DungeonInstantiate(new DungeonBluePrint(1));
        }
    }

    private void MakeImage()
    {
        for (int i = 0; i < currDungeon.floorCount; i++)
        {
            roomImages.Add(new List<RoomImage>());
            for (int j = 0; j < currDungeon.roomCount[i]; j++)
            {
                RoomImage r = Instantiate(roomPrefab).GetComponent<RoomImage>();
                r.transform.parent = scrollContent.transform;
                r.Init(currDungeon.GetRoom(i,j), this);
                r.SetPosition(new Vector3(0, 100, 0) + Vector3.right * 1080f * (j + 1) / (currDungeon.roomCount[i] + 1) + Vector3.up * currDungeon.GetRoom(i,j).floor * 300);
                roomImages[i].Add(r);
            }
        }

        for (int i = 0; i < currDungeon.floorCount - 1; i++)
        {
            for (int j = 0; j < currDungeon.roomCount[i]; j++)
            {
                for (int k = 0; k < currDungeon.GetRoom(i,j).next.Count; k++)
                {
                    roomConnectMgr.AddConnect(roomImages[i][j].rect, roomImages[i + 1][currDungeon.GetRoom(i,j).next[k]].rect);
                }

            }
        }
    }
    #endregion

    #region DungeonProcess
    public void Btn_RoomSelect(params int[] pos)
    {
        if (pos[0] != currPos[0] + 1 || !currDungeon.GetRoom(currPos[0], currPos[1]).next.Any(n => n == pos[1]))
        {
            Debug.Log("Can't move there");
            return;
        }

        currPos = (int[])pos.Clone();
        PlayerPrefs.SetInt("PosX", currPos[0]);
        PlayerPrefs.SetInt("PosY", currPos[1]);
    }

    public void Debug_NewDungeon()
    {
        PlayerPrefs.DeleteKey("PosX");
        PlayerPrefs.DeleteKey("PosY");
        PlayerPrefs.DeleteKey("Dungeon");
        SceneManager.LoadScene("Dungeon");
    }
    #endregion
}
