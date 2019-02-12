#version 150 core

in vec3 position;
in vec2 uv;

out vec2 UV;

uniform mat4 transformation;

void main() {
	gl_Position = transformation * vec4(position, 1.0);
	UV = uv;
}
