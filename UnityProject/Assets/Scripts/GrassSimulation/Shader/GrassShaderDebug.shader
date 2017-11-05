// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "GrassSimulation/GrassShaderDEBUG"
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
			
			
			struct hullIn 
			{
			    float4 pos : POS;
                //uint id : SV_VertexID;
			};
			
			struct hullConstOut
			{
			    float TessFactor[4] : SV_TessFactor;
			    float InsideTessFactor[2] : SV_InsideTessFactor;
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
			
			hullIn vert (float4 vertexPos: POSITION)
			{
				hullIn OUT;
				
				OUT.pos = float4(vertexPos.xyz, 1);
				return OUT;
			}
			
			hullConstOut hullPatchConstant( InputPatch<hullIn, 1> IN)
    		{
        		hullConstOut OUT = (hullConstOut)0;
        		OUT.TessFactor[0] = OUT.TessFactor[1] = OUT.TessFactor[2] = OUT.TessFactor[3] = 1.0;
        		OUT.InsideTessFactor[0] = OUT.InsideTessFactor[1] = 1.0;    
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
        		OUT.pos = IN[i].pos.xyz;
        		return OUT;
            }
               
            [domain("quad")]
    		domainOut domain( hullConstOut hullConstData, 
    		            const OutputPatch<hullOut, 1> IN, 
    					float2 uv : SV_DomainLocation)
    		{
        		domainOut OUT = (domainOut)0;
       
        		float3 pos = float3(IN[0].pos.xy + (uv - float2(0.5, 0.5)), IN[0].pos.z);
      
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
