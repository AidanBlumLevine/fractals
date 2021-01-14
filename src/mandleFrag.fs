#ifdef GL_ES
precision highp float;
#endif

uniform vec3 playerPos;
uniform vec3 playerFwd;
uniform vec3 playerRight;
uniform vec3 playerUp;
uniform vec2 resolution;

float fixed_radius2 = 1.9;
float min_radius2 = 0.1;
float folding_limit = 1.0;
float scale = -2.8;

float minEpsilon = .00001;
float scaleEpsilon = .001;

void sphere_fold(inout vec3 z, inout float dz) {
    float r2 = dot(z, z);
    if (r2 < min_radius2) {
        float temp = (fixed_radius2 / min_radius2);
        z *= temp;
        dz *= temp;
    }else if (r2 < fixed_radius2) {
        float temp = (fixed_radius2 / r2);
        z *= temp;
        dz *= temp;
    }
}

vec3 Colour(vec3 pos, float sphereR) 
{
	vec3 p = pos;
	vec3 p0 = p;
	float trap = 1.0;
    
	for (int i = 0; i < 6; i++)
	{
        
		p.xyz = clamp(p.xyz, -1.0, 1.0) * 2.0 - p.xyz;
		float r2 = dot(p.xyz, p.xyz);
		p *= clamp(max(minRad2/r2, minRad2), 0.0, 1.0);

		p = p*scale.xyz + p0.xyz;
		trap = min(trap, r2);
	}
	// |c.x|: log final distance (fractional iteration count)
	// |c.y|: spherical orbit trap at (0,0,0)
	vec2 c = clamp(vec2( 0.3333*log(dot(p,p))-1.0, sqrt(trap) ), 0.0, 1.0);

    float t = mod(length(pos) - gTime*150., 16.0);
    surfaceColour1 = mix( surfaceColour1, vec3(.4, 3.0, 5.), pow(smoothstep(0.0, .3, t) * smoothstep(0.6, .3, t), 10.0));
	return mix(mix(surfaceColour1, surfaceColour2, c.y), surfaceColour3, c.x);
}

void box_fold(inout vec3 z, inout float dz) {
    z = clamp(z, - folding_limit, folding_limit) * 2.0 - z;
}

float mb(vec3 z) {
    vec3 offset = z;
    float dr = 1.0;
    for(int n = 0; n < 10; ++ n) {
        box_fold(z, dr);
        sphere_fold(z, dr);
        
        z = scale * z + offset;
        dr = dr * abs(scale) + 1.0;
        //scale = -2.8 - 0.2 * stime;
    }
    float r = length(z);
    return r / abs(dr);
}

vec2 map(in vec3 rayP)
{
    return vec2(mb(rayP), 3);
}

vec3 intersect(in vec3 ro, in vec3 rd, in float maxdist)
{
    float dist = 0.0;
    for(float i = 0.0; i < 200.0; i ++ )
    {
        vec3 rayP = ro + dist * rd;
        vec2 mapData = map(rayP);
        if (mapData.x < (scaleEpsilon * dist + minEpsilon)|| dist > maxdist) {
            return vec3(dist, i, mapData.y);
        }
        dist += mapData.x;
    }
    return vec3(dist, 200, - 1);
}

vec3 colorRamp(float c)
{
    float f = 2.0 * c - 1.0;
    if (c < 0.5) {
        f = 2.0 * c;
        return (1.0 - f) * vec3(0.843, 0.098, 0.1098) + f * vec3(0.996, 0.988, 0.737);
    }
    return (1.0 - f) * vec3(0.996, 0.988, 0.737) + f * vec3(0.192, 0.529, 0.721);
}

//not mine------
float softshadow(vec3 ro, vec3 rd, float k) {
    float akuma = 1.0, h = 0.0;
    float t = 0.01;
    for(int i = 0; i < 50; ++ i) {
        h = map(ro + rd * t).x;
        if (h < 0.001)return 0.02;
        akuma = min(akuma, k * h / t);
        t += clamp(h, 0.01, 2.0);
    }
    return akuma;
}
//---------------

vec3 calcNormal(vec3 p, float e) {
    return normalize(vec3(
            map(vec3(p.x + e, p.y, p.z)).x - map(vec3(p.x - e, p.y, p.z)).x,
            map(vec3(p.x, p.y + e, p.z)).x - map(vec3(p.x, p.y - e, p.z)).x,
            map(vec3(p.x, p.y, p.z + e)).x - map(vec3(p.x, p.y, p.z - e)).x
        ));
    }
    
    vec3 render(in vec3 ro, in vec3 rd, bool effect)
    {
        vec3 light = vec3(1, - 1, 1); //point at player for backlit effect
        vec3 rayData = intersect(ro, rd, 1024.0);
        float dist = rayData.x;
        
        vec3 skyColor = vec3(0.91, 0.91, 0.76);
        vec3 solidColor = pow(colorRamp(rayData.z / 15.0), vec3(1.5));
        if (rayData.x > 1024.0 || rayData.z < 0.0) {
            return skyColor;
        }
        
        vec3 normal = calcNormal(ro + dist * rd, scaleEpsilon * dist + minEpsilon);
        
        float distOcclusion = 1.0 - rayData.y * 2.0 / 400.0; //hacky occlusion, more occlusion means higher number
        float diffuseLighting = 1.0 - clamp(dot(light, normal), 0.0, 1.0);
        
        float shadow = softshadow(ro + dist * rd, - light, 10.0);
        float combinedShading = diffuseLighting * 0.3 + distOcclusion * 0.2 + 0.3 * shadow + 0.2;
        return solidColor * combinedShading;
    }
    
    void main(void)
    {
        vec2 screenPos =- 1.0 + 2.0 * gl_FragCoord.xy / resolution.xy; // screenPos can range from -1 to 1
        screenPos.x *= resolution.x / resolution.y;
        
        vec3 ro = playerPos;
        vec3 rd = normalize(playerFwd + playerRight * screenPos.x + playerUp *- screenPos.y);
        vec3 col = render(ro, rd, screenPos.x > 0.0);
        gl_FragColor = vec4(col, 1.0);
    }