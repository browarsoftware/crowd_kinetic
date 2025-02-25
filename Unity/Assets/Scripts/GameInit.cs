// https://github.com/OlegDzhuraev/NavMeshAvoidance
// https://ped.fz-juelich.de/database/doku.php
// https://ped.fz-juelich.de/db/doku.php?id=bottleneck2
// DRAG
//https://docs.unity3d.com/2019.2/Documentation/ScriptReference/Rigidbody-drag.html
//
// Fundamental Diagram
// https://arxiv.org/html/2409.11857v1
//
// https://discussions.unity.com/t/hide-show-properties-dynamically-in-inspector/32760/9

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

using UnityEditor;
using NaughtyAttributes;
using System.Globalization;
using System.IO;
using System.Text;

public class RandomGaussian
{
    System.Random rand = new System.Random();
    public RandomGaussian()
    {
    }
    public RandomGaussian(System.Random rand)
    {
        this.rand = rand;
    }
    //Box-Muller transform
    public double Next(double mean, double stdDev)
    {
        double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
        double u2 = 1.0 - rand.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                     Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
        double randNormal =
                     mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
        return randNormal;
    }
}

public class GameInit : MonoBehaviour
{
    // https://github.com/dbrizov/NaughtyAttributes
    // https://discussions.unity.com/t/editing-a-list-in-inspector/145197

    //[ReorderableList]
    //public List<GameObject> agentModels = new List<GameObject>();

    public List<AgentGroup> agentsGroups;// = new List<DamagerLevel>();
    //public AgentGroup[] TestWithArray = new AgentGroup[5];

    public class TimePosition
    {
        public double time = 0;
        public Vector3 position;

        public TimePosition() { }

        public TimePosition(double time, float x, float y, float z)
        {
            this.time = time;
            this.position.x = x;
            this.position.y = y;
            this.position.z = z;
        }

    }

    public class AgentTimePosition
    {
        public int id = 0;
        public List<TimePosition> Positions = new List<TimePosition>();
        public int actualPosition = 0;
        public bool agentInitialized = false;
    }

    public List<RayWeight> RayWeights;

    [System.Serializable]
    public class RayWeight
    {
        [SerializeField, Range(0f, 1f)] public float agentRay = 1;
        [SerializeField, Range(0f, 1f)] public float wallRay = 1;
    }

    [System.Serializable]
    public class AgentGroup
    {
        [HideInInspector]
        public int groupId = 0;
        [ShowAssetPreview(32, 32)]
        public GameObject agentModel;
        public int agentsCount = 1;
        public float mass = 60;
        public float agentWeightStandardDeviation = 0;
        public float maxVelocity = 3f;
        public float maxVelocityStandardDeviation = 0;
        public float radius = 0.25f;
        public float radiusStandardDeviation = 0;
        public float height = 1.7f;
        public float heightStandardDeviation = 0;
        public object agentStartInitializer;
        public bool destroyIfTargetReached = false;
        public float distanceWhenDestroy = 1;
        public float angularDrag = 0.05f;
        [ShowAssetPreview(32, 32)]
        public GameObject start;
        [ShowAssetPreview(32, 32)]
        public GameObject target;
        public Vector3 targetStandardDeviation = Vector3.zero;
        public TextAsset textData = null;
        public double fpsTextData = 25;
        [HideInInspector]
        public List<AgentTimePosition> agentTimePosition = null;
    }


    public bool commonAgentsGroupsParametrs = false;

    //public GameObject red = null;
    //public GameObject blue = null;
    //public UnityEngine.Vector3 positionRed =  UnityEngine.Vector3.zero;
    //public UnityEngine.Vector3 positionBlue =  UnityEngine.Vector3.zero;
    //public GameObject goalRed = null;
    //public GameObject goalBlue = null;
    //public int agentsCount = 1;
    //[SerializeField]
    public bool useAvoidence = true;
    public bool waitUnitilPathsCalculated = true;

    public bool saveSimulatedData = true;
    [SerializeField, Range(0, 60)] int saveEveryXSeconds = 0;
    DateTime startTimeSaveEveryXSeconds = DateTime.Now;
    [EnableIf("saveSimulatedData")]
    public string simulatedDataPath = "c:/data/crowd/file";
    private int maxAgentId = 0;

