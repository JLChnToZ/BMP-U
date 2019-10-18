Shader "Unlit/BGA Shader" {
    Properties {
        _MainTex ("Texture", 2D) = "black" {}
    }
    SubShader {
        Tags {
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}
        LOD 100
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

			static const fixed BORDER_MIN_COLOR = 8. / 255.;
			static const fixed BORDER_MAX_COLOR = 16. / 255.;
			static const fixed BORDER_MIN_MAGNITUDE = length(BORDER_MIN_COLOR.xxx);
			static const fixed BORDER_MAX_MAGNITUDE = length(BORDER_MAX_COLOR.xxx);

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i): SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
				if(col.x < BORDER_MAX_COLOR && col.y < BORDER_MAX_COLOR && col.z < BORDER_MAX_COLOR)
					col.w *= smoothstep(BORDER_MIN_MAGNITUDE, BORDER_MAX_MAGNITUDE, length(col.xyz));
                return col;
            }
            ENDCG
        }
    }
}
