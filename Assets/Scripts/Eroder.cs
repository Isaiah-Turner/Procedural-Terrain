using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Droplet {
	public Vector2 pos;
	public Vector2 correctedPos;
	public Vector2 direction;
	public float velocity;
	public float waterAmount;
	public float sedimentAmount;
	public float sedimentCapacity;
	public Droplet(float x, float y, float speed, float waterCapacity) {
		pos =  new Vector2(x,y);
		correctedPos = new Vector2((int)pos.x, (int)pos.y);
		direction = new Vector2(0f, 0f);
		velocity = speed;
		waterAmount = waterCapacity;
		sedimentAmount = 0f;
		sedimentCapacity = 0f;
	}
	public string ToString() {
		return string.Format("Position: ({0}, {1}) \n Direction: ({2}, {3}) \n Velocity: {4} \n Water Amount: {5} \n Sediment Amount/Capacity: {6}/{7}", pos.x, pos.y, direction.x, direction.y, velocity, waterAmount, sedimentAmount, sedimentCapacity);
	}
}

public class ErodeWeightAndPosition {
	public float weight;
	public int heightMapIndex;
	public ErodeWeightAndPosition(float w, int i) {
		weight = w;
		heightMapIndex = i;
	}
}

public class Eroder : MonoBehaviour
{
	public float waterCapacity = 1.0f;
	public float initialSpeed = 1.0f;
	public float inertia = 0.1f; //0 to 1
	public float minSlope = 0.05f;
	public float carryCapacity = 8.0f;
	public float depositionSpeed = 0.1f; //0 to 1
	public float erosionSpeed = 0.9f; //0 to 1
	public float evaporationSpeed = 0.01f; //0 to 1; if over 0.5 little changes
	public int erosionRadius = 3; //bad results if <3 and terrain is unblurred
	public int maxPathLength = 30;
	public float gravity = 4.0f;

