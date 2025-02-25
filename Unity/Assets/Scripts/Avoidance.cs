using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Avoidance : MonoBehaviour
{
    readonly List<NavMeshAgent> agents = new List<NavMeshAgent>();

    [Tooltip("Agents will ignore others with distance greater than this value. Bigger value can decrease performance.")]
    [SerializeField, Range(0.1f, 100f)] float maxAvoidanceDistance = 3f;
    [Tooltip("Speed of agents \"pushing\" from each other in m/s. Increase to make avoidance more noticeable. Default is 1.")]
    [SerializeField, Range(0f, 300f)] float strength = 1;
    [Tooltip("Agents will try to keep this distance between them. Something like NavMeshAgent.Radius, but same for all agents. Do not make this value bigger than Max Avoidance Distance.")]
    [SerializeField, Range(0.1f, 100f)] float distance = 1;
    [SerializeField] bool showDebugGizmos = true;
    
    public float Distance => distance;

    float sqrMaxAvoidanceDistance;

    void Awake()
    {
        sqrMaxAvoidanceDistance = Mathf.Pow(maxAvoidanceDistance, 2);
    }

    void Update() => CalcualteAvoidance();

    void CalcualteAvoidance()
    {
    }

    public void AddAgent(NavMeshAgent agent) => agents.Add(agent);
    public void RemoveAgent(NavMeshAgent agent) => agents.Remove(agent);
    
    void OnDrawGizmos()
    {
        if (showDebugGizmos)
            for (var i = 0; i < agents.Count; i++)
                Gizmos.DrawRay(agents[i].destination, Vector3.up);
    }
}
