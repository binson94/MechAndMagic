using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PotionInfoPanel : MonoBehaviour
{
    [SerializeField] Image potionIcon;
    [SerializeField] Text potionNameTxt;
    [SerializeField] Text potionScriptTxt;

    public void InfoUpdate(int potionIdx, bool used = false)
    {
        if(potionIdx > 0)
        {
            Potion potion = new Potion(potionIdx);
            potionNameTxt.text = potion.name;
            if(used)
                potionNameTxt.text += "<color=#ed2929>(사용함)</color>";
            potionScriptTxt.text = potion.script;

            potionIcon.sprite = SpriteGetter.instance.GetPotionIcon(potionIdx);
            potionIcon.gameObject.SetActive(true);
        }
        else
        {
            potionNameTxt.text = potionScriptTxt.text = string.Empty;
            potionIcon.gameObject.SetActive(false);
        }
    }
}
