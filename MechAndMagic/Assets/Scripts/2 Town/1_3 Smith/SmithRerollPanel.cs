using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SmithRerollPanel : MonoBehaviour, ITownPanel
{
    [SerializeField] SmithPanel SP;

    ///<summary> 옵션 변경 시 필요한 재화 아이콘 </summary>
    [SerializeField] Image[] resourceIcons;
    ///<summary> 옵션 변경 시 필요한 재화 갯수 </summary>
    [SerializeField] Text[] resourceTxts;
    ///<summary> 옵션 변경 결과 텍스트 </summary>
    [SerializeField] Text resultTxt;
    ///<summary> 옵션 변경 가능 여부 </summary>
    bool canReroll;
    ///<summary> 광고 옵션 변경 가능 여부, 한 번으로 제한 </summary>
    bool canAdReroll;

    ///<summary> 옵션 변경 버튼, 가능 여부 투명도로 보여줌 </summary>
    [SerializeField] Image rerollBtn;
    ///<summary> 옵션 변경 텍스트, 가능 여부 투명도로 보여줌 </summary>
    [SerializeField] Text rerollTxt;
    ///<summary> 광고 옵션 변경 버튼, 가능 여부 투명도로 보여줌 </summary>
    [SerializeField] Image adRerollBtn;
    ///<summary> 광고 옵션 변경 텍스트, 가능 여부 투명도로 보여줌 </summary>
    [SerializeField] Text adRerollTxt;

    [SerializeField] GameObject rerollSet;
    [SerializeField] GameObject adRerollSet;

    public void ResetAllState()
    {
        canReroll = true;
        rerollSet.SetActive(true);
        adRerollSet.SetActive(false);

        LoadResourceInfo();

        Color color = canReroll ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.5f);
        rerollBtn.color = color; rerollTxt.color = color;
    }
    ///<summary> 소비 재화 정보 불러오기 </summary>
    void LoadResourceInfo()
    {
        int i, j;
        for(i = 0, j = 0;i < SP.SelectedEquip.Value.ebp.requireResources.Count;i++)
        {
            Pair<int, int> resourceInfo = SP.SelectedEquip.Value.ebp.requireResources[i];
            int require = Mathf.RoundToInt(0.4f * resourceInfo.Value);
            if(require <= 0) continue;

            resourceIcons[j].sprite = SpriteGetter.instance.GetResourceIcon(resourceInfo.Key);
            resourceIcons[j].gameObject.SetActive(true);
            resourceTxts[j].text = $"({GameManager.instance.slotData.itemData.basicMaterials[resourceInfo.Key]} / {require})";
            if(GameManager.instance.slotData.itemData.basicMaterials[resourceInfo.Key] < require)
            {
                resourceTxts[j].text = $"<color=#f93f3d>{resourceTxts[j].text}</color>";
                canReroll = false;
            }
            j++;
        }

        for (; j < 4; j++)
        {
            resourceIcons[j].gameObject.SetActive(false);
            resourceTxts[j].text = string.Empty;
        }
    }

    ///<summary> 옵션 변경 </summary>
    public void Btn_Reroll()
    {
        if (!canReroll) return;

        ItemManager.SwitchCommonStat(SP.SelectedEquip.Value);
        SP.OnEquipReroll();

        resultTxt.text = string.Empty;
        for(int i = 0;i <SP.SelectedEquip.Value.commonStatValue.Count;i++)
            resultTxt.text += $"{SP.SelectedEquip.Value.commonStatValue[i].Key} +{SP.SelectedEquip.Value.commonStatValue[i].Value}\n";

        canAdReroll = true;
        rerollSet.SetActive(false);
        adRerollSet.SetActive(true);
        adRerollBtn.color = new Color(1, 1, 1, 1);
        adRerollTxt.color = new Color(1, 1, 1, 1);
    }
    ///<summary> 광고 보고 추가로 옵션 변경 </summary>
    public void Btn_AdReroll()
    {
        if (!(canAdReroll && AdManager.instance.IsLoaded())) return;

        AdManager.instance.ShowRewardAd(OnAdReward);
    }

    void OnAdReward(object sender, GoogleMobileAds.Api.Reward reward)
    {
        canAdReroll = false;
        ItemManager.SwitchCommonStat(SP.SelectedEquip.Value);
        SP.OnEquipReroll();

        resultTxt.text = string.Empty;
        for(int i = 0;i <SP.SelectedEquip.Value.commonStatValue.Count;i++)
            resultTxt.text += $"{SP.SelectedEquip.Value.commonStatValue[i].Key} +{SP.SelectedEquip.Value.commonStatValue[i].Value}\n";

        adRerollBtn.color = new Color(1, 1, 1, 0.5f);
        adRerollTxt.color = new Color(1, 1, 1, 0.5f);
    }

    ///<summary> 옵션 변경 취소, 선택 부분으로 돌아감 </summary>
    public void Btn_Cancel() => SP.Btn_OpenWorkPanel(0);
    ///<summary> 옵션 변경 완료, 옵션 변경 시작 부분으로 돌아감 </summary>
    public void Btn_End() => SP.Btn_OpenWorkPanel(2);
}
