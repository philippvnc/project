using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using direction;

public class PlayerController : MonoBehaviour
{
    
    public InputManager inputManager;
    public GridScript grid;

    private LinearMovement LinearMovement;

    private Queue path;

    void OnEnable(){
        LinearMovement = gameObject.GetComponent<LinearMovement>();
        inputManager.OnTab += SetPath;
        LinearMovement.OnWayPointReached += TryMoveToNextWayPoint;
    }
    
    void OnDisable(){
        inputManager.OnTab -= SetPath;
        LinearMovement.OnWayPointReached -= TryMoveToNextWayPoint;
    }

    void Start(){
        path = new Queue();
    }

    private void SetPath(GameObject tabbedGameObject){
        CubeScript tabbedCube = tabbedGameObject.GetComponent<CubeScript>();

        if(tabbedCube == null){
            Debug.Log("Tabbed Object is not a Cube");
            return;
        }

        path = new Queue();
        CubeScript tempCurrentCube = grid.currentCube;
        bool lastCube = false;
        while(!lastCube){
            CubeScript successorCube = grid.GetSuccessorOnPath(tempCurrentCube, tabbedCube);
            if(successorCube == null){
                Debug.Log("Tabbed Cube is not on a path with current cube");
                return;
            } else {
                path.Enqueue(successorCube);
                tempCurrentCube = successorCube;
                if(successorCube.pos == tabbedCube.pos) {
                    lastCube = true;
                }
            }
        }
        
        Debug.Log("Tabbed Cube is on path of current cube, added " + path.Count + " path entries");
        TryMoveToNextWayPoint();
    
    }

    private void TryMoveToNextWayPoint(){
        if(LinearMovement.running) return;
        if(path.Count == 0) return;
        CubeScript nextCube = (CubeScript) path.Dequeue();

        MoveToCube(nextCube, (PlaneDirection) grid.currentCube.connectionsDirectionDictionary
            [grid.currentPerspective.id][nextCube]);
    }

    private void MoveToCube(CubeScript cube, PlaneDirection direction){
        //check if connection is currently available in perspective
        gameObject.GetComponent<LinearMovement>().Move(
            gameObject.transform.position,
            gameObject.transform.position + new Vector3(direction.pos.x, 0, direction.pos.z) / 2,
            cube.gameObject.transform.position + new Vector3(0, 1, 0) - new Vector3(direction.pos.x, 0, direction.pos.z) / 2,
            cube.gameObject.transform.position + new Vector3(0, 1, 0));
    
        grid.SetCurrentCube(cube);
        Debug.Log("Actually Moving in this direction");
    }
    
    private void MoveInDirection(PlaneDirection direction)
    {
        if( grid.currentCube.connectionsArray[grid.currentPerspective.id, direction.id] == null)
        {
            Debug.Log("Can not move in this direction");
            return;
        } else
        {   
            MoveToCube(grid.currentCube.connectionsArray[grid.currentPerspective.id, direction.id], direction);
        }
    }
}
