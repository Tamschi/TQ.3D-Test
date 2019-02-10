#version 150 core

in vec2 UV;

out vec4 outColor;

uniform sampler2D tex;

void main() {
	outColor = texture(tex, UV);
}
