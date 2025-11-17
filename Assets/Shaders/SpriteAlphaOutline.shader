Shader "Custom/SpriteAlphaOutline"
{
    Properties
    {
        _MainTex       ("Texture", 2D) = "white" {}
        _OutlineColor  ("Outline Color", Color) = (1,1,0.5,1)
        _ThicknessPx   ("Thickness (px)", Float) = 1
        _AlphaCutoff   ("Alpha Cutoff", Range(0,1)) = 0.1
        _Intensity     ("Intensity", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // x=1/width, y=1/height
            fixed4 _OutlineColor;
            float  _ThicknessPx;
            float  _AlphaCutoff;
            float  _Intensity;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };
            struct v2f {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Devuelve 1 si hay alpha “sólido”, 0 si es transparente
            inline float SolidAlphaAt(float2 uv){
                fixed4 c = tex2D(_MainTex, uv);
                return step(_AlphaCutoff, c.a);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Centro (dentro del sprite o no)
                float center = SolidAlphaAt(i.uv);

                // Si el centro ya es sólido, no dibujamos (sólo contorno externo)
                if (center > 0.5) return fixed4(0,0,0,0);

                // Calcular desplazamiento en píxeles (8 vecinos)
                float2 px = _ThicknessPx * _MainTex_TexelSize.xy;
                float s =
                      SolidAlphaAt(i.uv + float2( px.x,  0.0))
                    + SolidAlphaAt(i.uv + float2(-px.x,  0.0))
                    + SolidAlphaAt(i.uv + float2( 0.0,  px.y))
                    + SolidAlphaAt(i.uv + float2( 0.0, -px.y))
                    + SolidAlphaAt(i.uv + float2( px.x,  px.y))
                    + SolidAlphaAt(i.uv + float2(-px.x,  px.y))
                    + SolidAlphaAt(i.uv + float2( px.x, -px.y))
                    + SolidAlphaAt(i.uv + float2(-px.x, -px.y));

                // Si cualquier vecino es sólido, estamos “al borde” → dibujar color
                if (s > 0.0)
                {
                    fixed4 o = _OutlineColor;
                    o.a *= saturate(_Intensity); // control por script
                    return o;
                }

                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}
