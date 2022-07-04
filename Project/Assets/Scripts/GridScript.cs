using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;

public class GridScript : MonoBehaviour
{

    public bool[,,] cubeArray = new bool[9, 9, 9];
    public ArrayList cubeList = new ArrayList();

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateConnectivityForAllCubes(CamPerspective perspective)
    {
        foreach(CubeScript cube in cubeList)
        {
            cube.UpdateProjection(perspective);
        }
        foreach (CubeScript cube in cubeList)
        {
            cube.UpdateConnectivity(perspective);
        }
    }

}
