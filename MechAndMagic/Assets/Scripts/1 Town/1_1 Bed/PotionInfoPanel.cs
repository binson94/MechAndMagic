using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PotionInfoPanel : MonoBehaviour
{
    [SerializeField] Image potionIcon;
    [SerializeField] Text potionNameTxt;
    [SerializeField] Text potionScriptTxt;

    public void InfoUpdate(int potionIdx)
    {
        if(potionIdx > 0)
        {
            Potion potion = new Potion(potionIdx);
            potionNameTxt.text = potion.name;
            potionScriptTxt.text = potion.script;

            potionIcon.sprite = Resources.Load<Sprite>($"Sprites/Item/Potion/potion{potionIdx}");
            potionIcon.gameObject.SetActive(true);
        }
        else
        {
            potionNameTxt.text = potionScriptTxt.text = string.Empty;
            potionIcon.gameObject.SetActive(false);
        }
    }
}
