attribute vec2 position;
varying vec2 surf_pos;
uniform vec2 screen_ratio;
void main()
{
    surf_pos = position * screen_ratio;
    gl_Position = vec4(position, 0, 1);
}