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
        characterIllust.sprite = charSprites[GameManager.SlotClass];
        characterIllust.SetNativeSize();

        var height = characterIllust.rectTransform.rect.height;
        Vector2 rect = new Vector2(characterIllust.rectTransform.rect.width, height);
        rect = rect / height * 1030;
        characterIllust.rectTransform.sizeDelta = rect;
    }

    public void ResetAllState()
    {

    }
}
