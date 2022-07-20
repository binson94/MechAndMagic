using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourcePanel : MonoBehaviour, ITownPanel
{
    [SerializeField] Image[] resourceIcons;
    [SerializeField] Text[] resourceNames;
    [SerializeField] Text[] resourceCounts;

    public void ResetAllState() => LoadResourceData();
    
    void LoadResourceData()
    {
        for (int i = 1; i <= 15; i++)
        {
            resourceIcons[i].sprite = SpriteGetter.instance.GetResourceIcon(i);
            resourceNames[i].text = ItemManager.GetResourceName(i);
            resourceCounts[i].text = $"{GameManager.instance.slotData.itemData.basicMaterials[i]}";
        }
    }
}
