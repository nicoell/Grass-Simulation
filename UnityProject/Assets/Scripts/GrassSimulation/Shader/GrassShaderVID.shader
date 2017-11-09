Shader "GrassSimulation/GrassShaderVID"
{
	Properties
	{
	}
	SubShader
	{
		
		Pass
		{
		    Fog{Mode off}
		    Cull Off
			CGPROGRAM
			
			#pragma target 5.0
			
			#pragma only_renderers d3d11
			#pragma enable_d3d11_debug_symbols
			#pragma vertex vert
			#pragma hull hull
			#pragma domain domain
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			uniform float startIndex;
            uniform float4x4 patchModelMatrix;			
            												// X 			Y 			Z 			W
			StructuredBuffer<float4> SharedGrassDataBuffer;	// pos.x 		pos.z 		width, 		bend
			StructuredBuffer<float4> grassDataABuffer;		// up.x 		up.y 		up.z 		pos.y
			StructuredBuffer<float4> grassDataBBuffer;		// v1.x 		v1.y 		v1.z 		height
			StructuredBuffer<float4> grassDataCBuffer;		// v2.x 		v2.y 		v2.z 		dirAlpha
			StructuredBuffer<float4> tessDataBuffer;		// tessLevel 	NONE 		NONE 		NONE
			
			struct VSOut 
			{
			    uint bufferID : VertexID;
			};
			
			struct HSConstOut
			{
			    float TessFactor[4] : SV_TessFactor;
			    float InsideTessFactor[2] : SV_InsideTessFactor;
			};
			
			struct HSOut
			{
				float3 pos : POS;
			    uint bufferID : VertexID;
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
			
			VSOut vert (uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				VSOut OUT;
				OUT.bufferID = 64 * instanceID + vertexID;

				return OUT;
			}
			
			HSConstOut hullPatchConstant( InputPatch<VSOut, 1> IN)
    		{
        		HSConstOut OUT = (HSConstOut)0;
        		uint bufferID = IN[0].bufferID;
        		float level = tessDataBuffer[bufferID].x;
        		OUT.TessFactor[0] = level;
        		OUT.TessFactor[1] = 1.0;
        		OUT.TessFactor[2] = level;
        		OUT.TessFactor[3] = 1.0;
        		OUT.InsideTessFactor[0] = 1.0;
        		OUT.InsideTessFactor[1] = level;
        		return OUT;
            }

			[domain("quad")]
    		[partitioning("fractional_odd")]
    		[outputtopology("triangle_cw")]
    		[patchconstantfunc("hullPatchConstant")]
    		[outputcontrolpoints(1)]
    		HSOut hull( InputPatch<VSOut, 1> IN, uint i : SV_OutputControlPointID )
    		{
        		HSOut OUT = (HSOut)0;
        		OUT.pos = mul(patchModelMatrix, float4(SharedGrassDataBuffer[startIndex + IN[0].bufferID].x, grassDataABuffer[IN[0].bufferID].w, SharedGrassDataBuffer[startIndex + IN[0].bufferID].y, 1.0)).xyz;
        		OUT.bufferID = IN[0].bufferID;
        		return OUT;
            }
               
            [domain("quad")]
    		DSOut domain( HSConstOut hullConstData, 
    		            const OutputPatch<HSOut, 1> IN, 
    					float2 uv : SV_DomainLocation)
    		{
        		DSOut OUT = (DSOut)0;
        		
				//pos.xz, width, bend
				//xyz: upVector, w: pos.y
				//xyz: v1, w: height
				//xyz: v2, w: dirAlpha
        		float4 sharedGrassBufferData = SharedGrassDataBuffer[startIndex + IN[0].bufferID];
				float4 grassBufferAData = grassDataABuffer[IN[0].bufferID];
				float4 grassBufferBData = grassDataBBuffer[IN[0].bufferID];
				float4 grassBufferCData = grassDataCBuffer[IN[0].bufferID];
				//float4 tessBufferData = ;
				//OUT.grassDataA = float4(sharedGrassBufferData.x, grassBufferAData.w, sharedGrassBufferData.y, sharedGrassBufferData.z);
				float3 pos = IN[0].pos;
        		//float3 pos = mul(patchModelMatrix, float4(sharedGrassBufferData.x, grassBufferAData.w, sharedGrassBufferData.y, 1.0)).xyz;
        		float3 v1 = pos + grassBufferBData.xyz;
        		float3 v2 = pos + grassBufferCData.xyz;
        		float3 up = grassBufferAData.xyz;
        		float width = sharedGrassBufferData.z;
        		float bend = sharedGrassBufferData.w;
        		float height = grassBufferBData.w;
        		float dirAlpha = grassBufferCData.w;

                float sd = sin(dirAlpha);
                float cd = cos(dirAlpha); 
                float3 tmp = normalize(float3(sd, sd + cd, cd));
               	float3 bladeDir = normalize(cross(up, tmp));

        		float u = uv.x;
                float omu = 1.0f - u;
                float v = uv.y;
                float omv = 1.0f - v;
            
                float3 off = bladeDir * height;
                float3 off2 = off * 0.5f;
            
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
                float3 translation = normal * bend * (0.5f - abs(u - 0.5f)) * (1.0f - v); //position auf der normale verschoben bei mittelachse -> ca rechter winkel (u mit hat function)
	
                //teUV = vec2(u,v);
                //teNormal = normalize(cross(tangent, bitangent));
            
                //vec3 position = Form(i1, i2, u, v, teNormal, tcV2.w);
                float3 outpos = lerp(i1, i2, u + ((-v*u) + (v*omu))*0.5f + translation);
            
                /*if(dot(lightDirection, teNormal) > 0.0f)
                    teNormal = -teNormal;
            
                teDebug = tcDebug;
                gl_Position = vpMatrix * vec4(position, 1.0f);
                tePosition = vec4(position, 1.5f * abs(sin(shapeConstant * tcV1.w)));
       
        		float3 pos = float3(IN[0].pos.x + (uv.x - 0.5)*0.1, IN[0].pos.y + uv.y, IN[0].pos.z);*/
      
      
                OUT.pos = mul(UNITY_MATRIX_VP, float4(outpos, 1.0));
                OUT.color = float4(lerp(float3(0.5, 1, 0.3), float3(1, 1, 1), u + ((-v*u) + (v*omu))*0.5f + translation), 1);
           
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
