Shader "Custom/ProceduralTerrain"
{
    Properties
    {
        smoothness ("Smoothness", Range(0, 1)) = 0.5
        metallic ("Metallic", Range(0, 1)) = 0.0
        flatColor ("Flat Color", Color) = (0, 0, 0, 0)
        steepColor ("Steep Color", Color) = (0, 0, 0, 0)
        steepnessThreshold ("Steepness Threshold", Range(0, 1)) = 0.8
        colorBlend ("Color Blend", Range(0, 1)) = 0.1
        rimFactor ("Rim Factor", Range(0, 1)) = 0.1
        rimPower ("Rim Power", float) = 1
        rimColor ("Rim Color", Color) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma vertex vert
        #pragma target 3.0

        struct Input
        {
            float3 localPos;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
        };

        float smoothness;
        float metallic;
        float4 flatColor;
        float4 steepColor;
        float steepnessThreshold;
        float colorBlend;
        float rimFactor;
        float rimPower;
        float4 rimColor;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        float Blend(float startHeight, float blendDst, float height) {
        	 return smoothstep(startHeight - blendDst / 2, startHeight + blendDst / 2, height);
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.localPos = v.vertex.xyz;
        }

        void surf (Input i, inout SurfaceOutputStandard o)
        {
            //Calculate steepness: 0 where totally flat, 1 at max steepness
			float steepness = 1 - dot(i.worldNormal, float3(0, 1, 0));
            steepness = clamp(steepness, 0, 1);

            float flatStrength = 1 - Blend(steepnessThreshold, colorBlend, steepness);
            float3 blendAxes = abs(i.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

            float rim = 1 - saturate(dot(normalize(i.viewDir), i.worldNormal));
            float rimWeight = pow(rim, rimPower) * rimFactor;

            float3 compositeCol = lerp(steepColor, flatColor, flatStrength);
            compositeCol = rimColor * rimWeight + compositeCol * (1 - rimWeight);

			o.Albedo = compositeCol;
            o.Metallic = metallic;
            o.Smoothness = smoothness;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
