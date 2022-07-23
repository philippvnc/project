using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using direction;

public class CubeScript : MonoBehaviour
{

    public Material unPlantedMaterial;
    public Material plantedMaterial;
    public Material plantableMaterial;
    public Material unreachableMaterial;

    public GridScript gridScript;
    public Position3 pos;
    public Position2[] projection;

    public bool planted;
    public CubeScript[,] connectionsArray;
    public List<CubeScript>[] connectionsList;
    public IDictionary[] connectionsDirectionDictionary;

    private List<GameObject> sides;

    public void Init()
    {
        Debug.Log("Cube Init");
        pos = new Position3(
            (int)transform.position.x,
            (int)transform.position.y,
            (int)transform.position.z);
        sides = new List<GameObject>();
        foreach(Transform tr in this.transform)
        {
                if(tr.tag == "Side")
                {
                    sides.Add(tr.gameObject);
                    Debug.Log("side added to cube");
                }
        }
        ResetConnections();
        CalculateProjections();
    }

    private void ResetConnections(){
        connectionsArray = new CubeScript[PerspectiveCollection.perspectiveDirections.Length,
            PlaneDirectionCollection.planeDirections.Length];
        connectionsList = new List<CubeScript>[PerspectiveCollection.perspectiveDirections.Length];
        connectionsDirectionDictionary = new Dictionary<CubeScript,PlaneDirection>[
            PerspectiveCollection.perspectiveDirections.Length]; 
        foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
            connectionsList[perspective.id] = new List<CubeScript>();
            connectionsDirectionDictionary[perspective.id] = new Dictionary<CubeScript,PlaneDirection>();
        }
    }

    private void SetSidesMaterial(Material material){
        foreach(GameObject side in sides)
        {
            Renderer[] children;
            children = side.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in children)
            {
                var mats = new Material[rend.materials.Length];
                for (var j = 0; j < rend.materials.Length; j++)
                {
                    mats[j] = material;
                }
                rend.materials = mats;
            }
        }
    }

    public void MarkPlanted(){
        planted = true;
        SetSidesMaterial(plantedMaterial);
    }

    public void MarkPlantable(){
        if (planted) return;
        SetSidesMaterial(plantableMaterial);
        Debug.Log("Marked Cube plantable " + pos.ToString());
    }

    public void MarkReachable(){
        if (planted) return;
        SetSidesMaterial(unPlantedMaterial);
        Debug.Log("Marked Cube reachable " + pos.ToString());
    }

    public void MarkUnreachable(){
        SetSidesMaterial(unreachableMaterial);
        Debug.Log("Marked Cube unreachable " + pos.ToString());
    }

    private void CalculateProjections()
    {
        projection = new Position2[PerspectiveCollection.perspectiveDirections.Length];
        foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
            projection[perspective.id] = Projection.Project(pos, perspective);    
        }
    }
/*
    void OnDrawGizmos()
    {
        if (connectionsList == null) return;
        Gizmos.color = Color.red;
        foreach(CubeScript cube in connectionsList)
        {
            Gizmos.DrawLine(transform.position, new Vector3(cube.pos.x, cube.pos.y, cube.pos.z));
        }
        
    }
    */


    public void UpdateConnectivity()
    {
        ResetConnections();
        foreach(CamPerspective perspective in PerspectiveCollection.perspectiveDirections){
            foreach (PlaneDirection planeDirection in PlaneDirectionCollection.planeDirections)
            {
                if(!IsPhysicallyBlocked(planeDirection)){
                    Position2 projectionToCheck = Projection.Project(
                        new Position3(pos.x + planeDirection.pos.x, pos.y, pos.z + planeDirection.pos.z), perspective);
                    foreach(CubeScript cube in gridScript.cubeList)
                    {
                        if (projectionToCheck.Equals(cube.projection[perspective.id]))
                        {
                            if(!IsOccluded(cube, perspective, planeDirection))
                            {
                                if(!IsVisuallyBocked(cube, perspective)){
                                    RegisterNeighborCube(cube, perspective, planeDirection);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void RegisterNeighborCube(CubeScript cube,CamPerspective perspective,PlaneDirection planeDirection){
        if(connectionsArray[perspective.id, planeDirection.id] != null)
        {
            Debug.Log("Found Overlapping connection in same direction! Choosing higher cube for connection");
            connectionsList[perspective.id].Remove(connectionsArray[perspective.id, planeDirection.id]);
            connectionsList[perspective.id].Add(GetHigherCube(cube, connectionsArray[perspective.id, planeDirection.id]));
            connectionsDirectionDictionary[perspective.id].Remove(connectionsArray[perspective.id, planeDirection.id]);
            connectionsDirectionDictionary[perspective.id][GetHigherCube(cube, connectionsArray[perspective.id, planeDirection.id])] = planeDirection;
            connectionsArray[perspective.id, planeDirection.id] = GetHigherCube(cube, connectionsArray[perspective.id, planeDirection.id]);
            //gameObject.GetComponent<Renderer>().material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        } else
        {
            connectionsArray[perspective.id, planeDirection.id] = cube;
            connectionsList[perspective.id].Add(cube);
            connectionsDirectionDictionary[perspective.id][cube] = planeDirection;
        }
    }

    private static CubeScript GetHigherCube(CubeScript cube1, CubeScript cube2)
    {
        if (IsHeigherCube(cube1,cube2))
        {
            return cube1;
        } else
        {
            return cube2;
        }
    }

    public static bool IsEqualHeigh(CubeScript cube1, CubeScript cube2)
    {
        return (cube1.gameObject.transform.position.y == cube2.gameObject.transform.position.y);
    }

    public static bool IsHeigherCube(CubeScript cube1, CubeScript cube2)
    {
        return (cube1.gameObject.transform.position.y > cube2.gameObject.transform.position.y);
    }

    private bool IsOccluded(CubeScript cube, CamPerspective perspective, PlaneDirection direction){
        if (IsEqualHeigh(cube, this)) return false;
        return IsHeigherCube(cube, gameObject.GetComponent<CubeScript>()) == Occlusion.OccludedWhenHeigher[perspective.id, direction.id];
    }

    private bool IsPhysicallyBlocked(PlaneDirection direction){
        if (gridScript.IsOutOfGrid(pos.x + direction.pos.x, pos.y + 1, pos.z + direction.pos.z)) return false;
        return gridScript.cubeArray[pos.x + direction.pos.x, pos.y + 1, pos.z + direction.pos.z];
    }

    private bool IsVisuallyBocked(CubeScript cube, CamPerspective perspective){
        CubeScript higherCube = GetHigherCube(this, cube);
        return gridScript.IsCubeAtProjection(perspective,
        Projection.Project(   
            new Position3(
                higherCube.pos.x + perspective.viewDirection.pos.x, 
                higherCube.pos.y, 
                higherCube.pos.z + perspective.viewDirection.pos.z),
            perspective),
        Mathf.Min(this.pos.y, cube.pos.y) + 1, 
        Mathf.Max(this.pos.y, cube.pos.y));
    }
}