    int counter;
    Avoidance avoidance;

    /// <summary>
    /// /////////////////////////////////////
    /// </summary>
    public bool experiment = true;
    int experiment_removed_count = 0;
    int experiment_total_count = 0;
    List<double[]> ExperimentsSetup = new List<double[]>();
    int ExperimentsSetupId = 0;
    public void updateCounter()
    {
        lock(this)
        {
            counter++;
        }
    }
    /*
    public String aa
    {
        get;
        set;
    }
    private int a;
    public int ABC
    {
        get => a;
        set => a = value;
    }*/
    private List<AIControl> all_agents = new List<AIControl>();
    //bool firstRun = true;
    /*
    // Start is called before the first frame update
    public void AddToAvoidance(NavMeshAgent agent)
    {
        avoidance.AddAgent(agent);
    }*/
    readonly List<NavMeshAgent> agents = new List<NavMeshAgent>();

    [Tooltip("Agents will ignore others with distance greater than this value. Bigger value can decrease performance.")]
    [SerializeField, Range(0.1f, 100f)] float maxAvoidanceDistance = 3f;
    [Tooltip("Speed of agents \"pushing\" from each other in m/s. Increase to make avoidance more noticeable. Default is 1.")]
    [SerializeField, Range(0f, 5000f)] float strength = 1;
    //[Tooltip("Agents will try to keep this distance between them. Something like NavMeshAgent.Radius, but same for all agents. Do not make this value bigger than Max Avoidance Distance.")]
    //[SerializeField, Range(0.1f, 100f)] float distance = 1;
    [SerializeField] bool showDebugGizmos = true;

    [Tooltip("Agents will ignore others with distance greater than this value. Bigger value can decrease performance.")]
    [SerializeField, Range(0.1f, 100f)] float wallMaxAvoidanceDistance = 3f;
    [Tooltip("Speed of agents \"pushing\" from each other in m/s. Increase to make avoidance more noticeable. Default is 1.")]
    [SerializeField, Range(0f, 5000f)] float wallStrength = 1;
    //[Tooltip("Agents will try to keep this distance between them. Something like NavMeshAgent.Radius, but same for all agents. Do not make this value bigger than Max Avoidance Distance.")]
    //[SerializeField, Range(0.1f, 100f)] float wallDistance = 1;

    [SerializeField, Range(0f, 5000f)] public float forwardStrength = 1f;

    // https://docs.unity3d.com/ScriptReference/Time-timeScale.html
    [SerializeField, Range(0.05f, 5f)] float timeScale = 1;
    //public float Distance => distance;

    //float sqrMaxAvoidanceDistance;
    //float wallSqrMaxAvoidanceDistance;

    void Awake()
    {
        //sqrMaxAvoidanceDistance = Mathf.Pow(maxAvoidanceDistance, 2);
        //wallSqrMaxAvoidanceDistance = Mathf.Pow(wallMaxAvoidanceDistance, 2);
    }

    public static List<AgentTimePosition> loadAgentInfo(string fileText, double fps)
    {
        CultureInfo us = new CultureInfo("en-US");
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = us;
        List<AgentTimePosition> allAgentInfo = new List<AgentTimePosition>();
        //var lines = File.ReadAllLines(file_path);
        string[] lines = fileText.Split("\n");
        int actual_id = -1;
        AgentTimePosition ai = null;
        for (var i = 0; i < lines.Length; i += 1)
        {
            var line = lines[i];
            string[] all_fields = line.Split(" ");
            if (all_fields.Length == 4)
            {
                int agent_id = int.Parse(all_fields[0]);
                if (agent_id != actual_id)
                {
                    if (ai != null)
                    {
                        allAgentInfo.Add(ai);
                    }
                    ai = new AgentTimePosition();
                    ai.id = agent_id;
                    actual_id = agent_id;
                }
                ai.Positions.Add(new TimePosition(double.Parse(all_fields[1]) / fps,
                                                float.Parse(all_fields[2]),
                                                0,
                                                float.Parse(all_fields[3])
                ));
            }
        }
        if (ai != null)
        {
            allAgentInfo.Add(ai);
        }

        CultureInfo.CurrentCulture = previousCulture;
        return allAgentInfo;
    }

