using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using GoogleMobileAds.Api;

public class CashPanel : MonoBehaviour
{
    ///<summary> 현재 동기화된 레벨 </summary>
    int currLvl = 0;

    ///<summary> 획득하는 공통 재화 아이콘 </summary>
    [Header("Resource")]
    [SerializeField] Image resourceImage;
    ///<summary> 획득하는 공통 재화 이름과 갯수 </summary>
    [SerializeField] Text resourceTxt;
    ///<summary> 공통 재화 인덱스와 갯수 </summary>
    int resourceIdx, resourceCount;

    ///<summary> 제작법 획득 확률 </summary>
    [Header("Recipe")]
    [SerializeField] Text[] recipeTxts;
    ///<summary> 등급 별 확률 (common -> legend 오름차순) </summary>
    float[] recipeProbs = new float[5];
    ///<summary> 획득할 수 있는 레시피 리스트 </summary>
    List<int>[] possbleRecipeList;
    ///<summary> 제작법 획득 버튼, 모든 제작법 획득 시 비활성화 </summary>
    [SerializeField] Button recipeGetBtn;
    ///<summary> 제작법 획득 텍스트, 모든 제작법 획득 시 비활성화 </summary>
    [SerializeField] Text recipeGetTxt;

    ///<summary> 레시피 획득 가능 시 보여주는 UI Set </summary>
    [SerializeField] GameObject recipePossiblePanel;
    ///<summary> 레시피 획득 불가능 시 보여주는 UI Set </summary>
    [SerializeField] GameObject recipeImpossiblePanel;

    ///<summary> 스킬북 획득 확률 표시 텍스트 </summary>
    [Header("Skillbook")]
    [SerializeField] Text[] skillbookTxts;
    ///<summary> 스킬북 획득 확률, lvl 1부터 오름차순 </summary>
    float[] skillbookProbs = new float[5];

    [Header("Result")]
    ///<summary> 획득 결과 보여주는 UI Set </summary>
    [SerializeField] GameObject resultPanel;
    ///<summary> 획득 결과 표시 텍스트 </summary>
    [SerializeField] Text resultTxt;

    JsonData json = null;

    public void ResetAllState()
    {
        if(json == null)
            json = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Ad").text);

        if(GameManager.SlotLvl != currLvl)
        {
            LoadResourceData();
            LoadRecipeData();
            LoadSkillbookData();
            currLvl = GameManager.SlotLvl;
        }

        resultPanel.SetActive(false);
    }

    ///<summary> 공통 재화 획득 정보 불러오기 </summary>
    void LoadResourceData()
    {
        resourceIdx = GameManager.SlotLvl <= 2 ? 15 : (GameManager.SlotLvl <= 6 ? 14 : 13);
        resourceCount = (int)json[GameManager.SlotLvl - 1]["resource"];

        resourceImage.sprite = SpriteGetter.instance.GetResourceIcon(resourceIdx);
        resourceTxt.text = $"{ItemManager.GetResourceName(resourceIdx)} x{resourceCount}";
    }
    ///<summary> 제작법 획득 정보 불러오기 </summary>
    void LoadRecipeData()
    {
        for(Rarity rarity = Rarity.Common; rarity <= Rarity.Legendary; rarity++)
        {
            recipeProbs[rarity - Rarity.Common] = float.Parse(json[GameManager.SlotLvl -1][$"{rarity}"].ToString());
            recipeTxts[rarity - Rarity.Common].text = string.Format("{0:F0}%", recipeProbs[rarity - Rarity.Common] * 100);
        }

        possbleRecipeList = ItemManager.GetAdRecipe(GameManager.SlotLvl);

        RecipeCountCheck();
    }
    void RecipeCountCheck()
    {
        int count = 0;
        for(int i = 0;i < 5;i++) count += possbleRecipeList[i].Count;
        //모든 레시피 획득 시
        if(count <= 0)
        {
            recipeGetBtn.image.color = new Color(1, 1, 1, 0.5f);
            recipeGetBtn.interactable = false;
            recipeGetTxt.color = new Color(1, 1, 1, 0.5f);

            recipePossiblePanel.SetActive(false);
            recipeImpossiblePanel.SetActive(true);
        }
        else
        {
            recipeGetBtn.image.color = Color.white;
            recipeGetBtn.interactable = true;
            recipeGetTxt.color = Color.white;

            recipePossiblePanel.SetActive(true);
            recipeImpossiblePanel.SetActive(false);
        }
    }
    ///<summary> 스킬북 획득 정보 불러오기 </summary>
    void LoadSkillbookData()
    {
        float[] pivotValue = new float[5];
        float sum = 0;
        int lvl = GameManager.SlotLvl;
        pivotValue[0] = 1;
        pivotValue[1] = lvl - 1;
        pivotValue[2] = Mathf.Max(0,  2.5f * (lvl - 3));
        pivotValue[3] = Mathf.Max(0, 6 * (lvl - 5));
        pivotValue[4] = Mathf.Max(0, 13.5f * (lvl - 7));
        for (int i = 0; i < 5; i++) sum += pivotValue[i];
        for (int i = 0; i < 5; i++)
        {
            skillbookProbs[i] = pivotValue[i] / sum;
            skillbookTxts[i].text = string.Format("{0:F0}%", skillbookProbs[i] * 100);
        }
    }

