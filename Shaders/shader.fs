#version 330 core
in vec2 uv;
out vec4 FragColor;

uniform sampler2D screenTexture;
uniform float time;

float sdSphere(vec3 p, float r){
  return length(p) - r;
}
float sdBox(vec3 p, vec3 b)
{
  vec3 q = abs(p) -b;

  return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float smin(float a, float b, float k){
    float h = max(k - abs(a - b), 0.0) / k;
    return min(a, b) - h * h * k * 0.25;
}

float map(vec3 p) {
  vec3 spherePos = vec3(0, 0, 0);
  float sphere = sdSphere(p - spherePos, 1.0);
  
  float box = sdBox(p, vec3(.75));

  return smin(sphere, box, 2.0);
}


void main()
{

  // init proccess
  vec3 ro = vec3(0, 0, -5); // ray origin
  vec3 rd = normalize(vec3(uv, 1)); // ray direction
  vec3 col = vec3(0);

  float t = 0.0; // total dist traveled
  
  // raymarching
  for (int i = 0; i < 80; i++) {
    vec3 p = ro + rd * t; // position along the ray
  
    float d = map(p); // current distance to the scene
  
    t += d;

    if (d < 0.001) break; // early stops if close enough
    if (t > 100.0) break; // early stops if too far.
  }

  // coloring
  col = vec3(t * 0.2);

  FragColor = vec4(col, 1.0);
}

