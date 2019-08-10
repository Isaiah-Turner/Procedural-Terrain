using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileTerrain : MonoBehaviour
{
    [Header("Terrain Dimensions")]
    public int tileDepth;
    public int tileWidth;
    [Header("Terrain Features")]
    public int roadCount = 1;
    public int erosionIterations = 0;
    public int heightMultiplier = 100;

    [Header("Terrain Objects")]
    public GameObject tree;
    public GameObject tree2;
    public GameObject shrub;
    public GameObject shrub2;
    public GameObject shrub3;
    public GameObject rock;
    public GameObject rock2;
    public GameObject rock3;
    public GameObject grass;

    private int chunkSize;
    private float[] fullHeightMap;
    //private List<RoadInfo> roads =  new List<RoadInfo>();
    // Start is called before the first frame update
    void Start()
    {
        chunkSize = TerrainGenerator.chunkSize;
        Vector2 initialPos = new Vector2((int)Mathf.Floor(-tileWidth / 2) * chunkSize, (int)Mathf.Floor(-tileDepth / 2) * chunkSize);
        int totalSize = chunkSize * Mathf.Max(tileDepth, tileWidth);
        //fullHeightMap = FindObjectOfType<NoiseCreator>().GenerateHeightMap(totalSize, false, initialPos);
        fullHeightMap = FindObjectOfType<NoiseCreator>().GenerateHeightMap(totalSize, false, new Vector2(Random.Range(-1000, 1000), Random.Range(-1000, 1000)));
        Color[] roadMask = generateRoadMask(roadCount, totalSize - 50);
        for (int dropAmount = 0; dropAmount < erosionIterations; dropAmount++)
        {
            fullHeightMap = FindObjectOfType<Eroder>().erode(fullHeightMap, totalSize);
        }

        int currentY = 0;
        for (int tileY = (int)Mathf.Floor(-tileDepth / 2.0f); tileY < Mathf.RoundToInt((float)tileDepth / 2.0f); tileY++)
        {
            int currentX = 0;
            for (int tileX = (int)Mathf.Floor(-tileWidth / 2.0f); tileX < Mathf.RoundToInt((float)tileWidth / 2.0f); tileX++)
            {
                Vector2 tilePos = new Vector2(tileX * (chunkSize - 1), tileY * (chunkSize - 1));
                TerrainGenerator generator = new TerrainGenerator();
                TerrainGenerator.heightMultiplier = heightMultiplier;
                //generator.BuildErodedHeightMap(tilePos, 0);
                generator.setHeightMapFromReference(fullHeightMap, roadMask, chunkSize * currentX, chunkSize * currentY);
                GameObject terrain = generator.generateTerrain();
                terrain.transform.SetParent(this.transform);
                /*foreach(RoadInfo road in roads)
                {
                    if(road.start.x >= chunkSize * currentX && road.start.x <= chunkSize * (currentX+1))
                    {
                        if (road.start.y >= chunkSize * currentY && road.start.y <= chunkSize * (currentY+1))
                        {
                            road.roadObject.transform.SetParent(terrain.transform);
                            //road.roadObject.transform.position = new Vector3(tilePos.x, 0, tilePos.y) * 2;
                            Debug.Log("Setting parent here");
                        }
                    }

                }*/
                terrain.transform.position = new Vector3(tilePos.x, 0, tilePos.y) * 2;
                Vector2 treeScale = new Vector2(1.0f, 5.0f) * 0.3048f;
                Vector2 smallScale = new Vector2(0.2f, .35f);
                Vector2 rockScale = new Vector2(0.02f, 0.04f);

                //generator.scatterGrass(grass, 1.0f, 30);
                
                generator.scatterObject(0.01f, tree, treeScale);
				generator.scatterObject(0.01f, tree2, treeScale);
				/*generator.scatterObject(0.01f, shrub, smallScale);
				generator.scatterObject(0.01f, shrub2, smallScale);
				generator.scatterObject(0.01f, shrub3, smallScale);
                //generator.scatterObject(0.5f, grass, new Vector2(0.5f, 1.0f), false, 25000);

                generator.scatterObject(0.01f, rock, rockScale, true);
				generator.scatterObject(0.01f, rock2, rockScale, true);
				generator.scatterObject(0.01f, rock3, rockScale, true);*/
                
                currentX += 1;
                Debug.Log("TILE FINISHED");
                //break;
            }
            currentY += 1;
            //break;
        }
        float waterLevel = 0.3f * (float)TerrainGenerator.heightMultiplier; //.3 good for continents; .4 good otherwise
        FindObjectOfType<WaterController>().adjustPositionAndSize(Mathf.Max(tileDepth + 1, tileWidth + 1) * chunkSize * 1.414f, waterLevel);


    }
    public Color[] generateRoadMask(int roadCount, int minRoadLength)
    {
        int width = (int)Mathf.Sqrt(fullHeightMap.Length);
        Color[] colors = new Color[fullHeightMap.Length];
        for (int x = 0; x < width * width; x++)
            colors[x] = new Color(255, 255, 255);
        for (int i = 0; i < roadCount; i++)
        {
            Vector2 pointA = new Vector2(Random.Range(0, width), Random.Range(0, width));
            Vector2 pointB;
            do
            {
                pointB = new Vector2(Random.Range(0, width), Random.Range(0, width));
            } while (AStarPath.manhattanDistanceHeuristic(pointA, pointB) < minRoadLength);

            Debug.Log("Start: " + pointA + "   End: " + pointB);

            List<Vector2> path = AStarPath.findPath(pointA, pointB, width, fullHeightMap, heightMultiplier);
            foreach (Vector2 point in path)
            {
                int roadMapIndex = (int)(point.y * width + point.x);
                colors[roadMapIndex] = new Color(0, 0, 0);
                List<int> neighbors = AStarPath.getGridNeighbors(roadMapIndex, width);
                foreach (int index in neighbors)
                {
                    colors[index] = new Color(0, 0, 0);
                }
            }
        }
        return colors;
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

}