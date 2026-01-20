Shader "Custom/VHSEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.1
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.3
        _ScanlineCount ("Scanline Count", Float) = 300
        _ChromaticStrength ("Chromatic Aberration", Range(0, 0.05)) = 0.005
        _DistortionStrength ("Distortion", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "VHSEffect"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            float _NoiseStrength;
            float _ScanlineStrength;
            float _ScanlineCount;
            float _ChromaticStrength;
            float _DistortionStrength;
            
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                
                // Vertical distortion (tracking error simulation)
                float distortion = sin(uv.y * 50 + _Time.y * 5) * _DistortionStrength;
                uv.x += distortion * sin(_Time.y * 20);
                
                // Chromatic aberration
                float2 uvR = uv + float2(_ChromaticStrength, 0);
                float2 uvB = uv - float2(_ChromaticStrength, 0);
                
                half r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvR).r;
                half g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                half b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvB).b;
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                
                half4 col = half4(r, g, b, a);
                
                // Scanlines
                float scanline = sin(uv.y * _ScanlineCount) * 0.5 + 0.5;
                scanline = lerp(1, scanline, _ScanlineStrength);
                col.rgb *= scanline;
                
                // Noise
                float noise = rand(uv + _Time.y) * _NoiseStrength;
                col.rgb += noise;
                
                return col;
            }
            ENDHLSL
        }
    }
}
