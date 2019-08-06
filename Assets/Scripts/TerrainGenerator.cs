using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainGenerator : MonoBehaviour
{
	public Vector3[] vertices;
	public int[] triangles;
	public Vector2[] uvs;
	public float[] heightMap;

	public static int heightMultiplier = 100;
	public int erosionIterations = 10;
	public AnimationCurve heightCurve;
	public bool animatedErosion = false;
	public bool continentGeneration = false;

	[Range(0,6)]
	public int levelOfDetail;
	public const int chunkSize = 241;	

	private Mesh mesh;
	private int totalIterations = 0;
	private int iterationsPerFrame = 100;
	private float updateTime = 1;
	private int triangleIndex = 0;

	public IEnumerator erodeTimed(int iterations) {
		WaitForSeconds wait = new WaitForSeconds(0.01f);
		for(int dropAmount = 0; dropAmount < iterations; dropAmount++) {
				heightMap = FindObjectOfType<Eroder>().erode(heightMap, chunkSize);
				if(dropAmount % 1000 == 0)
					//generateTerrain(heightMap);
				yield return wait;
		}	
	}
	public void BuildErodedHeightMap(Vector2 center, int erosionIterations) {
		heightMap = FindObjectOfType<NoiseCreator>().GenerateHeightMap(chunkSize, continentGeneration, center);
		for(int dropAmount = 0; dropAmount < erosionIterations; dropAmount++) {
				heightMap = FindObjectOfType<Eroder>().erode(heightMap, chunkSize);		
		}	
	}
	public void setHeightMapFromReference(float[] referenceHeightMap, int startX, int startY) { //used when a larger heightmap has been generated already
		int myHeightMapIndex = 0;
		heightMap = new float[chunkSize*chunkSize];
		int width = (int)Mathf.Sqrt(referenceHeightMap.Length); 
		for(int y = startY; y < chunkSize+startY; y++)  {
			for(int x = startX; x < chunkSize+startX; x++) {
				int index = y*width + x;
				bool xUsed = false;
				if(x == chunkSize+startX-1 && x + 1 < width) { //x is at the max value => use the value one to the right if possible to remove seam between tiles
					index = y*width + x + 1;
					xUsed = true;
				}
				 if(y == chunkSize+startY-1 && (y+1)*width + x < referenceHeightMap.Length) { //y is at the max value => use the value one below if possible to remove seam between tiles
					index = (y+1)*width + x;
					if(xUsed && (y+1)*width + x+1 < referenceHeightMap.Length) {
						index = (y+1)*width + x+1;
					}
				}
				//Debug.Log(x + " " + y);
				//Debug.Log(index + " is filling: " + myHeightMapIndex);
				heightMap[myHeightMapIndex] = referenceHeightMap[index];
				myHeightMapIndex++;
			}
		}
	}
	public GameObject generateTerrain() {
		int simplificationIncrement = (levelOfDetail == 0) ? 1: levelOfDetail *2;
		int verticesPerLine = (chunkSize-1)/simplificationIncrement + 1;
		//Debug.Log(simplificationIncrement);
		//Debug.Log(verticesPerLine);
		vertices = new Vector3[verticesPerLine*verticesPerLine];
		triangles = new int[(verticesPerLine-1)*(verticesPerLine-1)*6];
		uvs = new Vector2[verticesPerLine*verticesPerLine];
		int vertexIndex = 0;
		for(int y = 0; y < chunkSize; y+=simplificationIncrement)  {
			for(int x = 0; x < chunkSize; x+=simplificationIncrement) {
				//Debug.Log(x +" " + y);
				float terrainHeight = heightMultiplier * heightMap[y*chunkSize + x];
				//Debug.Log(terrainHeight);
				float xPercent = (float)x / (chunkSize);
				float yPercent = (float)y / (chunkSize);

				vertices[vertexIndex] = new Vector3(xPercent*2 - 1, 0 , yPercent*2 - 1) * chunkSize;
				//vertices[vertexIndex] = new Vector3(x, 0 , y);
				vertices[vertexIndex].y = terrainHeight;
				uvs[vertexIndex] = new Vector2(xPercent, yPercent);
				if (x != chunkSize - 1 && y != chunkSize - 1) 
					addTriangle(vertexIndex, verticesPerLine); 
				vertexIndex++;
			}
		}
		triangleIndex = 0;
		return generateMesh();
	}
	public void scatterObject(float abundance, GameObject objectToScatter) {
		if(mesh != null)  {
			Vector3[] normals = mesh.normals;
			for(int i =0; i < vertices.Length; i++) {
				if(abundance >= Random.Range(0.0f, 1.0f)) {
					float slope = 1 - normals[i].y;
					if(slope < 0.3) {
						var clone = Instantiate(objectToScatter, vertices[i], Quaternion.identity);
				    	float scale = Random.Range(1, 5);
    					clone.transform.localScale = Vector3.one*scale;
					}
				}

		}
		}

	}
	public GameObject generateMesh() {
		GameObject toReturn = new GameObject();
		toReturn.AddComponent<MeshRenderer>();
		toReturn.AddComponent<MeshFilter>();
		toReturn.AddComponent<MeshCollider>();
		mesh = new Mesh();
		toReturn.GetComponent<MeshFilter>().mesh = mesh;	
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		float waterLevel =  0.3f*(float)heightMultiplier; //.3 good for continents; .4 good otherwise
		float snowLevel =  0.9f*(float)heightMultiplier;
		float sandLevel =  waterLevel/0.8f;
		FindObjectOfType<WaterController>().adjustPositionAndSize(chunkSize*1.414f, waterLevel);
		MeshRenderer rend = toReturn.GetComponent<MeshRenderer>();

		rend.material.shader = Shader.Find("Custom/TerrainShader");
		rend.material.SetFloat("_SandHeight", sandLevel); //0.3 for sand and .24 for water makes pools
		rend.material.SetFloat("_WaterHeight", waterLevel);
		rend.material.SetFloat("_SnowHeight", snowLevel);

		
		rend.material.SetTexture("_GrassTex", Resources.Load("groundGrass") as Texture);
		rend.material.SetTextureScale("_GrassTex", new Vector2(50, 50));

		rend.material.SetTexture("_StoneTex", Resources.Load("GroundStone") as Texture);
		rend.material.SetTextureScale("_StoneTex", new Vector2(50, 50));

		rend.material.SetTexture("_WaterTex", Resources.Load("water") as Texture);
		rend.material.SetTextureScale("_WaterTex", new Vector2(10, 10));

		rend.material.SetTexture("_SandTex", Resources.Load("sand") as Texture);
		rend.material.SetTextureScale("_SandTex", new Vector2(50, 50));

		rend.material.SetTexture("_SnowTex", Resources.Load("snow") as Texture);
		rend.material.SetTextureScale("_SnowTex", new Vector2(50, 50));

		mesh.name = "Terrain Mesh";
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.RecalculateNormals();
		toReturn.GetComponent<MeshCollider>().sharedMesh = mesh;
		toReturn.GetComponent<MeshCollider>().enabled = true;
		return toReturn;
	}

	void addTriangle(int vi, int width) {
			triangles[triangleIndex] = vi;
			triangles[triangleIndex + 1] = vi + width;
			triangles[triangleIndex + 2] = vi + 1;

			triangles[triangleIndex + 3] = vi + 1;
			triangles[triangleIndex + 4] = vi + width;
			triangles[triangleIndex + 5] = vi + width + 1;
			triangleIndex += 6;
	}
}