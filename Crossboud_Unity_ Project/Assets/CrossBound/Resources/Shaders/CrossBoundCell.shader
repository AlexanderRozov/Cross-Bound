// Crossword cell: paper fill with subtle grain and an ink border whose edge is
// slightly roughened to mimic letterpress printing. Image.color (vertex color)
// tints the result, which the view uses for in-word / correct / incorrect states.
Shader "Hidden/CrossBound/Cell"
{
    Properties
    {
        // Declared only so uGUI can bind its default texture to this material
        // (CanvasRenderer assigns _MainTex to every graphic) without warnings.
        _MainTex ("Texture", 2D) = "white" {}
        _FillColor ("Fill Color", Color) = (0.992, 0.984, 0.957, 1)
        _BorderColor ("Border Color", Color) = (0.125, 0.102, 0.070, 1)
        _BorderWidth ("Border Width", Range(0, 0.25)) = 0.055
        _Roughness ("Edge Roughness", Range(0, 0.03)) = 0.007
        _Grain ("Grain", Range(0, 0.2)) = 0.04
        _Seed ("Seed", Float) = 7.3
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

            fixed4 _FillColor;
            fixed4 _BorderColor;
            float _BorderWidth, _Roughness, _Grain, _Seed;

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

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float edge = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                // Per-fragment jitter of the border threshold: uneven printed ink.
                float rough = (hash21(floor(uv * 64.0)) - 0.5) * _Roughness * 2.0;
                float border = 1.0 - smoothstep(_BorderWidth + rough - 0.004, _BorderWidth + rough + 0.004, edge);

                float grain = (hash21(floor(uv * 140.0)) - 0.5) * _Grain * 2.0;
                fixed4 fill = _FillColor;
                fill.rgb = saturate(fill.rgb + grain);

                // Pre-multiplied-ish composite: border ink over paper fill.
                fixed4 col;
                col.rgb = lerp(fill.rgb, _BorderColor.rgb, border * _BorderColor.a);
                col.a = lerp(fill.a, max(fill.a, _BorderColor.a), border);
                return col * i.color;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
