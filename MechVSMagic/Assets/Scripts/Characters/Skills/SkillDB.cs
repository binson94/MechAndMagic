using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillDB : MonoBehaviour
{
    public string className;        //관련 클래스 이름
    public int classIdx;            //관련 클래스 인덱스 ex) 암드파이터 = 1

    protected int skillCount;       //스킬 갯수
    public Skill[] skills = null;

    public virtual void DataLoad()
    {

    }
}
