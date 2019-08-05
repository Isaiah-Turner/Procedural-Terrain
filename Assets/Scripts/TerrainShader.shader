Shader "Custom/TerrainShader" {
    Properties {
        _GrassSlopeThreshold ("Grass Slope Threshold", Range(0,1)) = .5
        _GrassBlendAmount ("Grass Blend Amount", Range(0,1)) = .5
        _SandHeight ("Sand Height", Range(0,100)) = 3
        _WaterHeight ("Water Height", Range(0,100)) = 2
        _SnowHeight ("Snow Height", Range(0,100)) = 2

        _GrassTex ("Grass Texture", 2D) = "groundGrass" {}
        _StoneTex ("Stone Texture", 2D) = "stone" {}
        _WaterTex ("Water Texture", 2D) = "water" {}
        _SandTex ("Sand Texture", 2D) = "sand" {}
        _SnowTex ("Snow Texture", 2D) = "sand" {}

        _StoneBumpmap ("Stone Bumpmap", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.5

        struct Input {
            float3 worldPos;
            float3 worldNormal;
            float2 uv_GrassTex;
            float2 uv_StoneTex;
            float2 uv_WaterTex;
            float2 uv_SandTex;
            float2 uv_SnowTex;
            float2 uv_StoneBumpmap;
            INTERNAL_DATA
        };

        half _MaxHeight;
        half _GrassSlopeThreshold;
        half _GrassBlendAmount;

        half _SandHeight;
        half _WaterHeight;
        half _SnowHeight;

        sampler2D _GrassTex;
        sampler2D _StoneTex;
        sampler2D _WaterTex;
        sampler2D _SandTex;
        sampler2D _SnowTex;
        sampler2D _StoneBumpmap;

        float smoothstep(float a, float b, float x)
        {
            float t = saturate((x - a)/(b - a));
            return t*t*(3.0 - (2.0*t));
        } 
        void surf (Input IN, inout SurfaceOutputStandard o) {
            float currentHeight = IN.worldPos.y;
            float slope = 1-IN.worldNormal.y; // slope = 0 when terrain is completely flat
            float stoneBlendHeight = _GrassSlopeThreshold * (1-_GrassBlendAmount);
            float stoneWeight = saturate((slope-stoneBlendHeight)/(_GrassSlopeThreshold-stoneBlendHeight));
            float3 blended = tex2D (_GrassTex, IN.uv_GrassTex).rgb * (1-stoneWeight) + tex2D (_StoneTex, IN.uv_StoneTex).rgb * stoneWeight; 

            o.Albedo = blended;
            //o.Normal = UnpackNormal (tex2D (_StoneBumpmap, IN.uv_StoneBumpmap));
            if(currentHeight > _SnowHeight) {
                float snowWeight = smoothstep(_SnowHeight, _SnowHeight/0.96f, currentHeight);
                float3 snowBlended = tex2D (_SnowTex, IN.uv_SnowTex).rgb * (1-stoneWeight) + tex2D (_StoneTex, IN.uv_StoneTex).rgb * stoneWeight;
                o.Albedo = tex2D (_SnowTex, IN.uv_SnowTex).rgb * (snowWeight) +  blended*(1-snowWeight);
            }
            else if(currentHeight < _SandHeight) {
                float sandWeight = smoothstep(_SandHeight*0.8, _SandHeight, currentHeight);
                o.Albedo = tex2D (_SandTex, IN.uv_SandTex).rgb * (1-sandWeight) + blended * (sandWeight);
                if(currentHeight < _WaterHeight)
                {
                    float waterWeight = smoothstep(_WaterHeight*0.8, _WaterHeight, currentHeight);
                    o.Albedo = tex2D (_WaterTex, IN.uv_WaterTex).rgb * (1-waterWeight) + tex2D (_SandTex, IN.uv_SandTex).rgb * waterWeight;  
                }
            }

        }
        ENDCG
    }
}