	private float[] heightMap;
	private int size;
	// USE https://www.firespark.de/resources/downloads/implementation%20of%20a%20methode%20for%20hydraulic%20erosion.pdf
	public float[] erode(float[] hMap, int sizeOfMap) 
	{
		//Debug.unityLogger.logEnabled=false;
		size = sizeOfMap;
		heightMap = hMap;
		//Create water drop at random point 
		float xCoord = Random.Range(0.0f, (float)(size-1));
		float yCoord = Random.Range(0.0f, (float)(size-1));
		Droplet current = new Droplet(xCoord, yCoord, initialSpeed, waterCapacity);
		//Debug.Log(current.ToString());
		for(int time = 0; time < maxPathLength; time++) {
			//calc height and direction of flow

			updateDirection(current);
			//Debug.Log("direction updated " + current.ToString());
			float startHeight = getHeight(current);

			//update droplet position (move 1 unit)
			Vector2 oldPosition = current.pos;
			updatePosition(current);
			//Debug.Log("position updated " + current.ToString());
			if(current.pos.x >= size - 1 || current.pos.y >= size - 1 || current.pos.x < 0 || current.pos.y < 0 || (current.direction.x == 0 && current.direction.y == 0)){ //fell off map
				break;
			}
			//find new height and calc deltaHeight			
			float newHeight = getHeight(current);
			float deltaHeight = newHeight - startHeight;
			//Debug.Log("delta height " + deltaHeight);
			//calc sediment capacity
			if(deltaHeight < 0) { //moving down
				current.sedimentCapacity = Mathf.Max(-deltaHeight * current.velocity * current.waterAmount * carryCapacity, minSlope) ;
				//Debug.Log("sediment capacity updated " +current.ToString());
			} 
			
			//if drop has more sediment than capacity or is moving up a slope
				//drop some sediment
			if(deltaHeight > 0 || current.sedimentAmount > current.sedimentCapacity) {
				float deposeAmount = (current.sedimentAmount - current.sedimentCapacity) * depositionSpeed;
				if(deltaHeight > 0)
				{
					deposeAmount = Mathf.Min(deltaHeight, current.sedimentAmount);
				}
				depose(deposeAmount, oldPosition);
				current.sedimentAmount -= deposeAmount;
			}

			//else erode some of remaining capacity (not more than delta height)
			else {
				//Debug.LogWarning("eroding");
				float erodeAmount = Mathf.Min((current.sedimentCapacity - current.sedimentAmount) * erosionSpeed, -deltaHeight);
				erode(erodeAmount, oldPosition);
				current.sedimentAmount += erodeAmount;
			}

			//update speed based on delta height
			current.velocity = Mathf.Sqrt(current.velocity*current.velocity + deltaHeight*gravity);
			//Debug.Log("velo updated " +current.ToString());
			//evaporate some water
			current.waterAmount *= (1 - evaporationSpeed);
			//Debug.Log("water amount updated " +current.ToString());
		}
        float max = -100f;
        float min = 100f;
		foreach(float height in heightMap) 
		{
            	max = Mathf.Max(height, max);
                min = Mathf.Min(height, min);
		}
		//normalize(heightMap, min, max);
		return heightMap;
	} 
	public void erodeRadiusOne(float erodeAmount, Vector2 pos) {
		Vector2 corrected =  new Vector2((int)pos.x, (int)pos.y);
		Vector2 NEPosition = corrected + Vector2.one;
		Vector2 EPosition = corrected + Vector2.right;
		Vector2 NPosition = corrected + Vector2.up;

		int currentIndex = VectToInt(corrected);
		int NEIndex = VectToInt(NEPosition);
		int EIndex = VectToInt(EPosition);
		int NIndex = VectToInt(NPosition);

		float weightCurrent = Mathf.Max(0, erosionRadius - (corrected - pos).sqrMagnitude);
		float weightNE = Mathf.Max(0, erosionRadius - (NEPosition - pos).sqrMagnitude);
		float weightE = Mathf.Max(0, erosionRadius - (EPosition - pos).sqrMagnitude);
		float weightN = Mathf.Max(0, erosionRadius - (NPosition - pos).sqrMagnitude);
		float weightSum = weightCurrent + weightNE + weightE +weightN;
		weightCurrent /= weightSum;
		weightNE /= weightSum;
		weightE /= weightSum;
		weightN /= weightSum;
		/*Debug.Log("w/e " + weightE + " at index " + EIndex);
		Debug.Log("w/ne " + weightNE + " at index " + NEIndex);
		Debug.Log("w/n " + weightN + " at index " + NIndex);
		Debug.Log("w/cur " + weightCurrent + " at index " + currentIndex);*/
		heightMap[currentIndex] -= erodeAmount * weightCurrent;
		heightMap[NEIndex] -= erodeAmount * weightNE;
		heightMap[EIndex] -= erodeAmount * weightE;
		heightMap[NIndex] -= erodeAmount * weightN;
	}
	public void erode(float erodeAmount, Vector2 pos) {
		//Debug.Log(pos);
		//Debug.Log(erosionRadius);
		ErodeWeightAndPosition[] weights = new ErodeWeightAndPosition[4*erosionRadius*erosionRadius];
		int weightCounter = 0;
		float weightSum = 0;
		for(int displacementX = -erosionRadius; displacementX < erosionRadius; displacementX++) {
			for(int displacementY = -erosionRadius; displacementY < erosionRadius; displacementY++) {
				Vector2 adjusted = pos + new Vector2(displacementX, displacementY);
				Vector2 corrected = new Vector2((int)Mathf.Ceil(adjusted.x), (int)Mathf.Ceil(adjusted.y)); //gives the point to compare to pos
				float weight = Mathf.Max(0, erosionRadius - (corrected - pos).sqrMagnitude);
				int index = VectToInt(corrected);
				if(index < 0 || index >= heightMap.Length) {
					weights[weightCounter] = new ErodeWeightAndPosition(0, VectToInt(pos));
				}
				else {
					weights[weightCounter] = new ErodeWeightAndPosition(weight, index);
				}
				weightSum += weights[weightCounter].weight;
				weightCounter++;
			}
		}
		foreach(ErodeWeightAndPosition e in weights) {
			e.weight /= weightSum;
			heightMap[e.heightMapIndex] -= erodeAmount * e.weight;
		}

	}
	public void depose(float deposeAmount, Vector2 pos) {
		Vector2 corrected =  new Vector2((int)pos.x, (int)pos.y);
		int currentIndex = VectToInt(corrected);
		int NEIndex = VectToInt(corrected + Vector2.one);
		int EIndex = VectToInt(corrected + Vector2.right);
		int NIndex = VectToInt(corrected + Vector2.up);
		Vector2 offset = pos - corrected;
		//Debug.Log("at depose" + offset + " " + deposeAmount);
		heightMap[currentIndex] += deposeAmount * (1 - offset.x) * (1 - offset.y);
		heightMap[NEIndex] += deposeAmount * offset.x * offset.y;
		heightMap[EIndex] += deposeAmount * offset.x * (1 - offset.y);
		heightMap[NIndex] += deposeAmount * (1 -  offset.x) * offset.y;
	}
	public float getHeight(Droplet d) {
		//Debug.Log("in height " + d.ToString());
		int currentIndex = VectToInt(d.correctedPos);
		int NEIndex = VectToInt(d.correctedPos + Vector2.one);
		int EIndex = VectToInt(d.correctedPos + Vector2.right);
		int NIndex = VectToInt(d.correctedPos + Vector2.up);
		Vector2 offset = d.pos - d.correctedPos;
		//Debug.Log(currentIndex + " in height");
		float height = heightMap[currentIndex] * (1 - offset.x) * (1 - offset.y);
		//if(EIndex < heightMap.Length)
			height += heightMap[EIndex] * offset.x * (1 - offset.y);
		//if(NIndex < heightMap.Length)
			height += heightMap[NIndex] * (1 -  offset.x) * offset.y;
		//if(NEIndex < heightMap.Length)
			height += heightMap[NEIndex] * offset.x * offset.y;
		/*Debug.Log("e " + heightMap[EIndex] + " at index " + EIndex);
		Debug.Log("ne " + heightMap[NEIndex] + " at index " + NEIndex);
		Debug.Log("n " + heightMap[NIndex] + " at index " + NIndex);
		Debug.Log("cur " + heightMap[currentIndex] + " at index " + currentIndex);*/
		 
		return height;
	}

