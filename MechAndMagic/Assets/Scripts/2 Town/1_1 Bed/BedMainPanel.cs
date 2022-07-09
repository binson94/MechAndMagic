using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BedMainPanel : MonoBehaviour, ITownPanel
{
    [SerializeField] Image characterIllust;
    [SerializeField] Sprite[] charSprites;

    private void Awake() 
    {
        characterIllust.sprite = charSprites[GameManager.instance.slotData.slotClass];
    }

    public void ResetAllState()
    {

    }
}
