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

            struct VSOut
            {
                float4 vertex : SV_POSITION;
            };

            VSOut vert (float4 vertex : POSITION)
            {
                VSOut OUT;
                OUT.vertex = UnityObjectToClipPos(vertex);
                return OUT;
            }
            
            fixed4 frag (VSOut IN) : SV_Target
            {
                return float4(collisionVelocity.xyz, IN.vertex.z);
            }
            ENDCG
        }
    }
}