using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using direction;

public class PlayerController : MonoBehaviour
{
    private InputActions inputActions;
    private InputAction north;
    private InputAction east;
    private InputAction south;
    private InputAction west;

    public GridScript grid;
    
    // Start is called before the first frame update
    void Awake()
    {
        inputActions = new InputActions();
    }

    // Update is called once per frame
    
    void OnEnable()
    {
        north = inputActions.Player.North;
        east = inputActions.Player.East;
        south = inputActions.Player.South;
        west = inputActions.Player.West;
        inputActions.Enable();

        north.performed += North;
        east.performed += East;
        south.performed += South;
        west.performed += West;
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    public void North(InputAction.CallbackContext context)
    {
        Debug.Log("MOVING North");
        MoveInDirection(new PlaneDirection(PlaneDirection.NORTH));
    }

    public void East(InputAction.CallbackContext context)
    {
        Debug.Log("MOVING East");
        MoveInDirection(new PlaneDirection(PlaneDirection.EAST));
    }

    public void South(InputAction.CallbackContext context)
    {
        Debug.Log("MOVING South");
        MoveInDirection(new PlaneDirection(PlaneDirection.SOUTH));
    }

    public void West(InputAction.CallbackContext context)
    {
        Debug.Log("MOVING West");
        MoveInDirection(new PlaneDirection(PlaneDirection.WEST));
    }

    private void MoveInDirection(PlaneDirection direction)
    {
        if( grid.currentCube.connections[direction.id] == null)
        {
            Debug.Log("Can not move in this direction");
            return;
        } else
        {   
            if(CubeScript.IsHeigherCube(grid.currentCube, grid.currentCube.connections[direction.id]))
            {
                // moving down
                gameObject.GetComponent<LinearMovement>().Move(
                    gameObject.transform.position,
                    gameObject.transform.position + new Vector3(direction.pos.x, 0, direction.pos.z),
                    grid.currentCube.connections[direction.id].gameObject.transform.position + new Vector3(0, 1, 0));
            } else
            {
                // moving up
                gameObject.GetComponent<LinearMovement>().Move(
                    grid.currentCube.connections[direction.id].gameObject.transform.position + new Vector3(0, 1, 0) - new Vector3(direction.pos.x, 0, direction.pos.z),
                    grid.currentCube.connections[direction.id].gameObject.transform.position + new Vector3(0, 1, 0),
                    grid.currentCube.connections[direction.id].gameObject.transform.position + new Vector3(0, 1, 0));
            }

            

            grid.currentCube = grid.currentCube.connections[direction.id]; 

            Debug.Log("Actually Moved in this direction");
        }
    }
}
