// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CartoonCoffee/Particle Additive"
{
	Properties
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		_NoiseTexture("Noise Texture", 2D) = "white" {}
		[Toggle(_ENABLEADJUSTCOLOR_ON)] _EnableAdjustColor("Enable Adjust Color", Float) = 0
		_AdjustColorFade("Adjust Color: Fade", Range( 0 , 1)) = 1
		_AdjustColorBrightness("Adjust Color: Brightness", Float) = 1
		_AdjustColorContrast("Adjust Color: Contrast", Float) = 1
		_AdjustColorSaturation("Adjust Color: Saturation", Float) = 1
		_AdjustColorHueShift("Adjust Color: Hue Shift", Range( 0 , 1)) = 0
		[Toggle(_ENABLECUSTOMFADE_ON)] _EnableCustomFade("Enable Custom Fade", Float) = 0
		_CustomFadeFadeMask("Custom Fade: Fade Mask", 2D) = "white" {}
		_CustomFadeSmoothness("Custom Fade: Smoothness", Float) = 2
		_CustomFadeNoiseScale("Custom Fade: Noise Scale", Vector) = (1,1,0,0)
		_CustomFadeNoiseFactor("Custom Fade: Noise Factor", Range( 0 , 0.5)) = 0
		_CustomFadeAlpha("Custom Fade: Alpha", Range( 0 , 1)) = 1
		[Toggle(_ENABLESPLITTONING_ON)] _EnableSplitToning("Enable Split Toning", Float) = 0
		_SplitToningFade("Split Toning: Fade", Range( 0 , 1)) = 1
		[HDR]_SplitToningHighlightsColor("Split Toning: Highlights Color", Color) = (1,0.1,0.1,0)
		[HDR]_SplitToningShadowsColor("Split Toning: Shadows Color", Color) = (0.1,0.4000002,1,0)
		_SplitToningContrast("Split Toning: Contrast", Float) = 1
		_SplitToningBalance("Split Toning: Balance", Float) = 1
		_SplitToningShift("Split Toning: Shift", Range( -1 , 1)) = 0
		[Toggle(_ENABLEBLACKTINT_ON)] _EnableBlackTint("Enable Black Tint", Float) = 0
		_BlackTintFade("Black Tint: Fade", Range( 0 , 1)) = 1
		[HDR]_BlackTintColor("Black Tint: Color", Color) = (1,0,0,0)
		_BlackTintPower("Black Tint: Power", Float) = 2
		[Toggle(_ENABLEALPHATINT_ON)] _EnableAlphaTint("Enable Alpha Tint", Float) = 0
		_AlphaTintFade("Alpha Tint: Fade", Range( 0 , 1)) = 1
		[HDR]_AlphaTintColor("Alpha Tint: Color", Color) = (23.96863,1.254902,23.96863,0)
		_AlphaTintPower("Alpha Tint: Power", Float) = 1
		_AlphaTintMinAlpha("Alpha Tint: Min Alpha", Range( 0 , 1)) = 0.05
		[Toggle(_ENABLEUVDISTORT_ON)] _EnableUVDistort("Enable UV Distort", Float) = 0
		_UVDistortFade("UV Distort: Fade", Range( 0 , 1)) = 1
		[NoScaleOffset]_UVDistortShaderMask("UV Distort: Shader Mask", 2D) = "white" {}
		_UVDistortSpace("UV Distort: Space", Int) = 0
		_UVDistortFrom("UV Distort: From", Vector) = (-0.02,-0.02,0,0)
		_UVDistortTo("UV Distort: To", Vector) = (0.02,0.02,0,0)
		_UVDistortSpeed("UV Distort: Speed", Vector) = (2,2,0,0)
		_UVDistortNoiseScale("UV Distort: Noise Scale", Vector) = (0.1,0.1,0,0)
		[Toggle(_ENABLEUVSCROLL_ON)] _EnableUVScroll("Enable UV Scroll", Float) = 0
		[ASEEnd]_UVScrollSpeed("UV Scroll: Speed", Vector) = (0.2,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

	}


	Category 
	{
		SubShader
		{
		LOD 0

			Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
			Blend One One
			ColorMask RGB
			Cull Off
			Lighting Off 
			ZWrite Off
			ZTest LEqual
			
			Pass {
			
				CGPROGRAM
				
				#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
				#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
				#endif
				
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_instancing
				#pragma multi_compile_particles
				#pragma multi_compile_fog
				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_FRAG_COLOR
				#pragma shader_feature _ENABLEADJUSTCOLOR_ON
				#pragma shader_feature _ENABLEALPHATINT_ON
				#pragma shader_feature _ENABLEBLACKTINT_ON
				#pragma shader_feature _ENABLESPLITTONING_ON
				#pragma shader_feature _ENABLECUSTOMFADE_ON
				#pragma shader_feature _ENABLEUVSCROLL_ON
				#pragma shader_feature _ENABLEUVDISTORT_ON


				#include "UnityCG.cginc"

				struct appdata_t 
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					
				};

				struct v2f 
				{
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD2;
					#endif
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
					float4 ase_texcoord3 : TEXCOORD3;
					float4 ase_texcoord4 : TEXCOORD4;
				};
				
				
				#if UNITY_VERSION >= 560
				UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				#else
				uniform sampler2D_float _CameraDepthTexture;
				#endif

				//Don't delete this comment
				// uniform sampler2D_float _CameraDepthTexture;

				uniform sampler2D _MainTex;
				uniform fixed4 _TintColor;
				uniform float4 _MainTex_ST;
				uniform float _InvFade;
				uniform float2 _UVDistortFrom;
				uniform float2 _UVDistortTo;
				uniform sampler2D _NoiseTexture;
				uniform int _UVDistortSpace;
				float4 _MainTex_TexelSize;
				uniform float2 _UVDistortSpeed;
				uniform float2 _UVDistortNoiseScale;
				uniform float _UVDistortFade;
				uniform sampler2D _UVDistortShaderMask;
				uniform float4 _UVDistortShaderMask_ST;
				uniform float2 _UVScrollSpeed;
				uniform sampler2D _CustomFadeFadeMask;
				uniform float2 _CustomFadeNoiseScale;
				uniform float _CustomFadeNoiseFactor;
				uniform float _CustomFadeSmoothness;
				uniform float _CustomFadeAlpha;
				uniform float4 _SplitToningShadowsColor;
				uniform float4 _SplitToningHighlightsColor;
				uniform float _SplitToningShift;
				uniform float _SplitToningBalance;
				uniform float _SplitToningContrast;
				uniform float _SplitToningFade;
				uniform float4 _BlackTintColor;
				uniform float _BlackTintPower;
				uniform float _BlackTintFade;
				uniform float4 _AlphaTintColor;
				uniform float _AlphaTintPower;
				uniform float _AlphaTintFade;
				uniform float _AlphaTintMinAlpha;
				uniform float _AdjustColorHueShift;
				uniform float _AdjustColorSaturation;
				uniform float _AdjustColorContrast;
				uniform float _AdjustColorBrightness;
				uniform float _AdjustColorFade;
				float3 HSVToRGB( float3 c )
				{
					float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
					float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
					return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
				}
				
				float3 RGBToHSV(float3 c)
				{
					float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
					float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
					float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
					float d = q.x - min( q.w, q.y );
					float e = 1.0e-10;
					return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
				}


				v2f vert ( appdata_t v  )
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					float3 ase_worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.ase_texcoord3.xyz = ase_worldPos;
					
					o.ase_texcoord4 = v.vertex;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					o.ase_texcoord3.w = 0;

					v.vertex.xyz +=  float3( 0, 0, 0 ) ;
					o.vertex = UnityObjectToClipPos(v.vertex);
					#ifdef SOFTPARTICLES_ON
						o.projPos = ComputeScreenPos (o.vertex);
						COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.texcoord = v.texcoord;
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}

				fixed4 frag ( v2f i  ) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( i );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( i );

					#ifdef SOFTPARTICLES_ON
						float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
						float partZ = i.projPos.z;
						float fade = saturate (_InvFade * (sceneZ-partZ));
						i.color.a *= fade;
					#endif

					float2 texCoord67 = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float3 ase_worldPos = i.ase_texcoord3.xyz;
					float2 appendResult2_g105 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
					float2 ifLocalVar2_g104 = 0;
					if( _UVDistortSpace > 1.0 )
					ifLocalVar2_g104 = (ase_worldPos).xy;
					else if( _UVDistortSpace == 1.0 )
					ifLocalVar2_g104 = (i.ase_texcoord4.xyz).xy;
					else if( _UVDistortSpace < 1.0 )
					ifLocalVar2_g104 = ( i.texcoord.xy / ( 100.0 / appendResult2_g105 ) );
					float2 lerpResult21_g103 = lerp( _UVDistortFrom , _UVDistortTo , tex2D( _NoiseTexture, ( ( ifLocalVar2_g104 + ( _UVDistortSpeed * _Time.y ) ) * _UVDistortNoiseScale ) ).r);
					float2 appendResult2_g107 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
					float2 uv_UVDistortShaderMask = i.texcoord.xy * _UVDistortShaderMask_ST.xy + _UVDistortShaderMask_ST.zw;
					float4 tex2DNode3_g106 = tex2D( _UVDistortShaderMask, uv_UVDistortShaderMask );
					#ifdef _ENABLEUVDISTORT_ON
					float2 staticSwitch65 = ( texCoord67 + ( lerpResult21_g103 * ( 100.0 / appendResult2_g107 ) * ( _UVDistortFade * ( tex2DNode3_g106.r * tex2DNode3_g106.a ) ) ) );
					#else
					float2 staticSwitch65 = texCoord67;
					#endif
					#ifdef _ENABLEUVSCROLL_ON
					float2 staticSwitch96 = ( ( ( _UVScrollSpeed * _Time.y ) + staticSwitch65 ) % float2( 1,1 ) );
					#else
					float2 staticSwitch96 = staticSwitch65;
					#endif
					float4 tex2DNode17 = tex2D( _MainTex, staticSwitch96 );
					float4 temp_output_1_0_g126 = tex2DNode17;
					float2 temp_output_57_0_g126 = staticSwitch96;
					float4 tex2DNode3_g126 = tex2D( _CustomFadeFadeMask, temp_output_57_0_g126 );
					float clampResult37_g126 = clamp( ( ( ( i.color.a * 2.0 ) - 1.0 ) + ( tex2DNode3_g126.r + ( tex2D( _NoiseTexture, ( temp_output_57_0_g126 * _CustomFadeNoiseScale ) ).r * _CustomFadeNoiseFactor ) ) ) , 0.0 , 1.0 );
					float4 appendResult13_g126 = (float4(( float4( (i.color).rgb , 0.0 ) * temp_output_1_0_g126 ).rgb , ( temp_output_1_0_g126.a * pow( clampResult37_g126 , ( _CustomFadeSmoothness / max( tex2DNode3_g126.r , 0.05 ) ) ) * _CustomFadeAlpha )));
					#ifdef _ENABLECUSTOMFADE_ON
					float4 staticSwitch64 = appendResult13_g126;
					#else
					float4 staticSwitch64 = ( tex2DNode17 * i.color );
					#endif
					float4 temp_output_1_0_g130 = staticSwitch64;
					float4 break2_g131 = temp_output_1_0_g130;
					float temp_output_3_0_g130 = ( ( break2_g131.x + break2_g131.y + break2_g131.z ) / 3.0 );
					float clampResult25_g130 = clamp( ( ( ( ( temp_output_3_0_g130 + _SplitToningShift ) - 0.5 ) * _SplitToningBalance ) + 0.5 ) , 0.0 , 1.0 );
					float3 lerpResult6_g130 = lerp( (_SplitToningShadowsColor).rgb , (_SplitToningHighlightsColor).rgb , clampResult25_g130);
					float temp_output_9_0_g132 = max( _SplitToningContrast , 0.0 );
					float saferPower7_g132 = max( ( temp_output_3_0_g130 + ( 0.1 * max( ( 1.0 - temp_output_9_0_g132 ) , 0.0 ) ) ) , 0.0001 );
					float3 lerpResult11_g130 = lerp( (temp_output_1_0_g130).rgb , ( lerpResult6_g130 * pow( saferPower7_g132 , temp_output_9_0_g132 ) ) , _SplitToningFade);
					float4 appendResult18_g130 = (float4(lerpResult11_g130 , temp_output_1_0_g130.a));
					#ifdef _ENABLESPLITTONING_ON
					float4 staticSwitch98 = appendResult18_g130;
					#else
					float4 staticSwitch98 = staticSwitch64;
					#endif
					float4 temp_output_1_0_g133 = staticSwitch98;
					float3 temp_output_4_0_g133 = (temp_output_1_0_g133).rgb;
					float4 break12_g133 = temp_output_1_0_g133;
					float3 lerpResult7_g133 = lerp( temp_output_4_0_g133 , ( temp_output_4_0_g133 + (_BlackTintColor).rgb ) , pow( ( 1.0 - min( max( max( break12_g133.r , break12_g133.g ) , break12_g133.b ) , 1.0 ) ) , max( _BlackTintPower , 0.001 ) ));
					float3 lerpResult13_g133 = lerp( temp_output_4_0_g133 , lerpResult7_g133 , _BlackTintFade);
					float4 appendResult11_g133 = (float4(lerpResult13_g133 , break12_g133.a));
					#ifdef _ENABLEBLACKTINT_ON
					float4 staticSwitch79 = appendResult11_g133;
					#else
					float4 staticSwitch79 = staticSwitch98;
					#endif
					float4 temp_output_1_0_g134 = staticSwitch79;
					float saferPower11_g134 = max( ( 1.0 - temp_output_1_0_g134.a ) , 0.0001 );
					float3 lerpResult4_g134 = lerp( (temp_output_1_0_g134).rgb , (_AlphaTintColor).rgb , ( pow( saferPower11_g134 , _AlphaTintPower ) * _AlphaTintFade * step( _AlphaTintMinAlpha , temp_output_1_0_g134.a ) ));
					float4 appendResult13_g134 = (float4(lerpResult4_g134 , temp_output_1_0_g134.a));
					#ifdef _ENABLEALPHATINT_ON
					float4 staticSwitch86 = appendResult13_g134;
					#else
					float4 staticSwitch86 = staticSwitch79;
					#endif
					float4 break2_g135 = staticSwitch86;
					float3 appendResult4_g135 = (float3(break2_g135.r , break2_g135.g , break2_g135.b));
					float3 hsvTorgb16_g135 = RGBToHSV( appendResult4_g135 );
					float clampResult18_g135 = clamp( ( hsvTorgb16_g135.y * _AdjustColorSaturation ) , 0.0 , 1.0 );
					float temp_output_9_0_g136 = max( _AdjustColorContrast , 0.0 );
					float saferPower7_g136 = max( ( hsvTorgb16_g135.z + ( 0.1 * max( ( 1.0 - temp_output_9_0_g136 ) , 0.0 ) ) ) , 0.0001 );
					float3 hsvTorgb24_g135 = HSVToRGB( float3(( hsvTorgb16_g135.x + _AdjustColorHueShift ),clampResult18_g135,( pow( saferPower7_g136 , temp_output_9_0_g136 ) * _AdjustColorBrightness )) );
					float3 lerpResult9_g135 = lerp( appendResult4_g135 , hsvTorgb24_g135 , _AdjustColorFade);
					float4 appendResult3_g135 = (float4(lerpResult9_g135 , break2_g135.a));
					#ifdef _ENABLEADJUSTCOLOR_ON
					float4 staticSwitch84 = appendResult3_g135;
					#else
					float4 staticSwitch84 = staticSwitch86;
					#endif
					float4 appendResult90 = (float4(( (staticSwitch84).rgb * staticSwitch84.a ) , 1.0));
					

					fixed4 col = appendResult90;
					UNITY_APPLY_FOG(i.fogCoord, col);
					return col;
				}
				ENDCG 
			}
		}	
	}
	CustomEditor "CartoonCoffee.ParticleShaderGUI"
	
	Fallback "2"
}
/*ASEBEGIN
Version=18909
181;193;1413;683;549.8807;519.7966;1;True;False
Node;AmplifyShaderEditor.TemplateShaderPropertyNode;41;-3188.404,-194.2;Inherit;False;0;0;_MainTex;Shader;False;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;67;-2996.457,3.141286;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;88;-3103.937,470.3728;Inherit;True;Property;_NoiseTexture;Noise Texture;0;0;Create;True;0;0;0;False;0;False;4addb5285d2d96b46bcc3d03bf698f23;4addb5285d2d96b46bcc3d03bf698f23;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.FunctionNode;89;-2669.609,189.0378;Inherit;False;_UVDistort;36;;103;d6b8c102b9317a0418c08eb00598bec7;0;3;1;FLOAT2;0,0;False;26;SAMPLER2D;;False;3;SAMPLER2D;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch;65;-2403.408,1.236556;Inherit;False;Property;_EnableUVDistort;Enable UV Distort;35;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;95;-2053.663,100.0231;Inherit;False;_UVScroll;46;;122;be39ff8debe04f84baeada43b5b8aeb7;0;1;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch;96;-1795.765,-11.26569;Inherit;False;Property;_EnableUVScroll;Enable UV Scroll;45;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;17;-1442.57,-117.9648;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;78;-1025.791,-240.246;Inherit;False;TintVertex;-1;;125;b0b94dd27c0f3da49a89feecae766dcc;0;1;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;87;-1012.893,140.5994;Inherit;False;_CustomFade;9;;126;09a17a2b3ff778e4baeae7d542f88dd6;0;3;57;FLOAT2;0,0;False;56;SAMPLER2D;;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StaticSwitch;64;-534.2484,-363.9702;Inherit;False;Property;_EnableCustomFade;Enable Custom Fade;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;97;-63.04102,-172.3311;Inherit;False;_SplitToning;17;;130;6b87c8196f94bcd478491aaa714b31ef;0;1;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StaticSwitch;98;333.6193,-355.7966;Inherit;False;Property;_EnableSplitToning;Enable Split Toning;16;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;81;746.9941,-187.6142;Inherit;False;_BlackTint;25;;133;e72823b8923579647a619869b654ace9;0;1;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StaticSwitch;79;1109.666,-351.9158;Inherit;False;Property;_EnableBlackTint;Enable Black Tint;24;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;85;1573.545,-214.9836;Inherit;False;_AlphaTint;30;;134;ae9f8d24855c66643be02b2aa90f050e;0;1;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StaticSwitch;86;1907.981,-348.1771;Inherit;False;Property;_EnableAlphaTint;Enable Alpha Tint;29;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;82;2407.541,-244.2802;Inherit;False;_AdjustColor;2;;135;e7083192d13fe334cab64b3d59374f2b;0;1;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;84;2707.779,-354.0338;Inherit;False;Property;_EnableAdjustColor;Enable Adjust Color;1;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;92;3205.363,-313.1724;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.BreakToComponentsNode;91;3141.936,-160.4294;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;93;3447.422,-218.6789;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DynamicAppendNode;90;3659.709,-210.912;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;75;3847.149,-305.2042;Float;False;True;-1;2;CartoonCoffee.ParticleShaderGUI;0;7;CartoonCoffee/Particle Additive;0b6a9f8b4f707c74ca64c0be8e590de0;True;SubShader 0 Pass 0;0;0;SubShader 0 Pass 0;2;False;True;4;1;False;-1;1;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;True;True;True;True;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;2;False;-1;True;3;False;-1;False;True;4;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;0;0;2;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;89;1;67;0
WireConnection;89;26;88;0
WireConnection;89;3;41;0
WireConnection;65;1;67;0
WireConnection;65;0;89;0
WireConnection;95;1;65;0
WireConnection;96;1;65;0
WireConnection;96;0;95;0
WireConnection;17;0;41;0
WireConnection;17;1;96;0
WireConnection;78;1;17;0
WireConnection;87;57;96;0
WireConnection;87;56;88;0
WireConnection;87;1;17;0
WireConnection;64;1;78;0
WireConnection;64;0;87;0
WireConnection;97;1;64;0
WireConnection;98;1;64;0
WireConnection;98;0;97;0
WireConnection;81;1;98;0
WireConnection;79;1;98;0
WireConnection;79;0;81;0
WireConnection;85;1;79;0
WireConnection;86;1;79;0
WireConnection;86;0;85;0
WireConnection;82;1;86;0
WireConnection;84;1;86;0
WireConnection;84;0;82;0
WireConnection;92;0;84;0
WireConnection;91;0;84;0
WireConnection;93;0;92;0
WireConnection;93;1;91;3
WireConnection;90;0;93;0
WireConnection;75;0;90;0
ASEEND*/
//CHKSM=87FA38E20CE5460D35DA6B1EEED7CE851994DAF1