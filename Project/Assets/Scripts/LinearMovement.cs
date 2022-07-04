using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearMovement : MonoBehaviour
{
    // Transforms to act as start and end markers for the journey.
    private Vector3 startMarker;
    private Vector3 fakeEndMarker;
    private Vector3 realEndMarker;

    // Movement speed in units per second.
    public float speed = 1.0F;

    // Time when the movement started.
    private float startTime;

    // Total distance between the markers.
    private float journeyLength;

    private bool running;

    public void Move(Vector3 start, Vector3 fakeEnd, Vector3 realEnd)
    {
        startMarker = start;
        fakeEndMarker = fakeEnd;
        realEndMarker = realEnd;

        // Keep a note of the time the movement started.
        startTime = Time.time;

        // Calculate the journey length.
        journeyLength = Vector3.Distance(startMarker, fakeEndMarker);

        // actually start
        running = true;
    }

    // Move to the target end position.
    void Update()
    {
        if (!running) return;

        // Distance moved equals elapsed time times speed..
        float distCovered = (Time.time - startTime) * speed;

        // Fraction of journey completed equals current distance divided by total distance.
        float fractionOfJourney = distCovered / journeyLength;

        if(fractionOfJourney >= 1)
        {
            fractionOfJourney = 1; 
            transform.position = realEndMarker;
        } else
        {
            // Set our position as a fraction of the distance between the markers.
            transform.position = Vector3.Lerp(startMarker, fakeEndMarker, fractionOfJourney);
        }
    }
}
