﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PedestrianBehavior : MonoBehaviour
{
    public enum PedestrianState
    {
        kPedestrianState_Searching,
        kPedestrianState_Walking,
        kPedestrianState_Waiting,
        kPedestrianState_Dead
    }

    bool StartAsLeader = false;
    public float MovementSpeed = 1.0f;

    bool mIsLeader;
    bool mInitialized = false;
    GameObject mLeader;
    List<GameObject> mNeighbours;

    List<Vector3> mPath = new List<Vector3>();
    Vector3 mNextLocation;
    float mMinDistance = 0.04f;

    PedestrianState mState;
    Rigidbody RigidBody;

    public Vector3 Velocity;

    private bool mCollided = false;

    private float mReSpawnTimer = 0.0f;

    void Start()
    {
        
    }

    // Call this function whenever the game is ready, to start updating the GameObject
    public void Init()
    {
        mState = PedestrianState.kPedestrianState_Searching;
        mLeader = null;
        mNeighbours = new List<GameObject>();
        RigidBody = gameObject.GetComponent<Rigidbody>();
        Velocity = transform.forward;

        ConvertToLeader(StartAsLeader);

        //Begin updating the GameObject
        mInitialized = true;
    }

    Vector3 off = new Vector3(0.0f, 1.5f, 0.0f);
    void Update()
    {
        if(mInitialized && mIsLeader)
        {
            Debug.DrawLine(transform.position, transform.position + new Vector3(0.0f, 5.0f, 0.0f),Color.red);
            foreach(var n in mNeighbours)
            {
                Debug.DrawLine(transform.position + off, n.transform.position + off, Color.green);
            }
        }

        if (mInitialized)
        {
            switch (mState)
            {
                case PedestrianState.kPedestrianState_Searching:
                    Searching();
                    break;

                case PedestrianState.kPedestrianState_Walking:
                    Walking();
                    break;

                case PedestrianState.kPedestrianState_Waiting:
                    Waiting();
                    break;

                case PedestrianState.kPedestrianState_Dead:
                    Dead();
                    break;

                default:
                    break;
            }
        }
    }

    void Dead()
    {
        mReSpawnTimer += Time.deltaTime;
        if (mReSpawnTimer >= 1.5f)
        {
            var crowd = GameObject.FindObjectOfType<CrowdController>();
            if (!crowd)
            {
                Debug.LogWarning("Could not find a crowd controller");
                return;
            }
            crowd.RemovePedestrian(this);

            Destroy(this.gameObject);
        }
    }

    void Searching()
    {
        //Start pathfinding (get nearest point and get a random point)
        GetNewPath();
        if(GetNextLocationStep())
            mState = PedestrianState.kPedestrianState_Walking;
    }

    void Walking()
    {
        //If the agent is affected by flocking
        if (GetLeader())
        {
            transform.forward = Velocity.normalized;
            RigidBody.MovePosition(transform.position + (Velocity * Time.deltaTime * MovementSpeed / 3.0f));
            
            //Check if the distance to the final location has increased
        }
        else
        {
            if (CheckCrosswalk())
                mState = PedestrianState.kPedestrianState_Waiting;
            else if (CheckCar())
                mState = PedestrianState.kPedestrianState_Waiting;
            else
                UpdatePathfindingMovement();    
        }
    }

    bool CheckCrosswalk()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 1.0f))
        {
            if (hit.collider.gameObject.tag == "CrossWalk")
            {
                CrossWalkBehaviour crosswalk = hit.collider.gameObject.GetComponent<CrossWalkBehaviour>();
                if (crosswalk.GetCrossWalkStates == CrossWalkBehaviour.CrossWalkStates.kCrossWalkStates_RedLight)
                {
                    crosswalk.SetIsPedestrianWaiting(true);
                    return true;
                }
            }
        }
        return false;
    }

    bool CheckCar()
    {
        Debug.DrawLine(transform.position + new Vector3(0.0f, 1.0f, 0.0f), transform.position + new Vector3(0.0f, 1.0f, 0.0f) + transform.TransformDirection(Vector3.forward) * 2.0f, Color.red);

        RaycastHit hit;
        if (Physics.Raycast(transform.position + new Vector3(0.0f, 1.0f, 0.0f), transform.TransformDirection(Vector3.forward), out hit, 2.0f))
        {
            if (hit.collider.gameObject.tag == "Car")
                return true;
        }

        return false;
    }

    void Waiting()
    {
        if (!CheckCrosswalk())
            mState = PedestrianState.kPedestrianState_Walking;
        else if (!CheckCar())
            mState = PedestrianState.kPedestrianState_Walking;
    }

    bool IsLeader() { return mIsLeader; }

    public GameObject GetLeader() { return mLeader; }

    public void SetLeader(GameObject leader) { mLeader = leader; }

    void ConvertToLeader(bool leader)
    {
        mLeader = null;
        mIsLeader = leader;
        mNeighbours.Clear();

        //Add yourself to the flocking group (for computing)
        if (mIsLeader)
            mNeighbours.Add(gameObject);
    }

    public List<GameObject> GetNeighbours()
    {
        if (IsLeader())
            return mNeighbours;

        else if (GetLeader() != null)
        {
            PedestrianBehavior leader = GetLeader().GetComponent<PedestrianBehavior>();
            return leader.GetNeighbours();
        }

        return null;
    }

    public void AddNeighbour(GameObject agent)
    {
        PedestrianBehavior agentBehavior = agent.GetComponent<PedestrianBehavior>();
        if (agentBehavior.mState == PedestrianState.kPedestrianState_Dead) return;

        if (agentBehavior.GetLeader() == null && !agentBehavior.IsLeader())
        {
            if (IsLeader() && !mNeighbours.Contains(agent))
            {
                agentBehavior.SetLeader(gameObject);
                mNeighbours.Add(agent);
            }

            //Autogenerate leader of the flocking group
            else if (!IsLeader() && GetLeader() == null)
            {
                ConvertToLeader(true);
                agentBehavior.ConvertToLeader(false);
                agentBehavior.SetLeader(gameObject);
                mNeighbours.Add(agent);
            }
        }
    }

    public void RemoveNeighbour(GameObject agent)
    {
        PedestrianBehavior agentBehavior = agent.GetComponent<PedestrianBehavior>();
        if (IsLeader() && mNeighbours.Contains(agent))
        {
            mNeighbours.Remove(agent);
            agentBehavior.RemoveLeader();

            //If the leader has no neighbours
            if (mNeighbours.Count <= 1)
                ConvertToLeader(false);
        }
        else if (mLeader != null)
        {
            if(mLeader == agent && agentBehavior.IsLeader())
                RemoveLeader();
        }
    }

    public void RemoveLeader()
    {
        //Debug.Log("Remove Leader!");
        mLeader.GetComponent<PedestrianBehavior>().RemoveNeighbour(this.gameObject);
        mLeader = null;
        mNeighbours.Clear();
        mState = PedestrianState.kPedestrianState_Searching;
        /*
        Debug.Log("--------- SEARCHING -------" + gameObject.name);
        GetNewPath();
        if (GetNextLocationStep())
            mState = PedestrianState.kPedestrianState_Walking;

        foreach (var node in mPath)
            Debug.Log("Path: " + node);
        Debug.Log("--------- END SEARCHING -------");
        */
    }

    void MoveToMouseClick()
    {
        //RigidBody.velocity = Velocity * MovementSpeed;
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                Velocity = hit.point - gameObject.transform.position;
                Velocity.Normalize();
            }
        }
    }

    void GetNewPath()
    {
        Vector3 start = AStarSearch.GetNearestWaypoint(WaypointsExample.PedestriansGraph, transform.position);
        //Debug.Log("Nearest waypoint: " + start + " Position: " + transform.position);
        Vector3 end = AStarSearch.GetRandomWaypoint(WaypointsExample.PedestriansGraph);
        mPath = AStarSearch.FindNewObjective(WaypointsExample.PedestriansGraph, start, end);
        mPath.Insert(0, start);
    }

    //Return whenever there is no more seps
    bool GetNextLocationStep()
    {
        if (mPath.Count > 0)
        {
            mNextLocation = mPath[mPath.Count - 1];
            mNextLocation.y = 0.0f;
            mPath.RemoveAt(mPath.Count - 1);

            return true;
        }
        return false;
    }

    void UpdatePathfindingMovement()
    {
        this.GetComponent<Rigidbody>().velocity = Vector3.zero;

        if (Vector3.Distance(transform.position, mNextLocation) > mMinDistance)
        {
            transform.forward = (mNextLocation - transform.position).normalized;
            transform.position = Vector3.MoveTowards(transform.position, mNextLocation, Time.deltaTime * MovementSpeed);
        }
        else
        {
            transform.position = mNextLocation;
            if (!GetNextLocationStep())
                mState = PedestrianState.kPedestrianState_Searching;
        }
    }

    public PedestrianState GetPedestrianState
    {
        get { return mState; }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.tag == "Car" && !mCollided)
        {
            if(mLeader)
                mLeader.GetComponent<PedestrianBehavior>().RemoveNeighbour(this.gameObject);

            mCollided = true;
            mState = PedestrianState.kPedestrianState_Dead;
            GetComponent<RagdollController>().EnableRagdoll();
            GetComponent<Animator>().enabled = false;
        }
    }
}