	public void updatePosition(Droplet d) {
		Vector2 oldPos = d.pos;
		d.pos = oldPos + d.direction;
		d.correctedPos = new Vector2((int)d.pos.x, (int)d.pos.y);

	}

	public void updateDirection(Droplet d) {
		int currentIndex = VectToInt(d.correctedPos);
		int NEIndex = VectToInt(d.correctedPos + Vector2.one);
		int EIndex = VectToInt(d.correctedPos + Vector2.right);
		int NIndex = VectToInt(d.correctedPos + Vector2.up);
		Vector2 offset = d.pos - d.correctedPos;
		//Debug.Log(currentIndex);
		//Debug.Log(NEIndex);
		//Debug.Log( d.ToString());
		/*Debug.Log("e " + heightMap[EIndex] + " at index " + EIndex);
		Debug.Log("ne " + heightMap[NEIndex] + " at index " + NEIndex);
		Debug.Log("n " + heightMap[NIndex] + " at index " + NIndex);
		Debug.Log("cur " + heightMap[currentIndex] + " at index " + currentIndex);*/
		float gradX = (heightMap[EIndex] - heightMap[currentIndex]) * (1-offset.y) + ( heightMap[NEIndex] - heightMap[NIndex]) * offset.y;


		float gradY = (heightMap[NIndex] - heightMap[currentIndex]) * (1-offset.x) + ( heightMap[NEIndex] - heightMap[EIndex]) * offset.x;
	
		Vector2 gradient =  new Vector2(gradX, gradY);
		//Debug.Log("offset: " + offset);
		//Debug.Log("gradient: " + gradient);
		Vector2 newDirection = d.direction * inertia - gradient * (1-inertia);
		newDirection.Normalize();
		//Debug.Log("new direction: " + newDirection);
		d.direction = newDirection;
		
	}

	public int VectToInt(Vector2 pos) {
		return (int)(pos.y*size + pos.x);
	}

    void normalize(float[] toNorm, float min, float max) {
        for(int i =0; i < heightMap.Length; i++) {
            heightMap[i] = (heightMap[i] - min) / (max - min);
        }
    }
}

