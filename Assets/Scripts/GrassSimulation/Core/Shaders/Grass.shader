Shader "GrassSimulation/Grass"
{
    Properties{
    [HideInInspector] _SrcBlend ("__src", Float) = 1.0
    [HideInInspector] _DstBlend ("__dst", Float) = 0.0
    [HideInInspector] _ZWrite ("__zw", Float) = 1.0
    [HideInInspector] _AlphaToMask ("__atm", Float) = 0.0
    }
	SubShader
	{
		Tags { "RenderType" = "GrassSimulation" }
		Pass
		{
		    Fog{Mode off}
		    Cull Off
		    Blend [_SrcBlend] [_DstBlend]
            ZWrite On //[_ZWrite]
            AlphaToMask [_AlphaToMask]
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
			
			#include "GrassFuncs.cginc"
			#include "GrassStageAttributes.cginc"
			#include "UnityCG.cginc"
			
			static const float PI = 3.141592;
			static const float PI_2_3 = 2.094394;
			static const float TRANSITION_EPSILON = 0.01;
			static const float GRASSMAP_WIDTH_INFLUENCE = 0.5;

            struct UvData { float2 Position; };
            
            //Billboard Specific
            uniform float GrassType;
            uniform float BillboardAspect;
            uniform float RepetitionCount;
            
            //Once
            uniform float BillboardAlphaCutoff;
            uniform float BladeTextureMaxMipmapLevel;
            uniform float BladeHeightCullingThreshold;
            uniform float LodInstancesGeometry;
            uniform float LodInstancesBillboardCrossed;
            uniform float LodInstancesBillboardScreen;
            uniform float LodDistanceGeometryStart;
            uniform float LodDistanceGeometryEnd;
            uniform float LodDistanceBillboardCrossedStart;		
            uniform float LodDistanceBillboardCrossedPeak;		
            uniform float LodDistanceBillboardCrossedEnd;		
            uniform float LodDistanceBillboardScreenStart;		
            uniform float LodDistanceBillboardScreenPeak;		
            uniform float LodDistanceBillboardScreenEnd;
            uniform float LodTessellationMin;
            uniform float LodTessellationMax;
            uniform float LodDistanceTessellationMin;
            uniform float LodDistanceTessellationMax;
            uniform float4 NormalHeightUvCorrection;
            uniform int VertexCount;
            uniform int EnableHeightTransition;
            
            Texture2DArray<float4> GrassBlades0;
            SamplerState samplerGrassBlades0;
            Texture2DArray<float4> GrassBlades1;
            SamplerState samplerGrassBlades1;
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
			Texture2D<float4> SimulationTexture0; //v1.xyz, collisionForce;
			SamplerState samplerSimulationTexture0;
			Texture2D<float4> SimulationTexture1; //v2.xyz, distance;
            
            //PerFrame
            uniform float4 CamPos;
            uniform float4 CamUp;
            uniform float4x4 ViewProjMatrix;

			float GetTessellationLevel(float distance, uint instanceID, float2 uv)
			{
                float transition = 0;
                float2 uvGlobal = lerp(PatchTexCoord.xy, PatchTexCoord.xy + PatchTexCoord.zw, uv);
                float4 grassMapData = GrassMapTexture.SampleLevel(samplerParameterTexture, uvGlobal, 0);
                
                #ifdef GRASS_BILLBOARD_CROSSED
                    transition = DoubleLerp(LodInstancesBillboardCrossed * grassMapData.y, distance,
                        LodDistanceBillboardCrossedStart, LodDistanceBillboardCrossedPeak, LodDistanceBillboardCrossedEnd);
                #elif GRASS_BILLBOARD_SCREEN
                    transition = DoubleLerp(LodInstancesBillboardScreen * grassMapData.y, distance,
                        LodDistanceBillboardScreenStart, LodDistanceBillboardScreenPeak, LodDistanceBillboardScreenEnd);
                #elif GRASS_GEOMETRY
                    transition = SingleLerp(LodInstancesGeometry * grassMapData.y, distance,
                        LodDistanceGeometryStart, LodDistanceGeometryEnd);
                #endif
                

                uint transitionInstanceID = floor(transition);
                
                //Cull if instance should not be visible
                 if (instanceID > transitionInstanceID) return 0;
                //Cull if transition is too small to reduce aliasing
                if (instanceID == transitionInstanceID && transition < TRANSITION_EPSILON) return 0;
                //Cull if height Modifier is too low ro teduce aliasing
                if (grassMapData.z < BladeHeightCullingThreshold) return 0;
            
                #ifdef GRASS_GEOMETRY
                    return SingleLerpMinMax(LodTessellationMin, LodTessellationMax, distance, LodDistanceTessellationMin, LodDistanceTessellationMax);
                #else
                    return 1.0;
                #endif
            }
			
			VSOut vert (uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				VSOut OUT;
				
				OUT.vertexID = vertexID;
				OUT.instanceID = instanceID;
				#ifdef GRASS_BILLBOARD_CROSSED
				    OUT.uvLocal = UvBuffer[StartIndex + (VertexCount * RepetitionCount) * instanceID + (vertexID % VertexCount) * RepetitionCount].Position;
				#elif GRASS_BILLBOARD_SCREEN
				    OUT.uvLocal = UvBuffer[StartIndex + (VertexCount * RepetitionCount) * instanceID + (vertexID * RepetitionCount)].Position;
				#else
				    OUT.uvLocal = UvBuffer[StartIndex + VertexCount * instanceID + vertexID].Position;
                #endif
                //#ifdef BILLBOARD_GENERATION
                //OUT.uvLocal = lerp(float3(0.25, 0.25, 0.25), float3(0.75, 0.75, 0.75), OUT.uvLocal);
                //#endif
				return OUT;
			}
			
			HSConstOut hullPatchConstant( InputPatch<VSOut, 1> IN)
    		{
        		HSConstOut OUT = (HSConstOut)0;
        		float distance = SimulationTexture1.SampleLevel(samplerSimulationTexture0, IN[0].uvLocal, 0).w;
        		
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
        		float2 uvGlobal = lerp(PatchTexCoord.xy, PatchTexCoord.xy + PatchTexCoord.zw, IN[0].uvLocal);
        		float4 normalHeight = NormalHeightTexture.SampleLevel(samplerNormalHeightTexture, uvGlobal, 0);
        		float4 SimulationData0 = SimulationTexture0.SampleLevel(samplerSimulationTexture0, IN[0].uvLocal, 0);
				float4 SimulationData1 = SimulationTexture1.SampleLevel(samplerSimulationTexture0, IN[0].uvLocal, 0);
        		float4 grassMapData = GrassMapTexture.SampleLevel(samplerNormalHeightTexture, uvGlobal, 0);
        		
        		OUT.pos = mul(PatchModelMatrix, float4(IN[0].uvLocal.x, normalHeight.w, IN[0].uvLocal.y, 1.0)).xyz;
        		
        		float3 camDir = OUT.pos - CamPos.xyz;
                //float distance = length(camDir);
        		float distance = SimulationData1.w;
        		
        		float transition = 0;
                #ifdef GRASS_BILLBOARD_CROSSED
                    transition = DoubleLerp(LodInstancesBillboardCrossed * grassMapData.y, distance,
                        LodDistanceBillboardCrossedStart, LodDistanceBillboardCrossedPeak, LodDistanceBillboardCrossedEnd);
                #elif GRASS_BILLBOARD_SCREEN
                    transition = DoubleLerp(LodInstancesBillboardScreen * grassMapData.y, distance,
                        LodDistanceBillboardScreenStart, LodDistanceBillboardScreenPeak, LodDistanceBillboardScreenEnd);
                #elif GRASS_GEOMETRY
                    transition = SingleLerp(LodInstancesGeometry * grassMapData.y, distance,
                        LodDistanceGeometryStart, LodDistanceGeometryEnd);
                #endif
                
        		//TODO: Compare performance of condition
        		//TODO: Check if height transition is disabled
        		uint instanceID = floor(transition);
        		if (instanceID == IN[0].instanceID){
        		    OUT.transitionFactor = smoothstep(0, 1, frac(transition)) * EnableHeightTransition;
        		} else {
        		    OUT.transitionFactor = 1;
        		}

        		OUT.parameters = ParameterTexture.SampleLevel(samplerParameterTexture, uvParameter, 0);
        		OUT.parameters.x -= OUT.parameters.x * GRASSMAP_WIDTH_INFLUENCE * (1 - grassMapData.z);
        		OUT.bladeUp = normalize(normalHeight.xyz);
        		OUT.v1 = SimulationData0.xyz;
        		OUT.v2 = SimulationData1.xyz;
        		
        		#ifdef GRASS_BILLBOARD_SCREEN
                    camDir = normalize(camDir);
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
                    float tesslevel = SingleLerpMinMax(LodTessellationMin, LodTessellationMax, distance, LodDistanceTessellationMin, LodDistanceTessellationMax);
                    OUT.parameters.w = lerp(BladeTextureMaxMipmapLevel, 0.0, clamp(tesslevel / LodTessellationMax, 0, 1));
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
                
				float3 pos = IN[0].pos;
        		float3 up = IN[0].bladeUp; //TODO: Use same correction as in compute shader
        		float3 bladeDir = IN[0].bladeDir;
                #ifdef GRASS_GEOMETRY
                    float3 v1 = pos + IN[0].v1 * IN[0].transitionFactor;
                    float3 v2 = pos + IN[0].v2 * IN[0].transitionFactor;
                    float width = IN[0].parameters.x;
                #else
                    float3 v1 = pos + IN[0].v1 * IN[0].transitionFactor;
                    float3 v2 = pos + IN[0].v2 * IN[0].transitionFactor;
                    float width = length(IN[0].v2) * BillboardAspect * IN[0].transitionFactor;
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
                
                //TODO: Can be removed?
                #ifdef BILLBOARD_GENERATION
                    float widthFactor = 1;
                #elif GRASS_GEOMETRY
                    //Width Correction
                    float4 i1V = mul(ViewProjMatrix, float4(i1, 1));
                    i1V = i1V / i1V.w;
                    float4 i2V = mul(ViewProjMatrix, float4(i2, 1));
                    i2V = i2V / i2V.w;
                    float4 widthV = i2V - i1V;
                    widthV.x = widthV.x * (_ScreenParams.x / 2);
                    widthV.y = widthV.y * (_ScreenParams.y / 2);
                    float screenWidth = length(widthV.xy);
                    float widthFactor = 1.0f - clamp((screenWidth - 1.0) / 2.0, 0, 1);
                    //widthFactor *= u;
                #endif
            
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
	            OUT.normal = normal;

                #ifdef GRASS_BILLBOARD_CROSSED
                    OUT.pos = mul(UNITY_MATRIX_VP, float4(lerp(i1, i2, u), 1.0));
                    //OUT.color = float4(0, 0, 0, 0);
                    OUT.uvwd = float4(uv.xy, IN[0].grassMapData.x, lerp(0.8, 0.2, IN[0].grassMapData.y));
                #elif GRASS_BILLBOARD_SCREEN
                    OUT.pos = mul(UNITY_MATRIX_VP, float4(lerp(i1, i2, u), 1.0));
                    //OUT.color = float4(0, 0, 0, 0);
                    OUT.uvwd = float4(uv.xy, IN[0].grassMapData.x, lerp(0.8, 0.2, IN[0].grassMapData.y));
                #elif GRASS_GEOMETRY
                    float4 texSample0 = GrassBlades0.SampleLevel(samplerGrassBlades0, float3(uv.xy, IN[0].grassMapData.x), IN[0].parameters.w);
                    
                    float3 translation = normal * width * (0.5 - abs(u - 0.5)) * ((1.0 - floor(v)) * texSample0.g); //position auf der normale verschoben bei mittelachse -> ca rechter winkel (u mit hat function)
                    float t = u + ((texSample0.r*u) + ((1.0-texSample0.r)*omu));
                    //t *= widthFactor;
                    float3 outpos = lerp(i1, i2, t) + translation;
                    OUT.pos = mul(UNITY_MATRIX_VP, float4(outpos, 1.0));
                    //OUT.color = float4(float3(texSample0.g, texSample0.b, texSample0.a), 1);
                    OUT.uvwd = float4(uv.xy, IN[0].grassMapData.x, lerp(0.8, 0.2, IN[0].grassMapData.y));
                #endif

        		return OUT;
            }

			float4 frag (FSIn IN) : SV_TARGET
			{
			    #ifdef BILLBOARD_GENERATION
                    float4 bladeColor = GrassBlades1.Sample(samplerGrassBlades1, IN.uvwd.xyz);
                    return bladeColor;
                #elif GRASS_GEOMETRY
                    float4 bladeColor = GrassBlades1.Sample(samplerGrassBlades1, IN.uvwd.xyz);
                    bladeColor.xyz *= lerp(IN.uvwd.w, 1, IN.uvwd.y);
                    return bladeColor;
                #else
                    float4 billboardSample = GrassBillboards.Sample(samplerGrassBillboards, IN.uvwd.xyz);
                    billboardSample.xyz *= lerp(IN.uvwd.w, 1, clamp(IN.uvwd.y + 0.2, 0, 1));
                    return billboardSample;
                #endif
			}
			ENDCG
		}
	}
}
