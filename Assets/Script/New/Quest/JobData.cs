using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif

public enum JobType { chef, fighter, explorer, technician }

[CreateAssetMenu(fileName = "NewJob", menuName = "Jobs/JobData")]
public class JobData : ScriptableObject
{
    public JobType jobType;
    public string jobName;
    public Sprite jobIcon;
    public string description;
}
