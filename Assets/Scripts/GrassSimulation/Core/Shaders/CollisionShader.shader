// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "GrassSimulation/CollisionShader"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            uniform float3 collisionVelocity;
            uniform float3 objectCenter;
            uniform float4x4 GrassCollisionViewProj;

            struct VSOut
            {
                float4 vertex : SV_POSITION;
                float3 centerVector : TEXCOORD0;
                float4 test : TEXCOORD1;
            };

            VSOut vert (float4 vertex : POSITION)
            {
                VSOut OUT;
                OUT.vertex = UnityObjectToClipPos(vertex);
                OUT.test = mul(GrassCollisionViewProj,  mul(unity_ObjectToWorld, vertex));
                OUT.centerVector = objectCenter - mul(unity_ObjectToWorld, vertex.xyz);
                return OUT;
            }
            
            fixed4 frag (VSOut IN) : SV_Target
            {
                //return float4(IN.centerVector * collisionVelocity.xyz, IN.vertex.z);
                return float4(IN.test.z, IN.vertex.z, IN.test.z, IN.vertex.z);
            }
            ENDCG
        }
    }
}