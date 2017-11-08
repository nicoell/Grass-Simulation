// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "GrassSimulation/GrassShader"
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
			#pragma vertex vert
			#pragma hull hull
			#pragma domain domain
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			uniform float startIndex;
            uniform float4x4 patchModelMatrix;
            uniform int currentAmountBlades;
			uniform StructuredBuffer<float4> SharedGrassDataBuffer;
			uniform StructuredBuffer<float4> tessDataBuffer;
			uniform StructuredBuffer<float4> grassDataABuffer;
			uniform StructuredBuffer<float4> grassDataBBuffer;
			uniform StructuredBuffer<float4> grassDataCBuffer;
			
			
			struct hullIn 
			{
			    //float4 pos : POSITION;
			    float4 sharedGrassData : POSITION;
			    float4 grassDataA : TEXCOORD0;
			    float4 grassDataB : TEXCOORD1;
			    float4 grassDataC : TEXCOORD2;
			    float4 tessData : TEXCOORD3;
			};
			
			struct hullConstOut
			{
			    float TessFactor[4] : SV_TessFactor;
			    float InsideTessFactor[2] : SV_InsideTessFactor;
			    
                float4 sharedGrassData : TEXCOORD0;
			    float4 grassDataA : TEXCOORD1;
			    float4 grassDataB : TEXCOORD2;
			    float4 grassDataC : TEXCOORD3;
			    float3 bladeDir : TEXCOORD4;
			    
			};
			
			struct hullOut
			{
			    float3 pos : POS;
			};
			
			struct domainOut
			{
			    float4 pos : SV_POSITION;
			};
			
			struct fragIn
			{
			    float4 pos : SV_POSITION;
			};
			
			hullIn vert (uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				hullIn OUT;
				uint id = 64 * instanceID + vertexID;
				OUT.sharedGrassData = SharedGrassDataBuffer[startIndex + id];
				OUT.grassDataA = grassDataABuffer[id];
				OUT.grassDataB = grassDataBBuffer[id];
				OUT.grassDataC = grassDataCBuffer[id];
				OUT.tessData = tessDataBuffer[id];
				/*OUT.pos = float4(0.0, grassDataA[vertexID].w, 0.0, 1.0);
				OUT.pos.xz = SharedGrassData[startIndex + vertexID].xy;
				OUT.pos = mul(localToObject, OUT.pos);*/
				//OUT.grassDataA = grassDataA[vertexID];
				//OUT.pos.xyz = UnityWorldToViewPos(OUT.pos.xyz);
				//OUT.pos = UnityWorldToClipPos(OUT.pos.xyz);
				//OUT.id = vertexID;
				return OUT;
			}
			
			hullConstOut hullPatchConstant( InputPatch<hullIn, 1> IN)
    		{
        		hullConstOut OUT = (hullConstOut)0;
        		OUT.TessFactor[0] = IN[0].tessData.x;
        		OUT.TessFactor[1] = 1.0;
        		OUT.TessFactor[2] = IN[0].tessData.x;
        		OUT.TessFactor[3] = 1.0;
        		OUT.InsideTessFactor[0] = 1.0;
        		OUT.InsideTessFactor[1] = IN[0].tessData.x;    
        		
        		OUT.sharedGrassData = IN[0].sharedGrassData;
        		OUT.grassDataA = IN[0].grassDataA;
        		OUT.grassDataB = IN[0].grassDataB;
        		OUT.grassDataC = IN[0].grassDataC;
        		
        		float dir = OUT.grassDataC.w;
                float sd = sin(dir);
                float cd = cos(dir); 
                float3 tmp = normalize(float3(sd, sd + cd, cd));
                OUT.bladeDir = normalize(cross(OUT.grassDataA.xyz, tmp));
        		return OUT;
            }
			
			[domain("quad")]
    		[partitioning("fractional_even")]
    		[outputtopology("triangle_cw")]
    		[patchconstantfunc("hullPatchConstant")]
    		[outputcontrolpoints(1)]
    		hullOut hull( InputPatch<hullIn, 1> IN, uint i : SV_OutputControlPointID )
    		{
        		hullOut OUT = (hullOut)0;
        		
        		OUT.pos = mul(patchModelMatrix, float4(IN[i].sharedGrassData.x, IN[i].grassDataA.w, IN[i].sharedGrassData.y, 1.0)).xyz;
        		return OUT;
            }
               
            [domain("quad")]
    		domainOut domain( hullConstOut hullConstData, 
    		            const OutputPatch<hullOut, 1> IN, 
    					float2 uv : SV_DomainLocation)
    		{
        		domainOut OUT = (domainOut)0;
        		
        		float u = uv.x;
                float omu = 1.0f - u;
                float v = uv.y;
                float omv = 1.0f - v;
            
                float3 off = hullConstData.bladeDir * hullConstData.grassDataB.w;
                float3 off2 = off * 0.5f;
            
                float3 p0 = IN[0].pos.xyz - off2;
                float3 p1 = IN[0].pos.xyz + hullConstData.grassDataB.xyz - off2;
                float3 p2 = IN[0].pos.xyz + hullConstData.grassDataC.xyz - off2;
            
                float3 h1 = p0 + v * (p1 - p0);
                float3 h2 = p1 + v * (p2 - p1);
                float3 i1 = h1 + v * (h2 - h1);
                float3 i2 = i1 + off;
            
                float3 bitangent = hullConstData.bladeDir;
                float3 tangent;
            
                float3 h1h2 = h2 - h1;
                if(dot(h1h2, h1h2) < 1e-3)
                {
                    tangent = hullConstData.grassDataA.xyz;
                }
                else
                {
                    tangent = normalize(h1h2);
                }
                
                float3 normal = normalize(cross(tangent, bitangent));
                float3 translation = normal * hullConstData.sharedGrassData.z * (0.5f - abs(u - 0.5f)) * (1.0f - v); //position auf der normale verschoben bei mittelachse -> ca rechter winkel (u mit hat function)
	
                //teUV = vec2(u,v);
                //teNormal = normalize(cross(tangent, bitangent));
            
                //vec3 position = Form(i1, i2, u, v, teNormal, tcV2.w);
                float3 pos = lerp(i1, i2, u + ((-v*u) + (v*omu))*0.5f + translation);
            
                /*if(dot(lightDirection, teNormal) > 0.0f)
                    teNormal = -teNormal;
            
                teDebug = tcDebug;
                gl_Position = vpMatrix * vec4(position, 1.0f);
                tePosition = vec4(position, 1.5f * abs(sin(shapeConstant * tcV1.w)));
       
        		float3 pos = float3(IN[0].pos.x + (uv.x - 0.5)*0.1, IN[0].pos.y + uv.y, IN[0].pos.z);*/
      
      
                OUT.pos = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
           
        		return OUT;
}
			
			fixed4 frag (fragIn IN) : SV_TARGET
			{
				fixed4 col = fixed4(1.0, 1.0, 1.0, 1.0);
				return col;
			}
			ENDCG
		}
	}
}
