using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;

[DefaultExecutionOrder(-8)]
public class CameraController : MonoBehaviour
{

    public InputManager inputManager;
    public GridScript grid;
    private SnapRotation snapRotation;

    private float directTouchVelocity = 0.0F;
    public float touchAngleFactor = 90;

    void OnEnable(){ 
        snapRotation = gameObject.GetComponent<SnapRotation>();
        inputManager.OnStartTouch += StartTouch;
        inputManager.OnMoveTouch += MoveTouch;
        inputManager.OnEndTouch += EndTouch;
        snapRotation.OnSettlingOnPerspective += UpdatePerspective;
    }
    
    void OnDisable(){
        inputManager.OnStartTouch -= StartTouch;
        inputManager.OnMoveTouch -= MoveTouch;
        inputManager.OnEndTouch -= EndTouch;
        snapRotation.OnSettlingOnPerspective -= UpdatePerspective;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Cam Start");
    }

    public void StartTouch(Vector2 screenPosition){
        Debug.Log("Started touch " + screenPosition.ToString());
    }

    public void MoveTouch(Vector2 screenDelta){
        //Debug.Log("Moved touch " + screenDelta.ToString());
        directTouchVelocity = (touchAngleFactor * screenDelta.x) / Time.deltaTime;
        snapRotation.Rotate(directTouchVelocity, true); // counter rotation slowdown
    }

    public void EndTouch(Vector2 screenPosition){
        Debug.Log("Ended touch " + screenPosition.ToString());
        snapRotation.Initialize(directTouchVelocity);
    }
   
    public void UpdatePerspective(CamPerspective perspective)
    {
        if (perspective.id != grid.currentPerspective.id)
        {
            grid.SetPerspective(perspective);
        }
    }
}
