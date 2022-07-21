using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class SmithMenuPanel : MonoBehaviour, ITownPanel
{
    [SerializeField] SmithPanel SP;

    ///<summary> 조합 강화 버튼, 불가능 시 투명도 설정 </summary>
    [SerializeField] Image mergeBtn;
    ///<summary> 조합 강화 텍스트, 불가능 시 투명도 설정 </summary>
    [SerializeField] Text mergeTxt;
    ///<summary> 옵션 변경 버튼, 불가능 시 투명도 설정 </summary>
    [SerializeField] Image rerollBtn;
    ///<summary> 옵션 변경 텍스트, 불가능 시 투명도 설정 </summary>
    [SerializeField] Text rerollTxt;

    bool canMerge;
    bool canReroll;

    public void ResetAllState()
    {
        canMerge = SP.SelectedEquip.Value.star < 3;
        canReroll = SP.SelectedEquip.Value.ebp.commonStats.Any(x => x == 13);
        Color mergeColor = canMerge ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.5f);
        Color rerollColor = canReroll ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.5f);
        
        mergeBtn.color = mergeColor; mergeTxt.color = mergeColor;
        rerollBtn.color = rerollColor; rerollTxt.color = rerollColor;
    }

    public void Btn_Merge()
    {
        if(!canMerge) return;

        SP.Btn_OpenWorkPanel(1);
    }
    public void Btn_Reroll()
    {
        if(!canReroll) return;

        SP.Btn_OpenWorkPanel(2);
    }
    public void Btn_Disassemble() => SP.Btn_OpenWorkPanel(3);
}
