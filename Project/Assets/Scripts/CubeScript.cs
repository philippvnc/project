using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;

public class CubeScript : MonoBehaviour
{

    private GridScript gridScript;
    private Position3 pos;
    public Position2 projection;

    public bool connected;
    public CubeScript[] connections;

    void Awake()
    {
        gridScript = transform.parent.gameObject.GetComponent<GridScript>();
        pos = new Position3(
            (int)transform.position.x,
            (int)transform.position.y,
            (int)transform.position.z);
        connections = new CubeScript[PlaneDirectionCollection.planeDirections.Length];
        gridScript.cubeArray[pos.x, pos.y, pos.z] = true;
        gridScript.cubeList.Add(this);
    }

    void OnDrawGizmos()
    {
        if (connections == null) return;
        Gizmos.color = Color.red;
        foreach(CubeScript cube in connections)
        {
            if(cube != null) Gizmos.DrawLine(transform.position, new Vector3(cube.pos.x, cube.pos.y, cube.pos.z));
        }
        
    }

    public void UpdateProjection(CamPerspective perspective)
    {
        projection = Projection.Project(pos, perspective);
    }

    public void UpdateConnectivity(CamPerspective perspective)
    {
        connections = new CubeScript[PlaneDirectionCollection.planeDirections.Length];
        foreach (PlaneDirection planeDirection in PlaneDirectionCollection.planeDirections)
        {
            Position2 projectionToCheck = Projection.Project(
                new Position3(pos.x + planeDirection.pos.x, pos.y, pos.z + planeDirection.pos.z), perspective);
            //Debug.Log("" + planeDirection.x + " " + planeDirection.z);
            foreach(CubeScript cube in gridScript.cubeList)
            {
                if (projectionToCheck.Equals(cube.projection))
                {
                    if(connections[planeDirection.id] != null)
                    {
                        Debug.Log("Found Overlapping connection in same direction! Choosing higher cube for connection");
                        connections[planeDirection.id] = GetHigherCube(cube, connections[planeDirection.id]);
                        //gameObject.GetComponent<Renderer>().material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                    } else
                    {
                        connections[planeDirection.id] = cube;
                    }

                    connected = true;
                    //gameObject.GetComponent<Renderer>().material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

                    //Debug.Log("connected to other cube on plane");
                }
            }
        }
    }

    private static CubeScript GetHigherCube(CubeScript cube1, CubeScript cube2)
    {
        if (IsHeigherCube(cube1,cube2))
        {
            return cube1;
        } else
        {
            return cube2;
        }
    }

    public static bool IsHeigherCube(CubeScript cube1, CubeScript cube2)
    {
        return (cube1.gameObject.transform.position.y > cube2.gameObject.transform.position.y);
    }
}
