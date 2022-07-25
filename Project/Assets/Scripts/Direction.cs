using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace direction
{

    public class Position3 : Position2
    {
        public int y;

        public Position3(int x, int y, int z) : base(x, z)
        {
            this.y = y;
        }

        new public string ToString(){
            return "x: " + x + " y: " + y + " z: " + z;  
        }

        public override bool Equals(object obj)  
        {  
            if (obj == null)  
                return false;  
   
            if (ReferenceEquals(obj, this))  
                return true;  
   
            if (obj.GetType() != this.GetType())  
                return false;  
   
            Position3 positionToCheck = obj as Position3;  
   
            return this.x == positionToCheck.x  
                && this.y == positionToCheck.y  
                && this.z == positionToCheck.z;  
        }  

        public override int GetHashCode()  
        {  
            return this.x.GetHashCode()  
                 ^ this.y.GetHashCode()  
                 ^ this.z.GetHashCode();  
        } 
    }

    public class Position2
    {
        public int x;
        public int z;

        public Position2(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        new public string ToString(){
            return "x: " + x + " z: " + z;  
        }

        public bool Equals(Position2 position2)
        {
            if(position2 is null)
            {
                return false;
            }
            return (x == position2.x) && (z == position2.z);
        }
    }

    public static class Projection
    {
        public static Position2 Project(Position3 pos, CamPerspective perspective)
        {
            switch (perspective.id)
            {
                case (CamPerspective.SOUTH_EAST):
                    return new Position2(pos.x - pos.y, pos.z + pos.y);
                case (CamPerspective.SOUTH_WEST):
                    return new Position2(pos.x + pos.y, pos.z + pos.y);
                case (CamPerspective.NORTH_WEST):
                    return new Position2(pos.x + pos.y, pos.z - pos.y);
                case (CamPerspective.NORTH_EAST):
                default:
                    return new Position2(pos.x - pos.y, pos.z - pos.y);
            }
        }
        
        public static Position3 GetShiftToHeigh(Position3 pos, CamPerspective perspective, int y)
        {
            int diff = pos.y - y;
            switch (perspective.id)
            {
                case (CamPerspective.SOUTH_EAST):
                    return new Position3(pos.x - diff, y, pos.z + diff);
                case (CamPerspective.SOUTH_WEST):
                    return new Position3(pos.x + diff, y, pos.z + diff);
                case (CamPerspective.NORTH_WEST):
                    return new Position3(pos.x + diff, y, pos.z - diff);
                case (CamPerspective.NORTH_EAST):
                default:
                    return new Position3(pos.x - diff, y, pos.z - diff);
            }
        }

        public static List<Position3> GetAllShiftsToHeigh(Position3 pos, CamPerspective perspective, int fromY, int toY)
        {
            List<Position3> shifts = new List<Position3>();
            for(int y = fromY; y < toY; y++){
                shifts.Add(GetShiftToHeigh(pos,perspective,y));
            }
            return shifts;
        }
    }

    public static class Occlusion
    {
        /*
        dim1: perspective
        SE
        SW
        NW
        NE
        dim2: direction 
        {N E S W}
        */
        public static bool[,] OccludedWhenHeigher = new bool[4,4]{
            {true, false, false, true},
            {true, true, false, false},
            {false, true, true, false},
            {false, false, true, true}};
    }

    public static class PerspectiveCollection
    {
        public static CamPerspective[] perspectiveDirections = new CamPerspective[4] {
            new CamPerspective(CamPerspective.SOUTH_EAST),
            new CamPerspective(CamPerspective.SOUTH_WEST),
            new CamPerspective(CamPerspective.NORTH_WEST),
            new CamPerspective(CamPerspective.NORTH_EAST)};

        public static CamPerspective GetClosestPerspective(float angle)
        {
            CamPerspective bestPers = null;
            float bestDist = float.MaxValue; 
            foreach(CamPerspective pers in perspectiveDirections)
            {
               if (Mathf.Abs(pers.angle - (int) angle) < bestDist)
                {
                    bestDist = Mathf.Abs(pers.angle - (int) angle);
                    bestPers = pers;
                } 
            }
            //Debug.Log("best perspective: " + bestPers.id + " " + bestPers.angle);
            return bestPers;
        }
    }

    public class CamPerspective
    {
        public const int SOUTH_EAST = 0;
        public const int SOUTH_WEST = 1;
        public const int NORTH_WEST = 2;
        public const int NORTH_EAST = 3;

        public int id;
        public int angle;
        public PlaneDirection viewDirection;

        public CamPerspective(int perspectiveId)
        {
            id = perspectiveId;
            switch (perspectiveId)
            {
                case SOUTH_EAST:
                    angle = 45;
                    viewDirection = new PlaneDirection(PlaneDirection.NORTH_WEST);
                    break;
                case SOUTH_WEST:
                    angle = 135;
                    viewDirection = new PlaneDirection(PlaneDirection.NORTH_EAST);
                    break;
                case NORTH_WEST:
                    angle = 225;
                    viewDirection = new PlaneDirection(PlaneDirection.SOUTH_EAST);
                    break;
                case NORTH_EAST:
                    angle = 315;
                    viewDirection = new PlaneDirection(PlaneDirection.SOUTH_WEST);
                    break;
            }
        }
    }

    public static class PlaneDirectionCollection
    {
        public static PlaneDirection[] planeDirections = new PlaneDirection[4] {
            new PlaneDirection(PlaneDirection.NORTH), 
            new PlaneDirection(PlaneDirection.EAST), 
            new PlaneDirection(PlaneDirection.SOUTH), 
            new PlaneDirection(PlaneDirection.WEST)};
    }

    public class PlaneDirection
    {
        public const int NORTH = 0;
        public const int EAST = 1;
        public const int SOUTH = 2;
        public const int WEST = 3;

        public const int SOUTH_EAST = 4;
        public const int SOUTH_WEST = 5;
        public const int NORTH_WEST = 6;
        public const int NORTH_EAST = 7;

        public int id;
        public Position2 pos;

        public PlaneDirection(int planeDirectionId)
        {
            id = planeDirectionId;
            switch (planeDirectionId)
            {
                case NORTH:
                    pos = new Position2(0, 1);
                    break;
                case NORTH_EAST:
                    pos = new Position2(1, 1);
                    break;
                case EAST:
                    pos = new Position2(1, 0);
                    break;
                case SOUTH_EAST:
                    pos = new Position2(1, -1);
                    break;
                case SOUTH:
                    pos = new Position2(0, -1);
                    break;
                case SOUTH_WEST:
                    pos = new Position2(-1, -1);
                    break;
                case WEST:
                    pos = new Position2(-1, 0);
                    break;
                case NORTH_WEST:
                    pos = new Position2(-1, 1);
                    break;
                
            }
        }
    }
}
