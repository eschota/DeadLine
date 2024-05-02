Shader "Custom/ParticleSpriteSheetUnlitTransparent"
{
    Properties
    {
        _MainTex ("Sprite Sheet", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha One // Additive blending mode
        ZWrite Off
        Cull Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Use color to pass frame index
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // The number of frames horizontally
            #define NUM_FRAMES 64

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Calculate UV offsets
                float frameIndex = v.color.a * 64; // Assume frame index is passed in the alpha channel and normalized [0, 1]
                float frameWidth = 1.0 / NUM_FRAMES;

                o.uv.x = v.uv.x * frameWidth + frameIndex * frameWidth;
                o.uv.y = v.uv.y;
                v.color.a=1;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.a *= i.color.a; // Apply alpha blending
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent"
}
