using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

public enum JobType { chef, fighter, explorer, technician }

[CreateAssetMenu(menuName = "Data/JobData")]
public class JobData : ScriptableObject
{
    public JobType jobType;
    public string jobName;
    public Sprite jobIcon;
    public string description;
}
