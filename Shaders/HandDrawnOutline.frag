#version 330 core
in vec2 uv;
out vec4 FragColor;

uniform sampler2D screenTexture;
uniform float time;

// ts im super fucking proud of, it basically finds the edges of the normals view and then puts outline there.

void main()
{
    vec2 texel = 1.0 / textureSize(screenTexture, 0);
    vec4 center = texture(screenTexture, uv);
    
    int outlineRadius = 2;

    float maxDiff = 0.0;

    for(int x = -outlineRadius; x <= outlineRadius; x++) {
        for(int y = -outlineRadius; y <= outlineRadius; y++) {
            if(x == 0 && y == 0) continue;
            vec4 neighbor = texture(screenTexture, uv + vec2(texel.x * x, texel.y * y));
            maxDiff = max(maxDiff, length(center.rgb - neighbor.rgb));
        }
    }

    // smoothstep for soft edges
    float outlineThreshold = 0.2;
    float edge = smoothstep(0.0, outlineThreshold, maxDiff);

    FragColor = vec4(center.rgb * 0.6, edge); // black edges, transparent elsewhere
}
