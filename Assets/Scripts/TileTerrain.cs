using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileTerrain : MonoBehaviour
{
	public int tileDepth, tileWidth;
	private int chunkSize;
	private float[] fullHeightMap;
	public int erosionIterations = 20000;
	public GameObject tree;
	public const int heightMultiplier = 100;
    // Start is called before the first frame update
    void Start()
    {
    	chunkSize = TerrainGenerator.chunkSize;
    	Vector2 initialPos = new Vector2((int)Mathf.Floor(-tileWidth/2)*chunkSize, (int)Mathf.Floor(-tileDepth/2)*chunkSize);
    	int totalSize = chunkSize * Mathf.Max(tileDepth, tileWidth);
    	fullHeightMap = FindObjectOfType<NoiseCreator>().GenerateHeightMap(totalSize, false, initialPos);
		for(int dropAmount = 0; dropAmount < erosionIterations; dropAmount++) {
				fullHeightMap = FindObjectOfType<Eroder>().erode(fullHeightMap, totalSize);
		}	

		int currentY = 0;
    	for(int tileY = (int)Mathf.Floor(-tileDepth/2); tileY < Mathf.RoundToInt((float)tileDepth/2.0f); tileY++) 
    	{
    		int currentX = 0;
    		for(int tileX = (int)Mathf.Floor(-tileWidth/2); tileX < Mathf.RoundToInt((float)tileWidth/2.0f); tileX++) {
    			Debug.Log(tileX + " " + tileY);
    			Vector2 tilePos = new Vector2(tileX*(chunkSize-1), tileY*(chunkSize-1));
    			Debug.Log(tilePos);
    			TerrainGenerator generator = new TerrainGenerator();
    			TerrainGenerator.heightMultiplier = heightMultiplier;
				//generator.BuildErodedHeightMap(tilePos, 0);
				Debug.Log(chunkSize*currentX + " " + chunkSize*currentY);
				generator.setHeightMapFromReference(fullHeightMap, chunkSize*currentX, chunkSize*currentY);
				GameObject terrain = generator.generateTerrain();
				terrain.transform.position = new Vector3(tilePos.x, 0, tilePos.y)*2;
				terrain.transform.SetParent(this.transform);
				currentX += 1;
				generator.scatterObject(1.0f, tree);
				Debug.Log("TILE FINISHED");
				//break;
    		}
    		currentY += 1;
    		//break;
    	}
		float waterLevel =  0.3f*(float)TerrainGenerator.heightMultiplier; //.3 good for continents; .4 good otherwise
		FindObjectOfType<WaterController>().adjustPositionAndSize(Mathf.Max(tileDepth + 1, tileWidth + 1)*chunkSize*1.414f, waterLevel);
    }
    // Update is called once per frame
    void Update() {

    	/*var clone = Instantiate(tree, new Vector3(Random.Range(-100, 100), 100, Random.Range(-100, 100)), Quaternion.identity);
    	float scale = Random.Range(1, 5);
    	clone.transform.localScale = Vector3.one*scale;*/
    }
}