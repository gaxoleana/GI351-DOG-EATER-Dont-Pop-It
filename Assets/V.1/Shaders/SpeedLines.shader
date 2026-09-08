Shader "UI/SpeedLines"
{
    Properties
    {
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        [HDR] _LineColorA ("Line Color A", Color) = (1,1,1,1)
        [HDR] _LineColorB ("Line Color B", Color) = (0,1,1,1)
        _LineThreshold ("Line Threshold", Range(0,1)) = 0.6
        _InverseSpeed ("Inverse Speed", Float) = 10.0
        _LineLength ("Line Length", Float) = 1000.0
        _Angle ("Angle", Range(0,360)) = 0.0
        _GapAmount ("Gap Amount", Range(0,1)) = 0.45
        _StreakCount ("Streak Count", Range(4,32)) = 14
        _Intensity ("Intensity", Range(0,1)) = 0.0
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

            sampler2D _NoiseTex;
            fixed4 _LineColorA;
            fixed4 _LineColorB;
            float _LineThreshold;
            float _InverseSpeed;
            float _LineLength;
            float _Angle;
            float _GapAmount;
            float _StreakCount;
            float _Intensity;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 value)
            {
                return frac(sin(dot(value, float2(127.1, 311.7))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float rad = radians(_Angle);
                float2 centeredUv = i.uv - 0.5;
                float2 uv = float2(
                    centeredUv.x * cos(rad) - centeredUv.y * sin(rad),
                    centeredUv.x * sin(rad) + centeredUv.y * cos(rad)
                ) + 0.5;

                float travel = _Time.y / max(_InverseSpeed, 0.01);
                float verticalScale = 1000.0 / max(_LineLength, 1.0);
                float movingY = uv.y * verticalScale + travel;
                float2 noiseUv = float2(uv.x, movingY);
                fixed4 noiseLine = tex2D(_NoiseTex, noiseUv);
                float lineMask = smoothstep(_LineThreshold, min(_LineThreshold + 0.12, 1.0), noiseLine.r);

                float segmentPosition = movingY * _StreakCount;
                float segmentIndex = floor(segmentPosition);
                float segmentUv = frac(segmentPosition);
                float columnIndex = floor(uv.x * 48.0);
                float randomValue = hash21(float2(columnIndex, segmentIndex));
                float streakStart = 0.05 + hash21(float2(columnIndex + 19.0, segmentIndex)) * 0.3;
                float streakLength = lerp(0.25, 0.8, randomValue);
                float streakEnd = min(streakStart + streakLength, 0.98);
                float streakMask = smoothstep(streakStart, streakStart + 0.04, segmentUv);
                streakMask *= 1.0 - smoothstep(streakEnd - 0.08, streakEnd, segmentUv);
                streakMask *= step(_GapAmount, randomValue);

                fixed4 color = lerp(_LineColorA, _LineColorB, 1.0 - noiseLine.r);
                color.a *= lineMask * streakMask * _Intensity;
                return color;
            }
            ENDCG
        }
    }
}
