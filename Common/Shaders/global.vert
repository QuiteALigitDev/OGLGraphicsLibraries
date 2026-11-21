#version 330 core
layout (location = 0) in vec3 aPosition;

layout(location = 1) in vec2 aTexCoord;

layout(location = 2) in vec3 aNormal;

out vec2 texCoord;
out vec3 FragPos;
out vec3 Normal;
out vec2 iResolution;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec2 Resolution;

void main()
{
    texCoord = aTexCoord;
    
    iResolution = Resolution;
    
    // for 3d
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;

    //gl_Position = vec4(aPosition.xy, 0.0, 1.0);
    FragPos = vec3(vec4(aPosition, 1.0) * model);
    Normal = aNormal * mat3(transpose(inverse(model)));
}