    private AIControl addAgent(AgentGroup ag, Vector3 rv, bool IsStopped)
    {
        var go = GameObject.Instantiate(ag.agentModel, rv,
                            UnityEngine.Quaternion.identity);
        var MyScript = go.GetComponent<AIControl>();
        //MyScript.avoidance = avoidance;
        MyScript.parentScript = this;
        MyScript.goal = ag.target;
        MyScript.maxVelocity = ag.maxVelocity;
        MyScript.mass = ag.mass;
        MyScript.angularDrag = ag.angularDrag;
        MyScript.sr = updateCounter;
        MyScript.destroyIfTargetReached = ag.destroyIfTargetReached;
        MyScript.distanceWhenDestroy = ag.distanceWhenDestroy;
        MyScript.IsStopped = IsStopped;
        MyScript.agentId = maxAgentId;
        maxAgentId++;
        MyScript.groupId = ag.groupId;
        lock (this)
        {
            all_agents.Add(MyScript);
        }
        return MyScript;
    }

    void Start()
    {
        // https://docs.unity3d.com/ScriptReference/Time-timeScale.html
        //Time.timeScale = 0.1f;
        //avoidance = GetComponent<Avoidance>();
        System.Random rand = new System.Random();
        RandomGaussian randGauss = new RandomGaussian();
        for (int idAgentGroup = 0; idAgentGroup < agentsGroups.Count; idAgentGroup++)
        {
            AgentGroup ag = agentsGroups[idAgentGroup];
            ag.groupId = idAgentGroup;
            if (ag.textData != null)
            {
                List<AgentTimePosition> atp = loadAgentInfo(ag.textData.text, ag.fpsTextData);
                ag.agentTimePosition = atp;
                if (experiment)
                    experiment_total_count += atp.Count;
            }
            else
            {
                for (int a_count = 0; a_count < ag.agentsCount; a_count++)
                {
                    UnityEngine.Vector3 rv = new UnityEngine.Vector3(
                            ag.start.transform.position.x + (float)randGauss.Next(0, ag.targetStandardDeviation.x),
                            ag.start.transform.position.y + (float)randGauss.Next(0, ag.targetStandardDeviation.y),
                            ag.start.transform.position.z + (float)randGauss.Next(0, ag.targetStandardDeviation.z)
                        );
                    addAgent(ag, rv, true);
                }
            }
         }
        if (experiment)
        {
            for (int forwardStrength = 1500; forwardStrength < 2200; forwardStrength += 200)
            {
                for (int maxAvoidanceDistance = 1; maxAvoidanceDistance < 3; maxAvoidanceDistance++)
                    for (int strength = 100; strength < 300; strength += 100)
                        for (int wallMaxAvoidanceDistance = 1; wallMaxAvoidanceDistance < 3; wallMaxAvoidanceDistance++)
                            for (int wallStrength = 100; wallStrength < 300; wallStrength += 100)
                            {
                                double[] v = { forwardStrength, maxAvoidanceDistance, strength, wallMaxAvoidanceDistance, wallStrength };
                                ExperimentsSetup.Add(v);
                            }
            } 
        }
    }


    public void removeAgent(AIControl aic)
    {
        lock (this)
        {
            RemoveAgent(aic.Agent);
            all_agents.Remove(aic);
            if (experiment)
                experiment_removed_count++;
        }
    }

    private void addAgentInUpdate()
    {
        for (int idAgentGroup = 0; idAgentGroup < agentsGroups.Count; idAgentGroup++)
        {
            AgentGroup ag = agentsGroups[idAgentGroup];
            if (ag.textData != null)
            {
                for (int a = 0; a < ag.agentTimePosition.Count; a++)
                {
                    if (!ag.agentTimePosition[a].agentInitialized)
                    {
                        if (ag.agentTimePosition[a].Positions[0].time < timeElapsedFromStart)
                        {
                            ag.agentTimePosition[a].agentInitialized = true;
                            AIControl aic = addAgent(ag, ag.agentTimePosition[a].Positions[0].position / 100.0f, false);
                        }
                    }
                }
            }
        }
    }
    // Update is called once per frame
    bool doneOnce = false;
    double timeElapsedFromStart = 0;

