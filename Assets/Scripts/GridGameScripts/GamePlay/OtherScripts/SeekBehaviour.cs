using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class SeekBehaviour : MonoBehaviour
{

    [SerializeField]
    private Vector3 target;
    [SerializeField]
    private Vector3 velocity;
    [SerializeField]
    private float max_speed;
    private Rigidbody body;
    public bool isTemporary;
    public float captureRange;
    public UnityEvent onTargetReached;
    public UnityEvent onMove;
    private bool seekEnabled = true;
    public bool destroyOnCompletion = true;
    public bool snapToDestination;
    public bool SeekEnabled
    {
        get
        {
            return seekEnabled;
        }

        set
        {
            if(value)
            {
                onMove.Invoke();
            }
            seekEnabled = value;
        }
    }

    // Use this for initialization
    void Start()
    {
        body = GetComponent<Rigidbody>();
        
    }
    void seek()
    {

        Vector3 seekforce = target - transform.position;
        seekforce = (seekforce.normalized * max_speed) - velocity;
        if (velocity.magnitude > max_speed)
        {
            velocity = velocity.normalized * max_speed;
        }
        transform.position += seekforce * Time.deltaTime;
    }
    private void TemporarySeek()
    {
        Vector3 seekforce = target - transform.position;
        seekforce = (seekforce.normalized * max_speed) - velocity;
        if (velocity.magnitude > max_speed)
        {
            velocity = velocity.normalized * max_speed;
        }
        transform.position += seekforce * Time.deltaTime;
        float distance = Vector3.Distance(transform.position, target);
        if (distance <= captureRange && isTemporary)
        {
            onTargetReached.Invoke();
            if(destroyOnCompletion)
            {
                Destroy(GetComponent<SeekBehaviour>());
            }
            else
            {
                SeekEnabled = false;
            }
        }
    }
    public void Init(Vector3 targetVal, Vector3 velocityVal, float speedVal, float rangeVal = 0,bool temporary = false, bool destroyOnTemp = true, bool snap = false)
    {
        target = targetVal;
        velocity = velocityVal;
        max_speed = speedVal;
        captureRange = rangeVal;
        isTemporary = temporary;
        destroyOnCompletion = destroyOnTemp;
        snapToDestination = snap;
        if(onTargetReached == null)
        {
            onTargetReached = new UnityEvent();
        }
        
    }
    public void SetTarget(Vector3 targetVal,float rangeVal = 0)
    {
        target = targetVal;
        captureRange = rangeVal;
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if(!SeekEnabled)
        {
            return;
        }
        float distance = Vector3.Distance(transform.position, target);
        if (distance <= captureRange)
        {
            if(snapToDestination)
            {
                transform.position = target;
            }
            onTargetReached.Invoke();
        }
        if (!isTemporary)
        {
            seek();
            return;
        }
        TemporarySeek();
    }
}