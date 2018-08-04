Shader "Custom/OceanDisplacement"
{
	Properties
	{
		_LookupTex("Lookup Tex", 2D) = "white" {}
		_MainTex("Prev Tex", 2D) = "white" {}
		_Mix("Mix", Range(0, 1)) = 0.5
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			sampler2D _LookupTex;
			float _Mix;

			float _OrbitRadius;
			float _WaveNumber;
			float _AngularSpeed; 
			float _MaxStretch;
			float _MaxDisplacement;
			float _DisplacementFactor; 
			float3 _Center;

			float2 wave(float x0, float t)
			{
				float phaseRaw = _WaveNumber * x0 - _AngularSpeed * t;
				float phase = fmod (phaseRaw, 2.0 * UNITY_PI);

				float stretch = (_MaxStretch * phase * phase) / UNITY_PI;

				float normalizedPhase = (phase + UNITY_PI) / (2.0 * UNITY_PI);
				float lookup = tex2Dlod(_LookupTex, float4(normalizedPhase, 0.5, 0, 0)).r;
				float orientation = lookup * UNITY_PI / 4.0;
				float displacement = lookup * _MaxDisplacement;

				float x = x0 + (_OrbitRadius * sin(phase) + stretch + displacement * _DisplacementFactor * cos(orientation));
				float y = -(_OrbitRadius * cos(phase) + displacement * _DisplacementFactor * sin(orientation)); // we assume z0 = 0
				return float2(x, y);
			}

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
			
			struct FragmentOutput
			{
				float4 color0 : COLOR0;
				float4 color1 : COLOR1;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			FragmentOutput frag (v2f i) : SV_Target
			{
				float3 position = float3(i.uv.x, 0, i.uv.y) - _Center;
				float t = length(position);
				float3 dirXZ = normalize(position);
				const float3 up = float3(0, 1, 0);

				// POSITION
				// wave profile
				float2 waveXY = wave(t, _Time.y);
				// 3D position from 2D profile
				float3 surfPosition = dirXZ * waveXY.x + up * waveXY.y + _Center;

				// NORMAL
				// wave profile derivative
				const float dt = 1e-4;
				float2 dWave = (wave(t + dt, _Time.y) - wave(t - dt, _Time.y)) / (2.0 * dt);
				float2 normal2D = dWave.yx; // rotate the derivative to get the normal
				// 3D position from 2D profile
				float3 surfNormal = normalize(dirXZ * normal2D.x + up * normal2D.y);
				
				FragmentOutput o;
				o.color0 = float4(surfPosition, 1) * _Mix;
				o.color1 = float4(surfNormal, 1) * _Mix *  0.5; // divide by 2 as normal components are in [0, 1]
				return o;
			}
			ENDCG
		}
	}
}