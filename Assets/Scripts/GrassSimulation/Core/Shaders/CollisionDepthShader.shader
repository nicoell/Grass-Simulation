Shader "GrassSimulation/CollisionDepthShader"
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
                return fixed4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}