    ///<summary> 보상 받기 버튼 </summary>
    public void Btn_GetReward(int idx)
    {
        if(!AdManager.instance.IsLoaded()) return;

        switch (idx)
        {
            case 0:
                AdManager.instance.ShowRewardAd(ResourceReward);
                break;
            case 1:
                AdManager.instance.ShowRewardAd(RecipeReward);
                break;
            case 2:
                AdManager.instance.ShowRewardAd(SkillbookReward);
                break;
        }
    }

    ///<summary> 공통 재화 보상 </summary>
    void ResourceReward(object sender, Reward reward)
    {
        ItemManager.ItemDrop(resourceIdx, resourceCount);
        ShowResult(resourceTxt.text);
    }
    ///<summary> 제작법 수령 </summary>
    void RecipeReward(object sender, Reward reward)
    {
        float rand;
        float prob;
        Rarity rarity;
        do
        {
            rand = Random.Range(0, 1f);
            for (rarity = Rarity.Common, prob = recipeProbs[0]; rarity < Rarity.Legendary && rand > prob; prob += recipeProbs[(int)(rarity++)]);
        }
        while(possbleRecipeList[rarity - Rarity.Common].Count <= 0);

        int idx = Random.Range(0, possbleRecipeList[rarity - Rarity.Common].Count);
        int recipeIdx = possbleRecipeList[rarity - Rarity.Common][idx];

        GameManager.Instance.slotData.itemData.RecipeDrop(recipeIdx);
        possbleRecipeList[rarity - Rarity.Common].RemoveAt(idx);
        GameManager.Instance.SaveSlotData();
        RecipeCountCheck();

        string resultTxt = GameManager.Instance.slotData.region == 10 ? "설계도 : " : "비법서 : ";
        ShowResult(resultTxt + ItemManager.GetEBP(recipeIdx).name);
    }
    void SkillbookReward(object sender, Reward reward)
    {
        float rand = Random.Range(0, 1f);
        float prob = skillbookProbs[0];
        int lvl;
        for (lvl = 1; lvl < 9 && rand > prob; prob += skillbookProbs[(lvl += 2) / 2]) ;

        int skillIdx = ItemManager.AdSkillbookDrop(lvl);

        string resultTxt = GameManager.Instance.slotData.region == 10 ? "교본 : " : "마법서 : ";
        ShowResult(resultTxt + SkillManager.GetSkill(GameManager.SlotClass, skillIdx).name);
    }

    void ShowResult(string resultName)
    {
        resultTxt.text = $"{resultName}(을)를 보급받았습니다.";
        resultPanel.SetActive(true);
    }
}
