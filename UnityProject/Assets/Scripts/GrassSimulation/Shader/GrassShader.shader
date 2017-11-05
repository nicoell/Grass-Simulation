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
			
			uniform int startIndex;
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
			
			hullIn vert (uint vertexID : SV_VertexID)
			{
				hullIn OUT;
				OUT.sharedData = SharedGrassData[startIndex + vertexID];
				OUT.dataA = grassDataA[vertexID];
				OUT.dataB = grassDataB[vertexID];
				OUT.dataC = grassDataC[vertexID];
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
        		OUT.TessFactor[0] = OUT.TessFactor[1] = OUT.TessFactor[2] = OUT.TessFactor[3] = 1.0;
        		OUT.InsideTessFactor[0] = OUT.InsideTessFactor[1] = 1.0;    
        		
        		OUT.sharedData = IN[0].sharedData;
        		OUT.dataA = IN[0].dataA;
        		OUT.dataB = IN[0].dataB;
        		OUT.dataC = IN[0].dataC;
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
       
        		float3 pos = float3(IN[0].pos.x + uv.x - 0.5, IN[0].pos.y + uv.y, IN[0].pos.z);
      
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
