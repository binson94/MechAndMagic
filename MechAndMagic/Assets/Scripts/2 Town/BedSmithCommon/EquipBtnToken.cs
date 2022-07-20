using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

///<summary> 4개의 장비 버튼을 묶어 한개의 클래스에서 관리 </summary>
public class EquipBtnToken : MonoBehaviour
{
    SmithPanel SM;
    BedItemPanel BM;
    [SerializeField] Image[] gridImages;
    [SerializeField] Image[] iconImages;
    [SerializeField] GameObject[] stars;
    [SerializeField] Text[] countTxts;

    #region TokenData
    ///<summary> 0: equip, 1 : blueprint, 2 : skillbook </summary>
    int tokenKind;
    KeyValuePair<int, Equipment>[] equipmentInfos = new KeyValuePair<int, Equipment>[4];
    KeyValuePair<int, EquipBluePrint>[] ebpInfos = new KeyValuePair<int, EquipBluePrint>[4];
    KeyValuePair<int, Skillbook>[] skillbookInfos = new KeyValuePair<int, Skillbook>[4];
    #endregion TokenData

    ///<summary> 버튼에 표시할 장비 정보 초기화(숙소, 대장간에서 모두 이용) </summary>
    ///<param name="startPos"> categorizedEquips에서 시작 인덱스, 최대 4개까지 수용 </param>
    ///<param name="categorizedEquips"> 선택한 카테고리에 만족하는 장비 전체 리스트 </param>
    public void Initialize(ITownPanel panel, int startPos, List<KeyValuePair<int, Equipment>> categorizedEquips)
    {
        //숙소일 경우 BM이 활성화, SM은 null, 대장간은 반대
        BM = panel as BedItemPanel;
        if((SM = panel as SmithPanel) != null)
            tokenKind = 0;

        int i = 0;
        if(categorizedEquips != null)
            for (; i < 4 && startPos + i < categorizedEquips.Count; i++)
            {
                equipmentInfos[i] = categorizedEquips[startPos + i];
                iconImages[i].sprite = SpriteGetter.instance.GetEquipIcon(categorizedEquips[startPos + i].Value.ebp);
                gridImages[i].sprite = SpriteGetter.instance.GetGrid(categorizedEquips[startPos + i].Value.ebp.rarity);
                iconImages[i].gameObject.SetActive(true);

                for(int j = 0;j < 3;j++)
                    stars[i * 3 + j].SetActive(j < categorizedEquips[i].Value.star);
            }

        for (; i < 4; i++)
        {
            iconImages[i].gameObject.SetActive(false);
            for(int j = 0;j < 3;j++)
                stars[i * 3 + j].SetActive(false);
        }
        foreach(Text t in countTxts) t.text = string.Empty;
    }
    ///<summary> 버튼에 표시할 제작법 정보 초기화(대장간에서 이용) </summary>
    ///<param name="startPos"> 시작 인덱스, 최대 4개까지 수용 </param>
    ///<param name="categorizedEquips"> 선택한 카테고리에 만족하는 제작법 전체 리스트 </param>
    public void Initialize(SmithPanel panel, int startPos, List<KeyValuePair<int, EquipBluePrint>> categorizedRecipes)
    {
        SM = panel;
        BM = null;
        int i = 0;
        tokenKind = 1;

        if(categorizedRecipes != null)
            for (; i < 4 && startPos + i < categorizedRecipes.Count; i++)
            {
                ebpInfos[i] = categorizedRecipes[startPos + i];
                iconImages[i].sprite = SpriteGetter.instance.GetRecipeIcon();
                gridImages[i].sprite = SpriteGetter.instance.GetGrid(categorizedRecipes[startPos + i].Value.rarity);
                iconImages[i].gameObject.SetActive(true);
            }

        for (; i < 4; i++)
            iconImages[i].gameObject.SetActive(false);
        foreach(GameObject star in stars) star.SetActive(false);
        foreach(Text t in countTxts) t.text = string.Empty;
    }
    ///<summary> 버튼에 표시할 스킬북 정보 초기화(대장간에서 이용) </summary>
    ///<param name="startPos"> 시작 인덱스, 최대 4개까지 수용 </param>
    ///<param name="categorizedEquips"> 선택한 카테고리에 만족하는 스킬북 전체 리스트 </param>
    public void Initialize(SmithPanel panel, int startPos, List<KeyValuePair<int, Skillbook>> categorizedSkillbooks)
    {
        SM = panel;
        BM = null;
        int i = 0;
        tokenKind = 2;

        if(categorizedSkillbooks != null)
            for (; i < 4 && startPos + i < categorizedSkillbooks.Count; i++)
            {
                Skill skill = SkillManager.GetSkill(GameManager.instance.slotData.slotClass, categorizedSkillbooks[startPos + i].Value.idx);

                skillbookInfos[i] = categorizedSkillbooks[startPos + i];
                iconImages[i].sprite = SpriteGetter.instance.GetSkillIcon(skill.icon);
                gridImages[i].sprite = SpriteGetter.instance.GetGrid((Rarity)(skill.reqLvl / 2 + 1));
                iconImages[i].gameObject.SetActive(true);
                countTxts[i].text = $"{categorizedSkillbooks[startPos + i].Value.count}";
            }

        for (; i < 4; i++)
            iconImages[i].gameObject.SetActive(false);
        foreach(GameObject star in stars) star.SetActive(false);
    }

    ///<summary> 장비 선택 버튼 </summary>
    public void Btn_Equip(int idx)
    {
        SoundManager.instance.PlaySFX(22);
        if (SM != null)
        {
            if (tokenKind == 0)
                SM.Btn_EquipToken(equipmentInfos[idx]);
            else if (tokenKind == 1)
                SM.Btn_RecipeToken(ebpInfos[idx].Value);
            else if(tokenKind == 2)
                SM.Btn_SkillbookToken(skillbookInfos[idx]);
            else
                SM.Btn_FusionToken(equipmentInfos[idx]);
        }
        else
            BM.Btn_EquipToken(equipmentInfos[idx]);
    }
}
