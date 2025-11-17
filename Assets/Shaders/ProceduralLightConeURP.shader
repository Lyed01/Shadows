Shader "URP/ProceduralLightCone"
{
    Properties
    {
        [MainColor]_Color("Color", Color) = (1,1,0,1)
        _Intensity("Intensity", Range(0,5)) = 1.5
        _EdgeSoftness("Edge Softness", Range(0,1)) = 0.35

        [Toggle]_InvertV("Invert V (flip vertical)", Float) = 0

        [Header(Texture (optional))]
        [NoScaleOffset]_MainTex("Light Texture", 2D) = "white" {}
        [Toggle]_UseTex("Use Texture", Float) = 0

        [Header(UV Controls)]
        _UVScale("UV Scale (U x V)", Vector) = (1,1,0,0)
        _UVOffset("UV Offset (U x V)", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend One One               // Aditivo (igual que Light2D). Si querés alfa, usar: Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _USE_TEX // para keyword
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0; // del mesh
                float4 color  : COLOR;     // por si querés tint por vértice
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 col : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float  _EdgeSoftness;
            float  _Intensity;
            float  _InvertV;
            float4 _UVScale;   // xy
            float4 _UVOffset;  // xy
            float  _UseTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);

                // UV del mesh + controles extra (para alinear/ajustar desde material)
                float2 uv = v.uv;
                if (_InvertV > 0.5) uv.y = 1.0 - uv.y;     // opcional: invertir V

                uv = uv * _UVScale.xy + _UVOffset.xy;      // escala/offset globales
                o.uv  = uv;
                o.col = v.color;
                return o;
            }

            float remap01(float x, float a, float b)
            {
                return saturate((x - a) / max(1e-5, (b - a)));
            }

            half4 frag (v2f i) : SV_Target
            {
                // Convención: tu mesh genera V=0 en origen y V=1 en borde (punta del haz).
                // Construimos un “falloff” suave hacia la punta, similar al Light2D.
                float v = saturate(i.uv.y);

                // Curva de caída (ajustable con EdgeSoftness). 0 = borde duro, 1 = muy suave.
                // d crece cerca del origen y cae hacia la punta.
                float d = smoothstep(0.0, 1.0, 1.0 - v);
                d = pow(d, lerp(0.25, 4.0, saturate(1.0 - _EdgeSoftness))); // mapa perceptivo

                float3 col = _Color.rgb;
                if (_UseTex > 0.5)
                {
                    float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                    col *= tex.rgb;
                    // Si quisieras usar alfa de la textura para edge: d *= tex.a;
                }

                // Intensidad aditiva
                float3 outRGB = col * d * _Intensity;

                // En aditivo el alpha no importa, pero devolvemos d por consistencia.
                return half4(outRGB, d);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
