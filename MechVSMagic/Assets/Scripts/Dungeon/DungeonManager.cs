using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    [SerializeField] Dungeon currDungeon;

    private void Start()
    {
        currDungeon = new Dungeon();

        currDungeon.DungeonInstantiate(new DungeonBluePrint(1));
        currDungeon.DebugShow();
    }
}
