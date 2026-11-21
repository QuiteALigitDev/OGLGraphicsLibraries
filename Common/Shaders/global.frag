#version 330 core

out vec4 FragColor;

in vec2 texCoord;
in vec3 FragPos;
in vec3 Normal;

uniform sampler2D texture1;
uniform sampler2D texture2;
uniform vec3 CamView;

float beckmannDistribution(float x, float roughness) {
  float NdotH = max(x, 0.0001);
  float cos2Alpha = NdotH * NdotH;
  float tan2Alpha = (cos2Alpha - 1.0) / cos2Alpha;
  float roughness2 = roughness * roughness;
  float denom = 3.141592653589793 * roughness2 * cos2Alpha * cos2Alpha;
  return exp(tan2Alpha / roughness2) / denom;
}


void main()
{
   //Variable info
   vec3 camview = CamView;
   vec3 LightSource = normalize(vec3(1.0, 1.0, 0.0));
   vec3 LightColor = vec3(1, 1, 1);
   vec3 ambientLight = vec3(1, 1, 1);
   float roughness = 0.4;
   float fresnel = 5.0;
   float threshold = 0.25;

   //Diffuse
   vec3 L = normalize(LightSource);
   vec3 N = normalize(Normal);

   float DiffuseStrength = max(0.0, dot(L, N));
   vec3 Diffuse = DiffuseStrength * LightColor;


   //Specular Lighting (https://github.com/glslify/glsl-specular-cook-torrance/blob/master/index.glsl)
   vec3 viewSource = normalize(camview - FragPos);
   vec3 ReflectSource = normalize(reflect(-L, N));

   float VdotN = max(dot(viewSource, N), 0.0);
   float LdotN = max(dot(L, N), 0.0);

   //Half angle vector
   vec3 H = normalize(LightSource + viewSource);

   //Geometric term
   float NdotH = max(dot(N, H), 0.0);
   float VdotH = max(dot(viewSource, H), 0.000001);
   float x = 2.0 * NdotH / VdotH;
   float G = min(1.0, min(x * VdotN, x * LdotN));
   
   //Distribution term
   float D = beckmannDistribution(NdotH, roughness);

   //Fresnel term
   float F = pow(1.0 - VdotN, fresnel);

   //Multiply terms and done
   float SpecularStrength =  G * F * D / max(3.14159265 * VdotN * LdotN, 0.000001);

   vec3 Specular = SpecularStrength * LightColor;
    
   // create the final Lighting Color
   vec3 Lighting = ambientLight * 0.5 + Diffuse * 1 + Specular * 0.5;
   
   // final color of that meshes Lighting with Texture
   FragColor = vec4(Lighting, 1.0f) *  texture(texture1, texCoord);


   //FragColor = mix(texture(texture1, texCoord), texture(texture2, texCoord), 0.2);
}

