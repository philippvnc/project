using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;
using pathfinding;


[DefaultExecutionOrder(-9)]
public class GridScript : MonoBehaviour
{

    public GameObject cubePrefab;
    public int gridSize = 7;
    public int cubeCount = 3;
    public int skipCurrentDirectionTill = 3;
    public bool skipDirectNeighbors = true;
    public int maxTriesForCubeCreation = 20;

    public CubeScript currentCube;
    public bool[,,] cubeArray; //x,y,z
    public bool[,,,] projectionArray; //p,x,z,y
    public List<CubeScript> cubeList;
    public IDictionary cubeDictionary;
    public int[,,] cubeSuccessors;

    public int[,] cubeSuccessorsInterPerspective;
    
    public CamPerspective currentPerspective;

    public void Start(){
        Debug.Log("Grid Start");
        cubeArray = new bool[gridSize, gridSize, gridSize];
        projectionArray = new bool[PerspectiveCollection.perspectiveDirections.Length, gridSize*3, gridSize*3, gridSize*3];
        cubeList = new List<CubeScript>();
        cubeDictionary = new Dictionary<CubeScript,int>(); 
        //hardcoded starting perspective for testing
        currentPerspective = new CamPerspective(CamPerspective.SOUTH_EAST);
        currentCube = CreateCube(new Vector3(3,3,3));

        CreateCubesAroundCurrentCube();
        Debug.Log("Game started");
    }

    public void CreateCubesAroundCurrentCube(){
        int tries = 0;
        while(cubeList.Count < cubeCount && tries < maxTriesForCubeCreation){
            CreateRandomNeighborWithReachabilityGaranty(cubeList.Count <= skipCurrentDirectionTill, skipDirectNeighbors);
            tries +=1;
        }
        Debug.Log("Created cubes in " + (tries - cubeCount + 1) + " extra tries");
        CalculateSuccessors();
        currentCube.MarkPlanted();
        MarkPlantable();
    }

    public void RemoveAllCubesButCurrent(){
        List<CubeScript> tempCubes = new List<CubeScript>();
        tempCubes.AddRange(cubeList);
        foreach(CubeScript cube in tempCubes){
            if(cube != currentCube) RemoveCube(cube);
        }
        cubeDictionary[currentCube] = 0;
    }

    public void SetCurrentCube(CubeScript cube){
        currentCube = cube;
        currentCube.MarkPlanted();
        if(IsEveryCubePlanted()){
           NewLevel();
        } 
    }

    public void NewLevel()
    {
        RemoveAllCubesButCurrent();
        Debug.Log("removed all cubes but current");
        cubeCount += 1;
        CreateCubesAroundCurrentCube();
        Debug.Log("created new cubes");
    }

    public bool IsEveryCubePlanted(){
        foreach(CubeScript cube in cubeList){
            if (!cube.planted) {
                //Debug.Log("cube is not planted: " + cube.pos.ToString());
                return false;
            }
        }
        //Debug.Log("every cube is planted");
        return true;
    }

    public void SetPerspective(CamPerspective perspective){
        currentPerspective = perspective;
        MarkUnreachableCubes();
        MarkPlantable();
    }

    public CubeScript CreateCube(Vector3 position){
        // create and init gameobject
        GameObject cubeGA = Instantiate(cubePrefab,position,Quaternion.identity) as GameObject;
        cubeGA.transform.parent=transform; 
        CubeScript cubeScript = cubeGA.GetComponent<CubeScript>();
        cubeScript.Init();
        cubeScript.gridScript = gameObject.GetComponent<GridScript>();

        // set boolean array values
        cubeArray[cubeScript.pos.x, cubeScript.pos.y, cubeScript.pos.z] = true;
        foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
            projectionArray[perspective.id,
                    cubeScript.projection[perspective.id].x + gridSize, 
                    cubeScript.projection[perspective.id].z + gridSize, 
                    cubeScript.pos.y + gridSize] = true;
        }

        // set list and dict
        cubeDictionary.Add(cubeScript, cubeList.Count);
        cubeList.Add(cubeScript);

