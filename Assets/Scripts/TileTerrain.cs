using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileTerrain : MonoBehaviour
{
	public int tileDepth, tileWidth;
	private int chunkSize;
    // Start is called before the first frame update
    void Start()
    {
    	chunkSize = TerrainGenerator.chunkSize - 1;
    	for(int tileY = (int)Mathf.Floor(-tileDepth/2); tileY < Mathf.RoundToInt((float)tileDepth/2.0f); tileY++) 
    	{
    		for(int tileX = (int)Mathf.Floor(-tileWidth/2); tileX < Mathf.RoundToInt((float)tileWidth/2.0f); tileX++) {
    			Debug.Log(tileX + " " + tileY);
    			Vector2 tilePos = new Vector2(tileX*chunkSize, tileY*chunkSize);
    			Debug.Log(tilePos);
    			TerrainGenerator generator = new TerrainGenerator();
				generator.BuildErodedHeightMap(tilePos, 0);
				//Debug.Log(generator.heightMap[73]);
				GameObject terrain = generator.generateTerrain();
				terrain.transform.position = new Vector3(tilePos.x, 0, tilePos.y)*2;
				terrain.transform.SetParent(this.transform);
    		}
    	}
		float waterLevel =  0.3f*(float)TerrainGenerator.heightMultiplier; //.3 good for continents; .4 good otherwise
		FindObjectOfType<WaterController>().adjustPositionAndSize(Mathf.Max(tileDepth + 1, tileWidth + 1)*chunkSize*1.414f, waterLevel);
    }

    // Update is called once per frame
    void Update() {

    }
}