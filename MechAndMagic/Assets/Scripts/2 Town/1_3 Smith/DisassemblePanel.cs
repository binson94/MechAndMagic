using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisassemblePanel : MonoBehaviour, ITownPanel
{
    [SerializeField] SmithPanel SP;

    ///<summary> 분해 시 획득하는 재화 아이콘 </summary>
    [SerializeField] Image[] resourceIcons;
    ///<summary> 분해 시 획득하는 재화 갯수 </summary>
    [SerializeField] Text[] resourceTxts;

    public void ResetAllState()
    {
        LoadResourceInfo();
    }
    ///<summary> 획득 재화 정보 불러오기 </summary>
    void LoadResourceInfo()
    {
        int i, j;
        for(i = 0, j = 0;i < SP.SelectedEquip.Value.ebp.requireResources.Count;i++)
        {
            Pair<int, int> resourceInfo = SP.SelectedEquip.Value.ebp.requireResources[i];
            int require = Mathf.RoundToInt(0.2f * Mathf.Pow(2, SP.SelectedEquip.Value.star) * resourceInfo.Value);
            if(require <= 0) continue;

            resourceIcons[j].sprite = SpriteGetter.instance.GetResourceIcon(resourceInfo.Key);
            resourceIcons[j].gameObject.SetActive(true);
            resourceTxts[j].text = $"({GameManager.instance.slotData.itemData.basicMaterials[resourceInfo.Key]} + {require})";
            j++;
        }

        for (; j < 4; j++)
        {
            resourceIcons[j].gameObject.SetActive(false);
            resourceTxts[j].text = string.Empty;
        }
    }
  
    public void Btn_Disassemble()
    {
        ItemManager.DisassembleEquipment(SP.SelectedEquip);
        SP.OnDisassemble();
    }
    public void Btn_Cancel() => SP.Btn_OpenWorkPanel(0);
}
