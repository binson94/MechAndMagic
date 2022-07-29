using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionPanel : MonoBehaviour
{
    [Header("Option")]
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider txtSpdSlider;

    private void Start() {
        bgmSlider.value = (float)SoundManager.Instance.option.bgm;
        sfxSlider.value = (float)SoundManager.Instance.option.sfx;
        txtSpdSlider.value = SoundManager.Instance.option.txtSpd / 2f;
    }
    public void Slider_BGM() => SoundManager.Instance.BGMSet(bgmSlider.value);
    public void Slider_SFX() => SoundManager.Instance.SFXSet(sfxSlider.value);
    public void Slider_TxtSpd()
    {
        txtSpdSlider.value = Mathf.RoundToInt(txtSpdSlider.value * 2) / 2f;
        SoundManager.Instance.TxtSet(txtSpdSlider.value);
    }
}
