Shader "GrassSimulation/DisplayRenderTexture"
{
    SubShader
    {
        Cull Off ZWrite Off
        Pass
        {
            Tags { "RenderType"="Opaque" }
        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = v.vertex;
                //o.vertex.xy *= 2;
                o.uv = v.uv;
                return o;
            }
            
            Texture2D<float4> MainTex; // width, bend, height, dirAlpha
            SamplerState samplerMainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = MainTex.SampleLevel(samplerMainTex, i.uv, 0);
                return fixed4(col.r * 100, col.g * 100, col.b, 1);
            }
            ENDCG
        }
    }
}