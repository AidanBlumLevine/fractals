#ifdef GL_ES
precision highp float;
#endif

uniform vec3 playerPos;
uniform vec3 playerFwd;
uniform vec3 playerRight;
uniform vec3 playerUp;
uniform vec2 resolution;

float sdBox(vec3 p, vec3 b)
{
	vec3 q = abs(p) - b;
	return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

vec2 DE(vec3 p)
{
	float d = sdBox(p, vec3(1.0));
	int cuts = 0;
	float s = 2.67;
	for(int m = 0; m < 15; m ++ )
	{
		vec3 a = mod(p * s, 2.0) - 1.0;
		s *= 3.0;
		vec3 r = abs(1.0 - 3.0 * abs(a));
		
		float da = max(r.x, r.y);
		float db = max(r.y, r.z);
		float dc = max(r.z, r.x);
		float c = (min(da, min(db, dc)) - 1.0) / s;
		
		if (c > d + 0.0000001) {
			d = c;
			cuts = m;
		}
	}
	return vec2(d, cuts);
}

vec2 map(in vec3 rayP)
{
	float Scale = 300.0;
	vec2 distanceData = DE(rayP / Scale);
	distanceData.x *= Scale;
	return distanceData;
}

vec3 intersect(in vec3 ro, in vec3 rd, in float tmax)
{
	float t = 0.0;
	for(float i = 0.0; i < 128.0; i ++ )
	{
		vec3 rayP = ro + t * rd;
		vec2 mapData = map(rayP);
		if (mapData.x < (0.0001 * t)|| t > tmax) {
			return vec3(t, i, mapData.y);
		}
		t += mapData.x;
	}
	return vec3(t, 128, - 1);
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

vec3 calcNormal(vec3 p, float e) {
	return normalize(vec3(
			map(vec3(p.x + e, p.y, p.z)).x - map(vec3(p.x - e, p.y, p.z)).x,
			map(vec3(p.x, p.y + e, p.z)).x - map(vec3(p.x, p.y - e, p.z)).x,
			map(vec3(p.x, p.y, p.z + e)).x - map(vec3(p.x, p.y, p.z - e)).x
		));
	}
	
	vec3 render(in vec3 ro, in vec3 rd, bool effect)
	{
		vec3 light =- normalize(rd); //point at player for backlit effect
		vec3 rayData = intersect(ro, rd, 1024.0);
		float dist = rayData.x;
		
		// vec3 skyColor = vec3(0.91, 0.91, 0.76);
		// vec3 solidColor = pow(colorRamp(rayData.z / 15.0), vec3(1.5));
		// if (rayData.z < 0.0) {
		// 	solidColor = skyColor;
		// }
		
		// vec3 normal = calcNormal(ro + dist * rd, 0.0001 * dist);
		
		// float distOcclusion = rayData.y * 2.0 / 256.0; //hacky occlusion, more occlusion means higher number
		// vec3 reflectDir = reflect(-light, normal);
		// float specular = pow(max(dot(playerFwd, reflectDir), 0.0), 32.0);
		// float diffuseLighting = 1.0 - clamp(dot(light, normal), 0.0, 1.0);
		
		// float combinedShading = diffuseLighting * 0.4 + distOcclusion * 0.2 + specular * 0.15 + 0.3;
		// float fogStrength = 1.0 - exp(-dist * 0.01);
		
		// return solidColor * combinedShading * (1.0 - fogStrength) + skyColor * fogStrength;
		
		// float colorInvert = 1.0; // can be turned to zero, dont forget to switch minus signs then!
		
						
		//colors
		vec3 skyColor = vec3(0.1); //vec3(0.56, 0.74, 0.53)*0.75;
		vec3 solidColor = 1.0-pow(colorRamp(rayData.z / 15.0), vec3(1.5));
		vec3 col;
		if (rayData.z < 0.0) {
		 	solidColor = skyColor;
		}
		if (dist > 1024.0)
		{
			col = skyColor;
		}
		else
		{
			vec3 normal = calcNormal(ro + dist*rd,0.0001 * dist);
			
			float distOcclusion = 1.0 - rayData.y * 2.0 / 256.0;
			float diffLighting = clamp(dot(light, normal), 0.0, 1.0);
			
			//inverted specular lighting, darkens the reflections, which lightens after inversion
			float specLighting = 1.0 - pow(clamp(dot(normal, normalize(light - rd)), 0.0, 1.0), 32.0);
			
			float combinedShading = diffLighting * 0.35 + distOcclusion * 0.4 + specLighting * 0.15 + 0.1;
			
			col = solidColor * combinedShading;
			
			//apply fog
			float fogStrength = exp(-dist * 0.01);
			col = skyColor * (1.0 - fogStrength) + col * fogStrength;
			
		}
		
		//inverting colors and contrast enhancing
		col = vec3(1.0) - col;
		col = pow(col,vec3(1.5));
		
		return col;
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