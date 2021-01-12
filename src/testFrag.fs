#ifdef GL_ES
precision highp float;
#endif

uniform vec2 resolution;
uniform mat4 camera_rotation;
uniform float zoom, time;
float sphere(vec3 pos, float radius, vec3 translation)
{
    return length(translation + pos) - radius;
}

float box(vec3 pos, vec3 size, vec3 translation)
{
    vec3 q = abs(translation + pos) - size;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float sdCross(vec3 p)
{
    float inf = 100000.0;
    float da = box(p.xyz, vec3(inf, 0.3333, 0.3333), vec3(0));
    float db = box(p.yzx, vec3(0.3333, inf, 0.3333), vec3(0));
    float dc = box(p.zxy, vec3(0.3333, 0.3333, inf), vec3(0));
    return min(da, min(db, dc));
}

vec3 map(vec3 p)
{
    float d = box(p, vec3(1.0), vec3(0));
    
    float s = 1.0;
    for(int m = 0; m < 6; m ++ )
    {
        vec3 r = mod(p * s+1.0, 2.0) - 1.0;
        float c = sdCross(r) / s;
        s *= 2.0;
        d = max(d, - c);
    }
    
    return vec3(d, 0.0, 0.0);
}

//http://www.fractalforums.com/ifs-iterated-function-systems/revenge-of-the-half-eaten-menger-sponge/15/
float mengersponge_de(vec3 pos) {//by recursively digging a box
    float x = pos.x, y = pos.y, z = pos.z;
    x = x*0.5 + 0.5; y = y*0.5 + 0.5; z = z*0.5 + 0.5; //center it by changing position and scale
    
    float xx = abs(x - 0.5) - 0.5, yy = abs(y - 0.5) - 0.5, zz = abs(z - 0.5) - 0.5;
    float d1 = max(xx, max(yy, zz)); //distance to the box
    float d = d1; //current computed distance
    float p = 1.0;
    for(int i = 1; i <= 6; ++ i) {
        float xa = mod(3.0 * x*p, 3.0);
        float ya = mod(3.0 * y*p, 3.0);
        float za = mod(3.0 * z*p, 3.0);
        p *= 3.0;
        
        //we can also translate/rotate (xa,ya,za) without affecting the DE estimate
        
        float xx = 0.5 - abs(xa - 1.5), yy = 0.5 - abs(ya - 1.5), zz = 0.5 - abs(za - 1.5);
        d1 = min(max(xx, zz), min(max(xx, yy), max(yy, zz))) / p; //distance inside the 3 axis-aligned square tubes
        
        d = max(d, d1); //intersection
    }
    //return d*2.0; //the distance estimate. The *2 is because of the scaling we did at the beginning of the function
    return d;
}

float distfunc(vec3 pos)
{
    int to = int(floor(mod(time / 200.0 / 2.0 / 3.1415826, 3.0)));
    float v = time / 200.0 - 3.141592 / 2.0;
    //vec3 modPos = pos * pow((abs(pos)),vec3(sin(time/100.)));
    if (to == 0) {
        return mix(sphere(pos, 1.0, vec3(0)), map(pos).x, (sin(v) + 1.0) / 2.0);
    }
    if (to == 1) {
        return mix(sphere(pos, 1.0, vec3(0)), mengersponge_de(pos), (sin(v) + 1.0) / 2.0);
    }
    if (to == 2) {
        return mix(sphere(pos, 1.0, vec3(0)), min(max(sphere(pos, 1.0, vec3(0)), - box(pos, vec3(0.8), vec3(0))), sphere(pos, 0.8, vec3(0))), (sin(v) + 1.0) / 2.0);
    }
    //return box(modPos,vec3(1),vec3(0));
    //return ;
    //return max(-sphere(pos,1.3,vec3(-1)),mengersponge_de(pos));
    //return max(-box(pos,vec3(10),vec3(0)),mengersponge_de(3,pos));
}

float softshadow(vec3 shadowOrigin, vec3 shadowDirection)
{
    float res = 1.0;
    float t = 0.01;
    float ph = 1e20;
    for(int i = 0; i < 100; i ++ )
    {
        float dist = distfunc(shadowOrigin + shadowDirection * t);
        if (abs(dist) < 0.001) {
            return 0.0;
        }
        float y = dist * dist / (2.0 * ph);
        float d = sqrt(dist * dist - y*y);
        res = min(res, 20.0 * d /max(0.0, t - y));
        t += dist;
    }
    return res;
}

float ambientOcclusion(vec3 origin, vec3 direction, float distance)
{
    float ao = 0.0;
    for(float i = 0.0; i < 8.0; i ++ )
    {
        float len = i /8.0 * distance;
        vec3 ray = direction * len;
        ao += (len - max(distfunc(origin + ray), 0.0)) / distance * 0.5;
    }
    return clamp(1.0 - ao / 8.0, 0.0, 1.0);
}

vec3 getNormal(vec3 pos, float e)
{
    vec3 dx = vec3(e, 0, 0);
    vec3 dy = vec3(0, e, 0);
    vec3 dz = vec3(0, 0, e);
    
    float d = distfunc(pos);
    
    return normalize(vec3(
            d - distfunc(vec3(pos - dx)),
            d - distfunc(vec3(pos - dy)),
            d - distfunc(vec3(pos - dz))
        ));
    }
    
    void main()
    {
        vec3 lightPosition = vec3(0.0, - 3.0, 6.0);
        vec3 ambient = vec3(0.2, 0.3, 0.6);
        
        vec4 cameraOrigin = camera_rotation * vec4(zoom, 0.0, 0.0, 0.0);
        vec3 cameraTarget = vec3(0.0, 0.0, 0.0);
        vec3 upDirection = vec3(0.0, 1.0, 0.0);
        vec3 cameraDir = normalize(cameraTarget - cameraOrigin.xyz);
        vec3 cameraRight = normalize(cross(upDirection, cameraOrigin.xyz));
        vec3 cameraUp = cross(cameraDir, cameraRight);
        
        vec2 screenPos =- 1.0 + 2.0 * gl_FragCoord.xy / resolution.xy; // screenPos can range from -1 to 1
        screenPos.x *= resolution.x / resolution.y; // Correct aspect ratio
        vec3 rayDir = normalize(cameraRight * screenPos.x + cameraUp * screenPos.y + cameraDir);
        
        const int MAX_ITER = 300; // 100 is a safe number to use, it won't produce too many artifacts and still be quite fast
        const float MAX_DIST = 100.0; // Make sure you change this if you have objects farther than 20 units away from the camera
        const float EPSILON = 0.00001; // At this distance we are close enough to the object that we have essentially hit it
        
        float totalDist = 0.0;
        vec3 pos = cameraOrigin.xyz;
        float dist = EPSILON;
        
        for(int i = 0; i < MAX_ITER; i ++ )
        {
            if (dist < EPSILON||totalDist > MAX_DIST) {
                if (dist < EPSILON)
                {
                    vec2 eps = vec2(0.0, EPSILON);
                    vec3 normal = getNormal(pos, EPSILON);
                    vec3 lightDir = normalize(pos - lightPosition);
                    vec3 reflectDir = reflect(-lightDir, normal);
                    float diffuse = max(0.0, dot(-lightDir, normal));
                    float specular = pow(max(dot(cameraDir, reflectDir), 0.0), 32.0);
                    //float shadow = softshadow(pos, -lightDir);
                    // float edge = pow(ambientOcclusion(pos, normal,.005),3.);
                    float ao = ambientOcclusion(pos, normal, 0.1);
                    vec3 color = vec3(ao * (diffuse / 1.5 + ambient + specular)); // + shadow/2. - .5  //
                    
                    gl_FragColor = vec4(color, 1.0);
                    break;
                }
                else
                {
                    gl_FragColor = vec4(0, 0, 0, 0.5);
                    break;
                }
            }
            
            dist = distfunc(pos); // Evalulate the distance at the current point
            totalDist += dist;
            pos += dist * rayDir; // Advance the point forwards in the ray direction by the distance
        }
        
    }