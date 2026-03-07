using System.Collections.Generic;
using UnityEngine;

public class WorkbenchData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public bool isBasic; //전문가인지 비전문가인지 판단
}
