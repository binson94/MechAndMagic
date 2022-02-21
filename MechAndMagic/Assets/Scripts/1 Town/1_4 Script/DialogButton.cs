using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogButton : MonoBehaviour 
{
    [SerializeField] Text btnTxt;

    public void Set(string s) => btnTxt.text = s;
}