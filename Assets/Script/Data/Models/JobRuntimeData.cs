using UnityEngine;

public class JobRuntimeData
{
    public JobType jobType;
    public string jobName;
    public string description;
    public string iconPath;

    [System.NonSerialized] public Sprite icon;
}