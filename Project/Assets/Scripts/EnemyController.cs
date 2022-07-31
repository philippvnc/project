using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using direction;

public class EnemyController : MonoBehaviour
{
    public GridScript grid;

    public CubeScript currentCube;

    private LinearMovement linearMovement;

    private Queue path;
    private CameraController cameraController;

    void OnEnable(){
        linearMovement = gameObject.GetComponent<LinearMovement>();
        cameraController = GameObject.Find("CameraAnker").GetComponent<CameraController>();
        grid = GameObject.Find("Grid").GetComponent<GridScript>();
        linearMovement.OnWayPointReached += TryMoveToNextWayPoint;
        cameraController.OnChangePerspective += TryMoveToNextWayPoint;
        grid.OnChangeCurrentCube += SetPath;
    } 

    void OnDisable(){
        linearMovement.OnWayPointReached -= TryMoveToNextWayPoint;
        cameraController.OnChangePerspective -= TryMoveToNextWayPoint;
        grid.OnChangeCurrentCube -= SetPath;
    }

    void Start(){
        path = new Queue();
        SetPath(grid.currentCube);
    }
 
    public void Init(GridScript grid, CubeScript currentCube){
        this.grid = grid;
        this.currentCube = currentCube;
    }

    public void SetPath(CubeScript goalCube){
        path = new Queue();
        CubeScript tempCurrentCube = currentCube;
        CamPerspective tempCurrentPerspective = grid.currentPerspective;
        bool lastCube = false;
        while(!lastCube){
            InterPerspectiveWaypoint waypoint = grid.GetSuccessorOnPathInterPerspective(tempCurrentPerspective, tempCurrentCube, goalCube);
            if(waypoint == null){
                Debug.Log("Goal Cube is not on a path with current cube");
                return;
            }
            

            if (waypoint.cube.pos == tempCurrentCube.pos){
                // connection to same cube means perspective change
                tempCurrentPerspective = waypoint.perspective;
            } else {
                // connection to other cube means cube change
                tempCurrentCube = waypoint.cube;
                path.Enqueue(waypoint);
                //Debug.Log("added waypoint");
            }
            
            if(waypoint.cube.pos == goalCube.pos) {
                lastCube = true;
            }
        }
        //Debug.Log("Current Player Cube is on path of current cube, added " + path.Count + " path entries");
        TryMoveToNextWayPoint();
    }

    private void TryMoveToNextWayPoint(){
        if(linearMovement.running) return;
        if(path.Count == 0) return;
        InterPerspectiveWaypoint nextwayPoint = (InterPerspectiveWaypoint) path.Peek();
        Debug.Log("enemy trying to move from: " + currentCube.pos.ToString() + " to: " + nextwayPoint.cube.pos.ToString());

        if(nextwayPoint.perspective.id != grid.currentPerspective.id){
            // cannot take this waypoint now
            Debug.Log("enemy next waypoint is not reachable in current perspective: " + grid.currentPerspective.id );
            return; 
        }
        Debug.Log("enemy next waypoint is reachable in current perspective: " + grid.currentPerspective.id );
        nextwayPoint = (InterPerspectiveWaypoint) path.Dequeue();
        if(nextwayPoint.direction == null){
            Debug.Log("enemy move is null, not moving");
            return;
        }
        Debug.Log("enemy move direction: " + nextwayPoint.direction.id);
        MoveToCube(nextwayPoint.cube, nextwayPoint.direction);
    }

    private void MoveToCube(CubeScript cube, PlaneDirection direction){
        //check if connection is currently available in perspective
        //Debug.Log(cube.pos.ToString());
        //Debug.Log(direction.id);
        linearMovement.Move(
            gameObject.transform.position,
            gameObject.transform.position + new Vector3(direction.pos.x, 0, direction.pos.z) / 2,
            cube.gameObject.transform.position + new Vector3(0, 1, 0) - new Vector3(direction.pos.x, 0, direction.pos.z) / 2,
            cube.gameObject.transform.position + new Vector3(0, 1, 0));
    
        currentCube = cube;
        Debug.Log("Actually Moving in this direction");
    }
}
