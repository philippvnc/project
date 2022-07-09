using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;
using pathfinding;

public class GridScript : MonoBehaviour
{

    public CubeScript currentCube;
    public bool[,,] cubeArray = new bool[9, 9, 9];
    public List<CubeScript> cubeList = new List<CubeScript>();
    public IDictionary cubeDictionary = new Dictionary<CubeScript,int>(); 
    public int[,] cubeSuccessors;

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
        cubeDictionary = new Dictionary<CubeScript,int>(); 
        for (int i = 0; i < cubeList.Count; i++){
            cubeDictionary.Add(cubeList[i], i);
        }
        ColorCurrentCubeAndNeighbors();
        GetSuccessors();
    }

    public void ColorCurrentCubeAndNeighbors(){
        foreach(CubeScript cube in cubeList)
        {
            cube.gameObject.GetComponent<Renderer>().material.color = Color.white;
        }
        currentCube.gameObject.GetComponent<Renderer>().material.color = Color.blue;
        foreach(CubeScript cube in currentCube.connectionsList)
        {
            cube.gameObject.GetComponent<Renderer>().material.color = Color.cyan;
        }
    }

    public void GetSuccessors(){
        int n = cubeList.Count;
        int[,] initialSuccessors = new int[n,n];
        // initialize successors with -1
        for (int i = 0; i < n; i++) {
            for (int j = 0; j < n; j++) {
                initialSuccessors[i,j] = -1;
            }
        }
        // add direct connections
        for (int i = 0; i < cubeList.Count; i++){
            foreach(CubeScript connectedCube in cubeList[i].connectionsList)
            {
                initialSuccessors[i,(int)cubeDictionary[connectedCube]] = (int)cubeDictionary[connectedCube];
            }
        }
        cubeSuccessors = Pathfinding.FloydWarshallSuccessors(initialSuccessors);
    }

    public CubeScript GetSuccessorOnPath(CubeScript startCube, CubeScript endCube){
        int successorId = cubeSuccessors[(int)cubeDictionary[startCube],(int)cubeDictionary[endCube]];
        if(successorId == -1) return null;
        return cubeList[successorId];
    }

    public CubeScript GetSuccessorOnPath(CubeScript endCube){
        return GetSuccessorOnPath(currentCube, endCube);
    }

}
