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

	public int heightMultiplier = 10;
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
	/*private void OnDrawGizmos () {
		Gizmos.color = Color.black;
		for (int i = 0; i < vertices.Length; i++) {
			Gizmos.DrawSphere(vertices[i], 0.1f);
		}
	}*/
	void Start() {
		BuildErodedHeightMap(new Vector2(0,0), 0);
		GameObject terrain = generateTerrain();
		/*heightMap = FindObjectOfType<NoiseCreator>().GenerateHeightMap(chunkSize, continentGeneration);
		if(!animatedErosion) {
			for(int dropAmount = 0; dropAmount < erosionIterations; dropAmount++) {
				heightMap = FindObjectOfType<Eroder>().erode(heightMap, chunkSize);		
			}
			generateTerrain(heightMap);
		}
		//StartCoroutine(erodeTimed(erosionIterations));
		//generateTerrain(heightMap);*/
	}
	void Update() {
		/*if (totalIterations < erosionIterations && animatedErosion) {
			for(int dropAmount = 0; dropAmount < iterationsPerFrame; dropAmount++) {
				heightMap = FindObjectOfType<Eroder>().erode(heightMap, chunkSize);		
			}	
			totalIterations += iterationsPerFrame;
			generateTerrain(heightMap);
			Debug.Log(totalIterations);
		}
		else if(Time.time >= updateTime){
			//heightMap = FindObjectOfType<NoiseCreator>().GenerateHeightMap(chunkSize, continentGeneration);
			//generateTerrain(heightMap);
			updateTime = Time.time + 3;
		}*/
	} 
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

	public GameObject generateMesh() {
		GameObject toReturn = new GameObject();
		toReturn.AddComponent<MeshRenderer>();
		toReturn.AddComponent<MeshFilter>();
		toReturn.AddComponent<MeshCollider>();
		mesh = new Mesh();
		toReturn.GetComponent<MeshFilter>().mesh = mesh;	
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

		rend.material.SetTexture("_StoneTex", Resources.Load("stone") as Texture);
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