// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Camera Filter Pack v4.0.0                  
//                                     
// by VETASOFT 2018                    

Shader "CameraFilterPack/CameraFilterPack_Classic_ThermalVision" {
Properties
{
	_MainTex("Base (RGB)", 2D) = "white" {}
	_TimeX("Time", Range(0.0, 1.0)) = 1.0
	_ScreenResolution("_ScreenResolution", Vector) = (0.,0.,0.,0.)
}
SubShader
{
	Pass
	{
		Cull Off ZWrite Off ZTest Always
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma target 3.0
		#pragma glsl
		#include "UnityCG.cginc"
		uniform sampler2D _MainTex;
		uniform float _TimeX;

		uniform float _Speed;
		uniform float Fade;
		uniform float Crt;
		uniform float Curve;
		uniform float Color1;
		uniform float Color2;
		uniform float Color3;


		uniform float4 _ScreenResolution;

		struct appdata_t
		{
			float4 vertex   : POSITION;
			float4 color    : COLOR;
			float2 texcoord : TEXCOORD0;
		};
		struct v2f
		{
			float2 texcoord  : TEXCOORD0;
			float4 vertex   : SV_POSITION;
			float4 color    : COLOR;
		};
		v2f vert(appdata_t IN)
		{
			v2f OUT;
			OUT.vertex = UnityObjectToClipPos(IN.vertex);
			OUT.texcoord = IN.texcoord;
			OUT.color = IN.color;
			return OUT;
		}

		float CFhash(float n)
		{
			return frac(sin(n) * 43812.175489);
		}

		float CFnoise(float2 p)
		{
			p*=128;
			float2 pi = floor(p);
			float2 pf = frac(p);
			float n = pi.x + 59.0 * pi.y;
			pf = pf * pf * (3.0 - 2.0 * pf);
			return lerp(lerp(CFhash(n), CFhash(n + 1.0), pf.x),
			lerp(CFhash(n + 59.0), CFhash(n + 1.0 + 59.0), pf.x),
			pf.y);
		}


		half4 _MainTex_ST;

		float3 thermal_vision(in float3 color) {
			float3 colors[3];
			colors[0] = float3(0.0, 0.0, Color1);
			colors[1] = float3(Color2, Color1, 0.0);
			colors[2] = float3(Color3, 0.0, 0.0);
			float luminance = dot(float3(0.40, 0.38, 0.25), color);
			if(luminance < 0.5) {
				color = lerp(colors[0], colors[1], luminance / 0.5);
			} else {
				color = lerp(colors[1], colors[2], (luminance - 0.5) / 0.5);
			}
			return color;
		}
		float scanline(float2 uv) {
			return sin(_ScreenResolution.y * uv.y * 0.7 - _Time*_Speed * 10.0);
		}
		float slowscan(float2 uv) {
			return sin(_ScreenResolution.y * uv.y * 0.02 + _Time*_Speed * 6.0);
		}
		float2 colorShift(float2 uv) {
			return float2(
			uv.x,
			uv.y + sin(_Time.y*_Speed)*0.02
			);
		}
		float noise(float2 uv) {
			return clamp(CFnoise( uv.xy + _Time.y*_Speed*6.0).r +
			CFnoise( uv.xy - _Time.y*_Speed*4.0), 0.96, 1.0);
		}
		float2 crt(float2 coord, float bend) {
			bend*=Curve;
			bend+=0.01;
			coord = (coord - 0.5) * 2.0;
			coord *= 0.5;
			coord.x *= 1.0 + pow((abs(coord.y) / bend), 2.0);
			coord.y *= 1.0 + pow((abs(coord.x) / bend), 2.0);
			coord  = (coord / 1.0) + 0.5;
			return coord;
		}
		float2 colorshift(float2 uv, float amount, float rand) {
			return float2(
				uv.x,
				uv.y + amount * rand * sin(uv.y * _ScreenResolution.y * 0.12 + _Time.y*_Speed)
			);
		}
		float2 scandistort(float2 uv) {
			float scan1 = clamp(cos(uv.y * 2.0 + _Time.y*_Speed), 0.0, 1.0);
			float scan2 = clamp(cos(uv.y * 2.0 + _Time.y*_Speed + 4.0) * 10.0, 0.0, 1.0) ;
			float amount = scan1 * scan2 * uv.x;
			uv.x -= 0.05 * lerp(CFnoise( float2(uv.x, amount)) * amount, amount, 0.9*Curve);
			return uv;
		}
		float4 frag(v2f i) : COLOR { 
			float4 cfresult=float4(0,0,0,0);
			float2 uv = i.texcoord;
			float3 color = tex2D(_MainTex, uv).rgb;
			float3 mcolor=color;
			color.rgb = thermal_vision(color.rgb);
			float2 sd_uv = lerp(uv,scandistort(uv),Curve);
			float2 crt_uv = crt(sd_uv, 2.0*Crt+0.01);
			float4 rand = CFnoise( float2(_Time.y*_Speed * 0.01, _Time.y*_Speed * 0.02));
			color.r = tex2D(_MainTex, crt(colorshift(sd_uv, 0.025, rand.r), 2.0)).r;
			color.g = tex2D(_MainTex, crt(colorshift(sd_uv, 0.01, rand.g), 2.0)).g;
			color.b = tex2D(_MainTex, crt(colorshift(sd_uv, 0.024, rand.b), 2.0)).b;
			float3 scanline_color = float3(scanline(crt_uv),scanline(crt_uv),scanline(crt_uv));
			float3 slowscan_color = float3(slowscan(crt_uv),slowscan(crt_uv),slowscan(crt_uv));
			cfresult.rgb = lerp(color, lerp(scanline_color, slowscan_color, 0.5), 0.05) * noise(uv);
			cfresult = float4(thermal_vision(cfresult.rgb), 1.0);
			cfresult.rgb=lerp(mcolor,cfresult,Fade);
			return cfresult;
		}

		ENDCG
	}

}
}
