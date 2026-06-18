Shader "UI/ProceduralVignette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0,0,0,1)
        _Radius ("Radio del Hueco Central", Range(0.0, 1.0)) = 0.3
        _Softness ("Suavizado del Borde", Range(0.01, 1.0)) = 0.5
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane" 
            "CanUseSpriteAtlas"="True" 
        }
        
        Cull Off 
        Lighting Off 
        ZWrite Off 
        ZTest [unity_GUIZTestMode] 
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t 
            { 
                float4 vertex : POSITION; 
                float4 color : COLOR; 
                float2 texcoord : TEXCOORD0; 
            };
            
            struct v2f 
            { 
                float4 vertex : SV_POSITION; 
                fixed4 color : COLOR; 
                float2 texcoord : TEXCOORD0; 
            };

            fixed4 _Color;
            float _Radius;
            float _Softness;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = distance(i.texcoord, float2(0.5, 0.5));
                
                float vignetteAlpha = smoothstep(_Radius, _Radius + _Softness, dist);
                
                i.color.a *= vignetteAlpha;
                
                return i.color;
            }
            ENDCG
        }
    }
}