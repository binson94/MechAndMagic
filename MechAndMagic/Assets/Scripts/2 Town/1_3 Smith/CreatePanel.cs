using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class CreatePanel : MonoBehaviour, ITownPanel
{
    [SerializeField] SmithPanel SP;
    [SerializeField] GameObject createSet;
    [SerializeField] GameObject resultSet;

    ///<summary> 제작 시 필요한 재화 아이콘 </summary>
    [Header("Create")]
    [SerializeField] Image[] resourceIcons;
    ///<summary> 제작 시 필요한 재화 갯수 </summary>
    [SerializeField] Text[] resourceTxts;

    ///<summary> 제작 버튼, 제작 불가 시 투명도 설정 </summary>
    [SerializeField] Image createBtn;
    ///<summary> 제작 텍스트, 제작 불가 시 투명도 설정 </summary>
    [SerializeField] Text createTxt;
    bool canCreate;
    Equipment createdEquip = null;

    ///<summary> 장비 이름 텍스트 </summary>
    [Header("Result")]
    [SerializeField] Text equipNameTxt;
    ///<summary> 제작 결과 스텟 텍스트 </summary>
    [SerializeField] Text[] statTxts;
    [SerializeField] Image gridImage;
    [SerializeField] Image iconImage;
    ///<summary> 다시 제작 버튼, 리롤 시 투명도 설정 </summary>
    [SerializeField] Image rerollBtn;
    ///<summary> 다시 제작 텍스트, 리롤 시 투명도 설정 </summary>
    [SerializeField] Text rerollTxt;
    bool canReroll;

    public void ResetAllState()
    {
        canCreate = true;
        createdEquip = null;
        LoadResourceInfo();
        createSet.SetActive(true);
        resultSet.SetActive(false);

        Color color = canCreate ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.5f);
        createBtn.color = color;
        createTxt.color = color;

    }

    void LoadResourceInfo()
    {
        int i;
        for(i = 0;i < SP.SelectedEBP.requireResources.Count;i++)
        {
            Pair<int, int> resourceInfo = SP.SelectedEBP.requireResources[i];
            resourceIcons[i].sprite = SpriteGetter.instance.GetResourceIcon(resourceInfo.Key);
            resourceIcons[i].gameObject.SetActive(true);
            resourceTxts[i].text = $"({GameManager.instance.slotData.itemData.basicMaterials[resourceInfo.Key]} / {resourceInfo.Value})";
            if(GameManager.instance.slotData.itemData.basicMaterials[resourceInfo.Key] < resourceInfo.Value)
            {
                resourceTxts[i].text = $"<color=#f93f3d>{resourceTxts[i].text}</color>";
                canCreate = false;
            }
        }

        for(;i<4;i++)
        {
            resourceIcons[i].gameObject.SetActive(false);
            resourceTxts[i].text = string.Empty;
        }
    }

    public void Btn_Create()
    {
        if (!canCreate) return;

        createdEquip = ItemManager.CreateEquipment(SP.SelectedEBP);
        ShowResult();
    }

    void ShowResult()
    {
        createSet.SetActive(false);

        equipNameTxt.text = createdEquip.ebp.name;
        statTxts[0].text = $"{createdEquip.mainStat}\t+{createdEquip.mainStatValue}\n";
        if (createdEquip.subStat != Obj.None)
            statTxts[0].text += $"{createdEquip.subStat}\t+{createdEquip.subStatValue}";

        statTxts[1].text = string.Empty;
        for (int i = 0; i < createdEquip.commonStatValue.Count; i++)
            statTxts[1].text += $"{createdEquip.commonStatValue[i].Key}\t+{createdEquip.commonStatValue[i].Value}\n";

        gridImage.sprite = SpriteGetter.instance.GetGrid(createdEquip.ebp.rarity);
        iconImage.sprite = SpriteGetter.instance.GetEquipIcon(createdEquip.ebp);

        //재제작 가능 여부 - 보조 스텟 또는 공통 옵션
        canReroll = (SP.SelectedEBP.part >= EquipPart.Ring) ||
                    (SP.SelectedEBP.rarity >= Rarity.Uncommon && SP.SelectedEBP.subStat == Obj.None && SP.SelectedEBP.part == EquipPart.Weapon) ||
                    (SP.SelectedEBP.commonStats.Any(x => x == 13));

        Color color = canReroll ? new Color(1, 1, 1, 1f) : new Color(1, 1, 1, 0.5f);
        rerollBtn.color = color;
        rerollTxt.color = color;

        resultSet.SetActive(true);
    }

    public void Btn_Reroll()
    {
        if (!canReroll || !AdManager.instance.IsLoaded()) return;

        AdManager.instance.ShowRewardAd(OnAdReward);
    }

    public void Btn_Done() => SP.ResetSelectInfo();
    

    ///<summary> 광고 보고 리롤 </summary>
    void OnAdReward(object sender, GoogleMobileAds.Api.Reward reward)
    {
        createdEquip.ReCreate();
        GameManager.instance.SaveSlotData();

        canReroll = false;
        rerollBtn.color = new Color(1, 1, 1, 0.5f);
        rerollTxt.color = new Color(1, 1, 1, 0.5f);

        statTxts[0].text = $"{createdEquip.mainStat}\t+{createdEquip.mainStatValue}\n";
        if (createdEquip.subStat != Obj.None)
            statTxts[0].text += $"{createdEquip.subStat}\t+{createdEquip.subStatValue}";

        statTxts[1].text = string.Empty;
        for (int i = 0; i < createdEquip.commonStatValue.Count; i++)
            statTxts[1].text += $"{createdEquip.commonStatValue[i].Key}\t+{createdEquip.commonStatValue[i].Value}\n";
    }
}
