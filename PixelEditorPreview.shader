Shader "NewChromantics/PopPixelEditor/PixelEditorPreview"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}

		TransparentColour0("TransparentColour0", COLOR ) = ( 0.5,0.7,1.0,1)
		TransparentColour1("TransparentColour1", COLOR ) = ( 0.9,0.9,0.9,1)
		TransparentSize("TransparentSize", Range(4,50) ) = 4

		GridColour0("GridColour0", COLOR ) = ( 0.9,0.9,0.9,1)
		GridSize0("GridSize0", Range(1,50) ) = 1
		
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
			float4 _MainTex_TexelSize;
			float3 TransparentColour0;
			float3 TransparentColour1;
			float TransparentSize;


			float4 GridColour0;
			float GridSize0;

			float4 ScreenRect;

			float3 GetTransparentColour(float2 uv)
			{
				uv *= ScreenRect.zw;
				float2 TransparentUv = fmod( uv, float2(TransparentSize*2,TransparentSize*2) ) / float2(TransparentSize,TransparentSize);
				int TransparentColour = floor( TransparentUv.x ) + ( 2 * floor( TransparentUv.y ) );
				float3 TransparentColours[4];
				TransparentColours[0] = TransparentColour0;
				TransparentColours[1] = TransparentColour1;
				TransparentColours[2] = TransparentColour1;
				TransparentColours[3] = TransparentColour0;
				return TransparentColours[TransparentColour];
			}

			float4 ApplyColour(float4 Destination,float4 Source)
			{
				Source.xyz *= Source.w;
				Destination.xyz *= 1 - Source.w;
				Destination.xyz += Source;
				return Destination;
			}

			float4 GetGridColour(float4 GridColour,float GridSize,float2 uv)
			{
				/*
				uv *= ScreenRect.zw;

				float2 Griduv = fmod( uv, float2(GridSize,GridSize) );

				//	gr: work out how to make this a minimum of 1 pixel for the screen.
				float GridThickness = 1;

				if ( Griduv.x > GridThickness && Griduv.y > GridThickness && Griduv.x < (1-GridThickness) && Griduv.y < (1-GridThickness) )
				*/	GridColour.w = 0;
				
				return GridColour;
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float4 rgba = tex2D(_MainTex, i.uv);
				float3 Trans = GetTransparentColour( i.uv );

				rgba = ApplyColour( float4(Trans,1), rgba );


				float4 Grid0 = GetGridColour( GridColour0, GridSize0, i.uv );
				rgba = ApplyColour( rgba, Grid0 );



				rgba.w = 1;

				return rgba;
			}
			ENDCG
		}
	}
}
