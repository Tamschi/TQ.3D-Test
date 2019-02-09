#version 150 core

in vec3 position;

uniform mat4 transformation;

void main() {
	gl_Position = transformation * vec4(position, 1.0);
}