    private void FixedUpdate()
    {
        for (var q = 0; q < all_agents.Count; q++)
        {
            if (all_agents[q].Agent != null)
            {
                all_agents[q]._rigidbody.AddForce(all_agents[q].moveForceVector * Time.fixedDeltaTime, ForceMode.Impulse);
                all_agents[q].moveForceVector = Vector3.zero;
            }
        }
    }

    private void runUpdate()
    {
        lock (this)
        {
            Time.timeScale = timeScale;
            float fixedDeltaTime = Time.fixedDeltaTime;
            Debug.Log("fixedDeltaTime=" + fixedDeltaTime + " " + DateTime.Now.ToString());
            int howMany = all_agents.Where(t => t.hasPath() == false).ToList().Count();
            if (!doneOnce)
                if (howMany > 0)
                {
                    Debug.Log("Agents count=" + all_agents.Count() + "no path=" + howMany.ToString() + " " + DateTime.Now.ToString());
                }
                else
                {
                    all_agents.ForEach(t => t.IsStopped = false);
                    all_agents.ForEach(t => t.Agent.speed = t.maxVelocity);
                    all_agents.ForEach(t => AddAgent(t.Agent));
                    doneOnce = true;
                }
            Debug.Log("Agents count=" + all_agents.Count() + "no path=" + howMany.ToString() + " " + DateTime.Now.ToString());

            if (doneOnce || !waitUnitilPathsCalculated)
            {
                if (useAvoidence)
                {

                    CultureInfo previousCulture = CultureInfo.CurrentCulture;
                    if (saveSimulatedData)
                    {
                        CultureInfo us = new CultureInfo("en-US");
                        CultureInfo.CurrentCulture = us;
                        CalculateForcesAddForce2(true);
                        CultureInfo.CurrentCulture = previousCulture;
                    }
                    else CalculateForcesAddForce2(false);
                }
            }
        }
    }

    void Update()
    {
        timeElapsedFromStart += Time.deltaTime;
        addAgentInUpdate();
        if (experiment)
        {
            double[] vv = ExperimentsSetup[ExperimentsSetupId];
            forwardStrength = (float)vv[0];
            maxAvoidanceDistance = (float)vv[1];
            //distance = maxAvoidanceDistance;
            strength = (float)vv[2];
            wallMaxAvoidanceDistance = (float)vv[3];
            //wallDistance = wallMaxAvoidanceDistance;
            wallStrength = (float)vv[4];


            runUpdate();
            //restertExperiment
            if (experiment_removed_count > 0 && experiment_removed_count == experiment_total_count)
            {
                experiment_removed_count = 0;
                //experiment_total_count = 0;
                timeElapsedFromStart = 0;
                //doneOnce = false;
                simulatedDataPathNow = null;
                ////
                for (int idAgentGroup = 0; idAgentGroup < agentsGroups.Count; idAgentGroup++)
                {
                    AgentGroup ag = agentsGroups[idAgentGroup];
                    if (ag.textData != null)
                    {
                        for (int a = 0; a < ag.agentTimePosition.Count; a++)
                            ag.agentTimePosition[a].agentInitialized = false;
                    }
                }
                ExperimentsSetupId++;
                if (ExperimentsSetupId >= ExperimentsSetup.Count)
                    Application.Quit();

                //restertExperiment
            }
        }
        else
        {
            runUpdate();
        }
    }


    // https://mathworld.wolfram.com/Circle-CircleIntersection.html
    private float circlesIntersection(float r, float R, float d)
    {
        float 
        result = ((r * r) * Mathf.Acos(((d * d) + (r * r) - (R * R)) / (2 * d * r)))
            + ((R * R) * Mathf.Acos(((d * d) + (R * R) - (r * r)) / (2 * d * R)))
            - (0.5f * Mathf.Sqrt((-d + r + R) * (d + r - R) * (d - r + R) * (d + r + R)));
        return result;
    }

