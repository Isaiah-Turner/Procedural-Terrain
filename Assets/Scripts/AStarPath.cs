using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class AStarPath
{
	public class Location {
		public int x;
		public int y;
		public float fPrime;
		public int steps;
		public Location parent;
		public Location(Vector2 point) {
			this.x = (int)point.x;
			this.y = (int)point.y;
			this.fPrime = 0;
			this.steps = 0;
		}
		public Location(Vector2 point, float fPrime, int steps) {
			this.x = (int)point.x;
			this.y = (int)point.y;
			this.fPrime = fPrime;
			this.steps = steps;
		}
	}
	public static List<Vector2> findPath(Vector2 start, Vector2 goal, int gridWidth) { //will need heightMap to find slopes
		Location startLocation = new Location(start);
		Location goalLocation = new Location(goal);
		List<Location> frontier = new List<Location>();
		frontier.Add(startLocation);
		List<Location> interior = new List<Location>();
		List<Vector2> path = new List<Vector2>();
		Location current = startLocation;
		int step = 0;
		while(frontier.Count > 0) {
			var lowest = frontier.Min(l => l.fPrime);
			current = frontier.First(l => l.fPrime == lowest);
			interior.Add(current);
			frontier.Remove(current);
			if(interior.FirstOrDefault(l => goalLocation.x == l.x && goalLocation.y == l.y) != null) {
				break;
			}
			List<int> neighbors = getGridNeighbors(current.x+current.y*gridWidth, gridWidth);
			//Debug.Log("finding neighbors");
			step++;
			foreach(int neighborIndex in neighbors) {
				Location neighborLocation = new Location(intToVector(neighborIndex, gridWidth));
				if(interior.FirstOrDefault(l => neighborLocation.x == l.x && neighborLocation.y == l.y) != null) { //if neighbor is already in interior, ignore
					continue;
				}
				if(frontier.FirstOrDefault(l => neighborLocation.x == l.x && neighborLocation.y == l.y) == null) { //if neighbor is not in frontier, keep going				continue;
					neighborLocation.steps = step;
					neighborLocation.fPrime = manhattanDistanceHeuristic(neighborLocation, goalLocation) + neighborLocation.steps;
					neighborLocation.parent = current;
					frontier.Insert(0, neighborLocation);
				}
				else { //if in frontier already, check to see if this path is better
					if(step + neighborLocation.fPrime - neighborLocation.steps < neighborLocation.fPrime) {
						neighborLocation.steps = step;
						neighborLocation.fPrime = manhattanDistanceHeuristic(neighborLocation, goalLocation) + neighborLocation.steps;
						neighborLocation.parent = current;
					}
				}
			}
		}
		while(current != null) { //reconstruct path
			path.Insert(0, new Vector2(current.x, current.y));
			current = current.parent;
		}	
		return path;
	}
	public static Vector2 intToVector(int index, int gridWidth) {
		return new Vector2(index % gridWidth, index / gridWidth);
	}
	public static List<int> getGridNeighbors(int startingIndex, int gridWidth) { //% gives col, / gives row
        //% gives col, / gives row
        List<int> neighbors = new List<int>();
        int arrayLength = gridWidth * gridWidth;
        if(startingIndex - 1 >= 0 && (startingIndex / gridWidth == (startingIndex - 1) / gridWidth)) { //left
            neighbors.Add(startingIndex - 1);
        }
        if(startingIndex + 1 < arrayLength && (startingIndex / gridWidth == (startingIndex + 1) / gridWidth)) { //right
            neighbors.Add(startingIndex + 1);
        }
        if(startingIndex - gridWidth >= 0) { //up
            neighbors.Add(startingIndex - gridWidth);
        }
        if(startingIndex + gridWidth < arrayLength) { //down
            neighbors.Add(startingIndex + gridWidth);
        }
        if(startingIndex - gridWidth + 1 >= 0 && (startingIndex % gridWidth - (startingIndex - gridWidth + 1) % gridWidth == -1)) { //top right
            neighbors.Add(startingIndex - gridWidth + 1);
        }
        if(startingIndex - gridWidth - 1 >= 0 && (startingIndex % gridWidth - (startingIndex - gridWidth - 1) % gridWidth == 1)) { //top left
            neighbors.Add(startingIndex - gridWidth - 1);
        }
        if(startingIndex + gridWidth  + 1 < arrayLength && (startingIndex % gridWidth - (startingIndex + gridWidth + 1) % gridWidth == -1)) { //bottom right
            neighbors.Add(startingIndex + gridWidth + 1);
        }
        if(startingIndex + gridWidth - 1 < arrayLength && (startingIndex % gridWidth - (startingIndex + gridWidth - 1) % gridWidth == 1)) { //bottom left
            neighbors.Add(startingIndex + gridWidth - 1);
        }
        return neighbors;
    }

	public static float manhattanDistanceHeuristic(Location a, Location b) {
		return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
	}
    public static float manhattanDistanceHeuristic(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