        return cubeScript;
    }

    public void RemoveCube(CubeScript cube){
        // set boolean array values
        cubeArray[cube.pos.x, cube.pos.y, cube.pos.z] = false;
        foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
            projectionArray[perspective.id,
                    cube.projection[perspective.id].x + gridSize, 
                    cube.projection[perspective.id].z + gridSize, 
                    cube.pos.y + gridSize] = false;
        }

        // set list and dict
        cubeList.Remove(cube);
        cubeDictionary.Remove(cube);

        // destroy gameobject
        Destroy(cube.gameObject);
    }

    public List<Vector3> GenerateNewNeighborPositions(bool skipCurrentPerspective, bool skipDirectNeighbors){ 
        List<Vector3> freeNeighbors = new List<Vector3>();
        bool[,,] freeNeighborsArray = new bool[gridSize, gridSize, gridSize];
        foreach(CubeScript cube in cubeList)
        {
            foreach (PlaneDirection planeDirection in PlaneDirectionCollection.planeDirections)
            {
                Position3 position = new Position3(cube.pos.x + planeDirection.pos.x, cube.pos.y, cube.pos.z + planeDirection.pos.z);
                Debug.Log("checking neighbor position " + position.ToString());
                foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
                    
                    // skip own perspective
                    if(skipCurrentPerspective && currentPerspective.id == perspective.id){
                        Debug.Log("skipping this perspective");
                        continue;
                    }
                    Debug.Log("checking perspective " + perspective.id);

                    // add shift 
                    List<Position3> shiftedNeighbors;
                    if(Occlusion.OccludedWhenHeigher[perspective.id,planeDirection.id]){
                        if(skipDirectNeighbors){
                            shiftedNeighbors = Projection.GetAllShiftsToHeigh(position, perspective, 0, position.y);
                        } else {
                            shiftedNeighbors = Projection.GetAllShiftsToHeigh(position, perspective, 0, position.y+1);
                        }
                    } else {
                        shiftedNeighbors = Projection.GetAllShiftsToHeigh(position, perspective, position.y+1, gridSize-1);
                    }

                    Debug.Log("generated shifts " + shiftedNeighbors.Count);

                    foreach(Position3 shiftedNeighbor in shiftedNeighbors)
                    {
                        if (IsOutOfGrid(shiftedNeighbor.x, shiftedNeighbor.z)) {
                            Debug.Log("dropping shift out of bounds " + shiftedNeighbor.ToString());
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
                        Debug.Log("register possible neighbor position " + shiftedNeighbor.ToString());
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
        foreach(Vector3 vector3 in GenerateNewNeighborPositions(false, false))
        {
            CreateCube(vector3);
        }
    }

    public CubeScript CreateRandomPossibleNeighbor(bool skipCurrentPerspective, bool skipDirectNeighbors){
        List<Vector3> positions = GenerateNewNeighborPositions(skipCurrentPerspective, skipDirectNeighbors);
        Debug.Log("generated n positions: " + positions.Count + " from " + cubeList.Count + " cubes");
        return CreateCube(positions[Random.Range(0, positions.Count)]);
    }

    public void CreateRandomNeighborWithReachabilityGaranty(bool skipCurrentPerspective, bool skipDirectNeighbors){
        CubeScript tempCube = CreateRandomPossibleNeighbor(skipCurrentPerspective, skipDirectNeighbors);
        Debug.Log("trying out cube at " + tempCube.pos.ToString());
        UpdateConnectivityForAllCubes();
        CalculateSuccessorsInterPerspective();

        foreach(CubeScript cube in cubeList)
        {
            if(cube != currentCube){
                if(!HasInterPerspectiveSuccessorOnPath(cube)){
                    RemoveCube(tempCube);
                    Debug.Log("cube would destroy some cubes connection to current cube, removing cube again");
                    return;
                }
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

    public void MarkPlantable(){
        foreach(CubeScript cube in cubeList)
        {
            if(GetSuccessorOnPath(cube) != null){
                cube.MarkPlantable();
            } 
        }
    }

    public void MarkUnreachableCubes(){
        foreach(CubeScript cube in cubeList)
        {
            if(cube != currentCube){
                if(HasInterPerspectiveSuccessorOnPath(cube)){
                    cube.MarkReachable();
                } else {
                    cube.MarkUnreachable();
                }
            } 
        }
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
        Debug.Log("Calculated Successors");
        /*
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

    public void CalculateSuccessorsInterPerspective(){
        cubeSuccessorsInterPerspective = Pathfinding.FloydWarshallSuccessors(GetInitialSuccessorsInterPerspective());
        Debug.Log("Calculated InterPerspective Successors");
        /*
        for (int j = 0; j < cubeSuccessorsInterPerspective.GetLength(0); j++)
        {
            string line = "  ";
            for (int k = 0; k < cubeSuccessorsInterPerspective.GetLength(1); k++)
            {
                line += cubeSuccessorsInterPerspective[j, k] + " ";
            } 
            Debug.Log(line);
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
        //Debug.Log("Looping through cube List of size: " + cubeList.Count);
        for (int i = 0; i < cubeList.Count; i++){
            //Debug.Log("Looping through cube connection List of size: " + cubeList[i].connectionsList[perspective.id].Count);
            foreach(CubeScript connectedCube in cubeList[i].connectionsList[perspective.id])
            {
                //Debug.Log("connected cube pos: " + connectedCube.pos.ToString());
                //Debug.Log("connected cube dict id: " + (int)cubeDictionary[connectedCube]);
                initialSuccessors[i,(int)cubeDictionary[connectedCube]] = (int)cubeDictionary[connectedCube];
            }
        
        }
        return initialSuccessors;
    }

    private int [,] GetInitialSuccessorsInterPerspective(){
        int n = cubeList.Count * PerspectiveCollection.perspectiveDirections.Length;
        int[,] initialSuccessorsInterPerspective = new int[n,n];
        // initialize successors with -1
        for (int i = 0; i < n; i++) {
            for (int j = 0; j < n; j++) {
                initialSuccessorsInterPerspective[i,j] = -1;
            }
        }
        // add direct connection per perspective
        foreach(CamPerspective perspectiveX in PerspectiveCollection.perspectiveDirections){
            foreach(CamPerspective perspectiveY in PerspectiveCollection.perspectiveDirections){
                for (int i = 0; i < cubeList.Count; i++){   
                    //if Perspective X = Y: add local connections
                    if(perspectiveX.id == perspectiveY.id){
                        foreach(CubeScript connectedCube in cubeList[i].connectionsList[perspectiveX.id])
                        {
                            initialSuccessorsInterPerspective[
                                i + cubeList.Count * perspectiveX.id,
                                (int)cubeDictionary[connectedCube] + cubeList.Count * perspectiveX.id] 
                                = (int)cubeDictionary[connectedCube] + cubeList.Count * perspectiveX.id;
                        }
                    //add interperspective connections to same cube
                    } else { 
                        initialSuccessorsInterPerspective[i + cubeList.Count * perspectiveX.id, i + cubeList.Count * perspectiveY.id] 
                        = i + cubeList.Count * perspectiveY.id;
                    }
                }
            }
        }
        /*
        Debug.Log("Calculated InterPerspective Initial Successors");
       
        for (int j = 0; j < initialSuccessorsInterPerspective.GetLength(0); j++)
        {
            string line = "  ";
            for (int k = 0; k < initialSuccessorsInterPerspective.GetLength(1); k++)
            {
                line += initialSuccessorsInterPerspective[j, k] + " ";
            } 
            Debug.Log(line);
        }
        */
        return initialSuccessorsInterPerspective;
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

    public bool HasInterPerspectiveSuccessorOnPath(CubeScript startCube, CubeScript endCube){
        int i = (int)cubeDictionary[startCube];
        int j = (int)cubeDictionary[endCube];
        int successorId = cubeSuccessorsInterPerspective[(int)cubeDictionary[startCube],(int)cubeDictionary[endCube]];
        return (successorId != -1);
    }

    public bool HasInterPerspectiveSuccessorOnPath(CubeScript endCube){
        return HasInterPerspectiveSuccessorOnPath(currentCube, endCube);
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
        //Debug.Log("cheking for projection " + projection.ToString() + " between y: " + minY + " - " + maxY);
        for(int y = minY; y < maxY+1; y++){
            //Debug.Log("y: " + y);
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
