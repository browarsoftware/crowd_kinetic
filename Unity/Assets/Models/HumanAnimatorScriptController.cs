using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ActionsAnimationInterface
{
    bool isWalking { get; set; }
    bool isRunning { get; set; }
}


public class HumanAnimatorScriptController : MonoBehaviour
{
    public bool isWalking = false;
    public bool isRunning = false;
    public GameObject parent = null;
    Animator animatator;
    public Rigidbody rigidbody = null;
    //ActionsAnimationInterface _ActionsAnimationInterface = null;
    // Start is called before the first frame update
    void Start()
    {
        animatator = GetComponent<Animator>();
        //_ActionsAnimationInterface = (ActionsAnimationInterface)gameObject.GetComponent<AgentController>();
        //_ActionsAnimationInterface = (ActionsAnimationInterface)parent.GetComponent<AgentController>();
        //_ActionsAnimationInterface = parent.GetComponent<ActionsAnimationInterface>();
    }

    // Update is called once per frame
    void Update()
    {
        if (rigidbody != null)
        {
            if (rigidbody.velocity.magnitude < 0.1)
            {
                animatator.SetBool("isWalking", false);
                animatator.SetBool("isRunning", false);
            } else if (rigidbody.velocity.magnitude < 2.5)
            {
                animatator.SetBool("isWalking", true);
                animatator.SetBool("isRunning", false);
            }
            else
            {
                animatator.SetBool("isWalking", false);
                animatator.SetBool("isRunning", true);
            }
        }
        //animatator.SetBool("isWalking", isWalking);
        //animatator.SetBool("isRunning", isRunning);
        /*
        if (_ActionsAnimationInterface == null)
        {
            _ActionsAnimationInterface = (ActionsAnimationInterface)parent.GetComponent<AgentController>();
        }*/
        /*
        if (_ActionsAnimationInterface == null)
        {
            _ActionsAnimationInterface = (ActionsAnimationInterface)gameObject.GetComponent<AgentController>();
            //animatator.SetBool("isWalking", true);
        }
        else*/
        {
            /*bool xxx = _ActionsAnimationInterface.isWalking;
            animatator.SetBool("isWalking", _ActionsAnimationInterface.isWalking);
            animatator.SetBool("isRunning", _ActionsAnimationInterface.isRunning);*/
        }
    }
}
