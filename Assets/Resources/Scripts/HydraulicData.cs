using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HydraulicData", menuName = "Hydraulic/HydraulicData")]
public class HydraulicData : ScriptableObject
{
    [SerializeField]
    private int dropletCount;
    [SerializeField][Tooltip("The maximum number of steps a droplet may take before terminating")]
    private int maxSteps;
    [SerializeField][Tooltip("Higher values of g will result in faster droplets and erosion. No visual difference")]
    private float gravity = 9.8f;

    [Space]

    [SerializeField][Range(0f, 1f)][Tooltip("The initial intertia given to each droplet. Higher values will send droplets farther from their origins")]
    private float intertia;
    [SerializeField][Tooltip("Determines the amount of sediment a droplet can carry")]
    private float carryCapacity;
    [SerializeField][Range(0f, 1f)][Tooltip("Limits the amount of sediment dropped if the carry capacity of a droplet is exceeded")]
    private float depositionRate;
    [SerializeField][Range(0f, 1f)][Tooltip("Determines how much free capacity of a droplet is filled with sediment when eroding")]
    private float erosionRate;
    [SerializeField][Range(0f, 1f)][Tooltip("Determines how fast droplets evaporate")]
    private float evaporationRate;
    [SerializeField][Tooltip("Determines the radius in which sediment is taken around the droplet when eroding")]
    private int erosionRadius;
    [SerializeField][Tooltip("The minimum height difference required for the carry capacity of a droplet to be computed")]
    private float minSlope;

    public int DropletCount { get { return dropletCount; } }
    public int MaxSteps { get { return maxSteps; } }
    public float Gravity { get { return gravity; } }
    public float Intertia { get { return intertia; } }
    public float CarryCapacity { get { return carryCapacity; } }
    public float DepositionRate { get { return depositionRate; } }
    public float ErosionRate { get { return erosionRate; } }
    public float EvaporationRate { get { return evaporationRate; } }
    public int ErosionRadius { get { return erosionRadius; } }
    public float MinSlope { get { return minSlope; } }
}