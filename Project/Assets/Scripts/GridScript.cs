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
    public bool[,,] cubeArray; //x,y,z
    public bool[,,,] projectionArray; //p,x,z,y
    public List<CubeScript> cubeList;
    public IDictionary cubeDictionary;
    public int[,,] cubeSuccessors;
    
    public CamPerspective currentPerspective;

    public void Start(){
        Debug.Log("Grid Start");
        cubeArray = new bool[gridSize, gridSize, gridSize];
        projectionArray = new bool[PerspectiveCollection.perspectiveDirections.Length, gridSize*3, gridSize*3, gridSize*3];
        cubeList = new List<CubeScript>();
        cubeDictionary = new Dictionary<CubeScript,int>(); 
        //hardcoded starting perspective for testing
        currentPerspective = new CamPerspective(CamPerspective.NORTH_WEST);
        currentCube = CreateCube(new Vector3(2,2,2));
        CreateRandomPossibleNeighbor();
        
        BuildCubeDictionary();
        UpdateConnectivityForAllCubes();
        CalculateSuccessors();

        GatherProjectionArray();
        ColorCurrentCubeAndReachableCubes();
    }

    public void SetPerspective(CamPerspective perspective){
        currentPerspective = perspective;
        ColorCurrentCubeAndReachableCubes();
    }

    public CubeScript CreateCube(Vector3 position){
        GameObject cubeGA = Instantiate(cubePrefab,position,Quaternion.identity) as GameObject;
        cubeGA.transform.parent=transform; 

        CubeScript cubeScript = cubeGA.GetComponent<CubeScript>();
        cubeScript.Init();
        cubeScript.gridScript = gameObject.GetComponent<GridScript>();
        cubeArray[cubeScript.pos.x, cubeScript.pos.y, cubeScript.pos.z] = true;
        cubeList.Add(cubeScript);
        return cubeScript;
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
                        if (IsOutOfGrid(shiftedNeighbor.x, shiftedNeighbor.z)) {
                            continue;
                        }
                        if (IsCubeAbove(shiftedNeighbor)) {
                            continue;
                        }
                        if (IsCubeBeneith(shiftedNeighbor)) {
                            continue;
                        }
                        //Debug.Log("Can i add this shifted position? " + shiftedNeighbor.ToString());
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
        //Debug.Log("generated possible neighbors positions: " + freeNeighbors.Count);
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

    public void GatherProjectionArray(){
        projectionArray = new bool[PerspectiveCollection.perspectiveDirections.Length, gridSize*3, gridSize*3, gridSize*3];
        foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
            foreach(CubeScript cube in cubeList)
            {
                //Debug.Log("adding projetion: " + cube.projection[perspective.id].ToString());
                projectionArray[perspective.id,
                    cube.projection[perspective.id].x + gridSize, 
                    cube.projection[perspective.id].z + gridSize, 
                    cube.pos.y + gridSize] = true;
            }
        }
    }

    public void UpdateConnectivityForAllCubes()
    {
        foreach (CubeScript cube in cubeList)
        {
            cube.UpdateConnectivity();
        }
    }

    public void BuildCubeDictionary(){
        cubeDictionary = new Dictionary<CubeScript,int>(); 
        for (int i = 0; i < cubeList.Count; i++){
            cubeDictionary.Add(cubeList[i], i);
        }
    }

    public void ColorCurrentCubeAndReachableCubes(){
        foreach(CubeScript cube in cubeList)
        {
            if(GetSuccessorOnPath(cube) != null){
                cube.gameObject.GetComponent<Renderer>().material = cube.reachableMaterial;
            } else {
                cube.gameObject.GetComponent<Renderer>().material = cube.unreachableMaterial;
            }
        }
        currentCube.gameObject.GetComponent<Renderer>().material = currentCube.reachableMaterial;
    }

    public void CalculateSuccessors(){
        int n = cubeList.Count;
        cubeSuccessors = new int[PerspectiveCollection.perspectiveDirections.Length,n,n];
        foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
            int[,] tempSuccessors = Pathfinding.FloydWarshallSuccessors(GetInitialSuccessors(perspective));
            for (int j = 0; j < cubeSuccessors.GetLength(1); j++)
            {
                for (int k = 0; k < cubeSuccessors.GetLength(2); k++)
                {
                    cubeSuccessors[perspective.id,j,k] = tempSuccessors[j,k];
                }
            }
        }
        /*
        Debug.Log("Calculated Successors: ");

        for (int i = 0; i < cubeSuccessors.GetLength(0); i++)
        {
            Debug.Log(" perspective id: " + i);
            for (int j = 0; j < cubeSuccessors.GetLength(1); j++)
            {
                string line = "  ";
                for (int k = 0; k < cubeSuccessors.GetLength(2); k++)
                {
                    line += cubeSuccessors[i, j, k] + " ";
                } 
                Debug.Log(line);
            }
           
        }
        */
       
    }

    private int[,] GetInitialSuccessors(CamPerspective perspective){
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
            foreach(CubeScript connectedCube in cubeList[i].connectionsList[perspective.id])
            {
                initialSuccessors[i,(int)cubeDictionary[connectedCube]] = (int)cubeDictionary[connectedCube];
            }
        }
        return initialSuccessors;
    }

    public CubeScript GetSuccessorOnPath(CamPerspective perspective, CubeScript startCube, CubeScript endCube){
        int i = (int)cubeDictionary[startCube];
        int j = (int)cubeDictionary[endCube];
        int successorId = cubeSuccessors[perspective.id,(int)cubeDictionary[startCube],(int)cubeDictionary[endCube]];
        if(successorId == -1) return null;
        return cubeList[successorId];
    }

    public CubeScript GetSuccessorOnPath(CubeScript endCube){
        return GetSuccessorOnPath(currentPerspective, currentCube, endCube);
    }

    public bool IsOutOfGrid(int x, int y, int z){
        return IsOutOfGrid(x) || IsOutOfGrid(y) || IsOutOfGrid(z);
    }

    public bool IsOutOfGrid(int x, int z){
        return IsOutOfGrid(x) || IsOutOfGrid(z);
    }

    public bool IsOutOfGrid(int x){
        return (x < 0 || x >= gridSize);
    }

    public bool IsCubeAtProjection(CamPerspective perspective, Position2 projection, int minY, int maxY){
        Debug.Log("cheking for projection " + projection.ToString() + " between y: " + minY + " - " + maxY);
        for(int y = minY; y < maxY+1; y++){
            Debug.Log("y: " + y);
            if(projectionArray[perspective.id, projection.x + gridSize, projection.z + gridSize, y+ gridSize]){
                Debug.Log("cube here");
                return true;
            }
        }
        return false;
    }

    public bool IsCubeAbove(Position3 position){
        if(position.y + 1 >= gridSize) return false;
        return cubeArray[position.x, position.y + 1, position.z];
    }

    public bool IsCubeBeneith(Position3 position){
        if(position.y - 1 < 0) return false;
        return cubeArray[position.x, position.y - 1, position.z];
    }

}
