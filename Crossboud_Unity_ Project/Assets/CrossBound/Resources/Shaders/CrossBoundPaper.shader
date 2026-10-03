// Newspaper paper background: aged color, fine grain, vertical fibers,
// soft stains and a vignette. Used on a full-screen uGUI Image.
Shader "Hidden/CrossBound/Paper"
{
    Properties
    {
        _BaseColor ("Paper Color", Color) = (0.937, 0.906, 0.812, 1)
        _Grain ("Grain", Range(0, 0.2)) = 0.05
        _Fiber ("Fibers", Range(0, 0.2)) = 0.05
        _Stain ("Stains", Range(0, 0.3)) = 0.10
        _Vignette ("Vignette", Range(0, 1)) = 0.45
        _Seed ("Seed", Float) = 13.7
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            fixed4 _BaseColor;
            float _Grain, _Fiber, _Stain, _Vignette, _Seed;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p + _Seed, float2(127.1, 311.7))) * 43758.5453);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), f.x),
                            lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), f.x), f.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Fine paper speckle.
                float grain = (hash21(floor(uv * 620.0)) - 0.5) * _Grain * 2.0;
                // Vertical fibers, like cheap newsprint.
                float fibers = (vnoise(float2(uv.x * 5.0, uv.y * 240.0)) - 0.5) * _Fiber * 2.0;
                // Large soft age blotches (darken only).
                float stains = max(vnoise(uv * 3.0) - 0.55, 0.0) * _Stain * 4.0;
                // Vignette darkening towards the edges of the sheet.
                float2 d = uv - 0.5;
                float vignette = 1.0 - dot(d, d) * _Vignette * 1.6;

                fixed4 col = _BaseColor;
                col.rgb *= vignette;
                col.rgb += grain + fibers;
                col.rgb -= stains;
                col.rgb = saturate(col.rgb);
                col.a = _BaseColor.a;
                return col * i.color;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
