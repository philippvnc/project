using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;

[DefaultExecutionOrder(-8)]
public class CameraController : MonoBehaviour
{

    public InputManager inputManager;
    public GridScript grid;
    public CamPerspective perspective;
    private LinearRotation linearRotation;

    public float touchAngleFactor = 90;
    private float momentum = 1;

    void OnEnable(){
        inputManager.OnStartTouch += StartTouch;
        inputManager.OnMoveTouch += MoveTouch;
        inputManager.OnEndTouch += EndTouch;
    }
    
    void OnDisable(){
        inputManager.OnStartTouch -= StartTouch;
        inputManager.OnMoveTouch -= MoveTouch;
        inputManager.OnEndTouch -= EndTouch;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Cam Start");
        perspective = PerspectiveCollection.GetClosestPerspective(transform.localEulerAngles.y); 
        linearRotation = gameObject.GetComponent<LinearRotation>();
        grid.UpdateConnectivityForAllCubes(perspective);
    }

    public void StartTouch(Vector2 screenPosition){
        Debug.Log("Started touch " + screenPosition.ToString());
    }

    public void MoveTouch(Vector2 screenDelta){
        //Debug.Log("Moved touch " + screenDelta.ToString());
        Rotate(touchAngleFactor * screenDelta.x);
    }

    public void EndTouch(Vector2 screenPosition){
        Debug.Log("Ended touch " + screenPosition.ToString());
        RotateToClosestPerspective();
    }
   
    public void CalculatePerspective()
    {
        CamPerspective pers = PerspectiveCollection.GetClosestPerspective(transform.localEulerAngles.y);
        //print("best perspective: " + pers.id + " " + pers.angle);
        if (perspective.id != pers.id)
        {
            perspective = pers;
            grid.UpdateConnectivityForAllCubes(perspective);
            print("Updated all connectivities");
        }
    }

    public void Rotate(float amount)
    {
        SetYEulerAngleTo(gameObject.transform.eulerAngles.y + amount);
        CalculatePerspective();
        momentum = amount;
    }

    private void SetYEulerAngleTo(float y)
    {
        gameObject.transform.eulerAngles = new Vector3(
             gameObject.transform.eulerAngles.x,
             y,
             gameObject.transform.eulerAngles.z
        );
    }

    public void RotateToClosestPerspective()
    {
        Debug.Log("Rotating camera anker to " + perspective.angle);
        linearRotation.currentSpeed = Mathf.Max(momentum, linearRotation.minSpeed);
        linearRotation.Rotate(gameObject.transform.eulerAngles.y, perspective.angle);
    }
}
