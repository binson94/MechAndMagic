using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;

    public static SoundManager sound = null;

    [Header("Play Data")]
    //0 : 기계 슬롯, 1 : 마법 슬롯
    public static int slotNumber;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            sound = transform.GetChild(0).GetComponent<SoundManager>();
            Screen.SetResolution(1080, 1920, true);
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
