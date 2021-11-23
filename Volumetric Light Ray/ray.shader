Shader "Unlit/ray"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Steps ("Number Of Posterize Steps", float) = 4

    }
    SubShader
    {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		Blend One One
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            //#pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                //UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4	_Color;
            float _Steps;

            float Posterize_float(float In, float Steps)
            {
                float Out;
                Out = floor(In / (1 / Steps)) * (1 / Steps);
                return Out;
            }
            

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //UNITY_TRANSFER_FOG(o,o.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
				fixed4 albedo = tex2D(_MainTex, i.uv);
				
                fixed4 color = albedo * _Color * Posterize_float(i.color,_Steps);
				return color;
            }
            ENDCG
        }
    }
}
