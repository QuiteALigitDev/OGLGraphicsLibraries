#version 330 core
in vec2 uv;
out vec4 FragColor;

uniform sampler2D screenTexture;
uniform float time;

void main() {
    FragColor = vec4(texture(screenTexture, uv).xyz, 1.0);
}