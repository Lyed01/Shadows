// Sombra de las transiciones: cubre la pantalla en circulo desde un punto
// (Shawy) con un tramado Bayer en pixeles del juego, y se retira igual.
// Va en una Image que cubre toda la pantalla: su UV por _Pantalla da el pixel,
// con el origen abajo a la izquierda en cualquier plataforma.
Shader "Custom/SombraTransicion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0,0,0,1)
        _Centro ("Centro (px de pantalla)", Vector) = (0,0,0,0)
        _Pantalla ("Pantalla (px)", Vector) = (1920,1080,0,0)
        _RadioMax ("Radio maximo (px de pantalla)", Float) = 1000
        _TamPunto ("Tamano del punto (px de pantalla)", Float) = 4
        _Grano ("Ancho del borde tramado", Range(0.05, 0.5)) = 0.25
        _Desde ("Desde", Range(0, 1)) = 0
        _Hasta ("Hasta", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float4 _Centro, _Pantalla;
            float _RadioMax, _TamPunto, _Grano, _Desde, _Hasta;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 pantalla : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pantalla = v.uv * _Pantalla.xy;
                return o;
            }

            static const float bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            fixed4 frag (v2f i) : SV_Target
            {
                float2 celda = floor(i.pantalla / _TamPunto);
                float2 m = fmod(celda, 4);
                float umbral = (bayer[(int)m.y * 4 + (int)m.x] + 0.5) / 16;

                // Distancia del centro de la celda, 0 en el centro y 1 en la esquina mas lejana.
                float d = saturate(distance((celda + 0.5) * _TamPunto, _Centro.xy) / _RadioMax);
                // Siempre menor que 1: con _Hasta = 1 y _Desde = 0 cubre todo.
                float v = d * (1 - _Grano) + umbral * _Grano;

                float oscuro = step(_Desde, v) * step(v, _Hasta - 0.0001);
                return fixed4(_Color.rgb, _Color.a * oscuro);
            }
            ENDCG
        }
    }
}
