shader_type spatial;

uniform vec3 color = vec3(1);
uniform sampler2D tex: source_color;

void fragment() {
	ALBEDO = texture(tex, UV).rgb * color;
}