    String saveToFileBuffer = "";
    StringBuilder fb = new StringBuilder();
    private void writeSimulationToFile(String dataToWrite, String pathToFile)
    {
        /*
        if (!File.Exists(pathToFile))
        {
            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(pathToFile))
            {
                //saveToFileBuffer += generateHeader();
                //saveToFileBuffer += dataToWrite;
                sw.WriteLine(generateHeader());
                sw.WriteLine(dataToWrite);
            }
        }
        else
        {*/
        // This text is always added, making the file longer over time
        // if it is not deleted.
        fb.Append(dataToWrite + "\n");
        //saveToFileBuffer += dataToWrite + "\n";
        
        if ((DateTime.Now - startTimeSaveEveryXSeconds).TotalSeconds > saveEveryXSeconds)
        {
            startTimeSaveEveryXSeconds = DateTime.Now;
            using (StreamWriter sw = File.AppendText(pathToFile))
            {
                sw.Write(fb.ToString());
                Debug.Log("Saved=" + DateTime.Now);
            }
            fb.Clear();
        }
        //}
    }

    private string generateHeader()
    {
        return "timeElapsedFromStart,groupId,agentId,position_x,position_y,position_z,"
            + "velocity_x,velocity_y,velocity_z,velocity_magnitude";//,denisty";
    }

    //https://mathworld.wolfram.com/Circle-CircleIntersection.html

