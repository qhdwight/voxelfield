Shader "Custom/Frosted Glass" {
    Properties {
        _Size ("Blur", Range(0, 30)) = 1
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint color", Color) = (1, 1, 1, 1)
    }

    Category {

        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

        SubShader  {
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
        
            GrabPass {
            }
        
            Pass {
                Name "Vertical"
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata {
                    float4 vertex   : POSITION;
                    fixed4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f {
                    float4 vertex   : POSITION;
                    fixed4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                    float4 texworld : TEXCOORD1;
                    float4 texgrab  : TEXCOORD2;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                fixed4 _Color;

                v2f vert(appdata v) {
                    v2f o;
                    o.texworld = v.vertex;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texgrab = ComputeGrabScreenPos(o.vertex);
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.color = v.color * _Color;
                    return o;
                }

                float _Size;
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                fixed4 _TextureSampleAdd;

                fixed4 frag(v2f i) : SV_Target {
                    fixed4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                    #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(i.texworld.xy, _ClipRect);
                    #endif
    
                    #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                    #endif
                
                    fixed4 sum = fixed4(0, 0, 0, 0);

                    #define GRABPIXEL(weight, kernel) tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(float4(i.texgrab.x, i.texgrab.y + _GrabTexture_TexelSize.y * kernel * _Size, i.texgrab.z, i.texgrab.w))) * weight

                    sum += GRABPIXEL(0.05, -4.0);
                    sum += GRABPIXEL(0.09, -3.0);
                    sum += GRABPIXEL(0.12, -2.0);
                    sum += GRABPIXEL(0.15, -1.0);
                    sum += GRABPIXEL(0.18,  0.0);
                    sum += GRABPIXEL(0.15, +1.0);
                    sum += GRABPIXEL(0.12, +2.0);
                    sum += GRABPIXEL(0.09, +3.0);
                    sum += GRABPIXEL(0.05, +4.0);

                    return sum * color;
                }
                ENDCG
            }
            
            Pass {
                Name "Horizontal"
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata {
                    float4 vertex   : POSITION;
                    fixed4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f {
                    float4 vertex   : POSITION;
                    fixed4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                    float4 texworld : TEXCOORD1;
                    float4 texgrab  : TEXCOORD2;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                fixed4 _Color;

                v2f vert(appdata v) {
                    v2f o;
                    o.texworld = v.vertex;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texgrab = ComputeGrabScreenPos(o.vertex);
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.color = v.color * _Color;
                    return o;
                }

                float _Size;
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                fixed4 _TextureSampleAdd;

                fixed4 frag(v2f i) : SV_Target {
                    fixed4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                    #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(i.texworld.xy, _ClipRect);
                    #endif
    
                    #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                    #endif
                
                    fixed4 sum = fixed4(0, 0, 0, 0);

                    #define GRABPIXEL(weight, kernel) tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(float4(i.texgrab.x + _GrabTexture_TexelSize.x * kernel * _Size, i.texgrab.y, i.texgrab.z, i.texgrab.w))) * weight

                    sum += GRABPIXEL(0.05, -4.0);
                    sum += GRABPIXEL(0.09, -3.0);
                    sum += GRABPIXEL(0.12, -2.0);
                    sum += GRABPIXEL(0.15, -1.0);
                    sum += GRABPIXEL(0.18,  0.0);
                    sum += GRABPIXEL(0.15, +1.0);
                    sum += GRABPIXEL(0.12, +2.0);
                    sum += GRABPIXEL(0.09, +3.0);
                    sum += GRABPIXEL(0.05, +4.0);

                    return sum * color;
                }
                ENDCG
            }
        }
    }
}