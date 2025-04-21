Shader "Custom/CRTDistortion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BarrelPower ("Barrel Distortion", Range(0.0, 1.0)) = 0.1
        _ChromaticAberration ("Chromatic Aberration", Range(0.0, 5.0)) = 1.0
        _VignetteIntensity ("Vignette", Range(0.0, 1.0)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _BarrelPower;
            float _ChromaticAberration;
            float _VignetteIntensity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            // Apply barrel distortion to UVs
            float2 barrelDistortion(float2 uv, float power)
            {
                float2 center = uv - 0.5;
                float distortion = pow(length(center), 2.0) * power;
                
                return uv + center * distortion;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Convert UV to -1 to 1 range
                float2 uv = i.uv * 2.0 - 1.0;
                
                // Get distance from center
                float d = length(uv);
                
                // Apply barrel distortion
                float2 distortedUV = barrelDistortion(i.uv, _BarrelPower);
                
                // Chromatic aberration
                float2 rUV = barrelDistortion(i.uv, _BarrelPower * (1.0 + _ChromaticAberration * 0.01));
                float2 gUV = distortedUV;
                float2 bUV = barrelDistortion(i.uv, _BarrelPower * (1.0 - _ChromaticAberration * 0.01));
                
                // Vignette effect
                float vignette = 1.0 - d * d * _VignetteIntensity;
                
                // Sample texture with distortion
                fixed4 col;
                col.r = tex2D(_MainTex, rUV).r;
                col.g = tex2D(_MainTex, gUV).g;
                col.b = tex2D(_MainTex, bUV).b;
                col.a = 1.0;
                
                // Apply vignette
                col.rgb *= vignette;
                
                return col;
            }
            ENDCG
        }
    }
}