Shader "Custom/EspacioProcedural"
{
    // Cielo de espacio 100% procedural: estrellas diminutas que titilan + nebulosas + giro lento.
    // No usa texturas, así que no se pixela sin importar la resolución de pantalla.
    Properties
    {
        [Header(Fondo)]
        _ColorFondo ("Color de fondo", Color) = (0.003, 0.004, 0.012, 1)
        _Exposicion ("Exposicion general", Range(0, 3)) = 1

        [Header(Estrellas)]
        _Densidad ("Densidad de celdas", Range(15, 120)) = 55
        _Probabilidad ("Probabilidad de estrella por celda", Range(0, 1)) = 0.5
        _TamanoMin ("Tamano minimo (celdas)", Range(0.01, 0.2)) = 0.035
        _TamanoMax ("Tamano maximo (celdas)", Range(0.01, 0.2)) = 0.09
        _MinPixeles ("Tamano minimo en pixeles", Range(0.3, 3)) = 0.8
        _BrilloEstrellas ("Brillo de estrellas", Range(0, 6)) = 1.8

        [Header(Titileo)]
        _Titileo ("Intensidad del titileo", Range(0, 1)) = 0.45
        _VelTitileo ("Velocidad del titileo", Range(0, 10)) = 3

        [Header(Nebulosa)]
        _ColorNebulosaA ("Color nebulosa A", Color) = (0.10, 0.25, 0.65, 1)
        _ColorNebulosaB ("Color nebulosa B", Color) = (0.55, 0.15, 0.55, 1)
        _IntensidadNebulosa ("Intensidad nebulosa", Range(0, 2)) = 0.3
        _EscalaNebulosa ("Escala nebulosa", Range(0.3, 6)) = 1.6
        _UmbralNebulosa ("Umbral (mayor = menos nube)", Range(0, 1)) = 0.45

        [Header(Movimiento)]
        _Rotacion ("Velocidad de rotacion (grados por segundo)", Float) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _ColorFondo;
            float _Exposicion;
            float _Densidad;
            float _Probabilidad;
            float _TamanoMin;
            float _TamanoMax;
            float _MinPixeles;
            float _BrilloEstrellas;
            float _Titileo;
            float _VelTitileo;
            float4 _ColorNebulosaA;
            float4 _ColorNebulosaB;
            float _IntensidadNebulosa;
            float _EscalaNebulosa;
            float _UmbralNebulosa;
            float _Rotacion;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Giro lento alrededor del eje vertical
                float a = radians(_Rotacion) * _Time.y;
                float s = sin(a);
                float c = cos(a);
                float3 d = v.vertex.xyz;
                o.dir = float3(c * d.x + s * d.z, d.y, -s * d.x + c * d.z);
                return o;
            }

            // ---------- Hash y ruido ----------
            float hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float3 hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            float vnoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash13(i);
                float n100 = hash13(i + float3(1, 0, 0));
                float n010 = hash13(i + float3(0, 1, 0));
                float n110 = hash13(i + float3(1, 1, 0));
                float n001 = hash13(i + float3(0, 0, 1));
                float n101 = hash13(i + float3(1, 0, 1));
                float n011 = hash13(i + float3(0, 1, 1));
                float n111 = hash13(i + float3(1, 1, 1));

                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            float fbm(float3 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 5; k++)
                {
                    v += a * vnoise(p);
                    p = p * 2.03 + float3(17.1, 9.2, 5.3);
                    a *= 0.5;
                }
                return v / 0.96875;
            }

            // ---------- Nebulosa ----------
            float3 Nebulosa(float3 dir)
            {
                float3 q = dir * _EscalaNebulosa;
                float n = fbm(q + 11.3);
                float masa = smoothstep(_UmbralNebulosa, _UmbralNebulosa + 0.3, n);
                float fino = fbm(q * 2.7 + 71.0);
                masa *= (0.55 + 0.9 * fino);

                float mezcla = smoothstep(0.35, 0.65, fbm(q * 0.8 + 150.0));
                float3 c = lerp(_ColorNebulosaA.rgb, _ColorNebulosaB.rgb, mezcla);
                return c * masa * _IntensidadNebulosa;
            }

            // ---------- Estrellas ----------
            // La direccion se escala y se divide en celdas 3D; cada celda puede tener una
            // estrella en un punto aleatorio. Se ve donde la esfera del cielo pasa cerca de ese punto.
            float3 Estrellas(float3 dir)
            {
                float3 p = dir * _Densidad;
                float px = length(fwidth(p));   // tamano de un pixel en unidades de celda

                float3 cell = floor(p);
                float3 r1 = hash33(cell);
                float3 r2 = hash33(cell + 37.7);
                if (r2.x > _Probabilidad) return float3(0, 0, 0);

                float brillo = pow(r2.z, 3.0);                       // pocas brillantes, muchas tenues
                float radioDeseado = lerp(_TamanoMin, _TamanoMax, brillo);
                float radio = min(max(radioDeseado, px * _MinPixeles), 0.22);

                float3 pos = cell + 0.22 + 0.56 * r1;                // margen para no cortarse en el borde de la celda
                float d = length(p - pos);

                float f = saturate(1.0 - d / radio);
                f *= f;
                float ratio = saturate(radioDeseado / radio);
                f *= ratio * ratio;                                   // si se agranda por el minimo en pixeles, se atenua

                // Color: azulado - blanco - calido
                float t = r1.y;
                float3 azul = float3(0.70, 0.82, 1.00);
                float3 blanco = float3(1.00, 1.00, 1.00);
                float3 calido = float3(1.00, 0.86, 0.66);
                float3 col = lerp(lerp(azul, blanco, saturate(t * 2.0)), calido, saturate(t * 2.0 - 1.0));

                // Titileo: cada estrella con su propia velocidad y fase
                float fase = r1.x * 6.2831 + r1.z * 40.0;
                float vel = _VelTitileo * (0.5 + r1.z * 1.5);
                float tw = 1.0 - _Titileo * (0.5 + 0.5 * sin(_Time.y * vel + fase));

                return col * f * (0.35 + brillo) * tw * _BrilloEstrellas;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float3 col = _ColorFondo.rgb;
                col += Nebulosa(dir);
                col += Estrellas(dir);
                return float4(col * _Exposicion, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
