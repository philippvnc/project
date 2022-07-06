using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearRotation : MonoBehaviour
{
    private float startAngle;
    private float endAngle;

    public float minSpeed = 5.0F;
    public float currentSpeed = 1.0F;

    private float startTime;

    private float journeyLength;

    private bool running;

    public void Rotate(float startAngle, float endAngle)
    {
        this.startAngle = startAngle;
        this.endAngle = endAngle;

        startTime = Time.time;

        journeyLength = Mathf.Abs(startAngle - endAngle);

        running = true;
    }

    void Update()
    {
        if (!running) return;

        // Distance moved equals elapsed time times speed..
        float distCovered = (Time.time - startTime) * currentSpeed;

        // Fraction of journey completed equals current distance divided by total distance.
        float fractionOfJourney = distCovered / journeyLength;

        if(fractionOfJourney >= 1)
        {
            fractionOfJourney = 1;
            running = false;
            //Debug.Log("Rotation Complete, setting to " + endAngle * (fractionOfJourney));
        } 
        gameObject.transform.eulerAngles = new Vector3(
                gameObject.transform.eulerAngles.x,
                startAngle * (1-fractionOfJourney) + endAngle * (fractionOfJourney),
                gameObject.transform.eulerAngles.z
        );

        //Debug.Log("Y Euler angle after setting " + gameObject.transform.eulerAngles.y);
    }
}
