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
			#pragma enable_d3d11_debug_symbols
			#pragma vertex vert
			#pragma hull hull
			#pragma domain domain
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			uniform float debugTest;
			float debugTestFactor;
			uniform float startIndex;
            uniform float4x4 patchModelMatrix;
            uniform int currentAmountBlades;
			uniform StructuredBuffer<float4> SharedGrassDataBuffer;
			uniform StructuredBuffer<float4> grassDataABuffer;
			uniform StructuredBuffer<float4> grassDataBBuffer;
			uniform StructuredBuffer<float4> grassDataCBuffer;
			uniform StructuredBuffer<float4> tessDataBuffer;
			
			//pos.xz, width, bend
			//xyz: upVector, w: pos.y
			//xyz: v1, w: height
			//xyz: v2, w: dirAlpha

			/*
				hullIn:
				grassDataA = xyz:pos.xyz 	w:width
				grassDataB = xyz:up.xyz 	w:bend
				grassDataC = xyz:v1.xyz 	w:height
				grassDataD = xyz:v2.xyz 	w:dirAlpha
				grassDataE = x:tessLevel 	yzw:FREE
				
				hullOut:
				grassDataA = xyz:pos.xyz 	w:width
				grassDataB = xyz:up.xyz 	w:bend
				grassDataC = xyz:v1.xyz 	w:height
				grassDataD = xyz:v2.xyz 	w:dirAlpha
				grassDataE = xyz:dir.xyz 	w:FREE
			*/
			struct hullIn 
			{
			    float4 grassDataA : POS;
			    float4 grassDataB : TEXCOORD0;
			    float4 grassDataC : TEXCOORD1;
			    float4 grassDataD : TEXCOORD2;
			    float4 grassDataE : TEXCOORD3;
			};
			
			struct hullConstOut
			{
			    float TessFactor[4] : SV_TessFactor;
			    float InsideTessFactor[2] : SV_InsideTessFactor;
			};
			
			struct hullOut
			{
			    float4 grassDataA : POSITION;
                float4 grassDataB : TEXCOORD0;
			    float4 grassDataC : TEXCOORD1;
			    float4 grassDataD : TEXCOORD2;
			    float4 grassDataE : TEXCOORD3;
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
				float4 sharedGrassBufferData = SharedGrassDataBuffer[startIndex + id];
				float4 grassBufferAData = grassDataABuffer[id];
				float4 grassBufferBData = grassDataBBuffer[id];
				float4 grassBufferCData = grassDataCBuffer[id];
				float4 tessBufferData = tessDataBuffer[id];
				OUT.grassDataA = float4(sharedGrassBufferData.x, grassBufferAData.w, sharedGrassBufferData.y, sharedGrassBufferData.z);
				OUT.grassDataB = float4(grassBufferAData.xyz, sharedGrassBufferData.w);
				OUT.grassDataC = grassBufferBData;
				OUT.grassDataD = grassBufferCData;
				OUT.grassDataE = tessBufferData;

				return OUT;
			}
			
			hullConstOut hullPatchConstant( InputPatch<hullIn, 1> IN)
    		{
        		hullConstOut OUT = (hullConstOut)0;

        		OUT.TessFactor[0] = IN[0].grassDataE.x;
        		OUT.TessFactor[1] = 1.0;
        		OUT.TessFactor[2] = IN[0].grassDataE.x * 1;
        		OUT.TessFactor[3] = 1.0;
        		OUT.InsideTessFactor[0] = 1.0;
        		OUT.InsideTessFactor[1] = IN[0].grassDataE.x;
        		/*
        		float dir = IN[0].grassDataD.w;
                float sd = sin(dir);
                float cd = cos(dir); 
                float3 tmp = normalize(float3(sd, sd + cd, cd));
                OUT.grassDataA.xyz = normalize(cross(IN[0].grassDataB.xyz, tmp));
                OUT.grassDataA.w = IN[0].grassDataA.w;
                OUT.grassDataB = IN[0].grassDataB;
                OUT.grassDataC = IN[0].grassDataC;
                OUT.grassDataD = IN[0].grassDataD;*/

        		return OUT;
            }

			[domain("quad")]
    		[partitioning("fractional_odd")]
    		[outputtopology("triangle_cw")]
    		[patchconstantfunc("hullPatchConstant")]
    		[outputcontrolpoints(1)]
    		hullOut hull( InputPatch<hullIn, 1> IN, uint i : SV_OutputControlPointID )
    		{
        		hullOut OUT = (hullOut)0;
        		float dir = IN[i].grassDataD.w;
                float sd = sin(dir);
                float cd = cos(dir); 
                float3 tmp = normalize(float3(sd, sd + cd, cd));
                OUT.grassDataE.xyz = normalize(cross(IN[i].grassDataB.xyz, tmp));

        		OUT.grassDataA.xyz = mul(patchModelMatrix, float4(IN[i].grassDataA.xyz, 1.0)).xyz;
        		OUT.grassDataA.w = IN[i].grassDataA.w;
        		OUT.grassDataB = IN[i].grassDataB;
        		OUT.grassDataC = IN[i].grassDataC;
        		OUT.grassDataD = IN[i].grassDataD;
        		OUT.grassDataE.w = 0.0;
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
            
                float3 off = IN[0].grassDataE.xyz * IN[0].grassDataA.w;
                float3 off2 = off * 0.5f;
            
                float3 p0 = IN[0].grassDataA.xyz - off2;
                float3 p1 = IN[0].grassDataA.xyz + IN[0].grassDataC.xyz - off2;
                float3 p2 = IN[0].grassDataA.xyz + IN[0].grassDataD.xyz - off2;
            
                float3 h1 = p0 + v * (p1 - p0);
                float3 h2 = p1 + v * (p2 - p1);
                float3 i1 = h1 + v * (h2 - h1);
                float3 i2 = i1 + off;
            
                float3 bitangent = IN[0].grassDataE.xyz;
                float3 tangent;
            
                float3 h1h2 = h2 - h1;
                if(dot(h1h2, h1h2) < 1e-3)
                {
                    tangent = IN[0].grassDataB.xyz;
                }
                else
                {
                    tangent = normalize(h1h2);
                }
                
                float3 normal = normalize(cross(tangent, bitangent));
                float3 translation = normal * IN[0].grassDataB.z * (0.5f - abs(u - 0.5f)) * (1.0f - v); //position auf der normale verschoben bei mittelachse -> ca rechter winkel (u mit hat function)
	
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
