using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace pathfinding
{
    public class PathfindingResult{
        public int[,] successors;
        public int[,] costs;

        public PathfindingResult(int[,] successors, int[,] costs){
            this.successors = successors;
            this.costs = costs;
        }
    }

    public static class Pathfinding
    {
        public static PathfindingResult FloydWarshallSuccessors(int[,] initialSuccessors){
            int n = initialSuccessors.GetLength(0);
            int[,] successors = new int[n,n];
            int[,] costs = new int[n,n];
            //initialize costs and successors
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++) {
                    successors[i,j] = initialSuccessors[i,j];
                    if (i == j) {
                        costs[i,j] = 0;
                    } else if (successors[i,j] != -1){
                        costs[i,j] = 1;
                    }
                    else {
                        costs[i,j] = int.MaxValue;
                    }
                }
            }
            for (int k = 0; k < n; k++) {
                for (int i = 0; i < n; i++) {
                    for (int j = 0; j < n; j++) {
                        int costViaNodeK = addCosts(costs[i,k], costs[k,j]);
                        if (costViaNodeK < costs[i,j]) {
                            costs[i,j] = costViaNodeK;
                            successors[i,j] = successors[i,k];
                        }
                    }
                }
            }
            return new PathfindingResult(successors, costs);
        }

        private static int addCosts(int a, int b) {
            if (a == int.MaxValue || b == int.MaxValue) {
                return int.MaxValue;
            }
            return a + b;
        }
    } 
}

