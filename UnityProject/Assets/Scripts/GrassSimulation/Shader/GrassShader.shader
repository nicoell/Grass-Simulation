Shader "GrassSimulation/GrassShader"
{
	SubShader
	{
		
		Pass
		{
		    Fog{Mode off}
		    Cull Off
			CGPROGRAM
			
			#pragma target 5.0
			#pragma only_renderers d3d11
			//#pragma enable_d3d11_debug_symbols
			#pragma vertex vert
			#pragma hull hull
			#pragma domain domain
			#pragma fragment frag
			// Create two shader variants, one for geometry grass and one for billboard grass
			#pragma multi_compile GRASS_BILLBOARD_CROSSED GRASS_BILLBOARD_SCREEN GRASS_GEOMETRY
			
			#include "UnityCG.cginc"

            struct UvData
            {
                float2 Position;
            };
            
            //Once
            uniform int vertexCount;
            uniform float billboardSize;
            
            uniform float LodInstancesGeometry;
            uniform float LodInstancesBillboardCrossed;
            uniform float LodInstancesBillboardScreen;
            
            uniform float LodDistanceGeometryStart;
            uniform float LodDistanceGeometryPeak;
            uniform float LodDistanceGeometryEnd;
            uniform float LodDistanceBillboardCrossedStart;		
            uniform float LodDistanceBillboardCrossedPeak;		
            uniform float LodDistanceBillboardCrossedEnd;		
            uniform float LodDistanceBillboardScreenStart;		
            uniform float LodDistanceBillboardScreenPeak;		
            uniform float LodDistanceBillboardScreenEnd;
            
            
			StructuredBuffer<UvData> UvBuffer;	// pos.x 		pos.z
            Texture2D<float4> ParameterTexture; // width, bend, height, dirAlpha
            SamplerState samplerParameterTexture;
			Texture2D GrassBlade;
            SamplerState samplerGrassBlade;
            
            //PerFrame
            uniform float4 camPos;
            uniform float4 camUp;
			
			//PerPatch
			uniform float startIndex;
			uniform float parameterOffsetX;
			uniform float parameterOffsetY;
            uniform float4x4 patchModelMatrix;
            uniform float4 PatchTexCoord; //x: xStart, y: yStart, z: width, w:height
			Texture2DArray<float4> SimulationTexture; //v1.xyz, tesslevel; v2.xyz, transition
			SamplerState samplerSimulationTexture;
			Texture2D NormalHeightTexture; //up.xyz, pos.y
			SamplerState samplerNormalHeightTexture;

			struct VSOut 
			{
			    uint vertexID : VertexID;
			    uint instanceID : InstanceID;
			    float2 uvLocal : TEXCOORD0;
			};
			
			struct HSConstOut
			{
			    float TessFactor[4] : SV_TessFactor;
			    float InsideTessFactor[2] : SV_InsideTessFactor;
			};
			
			struct HSOut
			{
				float3 pos : POS;
				float transitionFactor : TEXCOORD0;
			    float4 parameters : TEXCOORD1;
			    float3 bladeUp : TEXCOORD2;
			    float3 v1 : TEXCOORD3;
			    float3 v2 : TEXCOORD4;
			    float3 bladeDir : TEXCOORD5;
			};
			
			struct DSOut
			{
			    float4 pos : SV_POSITION;
			    float4 color : COLOR0;
			};
			
			struct FSIn
			{
			    float4 pos : SV_POSITION;
			    float4 color : COLOR0;
			};
			
			float SingleLerp(float value, float cur, float peak, float end)
            {
                float t1 = clamp((cur - peak) / (end - peak), 0, 1);
                return value - lerp(0, value, t1);
            }
            
            float DoubleLerp(float value, float cur, float start, float peak, float end)
            {
                float t0 = clamp((cur - start) / (peak - start), 0, 1);
                float t1 = clamp((cur - peak) / (end - peak), 0, 1);
                return value - (lerp(value, 0, t0) + lerp(0, value, t1));
            }
            
            float GetTessellationLevel(float distance, uint instanceID, float2 uv){
                float transition = 0;
                #ifdef GRASS_BILLBOARD_CROSSED
                transition = DoubleLerp(LodInstancesBillboardCrossed, distance,
                    LodDistanceBillboardCrossedStart, LodDistanceBillboardCrossedPeak, LodDistanceBillboardCrossedEnd);
                #elif GRASS_BILLBOARD_SCREEN
                transition = DoubleLerp(LodInstancesBillboardScreen, distance,
                    LodDistanceBillboardScreenStart, LodDistanceBillboardScreenPeak, LodDistanceBillboardScreenEnd);
                #elif GRASS_GEOMETRY
                transition = SingleLerp(LodInstancesGeometry, distance,
                    LodDistanceGeometryPeak, LodDistanceGeometryEnd);
                #endif
                
                uint transitionInstanceID = floor(transition);

                #ifdef GRASS_BILLBOARD_CROSSED
                    if (instanceID > transitionInstanceID){
                        return 0;
                    }
                    return 1.0;
                #elif GRASS_BILLBOARD_SCREEN
                    if (instanceID > transitionInstanceID){
                        return 0;
                    }
                    return 1.0;
                #elif GRASS_GEOMETRY
                    if (instanceID > transitionInstanceID){
                        return 0;
                    }
                    return SimulationTexture.SampleLevel(samplerSimulationTexture, float3(uv, 0), 0).w;
                #endif
            }
			
			VSOut vert (uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				VSOut OUT;
				OUT.vertexID = vertexID;
				OUT.instanceID = instanceID;
				OUT.uvLocal = UvBuffer[startIndex + vertexCount * instanceID + vertexID].Position;

				return OUT;
			}
			
			HSConstOut hullPatchConstant( InputPatch<VSOut, 1> IN)
    		{
        		HSConstOut OUT = (HSConstOut)0;
        		float distance = SimulationTexture.SampleLevel(samplerSimulationTexture, float3(IN[0].uvLocal, 1), 0).w;
        		
        		float level = GetTessellationLevel(distance, IN[0].instanceID, IN[0].uvLocal);
        		
        		#ifdef GRASS_BILLBOARD_CROSSED       		
        		OUT.TessFactor[0] = level;	//left
        		OUT.TessFactor[1] = level;	//bottom
        		OUT.TessFactor[2] = level;	//right
        		OUT.TessFactor[3] = level;	//top
        		OUT.InsideTessFactor[0] = level;
        		OUT.InsideTessFactor[1] = level;
        		#elif GRASS_BILLBOARD_SCREEN
        		OUT.TessFactor[0] = level;	//left
        		OUT.TessFactor[1] = level;	//bottom
        		OUT.TessFactor[2] = level;	//right
        		OUT.TessFactor[3] = level;	//top
        		OUT.InsideTessFactor[0] = level;
        		OUT.InsideTessFactor[1] = level;
        		#elif GRASS_GEOMETRY
        		OUT.TessFactor[0] = level;	//left
        		OUT.TessFactor[1] = 2.0;	//bottom
        		OUT.TessFactor[2] = level;	//right
        		OUT.TessFactor[3] = 1.0;	//top
        		OUT.InsideTessFactor[0] = 1.0;
        		OUT.InsideTessFactor[1] = level;
        		#endif
        	
        		return OUT;
            }

			[domain("quad")]
    		[partitioning("integer")]
    		[outputtopology("triangle_cw")]
    		[patchconstantfunc("hullPatchConstant")]
    		[outputcontrolpoints(1)]
    		HSOut hull( InputPatch<VSOut, 1> IN, uint i : SV_OutputControlPointID )
    		{
    		    //TODO: Its probably faster to only do the stuff here if GrassBlade does not get culled (tesslevel > 0)
    		
        		HSOut OUT = (HSOut)0;

        		float2 uvGlobal = float2(parameterOffsetX, parameterOffsetY) + IN[0].uvLocal;
        		float4 normalHeight = NormalHeightTexture.SampleLevel(samplerNormalHeightTexture, IN[0].uvLocal, 0);
        		float4 SimulationData0 = SimulationTexture.SampleLevel(samplerSimulationTexture, float3(IN[0].uvLocal, 0), 0);
				float4 SimulationData1 = SimulationTexture.SampleLevel(samplerSimulationTexture, float3(IN[0].uvLocal, 1), 0);
        		
        		float distance = SimulationData1.w;
        		
        		float transition = 0;
                #ifdef GRASS_BILLBOARD_CROSSED
                transition = DoubleLerp(LodInstancesBillboardCrossed, distance,
                    LodDistanceBillboardCrossedStart, LodDistanceBillboardCrossedPeak, LodDistanceBillboardCrossedEnd);
                #elif GRASS_BILLBOARD_SCREEN
                transition = DoubleLerp(LodInstancesBillboardScreen, distance,
                    LodDistanceBillboardScreenStart, LodDistanceBillboardScreenPeak, LodDistanceBillboardScreenEnd);
                #elif GRASS_GEOMETRY
                transition = SingleLerp(LodInstancesGeometry, distance,
                    LodDistanceGeometryPeak, LodDistanceGeometryEnd);
                #endif
                
        		//TODO: Compare performance of condition
        		//TODO: Check if height transition is disabled
        		uint instanceID = floor(transition);
        		if (instanceID == IN[0].instanceID){
        		    OUT.transitionFactor = frac(transition);
        		} else {
        		    OUT.transitionFactor = 1;
        		}

        		OUT.pos = mul(patchModelMatrix, float4(IN[0].uvLocal.x, normalHeight.w, IN[0].uvLocal.y, 1.0)).xyz;
        		OUT.parameters = ParameterTexture.SampleLevel(samplerParameterTexture, uvGlobal, 0);
        		OUT.bladeUp = normalize(normalHeight.xyz);
        		OUT.v1 = SimulationData0.xyz;
        		OUT.v2 = SimulationData1.xyz;
        		
        		#ifdef GRASS_BILLBOARD_SCREEN
        		float3 camDir = normalize(OUT.pos - camPos);
        		float3 right = cross(camDir, camUp.xyz);
        		OUT.bladeDir = normalize(cross(OUT.bladeUp, camDir));
        		#else
        		float dirAlpha = OUT.parameters.w;
        		float sd = sin(dirAlpha);
                float cd = cos(dirAlpha); 
                float3 tmp = normalize(float3(sd, sd + cd, cd));
               	OUT.bladeDir = normalize(cross(OUT.bladeUp, tmp));
               	#endif
                  
        		return OUT;
            }
               
            [domain("quad")]
    		DSOut domain( HSConstOut hullConstData, 
    		            const OutputPatch<HSOut, 1> IN, 
    					float2 uv : SV_DomainLocation)
    		{
        		DSOut OUT = (DSOut)0;
                
                //TODO: Is this an overhead to create local variables here?
				float3 pos = IN[0].pos;
        		float3 v1 = pos + IN[0].v1 * IN[0].transitionFactor;
        		float3 v2 = pos + IN[0].v2 * IN[0].transitionFactor;
        		float3 up = IN[0].bladeUp;
        		float3 bladeDir = IN[0].bladeDir;
        		#ifdef GRASS_BILLBOARD_CROSSED
        		float width = length(IN[0].v2) * IN[0].transitionFactor;
        		#elif GRASS_BILLBOARD_SCREEN
        		float width = length(IN[0].v2) * IN[0].transitionFactor;
        		#elif GRASS_GEOMETRY
        		float width = IN[0].parameters.x;
        		#endif
        		float bend = IN[0].parameters.y;
        		float height = IN[0].parameters.z;

        		float u = uv.x;
                float omu = 1.0 - u;
                float v = uv.y;
                float omv = 1.0 - v;
            
                float3 off = bladeDir * width;
                float3 off2 = off * 0.5;
            
                float3 p0 = pos - off2;
                float3 p1 = v1 - off2;
                float3 p2 = v2 - off2;
            
                float3 h1 = p0 + v * (p1 - p0);
                float3 h2 = p1 + v * (p2 - p1);
                float3 i1 = h1 + v * (h2 - h1);
                float3 i2 = i1 + off;
            
                float3 bitangent = bladeDir;
                float3 tangent;
            
                float3 h1h2 = h2 - h1;
                if(dot(h1h2, h1h2) < 1e-3)
                {
                    tangent = up;
                }
                else
                {
                    tangent = normalize(h1h2);
                }
                
                float3 normal = normalize(cross(tangent, bitangent));
	
                //teUV = vec2(u,v);
                //teNormal = normalize(cross(tangent, bitangent));
            
                //vec3 position = Form(i1, i2, u, v, teNormal, tcV2.w);
                //float3 outpos = lerp(i1, i2, u - pow(v, 2)*u) + translation;
                #ifdef GRASS_BILLBOARD_CROSSED
                float3 outpos = lerp(i1, i2, u);
                #elif GRASS_BILLBOARD_SCREEN
                float3 outpos = lerp(i1, i2, u);
                #elif GRASS_GEOMETRY
                float3 texSample = GrassBlade.SampleLevel(samplerGrassBlade, uv.xy, 0.0);
                float3 translation = normal * width * (0.5 - abs(u - 0.5)) * ((1.0 - floor(v)) * texSample.r); //position auf der normale verschoben bei mittelachse -> ca rechter winkel (u mit hat function)
                float t = u + ((texSample.g*u) + ((1.0-texSample.g)*omu));
                float3 outpos = lerp(i1, i2, t) + translation;
                #endif
                /*if(dot(lightDirection, teNormal) > 0.0)
                    teNormal = -teNormal;
            
                teDebug = tcDebug;
                gl_Position = vpMatrix * vec4(position, 1.0);
                tePosition = vec4(position, 1.5 * abs(sin(shapeConstant * tcV1.w)));
       
        		float3 pos = float3(IN[0].pos.x + (uv.x - 0.5)*0.1, IN[0].pos.y + uv.y, IN[0].pos.z);*/
      
                //DEBUG COLORING
                /*float distance = length(pos - camPos);
        		float debugT = (distance - LodDistanceFullDetail) / (LodDistanceBillboard - LodDistanceFullDetail);
        		float debugInterpolant = frac(lerp(LodDensityBillboardDistance, LodDensityFullDetailDistance, debugT));

        		if (debugT < 0){
        		    OUT.color = float4(1,1,1,1);
        		} else if (debugT > 1){
        		    OUT.color = float4(0,1,0,1);
        		} else {
        		    OUT.color = float4(lerp(float3(1,0,0), float3(0,0,1), debugInterpolant), 1);
        		}*/
      
                OUT.pos = mul(UNITY_MATRIX_VP, float4(outpos, 1.0));
                OUT.color = float4(lerp(float3(0.44, 0.61, 0.2), float3(0.12, 0.18, 0.055), 1-v), 1);
               
           
        		return OUT;
}
			
			fixed4 frag (FSIn IN) : SV_TARGET
			{
				fixed4 col = IN.color;
				return col;
			}
			ENDCG
		}
	}
}
