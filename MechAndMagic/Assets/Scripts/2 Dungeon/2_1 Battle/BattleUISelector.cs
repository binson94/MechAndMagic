using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleUISelector : MonoBehaviour
{
    [SerializeField] BattleManager[] BMs;

    private void Start() 
    {
        for (int i = 0; i < 2; i++) BMs[i].gameObject.SetActive(GameManager.instance.slotData.slotClass < 5 ^ i == 1);
        (GameManager.instance.slotData.slotClass < 5 ? BMs[0] : BMs[1]).OnStart();
    }
}
