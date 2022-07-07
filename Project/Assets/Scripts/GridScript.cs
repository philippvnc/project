using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;

public class GridScript : MonoBehaviour
{

    public CubeScript currentCube;
    public bool[,,] cubeArray = new bool[9, 9, 9];
    public ArrayList cubeList = new ArrayList();

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
        ColorCurrentCubeAndNeighbors();
    }

    public PlaneDirection IsConnectedToCurrentCube(GameObject gameObjectToCheck){
        foreach (PlaneDirection direction in PlaneDirectionCollection.planeDirections)
        {
            if(currentCube.connections[direction.id] != null) {
                    if(GameObject.ReferenceEquals( currentCube.connections[direction.id].gameObject, gameObjectToCheck)){
                    return direction;
                }
            }
        }
        return null;
    }

    public void ColorCurrentCubeAndNeighbors(){
        foreach(CubeScript cube in cubeList)
        {
            cube.gameObject.GetComponent<Renderer>().material.color = Color.white;
        }
        currentCube.gameObject.GetComponent<Renderer>().material.color = Color.blue;
        foreach(CubeScript cube in currentCube.connections)
        {
            if(cube != null){
                cube.gameObject.GetComponent<Renderer>().material.color = Color.cyan;
            }
        }
    }
}