    private float r_v_1 = Mathf.Sqrt(4 / Mathf.PI);
    private string simulatedDataPathNow = null;
    void CalculateForcesAddForce2(bool saveSimulation)
    {
        //var agentsTotal = agents.Count;
        //var deltaTime = Time.deltaTime * timeScale;
        for (var q = 0; q < all_agents.Count; q++)
        {
            if (all_agents[q].Agent != null)
            {
                string dataToWrite = "";
                //float density = 0;

                var agentAPos = all_agents[q].Agent.transform.position;
                var agentDirection = all_agents[q].Agent.transform.forward;
                // front
                //if (showDebugGizmos)
                //    Debug.DrawRay(agentAPos, agentDirection.normalized, Color.cyan);

                var wallAvoidanceVector = Vector3.zero;
                var avoidanceVector = Vector3.zero;

                //For eight rays
                //for (int a = 0; a < 8; a++)
                if (RayWeights.Count > 0)
                {
                    float angle = 360f / RayWeights.Count;
                    for (int a = 0; a < RayWeights.Count; a++)
                    {
                        //Vector3 v = UnityEngine.Quaternion.AngleAxis(45 * a, Vector3.up) * agentDirection;
                        Vector3 v = UnityEngine.Quaternion.AngleAxis(angle * a, Vector3.up) * agentDirection;
                        RaycastHit hit;

                        bool isHit = Physics.SphereCast(agentAPos, 0.05f, v, out hit, Mathf.Max(maxAvoidanceDistance, wallMaxAvoidanceDistance));
                        // wall
                        if (isHit && hit.transform.CompareTag("Wall"))
                        {
                            var direction = agentAPos - hit.point;
                            var sqrDistance = direction.sqrMagnitude;
                            var weakness = sqrDistance / wallMaxAvoidanceDistance;// wallSqrMaxAvoidanceDistance;

                            //if (showDebugGizmos)
                            //    Debug.DrawRay(agentAPos, direction.normalized, Color.red);
                            direction.y = 0; // i do not sure we need to use Y coord in navmesh directions, so ignoring it
                            //avoidanceVector += Vector3.Lerp(direction * strength, Vector3.zero, weakness);
                            if (weakness <= 1f)
                            {
                                direction.y = 0; // i do not sure we need to use Y coord in navmesh directions, so ignoring it
                                var scalledValue = Vector3.Lerp(direction * wallStrength, Vector3.zero, weakness);
                                scalledValue *= RayWeights[a].wallRay;
                                wallAvoidanceVector += scalledValue;
                                //wallAvoidanceVector += Vector3.Lerp(direction * wallStrength, Vector3.zero, weakness);
                                // wall
                                if (showDebugGizmos)
                                    Debug.DrawRay(agentAPos, direction.normalized, Color.red);
                            }
                        }
                        if (isHit && hit.transform.CompareTag("Agent"))
                        {
                            var direction = agentAPos - hit.transform.position;
                            var sqrDistance = direction.sqrMagnitude;
                            var weakness = sqrDistance / maxAvoidanceDistance;// sqrMaxAvoidanceDistance;


                            //Debug.DrawRay(agentAPos, direction * 2.0f, Color.red);
                            if (weakness <= 1f)
                            {
                                direction.y = 0; // i do not sure we need to use Y coord in navmesh directions, so ignoring it
                                var scalledValue = Vector3.Lerp(direction * strength, Vector3.zero, weakness);
                                scalledValue *= RayWeights[a].agentRay;
                                //tu rotation
                                //scalledValue = UnityEngine.Quaternion.AngleAxis(all_agents[q].additional_rotation, Vector3.up) * scalledValue;

                                //Vector3 v = UnityEngine.Quaternion.AngleAxis(45 * a, Vector3.up) * agentDirection;

                                avoidanceVector += scalledValue;
                                //avoidanceVector += Vector3.Lerp(direction * strength, Vector3.zero, weakness);
                                // agent
                                if (showDebugGizmos)
                                    Debug.DrawRay(agentAPos, direction.normalized, Color.blue);

                            }
                        }
                    }
                }
                //avoidanceVector *= distance;
                //wallAvoidanceVector *= wallDistance;
                avoidanceVector += wallAvoidanceVector;
                if (avoidanceVector.magnitude > 0.001)
                {
                }


                var dir = all_agents[q].Agent.GetComponent<NavMeshAgent>().nextPosition - all_agents[q].Agent.GetComponent<NavMeshAgent>().transform.position;
                avoidanceVector += dir.normalized * forwardStrength;

                //var random = new Vector3(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));
                //avoidanceVector += random;

                //!!
                //all_agents[q]._rigidbody.AddForce(avoidanceVector * Time.deltaTime, ForceMode.Impulse);
                all_agents[q].moveForceVector = avoidanceVector;
                //forceVector

                //all_agents[q]._rigidbody.AddForce(avoidanceVector * Time.fixedDeltaTime, ForceMode.Impulse);
                all_agents[q].Agent.GetComponent<NavMeshAgent>().nextPosition = all_agents[q]._rigidbody.transform.position;

                // final dir
                if (showDebugGizmos)
                    Debug.DrawRay(agentAPos, avoidanceVector.normalized, Color.green);
                if (saveSimulation)
                {
                    if (simulatedDataPathNow == null)
                    {
                        simulatedDataPathNow = simulatedDataPath + "_" + 
                            DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day +
                            "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + 
                            DateTime.Now.Second + ".csv";
                        if (experiment)
                            simulatedDataPathNow = simulatedDataPath + "_" +
                            forwardStrength + "_" + 
                            maxAvoidanceDistance + "_" + strength + "_" +
                            wallMaxAvoidanceDistance + "_" + wallStrength + ".csv";
                        if (!File.Exists(simulatedDataPathNow))
                            using (StreamWriter sw = File.CreateText(simulatedDataPathNow))
                            {
                                sw.WriteLine(generateHeader());
                            }

                    }
                    dataToWrite += "" + timeElapsedFromStart + "," +
                        all_agents[q].groupId + "," +
                        all_agents[q].agentId + "," +
                        all_agents[q]._rigidbody.position.x + "," +
                        all_agents[q]._rigidbody.position.y + "," +
                        all_agents[q]._rigidbody.position.z + "," +
                        all_agents[q]._rigidbody.velocity.x + "," +
                        all_agents[q]._rigidbody.velocity.y + "," +
                        all_agents[q]._rigidbody.velocity.z + "," +
                       all_agents[q]._rigidbody.velocity.magnitude;
                       //density;
                    writeSimulationToFile(dataToWrite, simulatedDataPathNow);
                }
            }
        }
    }
    public void AddAgent(NavMeshAgent agent) => agents.Add(agent);
    public void RemoveAgent(NavMeshAgent agent) => agents.Remove(agent);
    /*
    void OnDrawGizmos()
    {
        if (showDebugGizmos)
            for (var i = 0; i < agents.Count; i++)
                Gizmos.DrawRay(agents[i].destination, Vector3.up);
    }*/
}
