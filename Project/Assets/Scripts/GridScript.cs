using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;
using pathfinding;


[DefaultExecutionOrder(-9)]
public class GridScript : MonoBehaviour
{
    public delegate void OnChangeCurrentCubeEvent(CubeScript cube);
    public event OnChangeCurrentCubeEvent OnChangeCurrentCube;

    public CamPerspective currentPerspective;
    public ScoreController scoreController;

    public GameObject cubePrefab;
    public GameObject enemyPrefab;

    public int gridWidth = 7;
    public int gridHeight = 7;
    public int initialCubeCount = 2;
    public int increaseCubeCount = 2;
    public int cubesPerEnemy = 10;
    public int cubeCount;
    public int skipCurrentDirectionTill = 3;
    public bool skipDirectNeighbors = true;
    public int maxTriesForCubeCreation = 20;

    public CubeScript currentCube;
    public bool[,,] cubeArray; //x,y,z
    public bool[,,,] projectionArray; //p,x,z,y
    public bool[,,] projectionArray2d; //p,x,z
    public bool[,,,] prohibitedProjectionArray; //p,x,z,y
    public List<CubeScript> cubeList;
    public IDictionary cubeDictionary;
    public int[,,] cubeSuccessors;
    public int[,] cubeSuccessorsInterPerspective;
    public int[,] costsInterPerspective;

    public List<EnemyController> enemyList;

    public void Start(){
        Debug.Log("Grid Start");
        RestartGame();
    }

    public void RestartGame(){
        cubeArray = new bool[gridWidth, gridHeight, gridWidth];
        projectionArray = new bool[PerspectiveCollection.perspectiveDirections.Length, gridWidth*3, gridWidth*3, gridHeight*3];
        projectionArray2d = new bool[PerspectiveCollection.perspectiveDirections.Length, gridWidth*3, gridWidth*3];
        prohibitedProjectionArray = new bool[PerspectiveCollection.perspectiveDirections.Length, gridWidth*3, gridWidth*3, gridHeight*3];
        cubeList = new List<CubeScript>();
        cubeDictionary = new Dictionary<CubeScript,int>();
        cubeCount = initialCubeCount; 
        enemyList = new List<EnemyController>();
        //hardcoded starting perspective for testing
        SetPerspective(new CamPerspective(CamPerspective.SOUTH_EAST));

        currentCube = CreateCube(new Vector3(3,2,3));
        CreateCubesAroundCurrentCube();
        scoreController.Reset();
        Debug.Log("Game started");
    }

    private void CreateEnemies(){
        int desiredNumberEnemies = (int) cubeCount / cubesPerEnemy;

        for (int i = 0; i < desiredNumberEnemies - enemyList.Count; i++){
            CreateEnemy();
        }
    }

    private void CreateEnemy(){
        CubeScript furthestCube = getFurthestCube(currentCube);
        Position3 furthestCubePos = furthestCube.pos;
        Vector3 enemyPos = new Vector3(furthestCubePos.x, furthestCubePos.y + 1, furthestCubePos.z);
        GameObject enemy = Instantiate(enemyPrefab, enemyPos, Quaternion.identity) as GameObject;
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        enemyController.Init(this, furthestCube);
        enemyList.Add(enemyController);
    }

    private void CreateCubePillars(){
        foreach(CubeScript cube in cubeList){
            int nextCubeY = GetYPosOfNextCube(cube.pos);
            if(nextCubeY == -1){
                if(IsPillarInProhibitedProjection(cube)){
                    cube.InstantiateMinimalPillars(-1);
                } else {
                    cube.InstantiateMassivePillars();
                }
            } else {
                cube.InstantiateMinimalPillars(nextCubeY);
            }
        }
    }

    private bool IsPillarInProhibitedProjection(CubeScript cube){
        for(int y = 0; y < cube.pos.y; y++){
            if (IsPosition3InProhibitedProjection(new Position3(cube.pos.x, y, cube.pos.z))){
                return true;
            }
        }
        return false;
    }

