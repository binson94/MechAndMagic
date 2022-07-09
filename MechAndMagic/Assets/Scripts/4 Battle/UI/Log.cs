using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Log : MonoBehaviour
{
    [SerializeField] Text text;
    
    public void Set(string str)
    {
        text.text = str;
    }
}
