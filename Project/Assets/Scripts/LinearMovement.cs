using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearMovement : MonoBehaviour
{
    public delegate void OnWayPointReachedEvent();
    public event OnWayPointReachedEvent OnWayPointReached;

    // Transforms to act as start and end markers for the journey.
    private Vector3 startMarker;
    private Vector3 midEndMarker;
    private Vector3 midStartMarker;
    private Vector3 endMarker;

    public float speed = 1.0F;

    private float fraction;
    public bool running;
    private bool beforeMid;

    public void Move(Vector3 start, Vector3 midEnd, Vector3 midStart, Vector3 end)
    {
        startMarker = start;
        midEndMarker = midEnd;
        midStartMarker = midStart;
        endMarker = end;
        fraction = 0;
        running = true;
        beforeMid = true;
    }

    void Update()
    {
        if (!running) return;
        
        fraction +=  Time.deltaTime * speed;
        //Debug.Log("fraction: " + fraction);
        
        
        if(beforeMid){
            if(fraction >= 1)
            {
                transform.position = midStartMarker;
                beforeMid = false;
                fraction = 0;
            } else
            {
                transform.position = Vector3.Lerp(startMarker, midEndMarker, fraction);
            }
        } else {
            if(fraction >= 1)
            {
                transform.position = endMarker;
                running = false;
                if(OnWayPointReached != null) OnWayPointReached();
            } else
            {
                transform.position = Vector3.Lerp(midStartMarker, endMarker, fraction);
            }
        }
            
        //Debug.Log("transform.position: " + transform.position);
       
    }
}
