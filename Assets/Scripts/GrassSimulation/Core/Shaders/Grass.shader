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
			// Create four shader variants for blossoms, geometry grass, billboard crossed grass and billboard screen faced grass
			#pragma multi_compile GRASS_BILLBOARD_CROSSED GRASS_BILLBOARD_SCREEN GRASS_GEOMETRY GRASS_BLOSSOM
			#pragma multi_compile __ BILLBOARD_GENERATION
			
			#include "GrassFuncs.cginc"
			#include "GrassStageAttributes.cginc"
			#include "UnityCG.cginc"
			
			static const float PI = 3.141592;
			static const float PI_1_PI = 0.318309;
			static const float PI_2_3 = 2.094394;
			static const float TRANSITION_EPSILON = 0.05;
			static const float GRASSMAP_WIDTH_INFLUENCE = 0.5;

            struct UvData { 
                float2 Position;
                int type;
            };
            
            //Billboard Generation
            uniform int GrassType;
            uniform int RenderNormals;
            uniform float MinGrassBladeWidth;
            //Billboard Specific
            uniform float BillboardAspect;
            uniform float BillboardHeightAdjustment;
            uniform int RepetitionCount;
            
            //Once
            uniform float BillboardAlphaCutoff;
            uniform float BladeTextureMaxMipmapLevel;
            uniform float BladeHeightCullingThreshold;
            uniform float LodInstancesGeometry;
            uniform float LodInstancesBillboardCrossed;
            uniform float LodInstancesBillboardScreen;
            uniform float LodGeometryTransitionSegments;
            uniform float LodBillboardCrossedTransitionSegments;
            uniform float LodBillboardScreenTransitionSegments;
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
            uniform int BlossomCount;
            uniform int EnableHeightTransition;
            uniform int RenderDebugColor;
            
            #ifdef GRASS_BLOSSOM
                Texture2DArray<float4> GrassBlossom0;
                SamplerState samplerGrassBlossom0;
                Texture2DArray<float4> GrassBlossom1;
                SamplerState samplerGrassBlossom1;
            #else // Is needed everywhere except for blossoms
                Texture2DArray<float4> GrassBlades0;
                SamplerState samplerGrassBlades0;
            #endif
            #ifdef GRASS_GEOMETRY
                Texture2DArray<float4> GrassBlades1;
                SamplerState samplerGrassBlades1;
            #endif
            #ifdef GRASS_BILLBOARD_SCREEN
                Texture2DArray GrassBillboards;
                SamplerState samplerGrassBillboards;
                Texture2DArray GrassBillboardNormals;
            #endif
            #ifdef GRASS_BILLBOARD_CROSSED
                Texture2DArray GrassBillboards;
                SamplerState samplerGrassBillboards;
                Texture2DArray GrassBillboardNormals;
            #endif
            
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
            
            //Lightning
            uniform float AmbientLightFactor;
            uniform float3 LightDirection;
            uniform float3 LightColor;
            uniform float LightIntensity;

            uniform float4 GravityVec;
			
			float GetTessellationLevel(float distance, uint instanceID, float2 uv, int type)
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
                #else
                    transition = SingleLerp(LodInstancesGeometry * grassMapData.y, distance,
                        LodDistanceGeometryStart, LodDistanceGeometryEnd);
                #endif
                
                //instanceID *= 0.5;
                uint transitionInstanceID = (transition);
                
                //Cull if instance should not be visible
                if (transition < instanceID) return 0;
                //if (instanceID < transitionInstanceID) return 0;
                //Cull if transition is too small to reduce aliasing
                if (instanceID == transitionInstanceID) {
                    if (EnableHeightTransition == 0) return 0;
                    //if (smoothstep(0, 1, frac(transition)) < BladeHeightCullingThreshold) return 0;
                    if (lerp(0, 1, frac(transition)) < BladeHeightCullingThreshold) return 0;
                }
                //Cull if height Modifier is too low ro teduce aliasing
                if (grassMapData.z < BladeHeightCullingThreshold) return 0;
                
                #ifdef GRASS_GEOMETRY
                    return SingleLerpMinMax(LodTessellationMin, LodTessellationMax, distance, LodDistanceTessellationMin, LodDistanceTessellationMax);
                #elif GRASS_BLOSSOM
                    //if (type >= BlossomCount) return 0;
                    if (BlossomCount <= type) 
                    {
                        return 0;
                    }
                    return round(SingleLerpMinMax(2, 8, distance, LodDistanceTessellationMin, LodDistanceTessellationMax)) * 2;
                #else
                    return 1.0;
                #endif
            }
			
			VSOut vert (uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				VSOut OUT;
				
				OUT.vertexID = vertexID;
				#ifdef GRASS_BILLBOARD_CROSSED
				    int i = StartIndex + (VertexCount * RepetitionCount) * instanceID + (vertexID % VertexCount) * RepetitionCount;
				    OUT.instanceID = instanceID / LodBillboardCrossedTransitionSegments;
				#elif GRASS_BILLBOARD_SCREEN
				    int i = StartIndex + (VertexCount * RepetitionCount) * instanceID + (vertexID * RepetitionCount);
				    OUT.instanceID = instanceID / LodBillboardScreenTransitionSegments;
				#else
				    int i = StartIndex + VertexCount * instanceID + vertexID;
				    OUT.instanceID = instanceID / LodGeometryTransitionSegments;
                #endif
				#ifdef BILLBOARD_GENERATION
				    OUT.type = GrassType;
				#else
				    OUT.type = UvBuffer[i].type;
				#endif
				OUT.uvLocal = UvBuffer[i].Position;
				return OUT;
			}
			
			HSConstOut hullPatchConstant( InputPatch<VSOut, 1> IN)
    		{
        		HSConstOut OUT = (HSConstOut)0;
        		float distance = SimulationTexture1.SampleLevel(samplerSimulationTexture0, IN[0].uvLocal, 0).w;
        		
        		float level = GetTessellationLevel(distance, IN[0].instanceID, IN[0].uvLocal, IN[0].type);
        		
        		#if GRASS_GEOMETRY
                    OUT.TessFactor[0] = level;	//left
                    OUT.TessFactor[1] = 2.0;	//bottom
                    OUT.TessFactor[2] = level;	//right
                    OUT.TessFactor[3] = 1.0;	//top
                    OUT.InsideTessFactor[0] = 1.0;
                    OUT.InsideTessFactor[1] = level;
                #elif GRASS_BLOSSOM
                    OUT.TessFactor[0] = level;	//left
                    OUT.TessFactor[1] = level;	//bottom
                    OUT.TessFactor[2] = level;	//right
                    OUT.TessFactor[3] = level;	//top
                    OUT.InsideTessFactor[0] = level;
                    OUT.InsideTessFactor[1] = level;
                #else
                    OUT.TessFactor[0] = level;	//left
                    OUT.TessFactor[1] = level;	//bottom
                    OUT.TessFactor[2] = level;	//right
                    OUT.TessFactor[3] = level;	//top
                    OUT.InsideTessFactor[0] = level;
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
        		HSOut OUT = (HSOut)0;
                
        	    float2 uvGlobal = lerp(PatchTexCoord.xy, PatchTexCoord.xy + PatchTexCoord.zw, IN[0].uvLocal);
        		float2 uvParameter = float2(ParameterOffsetX, ParameterOffsetY) + IN[0].uvLocal;
        		float4 normalHeight = NormalHeightTexture.SampleLevel(samplerNormalHeightTexture, uvGlobal, 0);
        		float4 SimulationData0 = SimulationTexture0.SampleLevel(samplerSimulationTexture0, IN[0].uvLocal, 0);
				float4 SimulationData1 = SimulationTexture1.SampleLevel(samplerSimulationTexture0, IN[0].uvLocal, 0);
        		float4 grassMapData = GrassMapTexture.SampleLevel(samplerNormalHeightTexture, uvGlobal, 0);
        		
        		OUT.pos = mul(PatchModelMatrix, float4(IN[0].uvLocal.x, normalHeight.w, IN[0].uvLocal.y, 1.0)).xyz;

        		float distance = SimulationData1.w;
        		
        		float transition = 0;
                #ifdef GRASS_BILLBOARD_CROSSED
                    transition = DoubleLerp(LodInstancesBillboardCrossed * grassMapData.y, distance,
                        LodDistanceBillboardCrossedStart, LodDistanceBillboardCrossedPeak, LodDistanceBillboardCrossedEnd);
                #elif GRASS_BILLBOARD_SCREEN
                    transition = DoubleLerp(LodInstancesBillboardScreen * grassMapData.y, distance,
                        LodDistanceBillboardScreenStart, LodDistanceBillboardScreenPeak, LodDistanceBillboardScreenEnd);
                #else
                    transition = SingleLerp(LodInstancesGeometry * grassMapData.y, distance,
                        LodDistanceGeometryStart, LodDistanceGeometryEnd);
                #endif
                
        		//TODO: Compare performance of condition
        		//TODO: Check if height transition is disabled
        		#ifdef BILLBOARD_GENERATION
        		    OUT.transitionFactor = 1;
        		#else
                    uint instanceID = (transition);
                    if (instanceID == IN[0].instanceID){
                        //OUT.transitionFactor = smoothstep(0, 1, frac(transition)) * EnableHeightTransition;
                        OUT.transitionFactor = lerp(0, 1, frac(transition)) * EnableHeightTransition;
                    } else {
                        OUT.transitionFactor = 1;
                    }
        		#endif

        		OUT.parameters = ParameterTexture.SampleLevel(samplerParameterTexture, uvParameter, 0);
        		#ifdef BILLBOARD_GENERATION
        		#else
        		    OUT.parameters.x -= OUT.parameters.x * GRASSMAP_WIDTH_INFLUENCE * (1 - grassMapData.z);
        		#endif
        		//OUT.bladeUp = normalize(normalHeight.xyz);
        		
        		OUT.bladeUp = normalize(normalHeight.xyz - (GravityVec.xyz * GravityVec.w) * 0.5);
        		OUT.v1 = SimulationData0.xyz;
        		OUT.v2 = SimulationData1.xyz;
        		
        		float3 camDir = OUT.pos - CamPos.xyz;
        		
        		#ifdef GRASS_BILLBOARD_SCREEN
                    camDir = normalize(camDir);
                    //float3 right = cross(camDir, CamUp.xyz);
                    OUT.bladeDir = normalize(cross(OUT.bladeUp, camDir));
                    
                    float dirAlpha = OUT.parameters.w;
                    float sd = sin(dirAlpha);
                    float cd = cos(dirAlpha); 
                    float3 tmp = normalize(float3(sd, sd + cd, cd));
                    OUT.bitangent = normalize(cross(OUT.bladeUp, tmp));
                    //camDir = normalize(camDir);
                    //float3 right = cross(camDir, CamUp.xyz);
                    //float3 sunright = normalize(cross(normalize(LightDirection), OUT.bladeUp));
                    //OUT.bitangent = sunright;
                    //OUT.bitangent = normalize(cross(OUT.bladeUp, camDir));
                #elif GRASS_BILLBOARD_CROSSED
                    float dirAlpha = OUT.parameters.w + PI_2_3 * floor(IN[0].vertexID / VertexCount);
                    float sd = sin(dirAlpha);
                    float cd = cos(dirAlpha); 
                    float3 tmp = normalize(float3(sd, sd + cd, cd));
                    OUT.bladeDir = normalize(cross(OUT.bladeUp, tmp));
                    
                    dirAlpha = OUT.parameters.w;
                    sd = sin(dirAlpha);
                    cd = cos(dirAlpha); 
                    tmp = normalize(float3(sd, sd + cd, cd));
                    OUT.bitangent = OUT.bladeDir; //normalize(cross(OUT.bladeUp, tmp));
                    /*camDir = normalize(camDir);
                    float3 right = cross(camDir, CamUp.xyz);
                    OUT.bitangent = normalize(cross(OUT.bladeUp, camDir));*/
                    //float3 sunright = normalize(cross(normalize(LightDirection), OUT.bladeUp));
                    //OUT.bitangent = sunright;
        		#else
                    float dirAlpha = OUT.parameters.w;
                    float sd = sin(dirAlpha);
                    float cd = cos(dirAlpha); 
                    float3 tmp = normalize(float3(sd, sd + cd, cd));
                    OUT.bladeDir = normalize(cross(OUT.bladeUp, tmp));
                    #ifdef BILLBOARD_GENERATION
                        float dirFactor = SingleLerpMinMax(1, 0, distance, LodDistanceGeometryStart, LodDistanceGeometryEnd);
                        tmp = float3(1, 0, 0);
                        OUT.bladeDir = tmp;
                    #endif
                        
                    //OUT.bladeDir = normalize(lerp(OUT.bladeDir, tmp, dirFactor));
                    
                    /*camDir = normalize(camDir);
                    float3 right = cross(camDir, CamUp.xyz);
                    OUT.bitangent = normalize(cross(OUT.bladeUp, camDir));*/
                    /*#ifdef BILLBOARD_GENERATION
                        OUT.bitangent = float3(1, 0, 0);
                    #else
                        float3 sunright = normalize(cross(normalize(LightDirection), OUT.bladeUp));
                        OUT.bitangent = sunright;
                    #endif*/
               	#endif
                OUT.grassMapData.x = IN[0].type;

               	OUT.grassMapData.yzw = grassMapData.yzw;
               	//We do not need dirAlpha in domainshader so we can use OUT.parameters for something else
               	//Calculate mipmaplevel for grass texture lookup based on tessellationfactor 
               	#ifdef GRASS_GEOMETRY
                    float tesslevel = SingleLerpMinMax(LodTessellationMin, LodTessellationMax, distance, LodDistanceTessellationMin, LodDistanceTessellationMax);
                    OUT.parameters.w = lerp(BladeTextureMaxMipmapLevel, 0.0, saturate(tesslevel / LodTessellationMax));
                    #ifdef BILLBOARD_GENERATION
                        OUT.parameters.w = lerp(BladeTextureMaxMipmapLevel, 0.0, saturate(tesslevel / LodTessellationMax));
                    #else
                        OUT.parameters.w = lerp(BladeTextureMaxMipmapLevel, 0.0, saturate(tesslevel / LodTessellationMax));
                    #endif
                #elif GRASS_BLOSSOM
                    float tesslevel = round(SingleLerpMinMax(2, 8, distance, LodDistanceTessellationMin, LodDistanceTessellationMax)) * 2;
                    OUT.parameters.w = lerp(BladeTextureMaxMipmapLevel, 0.0, saturate(tesslevel / 8));
                    OUT.parameters.y = tesslevel;
                    #ifdef BILLBOARD_GENERATION
                        OUT.grassMapData.z = 1;
                    #else
                        OUT.grassMapData.z = SingleLerpMinMax(1, 0, distance, LodDistanceTessellationMin, LodDistanceTessellationMax);
                    #endif
                    //OUT.grassMapData.z = 0;
               	    //OUT.parameters.w = 0;
               	#else
               	    OUT.parameters.w = 0;
               	#endif
               	
                  
        		return OUT;
            }
               
            [domain("quad")]
    		DSOut domain( HSConstOut hullConstData, 
    		            const OutputPatch<HSOut, 1> IN, 
    					float2 uv : SV_DomainLocation)
    		{
        		DSOut OUT = (DSOut)0;

                #ifdef GRASS_BLOSSOM
                    float3 pos = IN[0].pos;
                    float3 bladeDir = IN[0].bladeDir;
                    float3 v1 = IN[0].v1 * IN[0].transitionFactor;
                    float3 v2 = IN[0].v2 * IN[0].transitionFactor;
                    float width = IN[0].parameters.x;
                    float height = IN[0].parameters.z * IN[0].transitionFactor;
                    
                    float3 tangent = normalize(lerp(normalize(v2 - v1), normalize(CamPos - (pos + v2)), IN[0].grassMapData.z));
                    float3 normal = normalize(cross(tangent, bladeDir));
                    float3 bitangent = cross(tangent, normal); 
                    
                    float2 uvDirection = (uv - float2(0.5, 0.5)) * 2;
                    uvDirection = length(uvDirection) == 0 ? float2(0, 0) : normalize(uvDirection);
                    
                    float uParam = abs((uv.x - 0.5) * 2);
                    float vParam = abs((uv.y - 0.5) * 2);
                    
                    //float h = (IN[0].parameters.w + 1.0) / 16;
                    //float t = max(uParam, vParam);
                    float t = lerp(0, 1, max(uParam, vParam));
                    float h = 0.5 / IN[0].parameters.y;
                    h = t == 1 ? -h : h;
                    float betaT = length(uvDirection) == 0 ? 0 : atan2(uvDirection.x, uvDirection.y) * PI_1_PI + 1;//1 - cos(uv.x * PI_1_2) * cos(uv.y * PI_1_2);
                    float4 blossomData0 = GrassBlossom0.SampleLevel(samplerGrassBlossom0, float3(float2(0, t), IN[0].grassMapData.x), IN[0].parameters.w);

                    float beta = GrassBlossom0.SampleLevel(samplerGrassBlossom0, float3(float2(0, betaT), IN[0].grassMapData.x), IN[0].parameters.w).r;
                    float gamma0 = blossomData0.g; // Translation Factor along uvDirection
                    float delta0 = blossomData0.b; // Translation Factor along tangent
                    delta0 *= height / 3; 
                    #ifdef BILLBOARD_GENERATION
                        delta0 *= -1;
                    #endif
                    
                    float2 uvDirection0 = uvDirection * gamma0 * width;
                    
                    float3 offset0 = (uvDirection0.x * normal + uvDirection0.y * bitangent + delta0 * tangent);
                    pos = pos + v2 + beta * offset0;
                    
                    OUT.pos = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
                    OUT.uvwd = float4(uv, IN[0].grassMapData.x, lerp(0.8, 0.2, IN[0].grassMapData.y));

                    if (length(uvDirection) == 0 || abs(delta0) < 1e-3) //test for delta instead
                    {
                        OUT.normal = tangent;
                    } else 
                    {
                        float4 blossomData1 = GrassBlossom0.SampleLevel(samplerGrassBlossom0, float3(float2(0, t + h), IN[0].grassMapData.x), IN[0].parameters.w);
                        float gamma1 = blossomData1.g;
                        float delta1 = blossomData1.b;  
                        delta1 *= height / 3;
                        #ifdef BILLBOARD_GENERATION
                            delta1 *= -1;
                        #endif
                        float2 uvDirection1 = uvDirection * gamma1 * width;
                        
                        float3 offset1 = (uvDirection1.x * normal + uvDirection1.y * bitangent + delta1 * tangent);
                        float3 derivate = t == 1 ? normalize(offset1 - offset0) : normalize(offset0 - offset1);
                        
                        float3 newTangent = normalize(uvDirection.x * normal + uvDirection.y * bitangent) * sign(delta0); 
                        float3 bitangentNew = normalize(cross(derivate, newTangent));
                        OUT.normal = normalize(cross(derivate, bitangentNew));
                    }
                    //OUT.normal = derivate;
                #else
                
                    float3 pos = IN[0].pos;
                    float3 up = IN[0].bladeUp; //TODO: Use same correction as in compute shader
                    float3 bladeDir = IN[0].bladeDir;
                    #ifdef GRASS_GEOMETRY
                        float3 v1 = IN[0].v1 * IN[0].transitionFactor;
                        float3 v2 = IN[0].v2 * IN[0].transitionFactor;
                        #ifdef BILLBOARD_GENERATION
                            float width = max(MinGrassBladeWidth, IN[0].parameters.x);
                        #else
                            float width = IN[0].parameters.x;
                         #endif
                    #else
                        //float3 v1 = pos + IN[0].v1 * IN[0].transitionFactor;
                        //float3 v2 = pos + IN[0].v2 * IN[0].transitionFactor;
                        float3 v1 = BillboardHeightAdjustment * IN[0].v1 * IN[0].transitionFactor;
                        float3 v2 = BillboardHeightAdjustment * IN[0].v2 * IN[0].transitionFactor;
                        float width = length(IN[0].v2) * BillboardAspect * IN[0].transitionFactor;
                    #endif
    
                    float u = uv.x;
                    float v = uv.y;

                    float3 off = bladeDir * width * 0.5;
                                
                    float3 h1 = pos + v * v1;
                    float3 h2 = pos + v1 + v * (v2 - v1);
                    float3 i1 = h1 + v * (h2 - h1);
                    float3 i2 = i1 + off;
                    i1 -= off;
                                
                    #ifdef GRASS_BILLBOARD_CROSSED
                        float3 bitangent = IN[0].bitangent;
                        float3 tangent = uv.y == 0 ? normalize(IN[0].v1) : normalize(IN[0].v2);
                    #elif GRASS_BILLBOARD_SCREEN
                        float3 bitangent = IN[0].bitangent;
                        float3 tangent = uv.y == 0 ? normalize(IN[0].v1) : normalize(IN[0].v2);
                        //float3 tangent = uv.y == 0 ? normalize(IN[0].v1) : normalize(IN[0].v2);
                    #else
                        float3 bitangent = IN[0].bladeDir;
                        float3 tangent;
                    
                        float3 h1h2 = h2 - h1;
                        if(dot(h1h2, h1h2) < 1e-3)
                        {
                            tangent = up;
                            //tangent = normalize(cross(normalize(v2 - pos), bitangent));
                        }
                        else
                        {
                            tangent = normalize(h1h2);
                        }
                    #endif
                
                    float3 normal = normalize(cross(tangent, bitangent));
    
                    #ifdef GRASS_BILLBOARD_CROSSED
                        OUT.pos = mul(UNITY_MATRIX_VP, float4(lerp(i1, i2, u), 1.0));
                        OUT.uvwd = float4(uv.xy, IN[0].grassMapData.x, lerp(0.9, 0.2, IN[0].grassMapData.y));
                        OUT.tangent = tangent;
                    #elif GRASS_BILLBOARD_SCREEN
                        OUT.pos = mul(UNITY_MATRIX_VP, float4(lerp(i1, i2, u), 1.0));
                        OUT.uvwd = float4(uv.xy, IN[0].grassMapData.x, lerp(0.9, 0.2, IN[0].grassMapData.y));
                        OUT.tangent = tangent;
                    #elif GRASS_GEOMETRY
                        float4 texSample0 = GrassBlades0.SampleLevel(samplerGrassBlades0, float3(uv.xy, IN[0].grassMapData.x), IN[0].parameters.w);
                        float3 translation = texSample0.g * normal * width * abs(u - 0.5); //* texSample0.r //position auf der normale verschoben bei mittelachse -> ca rechter winkel (u mit hat function)
                        
                        float t = 0.5 + (u - 0.5) * texSample0.r;
    
                        float3 outpos = lerp(i1, i2, t) + translation;
                        OUT.pos = mul(UNITY_MATRIX_VP, float4(outpos, 1.0));
                        OUT.uvwd = float4(uv.xy, IN[0].grassMapData.x, lerp(0.8, 0.2, IN[0].grassMapData.y));
                    #endif
                    
                    OUT.normal = normal;
                #endif
                
                

        		return OUT;
            }

            float GetRadiance(in float Kd, in float Ao, in float Ia, in float3 N, in float3 L, in float gamma, in float Id, in float d, in float beta)
            {
                // Following 2008 Boulanger Lambert reflectance model p.60
                /*
                float Kd; // Diffuse reflectance factor (0..1) of grass bladeColor
                float Ao; // Ambient occlusion factor (0..1), lower close to the ground  /// lerp(IN.uvwd.w, 1, IN.uvwd.y)
                float Ia; // Intensity ambient light
                
                float3 N; // Normal of grassblade at surface point
                float3 L; // Light Direction
                
                float gamma; // The blades translucency
                float Id; // Intensity light source
                float d;  // Distance between light source and surface point
                float beta; // light attenuation
                */
                float Iambient = Kd * Ao * Ia;
                float Idiffuse = Kd * max(dot(N, L), 0);
                float Itranslucent = gamma * Kd * max(dot(-N, L), 0);
                float attenuation = Id / (1 + beta * d * d);
                
                return Iambient + /* attenuation * */ (Idiffuse + Itranslucent);
            }

			float4 frag (FSIn IN) : SV_TARGET
			{
			    #ifdef BILLBOARD_GENERATION
			        if (RenderNormals == 1) {
			            return float4((IN.normal + 1)/2, 1);
			        }
			        #ifdef GRASS_GEOMETRY
                        float4 bladeColor = GrassBlades1.Sample(samplerGrassBlades1, IN.uvwd.xyz);
                        bladeColor.a = 1;
                        return bladeColor;
                    #else 
                        // GRASS_BLOSSOM
                        float4 blossomColor = GrassBlossom1.Sample(samplerGrassBlossom1, IN.uvwd.xyz);
                        return blossomColor;
                    #endif
                #elif GRASS_GEOMETRY
                    float2 bladeLightningData = GrassBlades0.Sample(samplerGrassBlades0, IN.uvwd.xyz).ba;
                    float Kd = bladeLightningData.x;
                    float Ao = lerp(IN.uvwd.w, 1, IN.uvwd.y);
                    float Ia = AmbientLightFactor;
                    float3 N = IN.normal;
                    float3 L = LightDirection;
                    float gamma = bladeLightningData.y;
                    float Id = LightIntensity;
                    float d = 0;
                    float beta = 0;
                    
                    float radiance = GetRadiance(Kd, Ao, Ia, N, L, gamma, Id, d, beta);
                    
                    float4 bladeColor = GrassBlades1.Sample(samplerGrassBlades1, IN.uvwd.xyz);
                    if (RenderDebugColor == 1) {
                        bladeColor.xyz = float3(0.86, 0.35, 0.27);
                    }
                    bladeColor.xyz *= radiance;
                    bladeColor.xyz *= LightColor;
                    //bladeColor.xyz = (N + 1)/2;
                    return bladeColor;
                #elif GRASS_BLOSSOM
                    float Kd = GrassBlossom0.Sample(samplerGrassBlossom0, IN.uvwd.xyz).a;
                    float Ao = 1;
                    float Ia = AmbientLightFactor;
                    float3 N = IN.normal;
                    float3 L = LightDirection;
                    float gamma = 1;
                    float Id = LightIntensity;
                    float d = 0;
                    float beta = 0;
                    
                    float radiance = GetRadiance(Kd, Ao, Ia, N, L, gamma, Id, d, beta);
                    
                    float4 blossomColor = GrassBlossom1.Sample(samplerGrassBlossom1, IN.uvwd.xyz);
                    if (RenderDebugColor == 1) {
                        blossomColor.xyz = float3(0.86, 0.49, 0);
                    }
                    blossomColor.xyz *= radiance;
                    blossomColor.xyz *= LightColor;
                    //blossomColor.xyz = (N + 1)/2;
                    //blossomColor.xyz = L;
                    //blossomColor.xyz = IN.uvwd.xyz;
                    return blossomColor;
                #else
                    float3 billboardNormalTangentSpace = 2 * GrassBillboardNormals.Sample(samplerGrassBillboards, IN.uvwd.xyz).xyz - 1.0;
                    float3 bitangent = normalize(cross(IN.normal, IN.tangent));
                    //float3 N = normalize(IN.tangent * billboardNormalTangentSpace.x + bitangent * billboardNormalTangentSpace.y + IN.normal * billboardNormalTangentSpace.z);
                    //float3 N = normalize(IN.tangent * billboardNormalTangentSpace.x + bitangent * billboardNormalTangentSpace.z + IN.normal * billboardNormalTangentSpace.y);
                    //float3 N = normalize(IN.tangent * billboardNormalTangentSpace.y + bitangent * billboardNormalTangentSpace.x + IN.normal * billboardNormalTangentSpace.z);
                    //float3 N = normalize(IN.tangent * billboardNormalTangentSpace.y + bitangent * billboardNormalTangentSpace.z + IN.normal * billboardNormalTangentSpace.x);
                    //float3 N = normalize(IN.tangent * billboardNormalTangentSpace.z + bitangent * billboardNormalTangentSpace.x + IN.normal * billboardNormalTangentSpace.y);
                    //float3 N = normalize(IN.tangent * billboardNormalTangentSpace.z + bitangent * billboardNormalTangentSpace.y + IN.normal * billboardNormalTangentSpace.x);
                    //N = IN.normal;
                    float3 N = IN.normal;
                    
                    float2 bladeLightningData = GrassBlades0.Sample(samplerGrassBlades0, IN.uvwd.xyz).ba;
                    float Kd = bladeLightningData.x;
                    //float Ao = lerp(IN.uvwd.w, 1, clamp(IN.uvwd.y + 0.5, 0, 1));
                    float Ao = lerp(IN.uvwd.w, 1, IN.uvwd.y);
                    float Ia = AmbientLightFactor;
                    float3 L = LightDirection;
                    float gamma = bladeLightningData.y;
                    float Id = LightIntensity;
                    float d = 0;
                    float beta = 0;
                    
                    float radiance = GetRadiance(Kd, Ao, Ia, N, L, gamma, Id, d, beta);
                    
                    float4 billboardSample = GrassBillboards.Sample(samplerGrassBillboards, IN.uvwd.xyz);
                    if (RenderDebugColor == 1) {
                        #ifdef GRASS_BILLBOARD_CROSSED
                            billboardSample.xyz = float3(0.23, 0.31, 0.72);
                        #else
                            billboardSample.xyz = float3(0.65, 0.87, 0.4);
                        #endif
                    }
                    billboardSample.xyz *= radiance;
                    billboardSample.xyz *= LightColor;
                    //billboardSample.xyz = (N + 1)/2;
                    return billboardSample;
                #endif
			}
			ENDCG
		}
	}
}
