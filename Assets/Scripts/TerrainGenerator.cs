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
    public float[] objectMap;

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
	private GameObject terrainObject;
	public IEnumerator erodeTimed(int iterations) {
		WaitForSeconds wait = new WaitForSeconds(0.01f);
		for(int dropAmount = 0; dropAmount < iterations; dropAmount++) {
				heightMap = FindObjectOfType<Eroder>().erode(heightMap, chunkSize);
				if(dropAmount % 1000 == 0)
					//generateTerrain(heightMap);
				yield return wait;
		}	
	}
	void Start() {
		//var clone = Instantiate(tree, Vector3.one*100, Quaternion.identity);
	}
	public void BuildErodedHeightMap(Vector2 center, int erosionIterations) {
		heightMap = FindObjectOfType<NoiseCreator>().GenerateHeightMap(chunkSize, continentGeneration, center);
		for(int dropAmount = 0; dropAmount < erosionIterations; dropAmount++) {
				heightMap = FindObjectOfType<Eroder>().erode(heightMap, chunkSize);		
		}	
	}
	public void setHeightMapFromReference(float[] referenceHeightMap, float[] referenceObjectMap, int startX, int startY) { //used when a larger heightmap has been generated already
		int myHeightMapIndex = 0;
		heightMap = new float[chunkSize*chunkSize];
        objectMap = new float[chunkSize * chunkSize];
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
                objectMap[myHeightMapIndex] = referenceObjectMap[index];
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
		terrainObject = generateMesh();
		return terrainObject;
	}
    public void scatterObject(float abundance, GameObject objectToScatter, Vector2 allowableScale) {
		//TODO: change how y scales so bushes go out and not up as much
		//Matrix4x4 localToWorld = transform.localToWorldMatrix;
		//Instantiate(objectToScatter, vertices[13], Quaternion.identity);
		int max = 1500;
		int treeCount = 0;
		if(mesh != null)  {
			Vector3[] normals = mesh.normals;
			for(int i =0; i < vertices.Length; i++) {
				if(treeCount >= max) 
					break;
				if(abundance >= Random.Range(0.0f, 1.0f)) {
					float slope = 1 - normals[i].y;
					if(slope < 0.7 && vertices[i].y > 0.3f*(float)heightMultiplier) {
						Vector3 worldPos = terrainObject.transform.TransformPoint(mesh.vertices[i]);
						GameObject clone = Instantiate(objectToScatter, worldPos, Quaternion.identity) as GameObject;
						//Debug.Log("Planting a tree at: " + worldPos + " slope is: " + slope);
				    	float scale = Random.Range(allowableScale.x, allowableScale.y);
    					clone.transform.localScale = Vector3.one*scale;
    					clone.transform.position = worldPos;
    					clone.transform.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    					treeCount++;
					}
				}

			}
		}

	}
    public void generateRoad()
    {
        int width = (int)Mathf.Sqrt(heightMap.Length);
        Vector2 pointA = new Vector2(Random.Range(0, width), Random.Range(0, width));
        Vector2 pointB;
        do
        {
            pointB = new Vector2(Random.Range(0, width), Random.Range(0, width));
        } while (AStarPath.manhattanDistanceHeuristic(pointA, pointB) < 230);

        Debug.Log("Start: " + pointA + "   End: " + pointB);
        List<Vector2> path = AStarPath.findPath(pointA, pointB, width);
        List<Vector4> roadPoints = new List<Vector4>();
        Texture2D roadMask = new Texture2D(width, width);
        Color[] colors = new Color[width * width];
        for (int x = 0; x < width * width; x++)
            colors[x] = new Color(255, 255, 255);
        foreach (Vector2 point in path)
        {
            int roadMapIndex = (int)(point.y * width + point.x);
            colors[roadMapIndex] = new Color(0, 0, 0);
            List<int> neighbors = AStarPath.getGridNeighbors(roadMapIndex, width);
            foreach(int index in neighbors)
            {
                colors[index] = new Color(0, 0, 0);
            }
            Vector3 worldPos = terrainObject.transform.TransformPoint(mesh.vertices[roadMapIndex]);
            roadPoints.Add(worldPos);
        }
        roadMask.SetPixels(colors);
        roadMask.Apply();
        terrainObject.GetComponent<MeshRenderer>().material.SetFloat("_RoadLocationCount", roadPoints.Count);
        terrainObject.GetComponent<MeshRenderer>().material.SetVectorArray("_RoadPositions", roadPoints);
        terrainObject.GetComponent<MeshRenderer>().material.SetTexture("_RoadMask", roadMask);
        var returned = terrainObject.GetComponent<MeshRenderer>().material.GetVectorArray("_RoadPositions");
        var count = terrainObject.GetComponent<MeshRenderer>().material.GetFloat("_RoadLocationCount");
        /*GameObject roadObject = new GameObject();
        roadObject.AddComponent<MeshRenderer>();
        roadObject.AddComponent<MeshFilter>();
        roadObject.AddComponent<MeshCollider>();
        Mesh roadMesh = extrudeAlongPath(roadPoints, path, 2.0f);
        roadObject.GetComponent<MeshFilter>().mesh = roadMesh;
        roadObject.transform.SetParent(terrainObject.transform);
        roadObject.name = "Road from: " + pointA;
        roadObject.transform.position += Vector3.up * 0.0f; //slightly raise over terrain
        roadObject.GetComponent<MeshRenderer>().material = Resources.Load("roadMaterial") as Material;
        roadObject.GetComponent<MeshCollider>().sharedMesh = roadMesh;
        roadObject.GetComponent<MeshCollider>().enabled = true;*/
    }
    public GameObject generateMesh()
    {
        GameObject toReturn = new GameObject();
        toReturn.AddComponent<MeshRenderer>();
        toReturn.AddComponent<MeshFilter>();
        toReturn.AddComponent<MeshCollider>();
        mesh = new Mesh();
        toReturn.GetComponent<MeshFilter>().mesh = mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        float waterLevel = 0.3f * (float)heightMultiplier; //.3 good for continents; .4 good otherwise
        float snowLevel = 0.9f * (float)heightMultiplier;
        float sandLevel = waterLevel / 0.8f;
        FindObjectOfType<WaterController>().adjustPositionAndSize(chunkSize * 1.414f, waterLevel);
        MeshRenderer rend = toReturn.GetComponent<MeshRenderer>();

        rend.material.shader = Shader.Find("Custom/TerrainShader");
        rend.material.SetFloat("_SandHeight", sandLevel); //0.3 for sand and .24 for water makes pools
        rend.material.SetFloat("_WaterHeight", waterLevel);
        rend.material.SetFloat("_SnowHeight", snowLevel);

        rend.material.SetTexture("_GrassTex", Resources.Load("groundGrass") as Texture);
        rend.material.SetTextureScale("_GrassTex", new Vector2(50, 50));

        rend.material.SetTexture("_StoneTex", Resources.Load("GroundStones01") as Texture);
        rend.material.SetTextureScale("_StoneTex", new Vector2(50, 50));

        rend.material.SetTexture("_WaterTex", Resources.Load("water") as Texture);
        rend.material.SetTextureScale("_WaterTex", new Vector2(10, 10));

        rend.material.SetTexture("_SandTex", Resources.Load("sand") as Texture);
        rend.material.SetTextureScale("_SandTex", new Vector2(50, 50));

        rend.material.SetTexture("_SnowTex", Resources.Load("snow") as Texture);
        rend.material.SetTextureScale("_SnowTex", new Vector2(50, 50));

        rend.material.SetTexture("_RoadTex", Resources.Load("dirtPath") as Texture);
        rend.material.SetTextureScale("_RoadTex", new Vector2(100, 100));

        rend.material.SetTexture("_RoadBumpmap", Resources.Load("cobblestone_N") as Texture);

        mesh.name = "Terrain Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        toReturn.GetComponent<MeshCollider>().sharedMesh = mesh;
        toReturn.GetComponent<MeshCollider>().enabled = true;
        toReturn.name = "Terrain Tile";
        return toReturn;
    }

    void addTriangle(int vi, int width)
    {
        triangles[triangleIndex] = vi;
        triangles[triangleIndex + 1] = vi + width;
        triangles[triangleIndex + 2] = vi + 1;

        triangles[triangleIndex + 3] = vi + 1;
        triangles[triangleIndex + 4] = vi + width;
        triangles[triangleIndex + 5] = vi + width + 1;
        triangleIndex += 6;
    }
    /*public Mesh extrudeAlongPath(List<Vector3> points, List<Vector2> heightMapCoords, float width) // from https://gamedev.stackexchange.com/questions/103047/generating-mesh-along-path
    {
        int gridWidth = (int)Mathf.Sqrt(heightMap.Length);
        if (points.Count < 2)
            return null;
        Mesh m = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < points.Count; i++) {
           
            if (i != points.Count - 1)
            {

                //Vector3 perpendicularDirection = new Vector3(-(points[i + 1].z - points[i].z), points[i].y, (points[i + 1].x - points[i].x)).normalized;
                Vector3 differenceVector = points[i + 1] - points[i];
                Vector3 perpendicularDirection = Vector3.Cross(differenceVector, Vector3.up).normalized;

                Vector3 roadSidePositive = points[i] + perpendicularDirection * width;
                Vector2 adjustedPositiveCoordinate = heightMapCoords[i] + new Vector2(perpendicularDirection.x, perpendicularDirection.z) * width;
                if((int)(adjustedPositiveCoordinate.y * gridWidth + adjustedPositiveCoordinate.x) < heightMap.Length)
                    roadSidePositive.y = roadSidePositive.y* 0.9f + 0.1f*heightMap[Mathf.RoundToInt(adjustedPositiveCoordinate.y) * gridWidth + Mathf.RoundToInt(roadSidePositive.x)] * heightMultiplier;
                verts.Add(roadSidePositive);
                norms.Add(Vector3.up);
                uvs.Add(new Vector2(1.0f, (float)i /points.Count));

                Vector3 roadSideNegative = points[i] + perpendicularDirection * -width;
                Vector2 adjustedNegativeCoordinate = heightMapCoords[i] + new Vector2(perpendicularDirection.x, perpendicularDirection.z) * -width;
                if ((int)(adjustedNegativeCoordinate.y * gridWidth + adjustedNegativeCoordinate.x) < heightMap.Length)
                    roadSideNegative.y = roadSideNegative.y * 0.9f + 0.1f * heightMap[Mathf.RoundToInt(adjustedNegativeCoordinate.y) * gridWidth + Mathf.RoundToInt(adjustedNegativeCoordinate.x)] * heightMultiplier;
                verts.Add(roadSideNegative);
                norms.Add(Vector3.up);
                uvs.Add(new Vector2(0.0f, ((float)i / points.Count)));
            }
            else
            {
                //Vector3 perpendicularDirection = new Vector3(-(points[i].z - points[i - 1].z), points[i].y, (points[i].x - points[i - 1].x)).normalized;
                Vector3 differenceVector = points[i] - points[i-1];
                Vector3 perpendicularDirection = Vector3.Cross(differenceVector, Vector3.up).normalized;
                Vector3 roadSideNegative = points[i] + perpendicularDirection * -width;
                Vector2 adjustedNegativeCoordinate = heightMapCoords[i] + new Vector2(perpendicularDirection.x, perpendicularDirection.z) * -width;
                if ((int)(adjustedNegativeCoordinate.y * gridWidth + adjustedNegativeCoordinate.x) < heightMap.Length)
                    roadSideNegative.y = roadSideNegative.y * 0.9f + 0.1f * heightMap[Mathf.RoundToInt(adjustedNegativeCoordinate.y) * gridWidth + Mathf.RoundToInt(adjustedNegativeCoordinate.x)]* heightMultiplier;
                verts.Add(roadSideNegative);
                norms.Add(Vector3.up);
                uvs.Add(new Vector2(0.0f, ((float)i / points.Count)));

                Vector3 roadSidePositive = points[i] + perpendicularDirection * width;
                Vector2 adjustedPositiveCoordinate = heightMapCoords[i] + new Vector2(perpendicularDirection.x, perpendicularDirection.z) * width;
                if ((int)(adjustedPositiveCoordinate.y * gridWidth + adjustedPositiveCoordinate.x) < heightMap.Length)
                    roadSidePositive.y = roadSidePositive.y * 0.9f + 0.1f * heightMap[Mathf.RoundToInt(adjustedPositiveCoordinate.y) * gridWidth + Mathf.RoundToInt(roadSidePositive.x)]* heightMultiplier;
                verts.Add(roadSidePositive);
                norms.Add(Vector3.up);
                uvs.Add(new Vector2(1.0f, (float)i / points.Count));
            }
        }
        m.vertices = verts.ToArray();
        m.normals = norms.ToArray();
        m.uv = uvs.ToArray();
        List<int> tris = new List<int>();
        //Changed i+=3 to i++
        for (int i = 0; i < m.vertices.Length - 3; i++)
        {
            if (i % 2 == 0)
            {
                tris.Add(i + 2);
                tris.Add(i + 1);
                tris.Add(i);
            }
            else
            {
                tris.Add(i);
                tris.Add(i + 1);
                tris.Add(i + 2);
            }
        }
        m.triangles = tris.ToArray();

        m.name = "pathMesh";
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
    public struct RoadInfo
    {
        public Vector2 start;
        public GameObject roadObject;
        public RoadInfo(Vector2 start, GameObject roadObject)
        {
            this.start = start;
            this.roadObject = roadObject;
        }
    }*/

}