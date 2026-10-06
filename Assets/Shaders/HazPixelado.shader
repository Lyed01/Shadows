// Haz de luz para pixel art.
//
// El shader que usa el juego hoy hace un degradado continuo a lo largo del haz.
// Sobre arte de 16 pixeles por unidad eso desentona: el mundo tiene pixeles
// grandes y visibles, y la luz tiene resolucion infinita.
//
// Este corta la intensidad en pocos escalones y, si se le pide, engancha esos
// escalones a la grilla de pixeles del arte, asi la luz tiene el mismo grano
// que el resto.
//
// Ademas separa el nucleo del borde. En el juego el borde tenue mata igual que
// el centro brillante, que es lo que hace sentir injustas las muertes: aca el
// nucleo se dibuja marcado para que se lea donde empieza lo que mata.
Shader "URP/HazPixelado"
{
    Properties
    {
        [MainColor]_Color("Color del nucleo", Color) = (1, 0.95, 0.7, 1)
        _ColorBorde("Color del borde", Color) = (1, 0.8, 0.45, 1)
        _Intensity("Intensidad", Range(0, 5)) = 1.5

        [Header(Pixelado)]
        _Bandas("Escalones de intensidad", Range(1, 32)) = 4
        _PPU("Pixeles por unidad del arte", Float) = 16
        [Toggle]_SnapAPixel("Enganchar a la grilla de pixeles", Float) = 1

        [Header(Forma)]
        _FraccionNucleo("Ancho del nucleo (0 a 1)", Range(0.05, 1)) = 0.55
        _BrilloNucleo("Refuerzo de brillo del nucleo", Range(0, 1)) = 0.5
        _CaidaLargo("Caida a lo largo del haz", Range(0, 1)) = 0.35
        _DurezaBorde("Dureza del borde lateral", Range(0, 1)) = 0.8
        [Toggle]_BandasCurvas("Bandas curvas (en vez de rectas)", Float) = 1

        [Header(Animacion)]
        _Pulso("Pulso del nucleo", Range(0, 1)) = 0
        _VelocidadPulso("Velocidad del pulso", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            float4 _Color;
            float4 _ColorBorde;
            float  _Intensity;
            float  _Bandas;
            float  _PPU;
            float  _SnapAPixel;
            float  _FraccionNucleo;
            float  _BrilloNucleo;
            float  _CaidaLargo;
            float  _DurezaBorde;
            float  _BandasCurvas;
            float  _Pulso;
            float  _VelocidadPulso;

            v2f vert (appdata v)
            {
                v2f o;
                float3 mundo = TransformObjectToWorld(v.vertex.xyz);
                o.pos = TransformWorldToHClip(mundo);
                o.worldPos = mundo.xy;
                o.uv = v.uv;
                return o;
            }

            // Corta un valor continuo en N escalones. Es lo que convierte el
            // degradado en bandas y le da el grano de pixel art.
            float Escalonar(float valor, float escalones)
            {
                return floor(saturate(valor) * escalones) / max(1.0, escalones - 1.0);
            }

            half4 frag (v2f i) : SV_Target
            {
                // u cruza el haz (0 y 1 son los bordes), v va de la boca a la punta
                float2 uv = i.uv;

                // Que tan al costado del eje esta este pixel: 0 en el centro, 1 en el borde
                float desvio = abs(uv.x - 0.5) * 2.0;

                // Enganche a la grilla del arte: se cuantiza la posicion de
                // mundo al pixel del tileset y se usa esa para apagar el haz a
                // lo largo. Asi los escalones quedan clavados al escenario en
                // lugar de deslizarse cuando la camara se mueve.
                float largo = uv.y;
                if (_SnapAPixel > 0.5)
                {
                    float tamPixel = 1.0 / max(1.0, _PPU);
                    float2 enGrilla = floor(i.worldPos / tamPixel) * tamPixel;

                    // Cuanto corrio el pixel al caer en la grilla, medido en
                    // pixeles enteros, aplicado sobre el largo normalizado.
                    float2 diferencia = (enGrilla - i.worldPos) / tamPixel;
                    largo = saturate(uv.y + (diferencia.x + diferencia.y) * tamPixel * 0.5);
                }

                // Perfil lateral: la luz baja hacia los costados antes de
                // cortarse. Sin esto el haz se lee como una chapa pintada y no
                // como algo luminoso.
                float lateral = 1.0 - pow(saturate(desvio), lerp(4.0, 1.2, _DurezaBorde));

                // Se apaga hacia la punta
                float caida = lerp(1.0, 1.0 - largo, _CaidaLargo);

                // El pulso respira solo en el nucleo: marca lo que mata sin
                // mover el borde, que es la referencia que el jugador mide.
                float pulso = 1.0 + _Pulso * 0.25 * sin(_Time.y * _VelocidadPulso * 6.2831);

                // Dos maneras de cortar la luz en escalones, y se ven muy
                // distinto:
                //
                // Curvas: se escalona el producto de las dos caidas. Las bandas
                // salen como curvas de nivel, o sea arcos que cruzan el haz y le
                // dan un borde redondeado.
                //
                // Rectas: se escalona cada eje por su cuenta. Quedan franjas
                // rectas, mas parecidas a un degradado de pixel art clasico.
                float marcaNucleo = 1.0 - smoothstep(_FraccionNucleo * 0.8, _FraccionNucleo, desvio);

                float luz, refuerzo;
                if (_BandasCurvas > 0.5)
                {
                    luz = Escalonar(lateral * caida, _Bandas) * pulso;
                    refuerzo = Escalonar(marcaNucleo * caida, _Bandas) * _BrilloNucleo;
                }
                else
                {
                    float caidaBandas = Escalonar(caida, _Bandas);
                    luz = Escalonar(lateral, _Bandas) * caidaBandas * pulso;
                    refuerzo = Escalonar(marcaNucleo, _Bandas) * caidaBandas * _BrilloNucleo;
                }

                float3 color = lerp(_ColorBorde.rgb, _Color.rgb, saturate(marcaNucleo));
                float3 salida = color * (luz + refuerzo) * _Intensity;

                return half4(salida, saturate(luz + refuerzo));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
