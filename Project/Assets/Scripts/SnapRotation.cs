using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;

[DefaultExecutionOrder(-8)]
public class SnapRotation : MonoBehaviour
{

    public delegate void OnSettlingOnPerspectiveEvent(CamPerspective perspective);
    public event OnSettlingOnPerspectiveEvent OnSettlingOnPerspective;

    private float startAngle;
    private float endAngle;
    private float startEndAngleDiff;

    private CamPerspective perspective;

    public float velocity = 0F;
    public float minVelocity = 10F;
    public float maxVelocity = 360F;
    public float slowDownVelocity = 120F;
    public float drag = 20F;
    public float snapRange = 00.1F;
    public float regardVelocity = 0.5F;
    public float minAngleForAction = 0.01F;

    public bool running;
    public bool settling;
    public bool turning;
    public bool deceletaring;
    public bool accelerating;

    public void Initialize(float velocity)
    {   
        if(Mathf.Abs(velocity) < minAngleForAction){
            float currentEndAngle = PerspectiveCollection.GetClosestPerspective(transform.localEulerAngles.y).angle;
            if(Mathf.Abs(currentEndAngle - transform.localEulerAngles.y) < minAngleForAction){
                Debug.Log("Not enough movement for action ");
                return;
            }
        }
        

        Debug.Log("unclamped velocity input " + velocity);
        this.velocity = Mathf.Clamp(velocity, -maxVelocity, maxVelocity);
        Debug.Log("clamped velocity input " + this.velocity);
        running = true;

        if(Mathf.Abs(velocity) < slowDownVelocity){
            Debug.Log("Initial Velocity < min Velocity! initialize settling");
            InitializeSettling();
        } else {
            Debug.Log("Initial Velocity > min Velocity! slowing down first");
        }
    }

    private void SlowDown(){
        //Debug.Log("velocity: " + velocity);
        Rotate(velocity, false);
        if(velocity > 0){
            velocity -= drag;
            if(velocity < slowDownVelocity){
                velocity = slowDownVelocity;
                InitializeSettling();
            }
        } else {
            velocity += drag;
            if(velocity > -slowDownVelocity){
                velocity = -slowDownVelocity;
                InitializeSettling();
            }
        }
        
    }

    private void InitializeSettling(){
        settling = true;   
        startAngle = transform.localEulerAngles.y;
        perspective = PerspectiveCollection.GetClosestPerspective(transform.localEulerAngles.y + (velocity*regardVelocity));
        endAngle = perspective.angle;
        startEndAngleDiff = endAngle - startAngle;
        Debug.Log("From " + startAngle + " to " + endAngle + " is dist " + startEndAngleDiff);
        Debug.Log("velocity " + velocity);
        if(velocity == 0){
            Debug.Log("No Movement, need to accelerate");
            InitializeAcceleration();
        } else if((startEndAngleDiff > 0) == (velocity > 0)){
            Debug.Log("Need to decelerate");
            deceletaring = true;
        } else {
            Debug.Log("Currently moving in wrong direction, need to turn");
            turning = true;
        }
    }

    private void Turn(){
        //Debug.Log("turning velocity: " + velocity);
        Rotate(velocity, false);
        if(velocity >= 0){
            velocity -= drag;
            if(velocity < 0){
                InitializeAcceleration();
            }
        } else {
            velocity += drag;
            if(velocity > 0){
                InitializeAcceleration();
            }
        }
    }

    private void InitializeAcceleration(){
        turning = false;
        accelerating = true;
        startAngle = transform.localEulerAngles.y;
        startEndAngleDiff = endAngle - startAngle;
    }

    private void Accelerate(){
        //Debug.Log("velocity: " + velocity);
        //Debug.Log("accelerating fraction: " + GetFraction());
        if(startEndAngleDiff >= 0){
            velocity = Interpolate(slowDownVelocity, minVelocity, GetSpikeFunctionReturn(GetAbsFraction()));
            Rotate(velocity, false);
            if(GetAbsFraction() < 0.49 && transform.localEulerAngles.y > endAngle - snapRange){
                SetMotionDone();
            }
        } else {
            velocity = Interpolate(-slowDownVelocity, -minVelocity, GetSpikeFunctionReturn(GetAbsFraction()));
            Rotate(velocity, false);
            if(GetAbsFraction() < 0.49 && transform.localEulerAngles.y < endAngle + snapRange){
                SetMotionDone();
            }
        }
    }

    private void Decelerate(){
        //Debug.Log("decelerating fraction: " + GetAbsFraction());
        if(velocity >= 0){
            velocity = Interpolate(minVelocity, slowDownVelocity, GetAbsFraction());
            Rotate(velocity, false);
            if(GetAbsFraction() < 0.49 && transform.localEulerAngles.y > endAngle - snapRange){
                SetMotionDone();
            }
        } else {
            velocity = Interpolate(-minVelocity, -slowDownVelocity, GetAbsFraction());
            Rotate(velocity, false);
            if(GetAbsFraction() < 0.49 && transform.localEulerAngles.y < endAngle + snapRange){
                SetMotionDone();
            }
        }
    }

    private void SetMotionDone(){
        velocity = 0;
        SetYEulerAngleTo(endAngle);
        CancelEverything();
        if(OnSettlingOnPerspective != null) OnSettlingOnPerspective(perspective);
    }

    private void CancelEverything(){
        running = false;
        settling = false;
        turning = false;
        accelerating = false;
        deceletaring = false;
    }

    private float Interpolate(float a, float b, float fraction){
        return a * (1-fraction) + b * fraction;
    }


    private float GetAbsFraction(){
        return Mathf.Min(Mathf.Abs(transform.localEulerAngles.y - endAngle) / Mathf.Abs(startEndAngleDiff), 1);
    }

    private float GetFraction(){
        return Mathf.Min((endAngle - transform.localEulerAngles.y) / startEndAngleDiff, 1);
    }

    private float GetSpikeFunctionReturn(float x){
        if(x < 0.5F){
            return x;
        } else {
            return 1 - x;
        }
    }

    void Update()
    {
        if (!running) return;

        if (!settling) {
            SlowDown();
            return;
        }

        if(turning) {
            Turn();
            return;
        }

        if(accelerating){
            Accelerate();
            return;
        }
        
        if(deceletaring){
            Decelerate();
            return;
        }
    }

    public void Rotate(float amount, bool cancelOnGoingMovements)
    {
        if(cancelOnGoingMovements) CancelEverything();
        SetYEulerAngleTo(transform.eulerAngles.y + (Mathf.Clamp(amount, -maxVelocity, maxVelocity) * Time.deltaTime));
    }

    private void SetYEulerAngleTo(float y)
    {
        transform.eulerAngles = new Vector3(
             transform.eulerAngles.x,
             y,
             transform.eulerAngles.z
        );
    }
}