    private bool IsPosition3InProhibitedProjection(Position3 position){
        //Debug.Log("checkin pos " + position.ToString());
        foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
            Position2 projection = Projection.Project(position, perspective);
            //Debug.Log("Prohibited projection check " + perspective.id + " " + projection.x + " " + projection.z + " " + position.y);
       
            if(prohibitedProjectionArray[perspective.id,
                projection.x + gridWidth,
                projection.z + gridWidth,
                position.y + gridHeight]){
                //Debug.Log("Pillar is in prohibited zone");
                return true;
            }
        }
        return false;
    }

    private int GetYPosOfNextCube(Position3 pos){
        for(int y = pos.y -1 ; y >= 0; y--){
            if(cubeArray[pos.x, y, pos.z]) return y;
        }
        return -1;
    }

    public void CreateCubesAroundCurrentCube(){
        int tries = 0;
        while(cubeList.Count < cubeCount && tries < maxTriesForCubeCreation){
            CreateRandomNeighborWithReachabilityGaranty(cubeList.Count <= skipCurrentDirectionTill, skipDirectNeighbors);
            tries +=1;
        }
        Debug.Log("Created cubes in " + (tries - cubeCount + 1) + " extra tries");
        CalculateSuccessors();
        CreateCubePillars();
        SetWayConnections();
        currentCube.MarkPlanted();
    }

    public void RemoveAllCubesButCurrent(){
        List<CubeScript> tempCubes = new List<CubeScript>();
        tempCubes.AddRange(cubeList);
        foreach(CubeScript cube in tempCubes){
            if(cube != currentCube) RemoveCube(cube);
        }
        prohibitedProjectionArray = new bool[PerspectiveCollection.perspectiveDirections.Length, gridWidth*3, gridWidth*3, gridHeight*3];
        cubeDictionary[currentCube] = 0;
    }

    public void SetCurrentCube(CubeScript cube){
        currentCube = cube;
        if(!currentCube.planted){
            currentCube.MarkPlanted();
            scoreController.Increase(1);
        }
        
        if(IsEveryCubePlanted()){
           NewLevel();
        } 

        if(OnChangeCurrentCube != null) OnChangeCurrentCube(cube);
    }

    public void NewLevel()
    {
        RemoveAllCubesButCurrent();
        Debug.Log("removed all cubes but current");
        cubeCount += increaseCubeCount;
        CreateCubesAroundCurrentCube();
        CreateEnemies();
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
        SetWayConnections();
        MarkUnreachableCubes();
    }

    private void SetWayConnections(){
        foreach(CubeScript cube in cubeList){
            cube.SetWayConnections(currentPerspective);
        }
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
                    cubeScript.projection[perspective.id].x + gridWidth, 
                    cubeScript.projection[perspective.id].z + gridWidth, 
                    cubeScript.pos.y + gridHeight] = true;
            projectionArray2d[perspective.id,
                    cubeScript.projection[perspective.id].x + gridWidth, 
                    cubeScript.projection[perspective.id].z + gridWidth] = true;
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
                    cube.projection[perspective.id].x + gridWidth, 
                    cube.projection[perspective.id].z + gridWidth, 
                    cube.pos.y + gridHeight] = false;
            projectionArray2d[perspective.id,
                    cube.projection[perspective.id].x + gridWidth, 
                    cube.projection[perspective.id].z + gridWidth] = false;
        }

        // set list and dict
        cubeList.Remove(cube);
        cubeDictionary.Remove(cube);

        //
        SetProjectionForAllCubes();

        // destroy gameobject
        Destroy(cube.gameObject);
    }

    private void SetProjectionForAllCubes(){
        foreach(CubeScript cube in cubeList)
        {
            foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
                projectionArray[perspective.id,
                        cube.projection[perspective.id].x + gridWidth, 
                        cube.projection[perspective.id].z + gridWidth, 
                        cube.pos.y + gridHeight] = true;
                projectionArray2d[perspective.id,
                        cube.projection[perspective.id].x + gridWidth, 
                        cube.projection[perspective.id].z + gridWidth] = true;
            }
        }
    }

    public List<Vector3> GenerateNewNeighborPositions(bool skipCurrentPerspective, bool skipDirectNeighbors){ 
        List<Vector3> freeNeighbors = new List<Vector3>();
        bool[,,] freeNeighborsArray = new bool[gridWidth, gridHeight, gridWidth];
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
                        shiftedNeighbors = Projection.GetAllShiftsToHeigh(position, perspective, position.y+1, gridHeight-1);
                    }

                    Debug.Log("generated shifts " + shiftedNeighbors.Count);

                    foreach(Position3 shiftedNeighbor in shiftedNeighbors)
                    {
                        if (IsOutOfGrid(shiftedNeighbor.x, shiftedNeighbor.y, shiftedNeighbor.z)) {
                            Debug.Log("dropping shift out of bounds " + shiftedNeighbor.ToString());
                            continue;
                        }
                        if(IsCubeAtProjectionInAnyPerspective(shiftedNeighbor)){
                            Debug.Log("skipping this position, there is already a cube at this projection");
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
                        // Projection prohibited?
                        if (IsPosition3InProhibitedProjection(shiftedNeighbor)){
                            continue;
                        }
                        //Debug.Log("register possible neighbor position " + shiftedNeighbor.ToString());
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
        // cube is accepted
        AddProhibitedProjections(tempCube);
    }

    private void AddProhibitedProjections(CubeScript cube){
        foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
            foreach (CubeScript connectedCube in cube.connectionsList[perspective.id]){
                AddProhibitedProjections(perspective,
                    cube, 
                    connectedCube);
            }
        }
    }

    private void AddProhibitedProjections(CamPerspective perspective,
        CubeScript cube, CubeScript otherCube){
        Position3 directlyAbovePosition1 = new Position3(cube.pos.x, cube.pos.y + 1, cube. pos.z);
        Position2 directlyAboveProjection1 = Projection.Project(directlyAbovePosition1, perspective);

        Position3 directlyAbovePosition2 = new Position3(otherCube.pos.x, otherCube.pos.y + 1, otherCube. pos.z);
        Position2 directlyAboveProjection2 = Projection.Project(directlyAbovePosition2, perspective);
      

        int minY = Mathf.Min(cube.pos.y, otherCube.pos.y);
        for(int y = minY; y < gridHeight; y++){
            prohibitedProjectionArray[perspective.id, 
                directlyAboveProjection1.x + gridWidth, 
                directlyAboveProjection1.z + gridWidth, 
                y + gridHeight] = true;
            //Debug.Log("Prohibited projection added " + perspective.id + " " + directlyAboveProjection1.x + " " + directlyAboveProjection1.z + " " + y);
        }
        for(int y = minY; y < gridHeight; y++){
            prohibitedProjectionArray[perspective.id, 
                directlyAboveProjection2.x + gridWidth, 
                directlyAboveProjection2.z + gridWidth, 
                y + gridHeight] = true;
            //Debug.Log("Prohibited projection added " + perspective.id + " " + directlyAboveProjection2.x + " " + directlyAboveProjection2.z + " " + y);
       
        }
    }


    public void UpdateConnectivityForAllCubes()
    {
        foreach (CubeScript cube in cubeList)
        {
            cube.UpdateConnectivity();
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
            int[,] tempSuccessors = Pathfinding.FloydWarshallSuccessors(GetInitialSuccessors(perspective)).successors;
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
        PathfindingResult pathfindingResult = Pathfinding.FloydWarshallSuccessors(GetInitialSuccessorsInterPerspective());
        cubeSuccessorsInterPerspective = pathfindingResult.successors;
        costsInterPerspective = pathfindingResult.costs;
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
    
    public InterPerspectiveWaypoint GetSuccessorOnPathInterPerspective(CamPerspective perspective, CubeScript startCube, CubeScript endCube){
        int i = (int)cubeDictionary[startCube] + cubeList.Count * perspective.id;
        int j = (int)cubeDictionary[endCube];
        int successorId = cubeSuccessorsInterPerspective[i,j];
        if(successorId == -1) return null;
        CamPerspective wayPointPerspective = new CamPerspective((successorId - successorId % cubeList.Count) / cubeList.Count);
        Debug.Log("caluculated perspective id: " + wayPointPerspective.id + " from successor id: " + successorId);
        CubeScript wayPointCube = cubeList[successorId % cubeList.Count]; 
        Debug.Log("waypoint cube: " + wayPointCube.pos.ToString());
        startCube.PrintConnections();
        PlaneDirection direction = (PlaneDirection) startCube.connectionsDirectionDictionary[perspective.id][wayPointCube];    
        return new InterPerspectiveWaypoint(wayPointCube, wayPointPerspective, direction);
    }

    public CubeScript GetSuccessorOnPath(CamPerspective perspective, CubeScript startCube, CubeScript endCube){
        int i = (int)cubeDictionary[startCube];
        int j = (int)cubeDictionary[endCube];
        int successorId = cubeSuccessors[perspective.id,i,j];
        if(successorId == -1) return null;
        return cubeList[successorId];
    }

    public CubeScript GetSuccessorOnPath(CubeScript startCube, CubeScript endCube){
        int i = (int)cubeDictionary[startCube];
        int j = (int)cubeDictionary[endCube];
        int successorId = cubeSuccessors[currentPerspective.id,i,j];
        if(successorId == -1) return null;
        return cubeList[successorId];
    }

    public CubeScript GetSuccessorOnPath(CubeScript endCube){
        return GetSuccessorOnPath(currentPerspective, currentCube, endCube);
    }

    public bool HasInterPerspectiveSuccessorOnPath(CubeScript startCube, CubeScript endCube){
        int i = (int)cubeDictionary[startCube];
        int j = (int)cubeDictionary[endCube];
        Debug.Log("can we go from cube " + i + " to cube " + j + " ?");
        int successorId = cubeSuccessorsInterPerspective[i,j];
        return (successorId != -1);
    }

    public bool HasInterPerspectiveSuccessorOnPath(CubeScript endCube){
        return HasInterPerspectiveSuccessorOnPath(currentCube, endCube);
    }

    public bool IsOutOfGrid(int x, int y, int z){
        return IsOutOfGridWidth(x) || IsOutOfGridHeight(y) || IsOutOfGridWidth(z);
    }

    public bool IsOutOfGridWidth(int x, int z){
        return IsOutOfGridWidth(x) || IsOutOfGridWidth(z);
    }

    public bool IsOutOfGridWidth(int x){
        return (x < 0 || x >= gridWidth);
    }
    
    public bool IsOutOfGridHeight(int y){
        return (y < 0 || y >= gridHeight);
    }

    public bool IsCubeAtProjectionInAnyPerspective(Position3 position){
        foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
            Position2 projection = Projection.Project(position, perspective); 
            if( projectionArray2d[perspective.id, projection.x + gridWidth, projection.z + gridWidth]){
                return true;
            }   
        }
        return false;
    }

    public bool IsCubeAtProjectionInYRange(CamPerspective perspective, Position2 projection, int minY, int maxY){
        //Debug.Log("cheking for projection " + projection.ToString() + " between y: " + minY + " - " + maxY);
        for(int y = minY; y < maxY+1; y++){
            //Debug.Log("y: " + y);
            if(projectionArray[perspective.id, projection.x + gridWidth, projection.z + gridWidth, y+ gridHeight]){
                Debug.Log("cube here");
                return true;
            }
        }
        return false;
    }

    public bool IsCubeAbove(Position3 position){
        if(position.y + 1 >= gridHeight) return false;
        return cubeArray[position.x, position.y + 1, position.z];
    }

    public bool IsCubeBeneith(Position3 position){
        if(position.y - 1 < 0) return false;
        return cubeArray[position.x, position.y - 1, position.z];
    }

    public CubeScript getFurthestCube(CubeScript cube){
        int cubeIndex = (int)cubeDictionary[cube];
        int highestCost = -1;
        int furthestCubeIndex = -1;
        for(int i = 0; i < cubeList.Count; i++){
            if(i == cubeIndex) continue;
            int tempCost = costsInterPerspective[i, cubeIndex];
            if(tempCost > highestCost){
                highestCost = tempCost;
                furthestCubeIndex = i;
            }
        }
        if(furthestCubeIndex == -1) return null;
        return cubeList[furthestCubeIndex];
    } 

}
