﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseCreator : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("Noise Settings")]
    public int octaves = 7;
    public float persistence = 0.5f; // makes it so subsquent octaves have smaller effects
    public float lacunarity = 1.9f; //makes it so subsequent octaves have higher frequencies (affect detail)
    public float initialScale = 2.0f;
    [Header("Distortion Settings")]
    [Range(0, 1)]
    public float warpStrength;
    [Range(0, 20)]
    public float warpSize;
    
    private float[] heightMap;
    private float[] distMap;
    private static int[] perm = {
        151,160,137,91,90,15,
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
        151
    };
    public float[] GenerateHeightMap(int size, bool makeContinent, Vector2 center)
    {
        // For each pixel in the texture...
        heightMap = new float[size * size];
        distMap = new float[size*size];
        float max = -100f;
        float min = 100f;
        float maxDist = -100f;
        float minDist = 100f;
        float xSeed = center.x;
        float ySeed = center.y;
        for(int y = 0; y < size; y++)
        {
            for(int x = 0; x < size; x++)
            {
                float scale = initialScale;
                float noiseValue = 0F;
                float weight = 1;
                for(int i = 0; i < octaves; i++) {
                    float xCoord = ((xSeed + (float)x) / (float)size) * scale;
                    float yCoord = ((ySeed + (float)y) / (float)size) * scale;
                    noiseValue += Perlin3D(xCoord, yCoord, warpStrength * Mathf.PerlinNoise(xCoord*warpSize, yCoord*warpSize)) * weight;
                    //Debug.Log("NOISE VAL: " + noiseValue);
                    weight *= persistence;
                    scale *= lacunarity;
                }
                float distToCenter = Mathf.Sqrt((x-size/2)*(x-size/2) + (y-size/2)*(y-size/2)); 
                heightMap[y * size + x] = noiseValue;
                distMap[y * size + x] = distToCenter;
                max = Mathf.Max(heightMap[y * size + x], max);
                min = Mathf.Min(heightMap[y * size + x], min);

                maxDist = Mathf.Max(distMap[y * size + x], maxDist);
                minDist = Mathf.Min(distMap[y * size + x], minDist);
            }
        }
        
        normalize(heightMap, min, max);
        if(makeContinent) {
            normalize(distMap, minDist, maxDist);
            maxDist = -100f;
            minDist = 100f;
            for(int i =0; i < distMap.Length; i++) {
                distMap[i] -= 0.5f;
                distMap[i] *= -2.0f;
                maxDist = Mathf.Max(distMap[i], maxDist);
                minDist = Mathf.Min(distMap[i], minDist);
            }
            normalize(distMap, minDist, maxDist);
            max = -100f;
            min = 100f;
            for(int i = 0; i < heightMap.Length; i++) { 
                heightMap[i] *= distMap[i];
                if(heightMap[i] > 0.0f ) {
                    heightMap[i] *= 20.0f;
                }
                max = Mathf.Max(heightMap[i], max);
                min = Mathf.Min(heightMap[i], min);
            }
            normalize(heightMap, min, max);
        }
        heightMap = smoothMap(heightMap);
        return smoothMap(heightMap);
    }
    public float[] smoothMap(float[] toSmooth) {
        float maxH = -100f;
        float minH = 100f;
        int width = (int)Mathf.Sqrt(toSmooth.Length);
        float[] toRet = new float[width*width];
        for(int i = 0; i < toSmooth.Length; i++) {
            float sum = toSmooth[i];
            float valuesUsed = 1.0f;
            if(i - 1 >= 0 && (i % width == (i - 1) % width)) {
                sum += toSmooth[i - 1];
                valuesUsed += 1;
            }
            if(i + 1 < toSmooth.Length && (i % width == (i + 1) % width)) {
                sum += toSmooth[i + 1];
                valuesUsed += 1;
            }
            if(i - width >= 0) {
                sum += toSmooth[i - width];
                valuesUsed += 1;
            }
            if(i + width < toSmooth.Length) {
                sum += toSmooth[i + width];
                valuesUsed += 1;
            }
            toRet[i] = sum/valuesUsed;
            maxH = Mathf.Max(toRet[i], maxH);
            minH = Mathf.Min(toRet[i], minH);
        }
        normalize(toRet, minH, maxH);
        return toRet;
    }
    void normalize(float[] toNorm, float min, float max) {
        //Debug.Log(max);
        //Debug.Log(min);
        for(int i =0; i < toNorm.Length; i++) {
            toNorm[i] = (toNorm[i] - min) / (max - min);
            //Debug.Log(toNorm[i]);
        }
    }


    public static float Perlin3D(float x, float y, float z)
    {
        var X = Mathf.FloorToInt(x) & 0xff;
        var Y = Mathf.FloorToInt(y) & 0xff;
        var Z = Mathf.FloorToInt(z) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
        z -= Mathf.Floor(z);
        var u = Fade(x);
        var v = Fade(y);
        var w = Fade(z);
        var A  = (perm[X  ] + Y) & 0xff;
        var B  = (perm[X+1] + Y) & 0xff;
        var AA = (perm[A  ] + Z) & 0xff;
        var BA = (perm[B  ] + Z) & 0xff;
        var AB = (perm[A+1] + Z) & 0xff;
        var BB = (perm[B+1] + Z) & 0xff;
        return Lerp(w, Lerp(v, Lerp(u, Grad(perm[AA  ], x, y  , z  ), Grad(perm[BA  ], x-1, y  , z  )),
                               Lerp(u, Grad(perm[AB  ], x, y-1, z  ), Grad(perm[BB  ], x-1, y-1, z  ))),
                       Lerp(v, Lerp(u, Grad(perm[AA+1], x, y  , z-1), Grad(perm[BA+1], x-1, y  , z-1)),
                               Lerp(u, Grad(perm[AB+1], x, y-1, z-1), Grad(perm[BB+1], x-1, y-1, z-1))));
    }
     static float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }
     static float Lerp(float t, float a, float b)
    {
        return a + t * (b - a);
    }

    static float Grad(int hash, float x)
    {
        return (hash & 1) == 0 ? x : -x;
    }

    static float Grad(int hash, float x, float y)
    {
        return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
    }

    static float Grad(int hash, float x, float y, float z)
    {
        var h = hash & 15;
        var u = h < 8 ? x : y;
        var v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
