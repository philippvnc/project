using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using direction;

public class PlayerController : MonoBehaviour
{
    
    public InputManager inputManager;
    public GridScript grid;

    void OnEnable(){
        inputManager.OnTab += SetGoal;
    }
    
    void OnDisable(){
        inputManager.OnTab -= SetGoal;
    }

    private void SetGoal(GameObject tabbedGameObject){
        PlaneDirection direction = grid.IsConnectedToCurrentCube(tabbedGameObject);
        if(direction != null){
            Debug.Log("Cube is neighbor of current cube");
            MoveToCube(tabbedGameObject.GetComponent<CubeScript>(), direction);
            grid.ColorCurrentCubeAndNeighbors();
        }
    }

    private void MoveToCube(CubeScript cube, PlaneDirection direction){
        if(CubeScript.IsHeigherCube(grid.currentCube, cube))
        {
            // moving down
            gameObject.GetComponent<LinearMovement>().Move(
                gameObject.transform.position,
                gameObject.transform.position + new Vector3(direction.pos.x, 0, direction.pos.z),
                cube.gameObject.transform.position + new Vector3(0, 1, 0));
        } else
        {
            // moving up
            gameObject.GetComponent<LinearMovement>().Move(
                cube.gameObject.transform.position + new Vector3(0, 1, 0) - new Vector3(direction.pos.x, 0, direction.pos.z),
                cube.gameObject.transform.position + new Vector3(0, 1, 0),
                cube.gameObject.transform.position + new Vector3(0, 1, 0));
        }

        grid.currentCube = cube; 
        Debug.Log("Actually Moved in this direction");
    }
    
    private void MoveInDirection(PlaneDirection direction)
    {
        if( grid.currentCube.connections[direction.id] == null)
        {
            Debug.Log("Can not move in this direction");
            return;
        } else
        {   
            MoveToCube(grid.currentCube.connections[direction.id], direction);
        }
    }
}
