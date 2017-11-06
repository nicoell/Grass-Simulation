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
            uniform float4x4 patchMatrix;
            uniform int currentAmountBlades;
			uniform StructuredBuffer<float4> SharedGrassData;
			uniform StructuredBuffer<float4> grassDataA;
			uniform StructuredBuffer<float4> grassDataB;
			uniform StructuredBuffer<float4> grassDataC;
			
			
			struct hullIn 
			{
			    //float4 pos : POSITION;
			    float4 sharedData : POSITION;
			    float4 dataA : TEXCOORD0;
			    float4 dataB : TEXCOORD1;
			    float4 dataC : TEXCOORD2;
			};
			
			struct hullConstOut
			{
			    float TessFactor[4] : SV_TessFactor;
			    float InsideTessFactor[2] : SV_InsideTessFactor;
			    
                float4 sharedData : TEXCOORD0;
			    float4 dataA : TEXCOORD1;
			    float4 dataB : TEXCOORD2;
			    float4 dataC : TEXCOORD3;
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
				OUT.sharedData = SharedGrassData[startIndex + id];
				OUT.dataA = grassDataA[id];
				OUT.dataB = grassDataB[id];
				OUT.dataC = grassDataC[id];
				/*OUT.pos = float4(0.0, grassDataA[vertexID].w, 0.0, 1.0);
				OUT.pos.xz = SharedGrassData[startIndex + vertexID].xy;
				OUT.pos = mul(patchMatrix, OUT.pos);*/
				//OUT.dataA = grassDataA[vertexID];
				//OUT.pos.xyz = UnityWorldToViewPos(OUT.pos.xyz);
				//OUT.pos = UnityWorldToClipPos(OUT.pos.xyz);
				//OUT.id = vertexID;
				return OUT;
			}
			
			hullConstOut hullPatchConstant( InputPatch<hullIn, 1> IN)
    		{
        		hullConstOut OUT = (hullConstOut)0;
        		OUT.TessFactor[0] = IN[0].dataC.w;
        		OUT.TessFactor[1] = IN[0].dataC.w;
        		OUT.TessFactor[2] = IN[0].dataC.w;
        		OUT.TessFactor[3] = IN[0].dataC.w;
        		OUT.InsideTessFactor[0] = IN[0].dataC.w;
        		OUT.InsideTessFactor[1] = IN[0].dataC.w;    
        		
        		OUT.sharedData = IN[0].sharedData;
        		OUT.dataA = IN[0].dataA;
        		OUT.dataB = IN[0].dataB;
        		OUT.dataC = IN[0].dataC;
        		
        		float dir = OUT.dataC.w;
                float sd = sin(dir);
                float cd = cos(dir); 
                float3 tmp = normalize(float3(sd, sd + cd, cd));
                OUT.bladeDir = normalize(cross(OUT.dataA.xyz, tmp));
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
        		OUT.pos = mul(patchMatrix, float4(IN[i].sharedData.x, IN[i].dataA.w, IN[i].sharedData.y, 1.0)).xyz;
        		//OUT.pos = IN[i].pos.xyz;
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
            
                float3 off = hullConstData.bladeDir * hullConstData.dataB.w;
                float3 off2 = off * 0.5f;
            
                float3 p0 = IN[0].pos.xyz - off2;
                float3 p1 = IN[0].pos.xyz + hullConstData.dataB.xyz - off2;
                float3 p2 = IN[0].pos.xyz + hullConstData.dataC.xyz - off2;
            
                float3 h1 = p0 + v * (p1 - p0);
                float3 h2 = p1 + v * (p2 - p1);
                float3 i1 = h1 + v * (h2 - h1);
                float3 i2 = i1 + off;
            
                float3 bitangent = hullConstData.bladeDir;
                float3 tangent;
            
                float3 h1h2 = h2 - h1;
                if(dot(h1h2, h1h2) < 1e-3)
                {
                    tangent = hullConstData.dataA.xyz;
                }
                else
                {
                    tangent = normalize(h1h2);
                }
                
                float3 normal = normalize(cross(tangent, bitangent));
                float3 translation = normal * hullConstData.sharedData.z * (0.5f - abs(u - 0.5f)) * (1.0f - v); //position auf der normale verschoben bei mittelachse -> ca rechter winkel (u mit hat function)
	
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
      
        		OUT.pos = UnityObjectToClipPos (float4(pos.xyz,1.0)); 
           
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
