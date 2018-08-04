Shader "Custom/Curvature"
{
	Properties
	{
		_MinHue ("Min Hue", Range (0, 1)) = 0
		_MaxHue ("Max Hue", Range (0, 1)) = 1
		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			float _MinHue;
			float _MaxHue;
			float4 _Color;

			struct v2f
			{
				float4 color : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			// http://www.chilliant.com/rgb2hsv.html
			float3 HUEtoRGB(in float H)
			{
				float R = abs(H * 6 - 3) - 1;
				float G = 2 - abs(H * 6 - 2);
				float B = 2 - abs(H * 6 - 4);
				return saturate(float3(R,G,B));
			}

			v2f vert (appdata_base v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				float h = lerp(_MinHue, _MaxHue, (1 + dot(v.normal, float3(0, -1, 0))) * 0.5f);
				o.color = _Color * float4(HUEtoRGB(h), 1);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return i.color;
			}
			ENDCG
		}
	}
}
