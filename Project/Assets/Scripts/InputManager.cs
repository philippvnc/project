using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using direction;

[DefaultExecutionOrder(-10)]
public class InputManager : MonoBehaviour
{
    public delegate void StartTouchEvent(Vector2 position);
    public event StartTouchEvent OnStartTouch;

    public delegate void EndMoveEvent(Vector2 delta);
    public event EndMoveEvent OnMoveTouch;

    public delegate void EndTouchEvent(Vector2 position);
    public event EndTouchEvent OnEndTouch;

    public delegate void TabEvent(GameObject tabbedObject);
    public event TabEvent OnTab;

    private InputActions inputActions;
    private InputAction north;
    private InputAction east;
    private InputAction south;
    private InputAction west;
    private InputAction touchInput;
    private InputAction touchPress;
    private InputAction touchPosition;
    private InputAction touchDelta;
    private InputAction touchTab;

    private bool tracingTouch;

    private Camera mainCamera;
    
    void Awake()
    {
        inputActions = new InputActions();
    }

    void Start()
    {
        north.performed += ctx => North(ctx);
        east.performed += ctx => East(ctx);
        south.performed += ctx => South(ctx);
        west.performed += ctx => West(ctx);

        touchPress.started += ctx => StartTouch(ctx);
        touchPress.canceled += ctx => EndTouch(ctx);

        touchTab.performed += ctx => Tab(ctx);

        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!tracingTouch) return;
        //Debug.Log("tracing Touch " + touchDelta.ReadValue<Vector2>());
        if(OnMoveTouch != null) OnMoveTouch(GetRelativeScreenPosition(touchDelta.ReadValue<Vector2>()));
    }

    void OnEnable()
    {
        north = inputActions.Player.North;
        east = inputActions.Player.East;
        south = inputActions.Player.South;
        west = inputActions.Player.West;
        touchInput = inputActions.Touch.TouchInput;
        touchPress = inputActions.Touch.TouchPress;
        touchPosition = inputActions.Touch.TouchPosition;
        touchDelta = inputActions.Touch.TouchDelta;
        touchTab = inputActions.Touch.TouchTab;
        inputActions.Enable();

    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    public void North(InputAction.CallbackContext context)
    {
        Debug.Log("MOVING North");
        //MoveInDirection(new PlaneDirection(PlaneDirection.NORTH));
    }

    public void East(InputAction.CallbackContext context)
    {
        Debug.Log("MOVING East");
        //MoveInDirection(new PlaneDirection(PlaneDirection.EAST));
    }

    public void South(InputAction.CallbackContext context)
    {
        Debug.Log("MOVING South");
        //MoveInDirection(new PlaneDirection(PlaneDirection.SOUTH));
    }

    public void West(InputAction.CallbackContext context)
    {
        Debug.Log("MOVING West");
        //MoveInDirection(new PlaneDirection(PlaneDirection.WEST));
    }

    public void StartTouch(InputAction.CallbackContext context)
    {
        //Debug.Log("Touch started " + touchPosition.ReadValue<Vector2>());
        if(OnStartTouch != null) OnStartTouch(GetRelativeScreenPosition(touchPosition.ReadValue<Vector2>()));
        tracingTouch = true;
    }

    public void EndTouch(InputAction.CallbackContext context)
    {
        //Debug.Log("Touch ended" + touchPosition.ReadValue<Vector2>());
        if(OnEndTouch != null) OnEndTouch(GetRelativeScreenPosition(touchPosition.ReadValue<Vector2>()));
        tracingTouch = false;
    }

    private Vector2 GetRelativeScreenPosition(Vector2 screenPosition){
        return new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
    }

    public void Tab(InputAction.CallbackContext context){
        Debug.Log("Tab " + touchPosition.ReadValue<Vector2>());
         if(OnTab != null) {
            GameObject tabbedGameObject = GetObjectAtTab(touchPosition.ReadValue<Vector2>());
            if(tabbedGameObject != null){
                Debug.Log("Raycast hit " + tabbedGameObject.transform.name);
                OnTab(tabbedGameObject);
            }
         }
    }

    private GameObject GetObjectAtTab(Vector2 screenPos){
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        Debug.DrawRay(ray.origin, ray.direction*1000, Color.red, 10, true);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            ////Logger.Log("RAYCAST HIT " + hit.transform.name + " ON " + hit.point, this, true);
            //Debug.DrawRay(hit.transform.position, Vector3.up * hit.distance, Color.red, 10, false);
            return hit.transform.gameObject;
        }
        return null;
    }

}
