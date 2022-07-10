using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;
using pathfinding;


[DefaultExecutionOrder(-9)]
public class GridScript : MonoBehaviour
{
    public GameObject cubePrefab;
    public int gridSize = 5;

    public CubeScript currentCube;
    public bool[,,] cubeArray;
    public List<CubeScript> cubeList;
    public IDictionary cubeDictionary;
    public int[,] cubeSuccessors;
    
    public CamPerspective currentPerspective;

    public void Start(){
        Debug.Log("Grid Start");
        cubeArray = new bool[gridSize, gridSize, gridSize];
        cubeList = new List<CubeScript>();
        cubeDictionary = new Dictionary<CubeScript,int>(); 
        //hardcoded for testing
        currentPerspective = new CamPerspective(CamPerspective.NORTH_WEST);
        CreateCube(new Vector3(2,2,2));
        //CreateEveryPossibleNeighbor();
        CreateRandomPossibleNeighbor();
        CreateRandomPossibleNeighbor();
        CreateRandomPossibleNeighbor();
        CreateRandomPossibleNeighbor();
        currentCube = cubeList[0];
    }

    public void CreateCube(Vector3 position){
        GameObject cubeGA = Instantiate(cubePrefab,position,Quaternion.identity) as GameObject;
        cubeGA.transform.parent=transform; 

        CubeScript cubeScript = cubeGA.GetComponent<CubeScript>();
        cubeScript.Init();
        cubeScript.gridScript = gameObject.GetComponent<GridScript>();
        cubeArray[cubeScript.pos.x, cubeScript.pos.y, cubeScript.pos.z] = true;
        cubeList.Add(cubeScript);
    }

    public List<Vector3> GenerateNewNeighborPosition(){ 
        List<Vector3> freeNeighbors = new List<Vector3>();
        bool[,,] freeNeighborsArray = new bool[gridSize, gridSize, gridSize];
        foreach(CubeScript cube in cubeList)
        {
            foreach (PlaneDirection planeDirection in PlaneDirectionCollection.planeDirections)
            {
                Position3 position = new Position3(cube.pos.x + planeDirection.pos.x, cube.pos.y, cube.pos.z + planeDirection.pos.z);
                foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
                    // skip own perspective
                    if(currentPerspective.id == perspective.id){
                        continue;
                    }
                    // add shift 
                    List<Position3> shiftedNeighbors;
                    if(Occlusion.OccludedWhenHeigher[perspective.id,planeDirection.id]){
                        shiftedNeighbors = Projection.GetAllShiftsToHeigh(position, perspective, 0, position.y);
                    } else {
                        shiftedNeighbors = Projection.GetAllShiftsToHeigh(position, perspective, position.y+1, gridSize-1);
                    }
                    foreach(Position3 shiftedNeighbor in shiftedNeighbors)
                    {
                        // out of gridSize?
                        if(shiftedNeighbor.x < 0 || shiftedNeighbor.x >= gridSize 
                            || shiftedNeighbor.z < 0 || shiftedNeighbor.z >= gridSize) {
                            continue;
                        }
                        Debug.Log("Can i add this shifted position? " + shiftedNeighbor.ToString());
                        // cube or neighbor position already there?
                        if(cubeArray[shiftedNeighbor.x,shiftedNeighbor.y,shiftedNeighbor.z] 
                            || freeNeighborsArray[shiftedNeighbor.x,shiftedNeighbor.y,shiftedNeighbor.z])
                        {
                            continue;
                        }
                        freeNeighbors.Add(new Vector3(shiftedNeighbor.x,shiftedNeighbor.y,shiftedNeighbor.z));
                        freeNeighborsArray[shiftedNeighbor.x,shiftedNeighbor.y,shiftedNeighbor.z] = true;
                    }
                }
            }   
        }
        Debug.Log("generated possible neighbors positions: " + freeNeighbors.Count);
        return freeNeighbors;
    }

    public void CreateEveryPossibleNeighbor(){
        foreach(Vector3 vector3 in GenerateNewNeighborPosition())
        {
            CreateCube(vector3);
        }
    }

    public void CreateRandomPossibleNeighbor(){
        List<Vector3> positions = GenerateNewNeighborPosition();
        CreateCube(positions[Random.Range(0, positions.Count)]);
    }

    public void RemoveOldestCube(){
        //TODO implement
    }

    public void UpdateConnectivityForAllCubes(CamPerspective perspective)
    {
        currentPerspective = perspective;
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
        //ColorCurrentCubeAndNeighbors();
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
        /*
        for (int i = 0; i < cubeSuccessors.GetLength(0); i++)
        {
            string line = "";
            for (int j = 0; j < cubeSuccessors.GetLength(1); j++)
            {
                line += cubeSuccessors[i, j] + " ";
            }
            Debug.Log(line);
        }
        */
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
