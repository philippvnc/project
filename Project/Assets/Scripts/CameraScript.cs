using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;

public class CameraScript : MonoBehaviour
{

    public GridScript grid;
    public CamPerspective perspective;

    // Start is called before the first frame update
    void Start()
    {
        perspective = PerspectiveCollection.GetClosestPerspective(transform.localEulerAngles.y); 
        grid.UpdateConnectivityForAllCubes(perspective);
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged)
        {
            //print("The transform has changed!");
            transform.hasChanged = false;
            CamPerspective pers = PerspectiveCollection.GetClosestPerspective(transform.localEulerAngles.y);
            //print("best perspective: " + pers.id + " " + pers.angle);
            if(perspective.id != pers.id)
            {
                perspective = pers;
                grid.UpdateConnectivityForAllCubes(perspective);
                print("Updated all connectivities");
            }
        }
    }
}
