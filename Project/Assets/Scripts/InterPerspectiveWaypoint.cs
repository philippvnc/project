using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;

public class InterPerspectiveWaypoint
{
    public CubeScript cube;
    public CamPerspective perspective;
    public PlaneDirection direction;

    public InterPerspectiveWaypoint(CubeScript cube, CamPerspective perspective, PlaneDirection direction)
    {
        this.cube = cube;
        this.perspective = perspective;
        this.direction = direction;
    }
}

