using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteGetter : MonoBehaviour
{
    public static SpriteGetter instance = null;

    private void Awake() {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    ///<summary> 클래스-레벨 순 정렬 </summary>
    [Header("Equipment")]
    [Tooltip("클래스-레벨 순")]
    [SerializeField] Sprite[] weaponSprites;
    ///<summary> 진영-부위 순 정렬 </summary>
    [Tooltip("진영-부위 순")]
    [SerializeField] Sprite[] armorSprites;
    ///<summary> 부위-레벨 순 정렬 </summary>
    [Tooltip("부위-레벨 순")]
    [SerializeField] Sprite[] accessorySprites;
    ///<summary> 장비 그리드 스프라이트 </summary>
    [SerializeField] Sprite[] gridSprites;
    ///<summary> 장비 세트 스프라이트 </summary>
    [SerializeField] Sprite[] setSprites;

    ///<summary> 제작법 스프라이트(0 기계, 1 마법) </summary>
    [Header("Recipe")]
    [SerializeField] Sprite[] recipeSprites;

    ///<summary> 포션 스프라이트 </summary>
    [Header("Potion")]
    [SerializeField] Sprite[] potionSprites;

    ///<summary> 스킬 스프라이트 </summary>
    [Header("Skill")]
    [SerializeField] Sprite[] skillSprites;
    ///<summary> 버프 아이콘 스프라이트 </summary>
    [SerializeField] Sprite[] buffIconSprites;
    ///<summary> 버프, 디버프 배경 스프라이트 </summary>
    [SerializeField] Sprite[] buffBGSprites;

    ///<summary> 6개, 진영 - 상중하 순 </summary>
    [Header("Resource")]
    [Tooltip("6개, 진영 - 상중하 순")]
    [SerializeField] Sprite[] skillResourceSprites;
    ///<summary> 24개, 직업 - 상중하 순 </summary>
    [Tooltip("24개, 직업 - 상중하 순")]
    [SerializeField] Sprite[] weaponResourceSprites;
    ///<summary> 6개, 진영 - 상중하 순 </summary>
    [Tooltip("6개, 진영 - 상중하 순")]
    [SerializeField] Sprite[] armorResourceSprites;
    ///<summary> 3개, 상중하 순 </summary>
    [Tooltip("3개, 상중하 순")]
    [SerializeField] Sprite[] accessoryResourceSprites;
    ///<summary> 6개, 진영 - 상중하 순 </summary>
    [Tooltip("6개, 진영 - 상중하 순")]
    [SerializeField] Sprite[] commonResourceSprites;

    ///<summary> 장비 아이콘 반환 </summary>
    public Sprite GetEquipIcon(EquipBluePrint ebp)
    {
        if(ebp == null) return null;

        if(ebp.part <= EquipPart.Weapon)
            return weaponSprites[(ebp.useClass - 1) * 5 + (int)ebp.reqlvl / 2];
        else if (ebp.part <= EquipPart.Shoes)
            return armorSprites[(GameManager.Instance.slotData.region / 11 * 20) + (ebp.part - EquipPart.Top) * 5 + ebp.reqlvl / 2];
        else
            return accessorySprites[((int)ebp.part / 7 * 5) + (int)ebp.reqlvl / 2];
    }
    ///<summary> 아이템 그리드 반환 </summary>
    public Sprite GetGrid(Rarity rarity) => gridSprites[rarity - Rarity.Common];
    ///<summary> 장비 세트 아이콘 반환 </summary>
    public Sprite GetSetIcon(int setIdx) => setSprites[Mathf.Max(0, setIdx - 1)];
    ///<summary> 포션 아이콘 반환 </summary>
    public Sprite GetPotionIcon(int potionIdx) => potionSprites[Mathf.Max(0, potionIdx - 1)];
    ///<summary> 레시피 아이콘 반환 </summary>
    public Sprite GetRecipeIcon() => recipeSprites[GameManager.Instance.slotData.region / 11];
    ///<summary> 자원 아이콘 반환 
    ///<para> 1 ~ 3 : 스킬 재화(상중하) </para>
    ///<para> 4 ~ 6 : 무기 재화(상중하) </para>
    ///<para> 7 ~ 9 : 방어구 재화(상중하) </para>
    ///<para> 10 ~ 12 : 악세서리 재화(상중하) </para>
    ///<para> 13~15 : 아이템 공통 재화 </para> </summary>
    public Sprite GetResourceIcon(int resourceIdx)
    {
        int pivot = (resourceIdx - 1) % 3;
        if(resourceIdx <= 3)
            return skillResourceSprites[GameManager.Instance.slotData.region / 11 * 3 + pivot];
        if(resourceIdx <= 6)
            return weaponResourceSprites[(GameManager.SlotClass - 1) * 3 + pivot];
        else if(resourceIdx <= 9)
            return armorResourceSprites[GameManager.Instance.slotData.region / 11 * 3 + pivot];
        else if(resourceIdx <= 12)
            return accessoryResourceSprites[pivot];
        else
            return commonResourceSprites[GameManager.Instance.slotData.region / 11 * 3 + pivot];
    }

    ///<summary> 스킬 아이콘 반환 </summary>
    public Sprite GetSkillIcon(int iconIdx) => skillSprites[iconIdx - 1];
    public Sprite GetBuffIcon(Obj obj) => buffIconSprites[(int)obj - 1];
    public Sprite GetBuffBG(bool isBuff) => buffBGSprites[isBuff ? 0 : 1];
}