Shader "Custom/OceanSurface"
{
	Properties
	{
		_DisplacementTex ("Water Surface Displacement", 2D) = "white" {}
		_RefractedColor("Refracted Color", Color) = (1, 1, 1, 1)
		_ReflectedColor("Reflected Color", Color) = (1, 1, 1, 1)
		_FresnelPower("Fresnel Power", Range(0, 12)) = 1
		_FresnelBias("Fresnel Bias", Range(-1, 1)) = 0
		_FresnelScale("Fresnel Scale", Range(0, 1)) = 0.5
		_Skybox ("Skybox", Cube) = "" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			// As we use Graphics.DrawProcedural to render,
			// We have to handle the model matrix explicitely ourself
			float4x4 _ModelMatrix;

			float4 _RefractedColor;
			float4 _ReflectedColor;
			float _SpecularPow;
			sampler2D _PositionTex;
			sampler2D _NormalTex;
			samplerCUBE _Skybox;

			float _FresnelPower;
			float _FresnelBias;
			float _FresnelScale;

			struct Point
			{
				float3 position;
			};
			
			StructuredBuffer<Point> _Points;

			struct FragInput
			{
				float4 position : SV_POSITION;
				float3 normal : NORMAL;
				float3 viewDir : CUSTOM0;
				float3 tmp : CUSTOM1;
			};

			FragInput vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				// read surface position from texture
				float3 position = _Points[id].position;
				float4 sampleSurface = tex2Dlod(_PositionTex, float4(position.xz, 0, 0));
				// read normal from texture
				float3 normal = (tex2Dlod(_NormalTex, float4(position.xz, 0, 0)).xyz - float3(0.5, 0.5, 0.5)) * 2;

				float4 objVertex = mul(_ModelMatrix, sampleSurface);
				FragInput fragInput;
				fragInput.position = mul(UNITY_MATRIX_VP, objVertex);
				fragInput.normal = UnityObjectToWorldNormal(float4(normal, 1));
				fragInput.viewDir = normalize(WorldSpaceViewDir(objVertex));

				fragInput.tmp = sampleSurface.xyz;

				return fragInput;
			}

			fixed4 frag(FragInput fragInput) : COLOR
			{
				float3 reflectedDir = reflect(-fragInput.viewDir, fragInput.normal);
				float4 reflectColor = texCUBE(_Skybox, reflectedDir) * _ReflectedColor;
				float4 refractColor = _RefractedColor;

				float normDotLight = dot(_WorldSpaceLightPos0.xyz, fragInput.normal);
				float fresnel = clamp(_FresnelBias + _FresnelScale * pow(1 + normDotLight, _FresnelPower) , 0, 1);

				return lerp(refractColor, reflectColor, fresnel);

				//return float4(fragInput.tmp.y * 12, 0, 0, 1);
			}

			ENDCG
		}
	}
}
