Shader "GrassSimulation/GrassShader"
{
    Properties{
    [HideInInspector] _SrcBlend ("__src", Float) = 1.0
    [HideInInspector] _DstBlend ("__dst", Float) = 0.0
    [HideInInspector] _ZWrite ("__zw", Float) = 1.0
    }
	SubShader
	{
		Tags { "RenderType" = "GrassSimulation" }
		Pass
		{
		    Fog{Mode off}
		    Cull Off
		    Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
			CGPROGRAM
			
			#pragma target 5.0
			#pragma only_renderers d3d11
			//#pragma enable_d3d11_debug_symbols
			#pragma vertex vert
			#pragma hull hull
			#pragma domain domain
			#pragma fragment frag
			// Create three shader variants for geometry grass, billboard crossed grass and billboard screen faced grass
			#pragma multi_compile GRASS_BILLBOARD_CROSSED GRASS_BILLBOARD_SCREEN GRASS_GEOMETRY
			#pragma multi_compile __ BILLBOARD_GENERATION
			
			#include "GrassSimulation.cginc"
			#include "GrassShaderStageAttributes.cginc"
			#include "UnityCG.cginc"
			
			static const float PI = 3.141592;
			static const float PI_2_3 = 2.094394;

            struct UvData { float2 Position; };
            
            //Billboard Generation
            uniform float GrassType;
            
            //Once
            uniform float BillboardSize;
            uniform float BladeTextureMaxMipmapLevel;
            uniform float LodTessellationMax;
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
            uniform float4 NormalHeightUvCorrection;
            uniform int VertexCount;
            
            Texture2DArray<float4> GrassBlades0;
            SamplerState samplerGrassBlades0;
            Texture2DArray<float4> GrassBlades1;
            Texture2DArray GrassBillboards;
            SamplerState samplerGrassBillboards;
            Texture2D GrassMapTexture;
            Texture2D<float4> ParameterTexture; // width, bend, height, dirAlpha
            SamplerState samplerParameterTexture;
			StructuredBuffer<UvData> UvBuffer;	// pos.x 		pos.z
			
			//PerPatch
            uniform float4x4 PatchModelMatrix;
            uniform float4 PatchTexCoord; //x: xStart, y: yStart, z: width, w:height
			uniform float ParameterOffsetX;
			uniform float ParameterOffsetY;
			uniform float StartIndex;
			
			Texture2D NormalHeightTexture; //up.xyz, pos.y
			SamplerState samplerNormalHeightTexture;
			Texture2DArray<float4> SimulationTexture; //v1.xyz, tesslevel; v2.xyz, transition
			SamplerState samplerSimulationTexture;
            
            //PerFrame
            uniform float4 CamPos;
            uniform float4 CamUp;
            
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
            
                #ifdef GRASS_GEOMETRY
                    if (instanceID > transitionInstanceID){
                        return 0;
                    }
                    return SimulationTexture.SampleLevel(samplerSimulationTexture, float3(uv, 0), 0).w;
                #else
                    if (instanceID > transitionInstanceID){
                        return 0;
                    }
                    return 1.0;
                #endif
            }
			
			VSOut vert (uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				VSOut OUT;
				
				OUT.vertexID = vertexID;
				OUT.instanceID = instanceID;
				#ifdef GRASS_BILLBOARD_CROSSED
				OUT.uvLocal = UvBuffer[StartIndex + VertexCount * instanceID + (vertexID % VertexCount)].Position;
				#else
				OUT.uvLocal = UvBuffer[StartIndex + VertexCount * instanceID + vertexID].Position;
                #endif
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

        		float2 uvParameter = float2(ParameterOffsetX, ParameterOffsetY) + IN[0].uvLocal;
        		float2 uvNormalHeight = lerp(NormalHeightUvCorrection.xy, NormalHeightUvCorrection.zw, IN[0].uvLocal);
        		float2 uvGrassMap = lerp(PatchTexCoord.xy, PatchTexCoord.xy + PatchTexCoord.zw, IN[0].uvLocal);
        		float4 normalHeight = NormalHeightTexture.SampleLevel(samplerNormalHeightTexture, uvNormalHeight, 0);
        		float4 SimulationData0 = SimulationTexture.SampleLevel(samplerSimulationTexture, float3(IN[0].uvLocal, 0), 0);
				float4 SimulationData1 = SimulationTexture.SampleLevel(samplerSimulationTexture, float3(IN[0].uvLocal, 1), 0);
        		float4 grassMapData = GrassMapTexture.SampleLevel(samplerParameterTexture, uvGrassMap, 0);
        		
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

        		OUT.pos = mul(PatchModelMatrix, float4(IN[0].uvLocal.x, normalHeight.w, IN[0].uvLocal.y, 1.0)).xyz;
        		OUT.parameters = ParameterTexture.SampleLevel(samplerParameterTexture, uvParameter, 0);
        		OUT.bladeUp = normalize(normalHeight.xyz);
        		OUT.v1 = SimulationData0.xyz;
        		OUT.v2 = SimulationData1.xyz;
        		
        		#ifdef GRASS_BILLBOARD_SCREEN
        		float3 camDir = normalize(OUT.pos - CamPos);
        		float3 right = cross(camDir, CamUp.xyz);
        		OUT.bladeDir = normalize(cross(OUT.bladeUp, camDir));
        		#elif GRASS_BILLBOARD_CROSSED
        		float dirAlpha = OUT.parameters.w + PI_2_3 * floor(IN[0].vertexID / VertexCount);
        		float sd = sin(dirAlpha);
                float cd = cos(dirAlpha); 
                float3 tmp = normalize(float3(sd, sd + cd, cd));
               	OUT.bladeDir = normalize(cross(OUT.bladeUp, tmp));
        		#elif GRASS_GEOMETRY
        		float dirAlpha = OUT.parameters.w;
        		float sd = sin(dirAlpha);
                float cd = cos(dirAlpha); 
                float3 tmp = normalize(float3(sd, sd + cd, cd));
               	OUT.bladeDir = normalize(cross(OUT.bladeUp, tmp));
               	#endif
               	//We do not need dirAlpha in domainshader so we can use OUT.parameters for something else
               	//Calculate mipmaplevel for grass texture lookup based on tessellationfactor 
               	#ifdef GRASS_GEOMETRY
               	OUT.parameters.w = lerp(BladeTextureMaxMipmapLevel, 0.0, clamp(SimulationData0.w / LodTessellationMax, 0, 1));
               	#else
               	OUT.parameters.w = 0;
               	#endif
               	
               	#ifdef BILLBOARD_GENERATION
               	OUT.grassMapData.x = GrassType;
               	#else
               	OUT.grassMapData.x = grassMapData.x * 255;
               	#endif
               	OUT.grassMapData.yzw = grassMapData.yzw;
                  
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
        		float3 up = IN[0].bladeUp;
        		float3 bladeDir = IN[0].bladeDir;
        		#ifdef GRASS_GEOMETRY
        		float3 v1 = pos + IN[0].v1 * IN[0].transitionFactor;
        		float3 v2 = pos + IN[0].v2 * IN[0].transitionFactor;
        		float width = IN[0].parameters.x;
        		#else
        		float3 v1 = pos + IN[0].v1 * BillboardSize * IN[0].transitionFactor;
        		float3 v2 = pos + IN[0].v2 * BillboardSize * IN[0].transitionFactor;
        		float width = length(IN[0].v2) * BillboardSize * IN[0].transitionFactor;
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
                //float4 texSample0 = GrassBillboards.SampleLevel(samplerGrassBillboards, float3(uv.xy, IN[0].grassMapData.x), IN[0].parameters.w);
                OUT.pos = mul(UNITY_MATRIX_VP, float4(lerp(i1, i2, u), 1.0));
                OUT.color = float4(0, 0, 0, 0);
                OUT.uvw = float3(uv.xy, IN[0].grassMapData.x);
            #elif GRASS_BILLBOARD_SCREEN
                OUT.pos = mul(UNITY_MATRIX_VP, float4(lerp(i1, i2, u), 1.0));
                OUT.color = float4(0, 0, 0, 0);
                OUT.uvw = float3(uv.xy, IN[0].grassMapData.x);
            #elif GRASS_GEOMETRY
                float4 texSample0 = GrassBlades0.SampleLevel(samplerGrassBlades0, float3(uv.xy, IN[0].grassMapData.x), IN[0].parameters.w);
                float4 texSample1 = GrassBlades1.SampleLevel(samplerGrassBlades0, float3(uv.xy, IN[0].grassMapData.x), IN[0].parameters.w);
                float3 translation = normal * width * (0.5 - abs(u - 0.5)) * ((1.0 - floor(v)) * texSample1.r); //position auf der normale verschoben bei mittelachse -> ca rechter winkel (u mit hat function)
                float t = u + ((texSample0.r*u) + ((1.0-texSample0.r)*omu));
                float3 outpos = lerp(i1, i2, t) + translation;
                OUT.pos = mul(UNITY_MATRIX_VP, float4(outpos, 1.0));
                OUT.color = float4(float3(texSample0.g, texSample0.b, texSample0.a), 1);
                OUT.uvw = float3(uv.xy, IN[0].grassMapData.x);
            #endif
                /*if(dot(lightDirection, teNormal) > 0.0)
                    teNormal = -teNormal;
            
                teDebug = tcDebug;
                gl_Position = vpMatrix * vec4(position, 1.0);
                tePosition = vec4(position, 1.5 * abs(sin(shapeConstant * tcV1.w)));
       
        		float3 pos = float3(IN[0].pos.x + (uv.x - 0.5)*0.1, IN[0].pos.y + uv.y, IN[0].pos.z);*/
      
                //DEBUG COLORING
                /*float distance = length(pos - CamPos);
        		float debugT = (distance - LodDistanceFullDetail) / (LodDistanceBillboard - LodDistanceFullDetail);
        		float debugInterpolant = frac(lerp(LodDensityBillboardDistance, LodDensityFullDetailDistance, debugT));

        		if (debugT < 0){
        		    OUT.color = float4(1,1,1,1);
        		} else if (debugT > 1){
        		    OUT.color = float4(0,1,0,1);
        		} else {
        		    OUT.color = float4(lerp(float3(1,0,0), float3(0,0,1), debugInterpolant), 1);
        		}*/
      
                //OUT.pos = mul(UNITY_MATRIX_VP, float4(outpos, 1.0));
                //OUT.color = float4(float3(texSample0.g, texSample0.b, texSample0.a), 1);
                //#ifdef GRASS_GEOMETRY
                //OUT.color = float4(float3(pos.y / 40, pos.y / 40, pos.y / 40), 1);
                //#else
                //OUT.color = float4(lerp(float3(0.44, 0.61, 0.2), float3(0.12, 0.18, 0.055), 1-v), 1);
                //#endif
           
        		return OUT;
}
			
			float4 frag (FSIn IN) : SV_TARGET
			{
			    #ifdef GRASS_GEOMETRY
			    return IN.color;
			    #else
			    //float4 billboardSample = SimulationTexture.SampleLevel(samplerSimulationTexture, float3(IN.uvw.xy, 0), 0);
                float4 billboardSample = GrassBillboards.SampleLevel(samplerGrassBillboards, IN.uvw, 0);
                return billboardSample;
                #endif
			}
			ENDCG
		}
	}
}
