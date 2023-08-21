using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MergePanel : MonoBehaviour, ITownPanel
{
    [SerializeField] SmithPanel SP;

    ///<summary> 스크롤뷰 컨텐츠 </summary>
    [SerializeField] RectTransform tokenParent;
    List<EquipBtnToken> btnTokens = new List<EquipBtnToken>();

    ///<summary> 재료로 쓰이는 장비 정보 </summary>
    KeyValuePair<int, Equipment> resourceEquip;
    ///<summary> 재료로 쓰이는 장비 없을 시 상태 </summary>
    KeyValuePair<int, Equipment> dummyEquip = new KeyValuePair<int, Equipment>(-1, null);
    ///<summary> 재료로 쓰이는 장비 메인, 서브 스텟 </summary>
    [SerializeField] Text resourceEquipStat;
    ///<summary> 재료로 쓰이는 장비 메인, 공통 옵션 </summary>
    [SerializeField] Text resourceEquipCommonStat;

    [SerializeField] GameObject mergeBtn;

    public void ResetAllState()
    {
        resourceEquip = dummyEquip;
        TokenBtnReset();
        LoadResourceEquipInfo();
        ResourceInfoUpdate();
    }

    ///<summary> 재료로 쓰일 수 있는 장비 불러오기 </summary>
    void LoadResourceEquipInfo()
    {
        //카테고리에 맞는 장비만 얻기
        List<KeyValuePair<int, Equipment>> categorizedEquips = ItemManager.GetResourceEquipData(SP.SelectedEquip);

        for (int i = 0; i < categorizedEquips.Count; i += 4)
        {
            //풀에서 버튼 토큰 가져오기
            EquipBtnToken token = GameManager.GetToken(SP.equipBtnPool, tokenParent, SP.equipBtnPrefab);
            token.Initialize(this, i, categorizedEquips);
            btnTokens.Add(token);
            token.gameObject.SetActive(true);
        }

        for (int i = btnTokens.Count; i < 4; i++)
        {
            EquipBtnToken token = GameManager.GetToken(SP.equipBtnPool, tokenParent, SP.equipBtnPrefab);
            token.Initialize(this, 0, null);
            btnTokens.Add(token);
            token.gameObject.SetActive(true);
        }
    }
    ///<summary> 스크롤뷰에 있는 버튼들 초기화 </summary>
    void TokenBtnReset()
    {
        for(int i = 0; i < btnTokens.Count; i++)
        {
            btnTokens[i].gameObject.SetActive(false);
            btnTokens[i].transform.SetParent(SP.poolParent);
            SP.equipBtnPool.Enqueue(btnTokens[i]);
        }
        btnTokens.Clear();
    }

    ///<summary> 재료로 쓰이는 장비 정보 불러오기 </summary>
    void ResourceInfoUpdate()
    {
        if(resourceEquip.Value != null)
        {
            resourceEquipStat.text = $"{resourceEquip.Value.mainStat}\t+{resourceEquip.Value.mainStatValue}\n";
            if(resourceEquip.Value.subStat != Obj.None)
                resourceEquipStat.text += $"{resourceEquip.Value.subStat}\t+{resourceEquip.Value.subStatValue}";

            resourceEquipCommonStat.text = string.Empty;
            for(int i = 0;i < resourceEquip.Value.commonStatValue.Count;i++)
                resourceEquipCommonStat.text += $"{resourceEquip.Value.commonStatValue[i].Key}\t+{resourceEquip.Value.commonStatValue[i].Value}\n";
        }
        else
            resourceEquipStat.text = resourceEquipCommonStat.text = string.Empty;
        mergeBtn.SetActive(resourceEquip.Value != null);
    }

    ///<summary> 재료 장비 선택 </summary>
    public void Btn_EquipToken(KeyValuePair<int, Equipment> equipInfo)
    {
        resourceEquip = equipInfo;
        ResourceInfoUpdate();
    }

    public void Btn_Merge()
    {
        if(resourceEquip.Value == null) return;

        ItemManager.MergeEquipment(SP.SelectedEquip, resourceEquip);
        SP.ResetSelectInfo();
    }

    public void Btn_Cancel() => SP.Btn_OpenWorkPanel(0);
}
