shader_type spatial;
render_mode unshaded;

uniform sampler2D tex: source_color;

void fragment() {
	ALBEDO = texture(tex, UV * 16.).rgb * 0.1;
	ALPHA = clamp(1.0 - distance(UV, vec2(0.5, 0.5)) * 2., 0., 1.);
}
