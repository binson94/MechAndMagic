using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleFade : MonoBehaviour
{
    [SerializeField] BattleManager BM;
    public void EndFadeIn()
    {
        BM.BattleStart();
        gameObject.SetActive(false);
    }
}
