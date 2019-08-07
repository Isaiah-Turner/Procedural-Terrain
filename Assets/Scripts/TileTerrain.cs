using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileTerrain : MonoBehaviour
{
    public int tileDepth, tileWidth;
    private int chunkSize;
    private float[] fullHeightMap;
    private float[] fullRoadMap;
    public int erosionIterations = 0;
    public GameObject tree;
    public GameObject tree2;
    public GameObject shrub;
    public GameObject shrub2;
    public GameObject shrub3;
    public GameObject rock;
    public GameObject rock2;
    public GameObject rock3;
    public const int heightMultiplier = 100;
    public int maxRoads = 10;
    //private List<RoadInfo> roads =  new List<RoadInfo>();
    // Start is called before the first frame update
    void Start()
    {
        chunkSize = TerrainGenerator.chunkSize;
        Vector2 initialPos = new Vector2((int)Mathf.Floor(-tileWidth / 2) * chunkSize, (int)Mathf.Floor(-tileDepth / 2) * chunkSize);
        int totalSize = chunkSize * Mathf.Max(tileDepth, tileWidth);
        fullHeightMap = FindObjectOfType<NoiseCreator>().GenerateHeightMap(totalSize, false, initialPos);
        fullRoadMap = new float[totalSize * totalSize];
        /*for (int roadCount = 0; roadCount < maxRoads; roadCount++)
        {
            roads.Add(generateRoad());
        }*/
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
                Debug.Log(tileX + " " + tileY);
                Vector2 tilePos = new Vector2(tileX * (chunkSize - 1), tileY * (chunkSize - 1));
                Debug.Log(tilePos);
                TerrainGenerator generator = new TerrainGenerator();
                TerrainGenerator.heightMultiplier = heightMultiplier;
                //generator.BuildErodedHeightMap(tilePos, 0);
                Debug.Log(chunkSize * currentX + " " + chunkSize * currentY);
                generator.setHeightMapFromReference(fullHeightMap, fullRoadMap, chunkSize * currentX, chunkSize * currentY);
                GameObject terrain = generator.generateTerrain();
                generator.generateRoad();
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
                Vector2 shrubScale = new Vector2(0.2f, .35f);
                /*generator.scatterObject(0.01f, tree, treeScale);
				generator.scatterObject(0.01f, tree2, treeScale);
				generator.scatterObject(0.01f, shrub, shrubScale);
				generator.scatterObject(0.01f, shrub2, shrubScale);
				generator.scatterObject(0.01f, shrub3, shrubScale);

				/*generator.scatterObject(0.01f, rock);
				generator.scatterObject(0.01f, rock2);
				generator.scatterObject(0.01f, rock3);*/
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
    // Update is called once per frame
    void Update()
    {

        /*var clone = Instantiate(tree, new Vector3(Random.Range(-100, 100), 100, Random.Range(-100, 100)), Quaternion.identity);
    	float scale = Random.Range(1, 5);
    	clone.transform.localScale = Vector3.one*scale;*/
    }
    
}