#version 330 core

out vec4 FragColor;

in vec2 texCoord;
in vec3 FragPos;
in vec3 Normal;

uniform sampler2D texture1;
uniform sampler2D texture2;
uniform vec3 CamView;

void main()
{
  vec3 norm = normalize(Normal);

  vec3 color = norm * 0.5 + 0.5;

  FragColor = vec4(color, 1.0);
}

