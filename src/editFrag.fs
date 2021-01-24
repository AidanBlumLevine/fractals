#ifdef GL_ES
precision highp float;
#endif

uniform vec3 playerPos;
uniform vec3 playerFwd;
uniform vec3 playerRight;
uniform vec3 playerUp;
uniform vec2 resolution;

uniform int shadow_count;
uniform int render_count;
uniform float detail;

const float scaleEpsilon = 0.001;

float box(vec3 p, vec3 center, vec3 b)
{
    vec3 q = abs(p - center) - b;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float sphere(vec3 p, vec3 center, float s)
{
    return length(p - center) - s;
}

void sphere_fold(inout vec4 z, float min_radius, float fixed_radius) {
    z *= fixed_radius / clamp(dot(z.xyz, z.xyz), min_radius, fixed_radius);
}

void box_fold(inout vec4 z, float folding_limit) {
    z.xyz = clamp(z.xyz, - folding_limit, folding_limit) * 2.0 - z.xyz;
}

void shiftXY(inout vec4 z, float angle, float radius) {
    float c = cos(angle);
    float s = sin(angle);
    z = vec4(vec2(c, s) * radius + z.xy, z.zw);
}
void invertRadius(inout vec4 z, float radius2, float limit) {
    float r2 = dot(z.xyz, z.xyz);
    float f = clamp(radius2 / r2, 1.0, limit);
    z *= f;
}
void rotateZ(inout vec4 z, float angle) {
    float c = cos(angle);
    float s = sin(angle);
    vec4 rotated = z;
    rotated.x = z.x * c-z.y * s;
    rotated.y = z.y * c+z.x * s;
    z = rotated;
}
void rotateY(inout vec4 z, float angle) {
    float c = cos(angle);
    float s = sin(angle);
    vec4 rotated = z;
    rotated.x = z.x * c-z.z * s;
    rotated.z = z.z * c+z.x * s;
    z = rotated;
}
void rotateX(inout vec4 z, float angle) {
    float c = cos(angle);
    float s = sin(angle);
    vec4 rotated = z;
    rotated.z = z.z * c-z.y * s;
    rotated.y = z.y * c+z.z * s;
    z = rotated;
}

void rotate(inout vec4 z, vec3 angle) {
    rotateX(z, angle.x);
    rotateY(z, angle.y);
    rotateZ(z, angle.z);
}

void translate(inout vec4 z, vec3 transform) {
    z.xyz += transform;
}
void scale(inout vec4 z, float transform) {
    z *= transform;
}
void tetrahedral(inout vec4 z) {
    if (z.x + z.y < 0.0)z.xy =- z.yx; // fold 1
    if (z.x + z.z < 0.0)z.xz =- z.zx; // fold 2
    if (z.y + z.z < 0.0)z.zy =- z.yz; // fold 3
}

// void mendelbox(inout vec4 z,vec3 pos){
    //     //box_fold(z);
    //     //sphere_fold(z);
    //    // z.xyz=scale*z.xyz+pos;
    //     z.w=z.w*abs(scale)+1.;
// }

void mandel(inout vec4 z, vec3 pos, float scale) {
    z.xyz = scale * z.xyz + pos;
    z.w = z.w * abs(scale) + 1.0;
}

// void sharpbox(inout vec4 z){
    //     tetrahedral(z);
    //     //box_fold(z);
    //     rotateZ(z,10.);
    //     //z*=scale;
// }

void menger(inout vec4 z, vec3 c, float scale) {
    
    z = abs(z);
    if (z.x - z.y < 0.0) {z.xy = z.yx; }
    if (z.x - z.z < 0.0) {z.xz = z.zx; }
    if (z.y - z.z < 0.0) {z.yz = z.zy; }
    
    z.z -= 0.5 * c.z * (scale - 1.0) / scale;
    z.z =- abs(-z.z);
    
    z.z += 0.5 * c.z * (scale - 1.0) / scale;
    z *= scale;
    z.x -= c.x * (scale - 1.0);
    z.y -= c.y * (scale - 1.0);
}

// void menger_c(inout vec4 z,vec3 c,inout vec3 color){
    //     rotateX(z,3.14159/3.);
    //     z=abs(z);
    //     if(z.x-z.y<0.){z.xy=z.yx;}
    //     if(z.x-z.z<0.){z.xz=z.zx;}
    //     if(z.y-z.z<0.){z.yz=z.zy;}
    
    //     z.z-=.5*c.z*(scale-1.)/scale;
    //     float ozz=z.z;
    //     z.z=-abs(-z.z);
    //     if(ozz>z.z){
        //         color.x++;
    //     }
    //     rotateZ(z,3.14159/60.);
    //     rotateX(z,3.14159/30.);
    
    //     z.z+=.5*c.z*(scale-1.)/scale;
    //     z*=scale;
    //     z.x-=c.x*(scale-1.);
    //     z.y-=c.y*(scale-1.);
// }

float map(vec3 pos) {
    vec4 z = vec4(pos, 1.0);
    INSERTFRACTALHERE
}

vec2 intersect(in vec3 ro, in vec3 rd, in float maxdist)
{
    float dist = 0.0;
    for(int i = 0; i < 800; i ++ )
    {
        if (i == render_count)break;
        vec3 rayP = ro + dist * rd;
        float mapDist = map(rayP);
        if (mapDist < (scaleEpsilon * dist + detail)||dist > maxdist) {
            return vec2(dist, i);
        }
        dist += mapDist;
    }
    return vec2(dist, render_count);
}

//not mine------
float softshadow(vec3 ro, vec3 rd, float k) {
    float akuma = 1.0, h = 0.0;
    float t = 0.001;
    for(int i = 0; i < 50; ++ i) {
        if (i >= shadow_count) {
            return akuma;
        }
        h = map(ro + rd * t);
        if (h < 0.0001)return.02;
        akuma = min(akuma, k * h / t);
        t += h;
    }
    return akuma;
}
//---------------

vec3 calcNormal(vec3 p, float e) {
    return normalize(vec3(map(vec3(p.x + e, p.y, p.z)) - map(vec3(p.x - e, p.y, p.z)), map(vec3(p.x, p.y + e, p.z)) - map(vec3(p.x, p.y - e, p.z)), map(vec3(p.x, p.y, p.z + e)) - map(vec3(p.x, p.y, p.z - e))));
}

// void sphere_fold_c(inout vec4 z,inout vec3 color){
    //     float r2=dot(z.xyz,z.xyz);
    //     color+=vec3(0,-.25,.15);
    //     if(r2<min_radius){
        //         z*=(fixed_radius/min_radius);
        //         color+=vec3(0,2.25,1.75);
    //     }else if(r2<fixed_radius){
        //         z*=(fixed_radius/r2);
        //         color+=vec3(.45,.6,2.2);
    //     }
// }

// void box_fold_c(inout vec4 z,inout vec3 color){
    //     vec3 pos=z.xyz;
    //     z.xyz=clamp(z.xyz,-folding_limit,folding_limit);
    //     color.x+=pos==z.xyz?float(recursions)/1.5:0.;
    //     z.xyz=z.xyz*2.-pos;
// }

vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

vec3 orbit_trap(vec3 pos) {
    float orbit = 1000.0;
    vec4 z = vec4(pos, 1.0);
    for(int n = 0; n < 4; ++ n) {
        box_fold(z, 1.0);
        sphere_fold(z, 0.0, 1.0);
        z.xyz = -2.0 * z.xyz + pos;
        z.w = z.w * abs(-2.0) + 1.0;
        orbit = min(orbit, abs(length(z.xyz) / z.w));
    }
    return hsv2rgb(vec3(orbit * 10.0, 0.5, 0.5));
}

vec3 map_color(vec3 pos) {
    float orbit = 1000.0;
    vec4 z = vec4(pos, 1.0);
    INSERTCOLORHERE
    //return vec3(48.0 / 255.0, 204.0 / 255.0, 0.0);
}

vec3 render(in vec3 ro, in vec3 rd, bool effect)
{
    vec3 light = vec3(4, - 2, 1);
    vec3 lightColor = vec3(0.5, 0.34, 0.26);
    
    vec2 rayData = intersect(ro, rd, 1024.0);
    float dist = rayData.x;
    
    vec3 color = map_color(ro + rd * dist);
    
    if (dist > 1024.0||int(rayData.y) == render_count) {
        return vec3(0.91, 0.91, 0.76) + vec3(smoothstep(0.98, 1.0, dot(rd, - normalize(light))));
    }
    
    vec3 normal = calcNormal(ro + dist * rd, scaleEpsilon * dist + detail);
    float specular = pow(dot(-normalize(rd), reflect(normalize(light), normal)), 32.0);
    
    float distOcclusion = 1.0 - rayData.y / float(render_count); //hacky occlusion, more occlusion means higher number
    float diffuseLighting = 1.0 - clamp(dot(light, normal), 0.0, 1.0);
    
    float shadow = softshadow(ro + dist * rd, - light, 10.0);
    specular *= shadow;
    specular *= diffuseLighting;
    return color * (diffuseLighting * 0.3 + distOcclusion * 0.2 + 0.3 * shadow) + vec3(0.1 + 0.5 * clamp(specular, 0.0, 1.0));
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