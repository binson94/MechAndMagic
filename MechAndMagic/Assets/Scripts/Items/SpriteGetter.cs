using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteGetter : MonoBehaviour
{
    public static SpriteGetter instance;

    private void Awake() {
        instance = this;
    }

    ///<summary> 클래스-레벨 순 정렬 </summary>
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
    [SerializeField] Sprite[] recipeSprites;

    ///<summary> 포션 스프라이트 </summary>
    [SerializeField] Sprite[] potionSprites;
    ///<summary> 스킬 스프라이트 </summary>
    [SerializeField] Sprite[] skillSprites;

    ///<summary> 장비 아이콘 반환 </summary>
    public Sprite GetEquipIcon(Equipment e)
    { 
        if(e.ebp.part <= EquipPart.Weapon)
            return weaponSprites[(e.ebp.useClass - 1) * 5 + (int)e.ebp.reqlvl - 1];
        else if (e.ebp.part <= EquipPart.Shoes)
            return armorSprites[(e.ebp.useClass / 11 * 4) + (e.ebp.part - EquipPart.Top)];
        else
            return accessorySprites[((int)e.ebp.part / 7 * 5) + (int)e.ebp.reqlvl - 1];
    }
    ///<summary> 아이템 그리드 반환 </summary>
    public Sprite GetGrid(Rarity rarity) => gridSprites[rarity - Rarity.Common];
    ///<summary> 포션 아이콘 반환 </summary>
    public Sprite GetPotionIcon(int potionIdx) => potionSprites[Mathf.Max(0, potionIdx - 1)];
    ///<summary> 스킬 아이콘 반환 </summary>
    public Sprite GetSkillIcon(int iconIdx) => skillSprites[iconIdx - 1];

    public Sprite GetRecipeIcon() => recipeSprites[GameManager.instance.slotData.region / 11];
    //public Sprite GetResourceIcon(int resourceIdx)
    //{
    //    
    //    if(resourceIdx <= 6)
    //}
}
