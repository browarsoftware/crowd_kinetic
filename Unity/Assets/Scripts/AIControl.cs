using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class AIControl : MonoBehaviour
{
    public GameObject goal;
    NavMeshAgent agent = null;
    public NavMeshAgent Agent{
        get => agent;
    }
    public GameInit parentScript;
    public bool IsStopped = true;
    public float additional_rotation = 0;
    public delegate void signalRender();
    public signalRender sr;

    public delegate void signalDestroy();
    public signalDestroy sd;

    public Rigidbody _rigidbody = null;
    public CapsuleCollider _capsuleCollider = null;

    public float mass = 50;
    int path_id = 0;
    public bool destroyIfTargetReached = true;
    public float distanceWhenDestroy = 1.0f;

    public float angularDrag = 0.05f;
    public HumanAnimatorScriptController humanAnimatorScriptController = null;
    public bool isDestoyed = false;
    public int groupId = 0;
    public int agentId = 0;
    [HideInInspector]
    public Vector3 moveForceVector = Vector3.zero;
    public bool hasPath()
    {
        if (agent != null)
            return agent.hasPath;
        return false;
    }
    // Start is called before the first frame update
    void Start()
    {
        additional_rotation = Random.Range(-30f, 30f);
        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        agent = this.GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.isStopped = true;
        agent.SetDestination(goal.transform.position);
       
        agent.updatePosition = false;
    }

    // Update is called once per frame
    private Vector3 previousPos = Vector3.zero;
    public float howMuchPass = 5;

    //public Vector3 forceVector = Vector3.zero;
    /*public void SetForcesAddForce(Vector3 dir)
    {
        forceVector = dir;
    }*/
    void Update()
    {
        if (agent.hasPath && !agent.isStopped)
        {
            agent.speed = maxVelocity;
            if (previousPos == Vector3.zero)
            {
                previousPos = transform.position;
            }
            else
            {
                if (howMuchPass > 0)
                {
                    howMuchPass -= Time.deltaTime;
                    if (GetComponent<Rigidbody>().isKinematic)
                    {
                        GetComponent<Rigidbody>().isKinematic = false;
                        agent.ResetPath();
                        agent.SetDestination(goal.transform.position);
                        //agent.ResetPath();
                        //agent.transform.position = transform.position;
                        //agent.SetDestination(goal.transform.position);
                    }
                }
                else
                {
                    howMuchPass = Random.Range(10f, 20f);
                    GetComponent<Rigidbody>().isKinematic = true;
                }
            }
            
            //agent.Move(agent.desiredVelocity);
            if (false)
            {
                agent.Move(agent.desiredVelocity);
            }
            var npnp = agent.nextPosition;
            NavMeshPath nmp = agent.path;
            var dir = GetComponent<NavMeshAgent>().nextPosition - transform.position;
            GetComponent<Rigidbody>().AddForce(dir.normalized * Time.deltaTime, ForceMode.Impulse);
            GetComponent<NavMeshAgent>().nextPosition = transform.position;
            //GetComponent<NavMeshAgent>().wa

            /*
            var path = agent.path.corners[path_id];
            //var dv = agent.desiredVelocity;
            var dv = (path - transform.position).normalized * Time.deltaTime;
            GetComponent<Rigidbody>().AddForce(dv, ForceMode.VelocityChange);
            //agent.velocity = GetComponent<Rigidbody>().velocity;
            */
            //agent.updatePosition = false;
            //GetComponent<Rigidbody>().velocity = agent.desiredVelocity;
            //var dv = agent.desiredVelocity.normalized * Time.deltaTime * 2f;
            //GetComponent<Rigidbody>().AddForce(dv, ForceMode.Impulse);
            //agent.updatePosition = true;
        }
        agent.isStopped = IsStopped;
    }

    public float maxVelocity = 3f;//Replace with your max speed m/s (1 m/s = 3.6 km/h)

    void FixedUpdate()
    {
        _rigidbody.mass = this.mass;
        _rigidbody.angularDrag = this.angularDrag;
        if (_rigidbody.velocity.magnitude > maxVelocity)
        {
            _rigidbody.velocity = _rigidbody.velocity.normalized * maxVelocity;
        }
        if (destroyIfTargetReached)
        {
            if (GetComponent<Collider>().bounds.Intersects(goal.GetComponent<Collider>().bounds))
            {
                lock (this.parentScript)
                {
                    isDestoyed = true;
                    Destroy(this.gameObject);
                    parentScript.RemoveAgent(this.agent);
                    parentScript.removeAgent(this);
                }
            }
            /*
            if ((goal.transform.position - _rigidbody.transform.position).magnitude < distanceWhenDestroy)
            {
                if (parentScript != null)
                {
                    lock (this.parentScript)
                    {
                        isDestoyed = true;
                        Destroy(this.gameObject);
                        parentScript.RemoveAgent(this.agent);
                        parentScript.removeAgent(this);
                    }
                }
            }*/
        }
    }
}
