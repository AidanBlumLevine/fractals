#ifdef GL_ES
precision highp float;
#endif

uniform vec3 playerPos;
uniform vec3 playerFwd;
uniform vec3 playerRight;
uniform vec3 playerUp;
uniform vec2 resolution;

uniform float fixed_radius;
uniform float min_radius;
uniform float folding_limit;
uniform float scale;
uniform int shadow_count;
uniform float detail;

float scaleEpsilon=.001;

void sphere_fold(inout vec3 z,inout float dz){
    float r2=dot(z,z);
    if(r2<min_radius){
        float temp=(fixed_radius/min_radius);
        z*=temp;
        dz*=temp;
    }else if(r2<fixed_radius){
        float temp=(fixed_radius/r2);
        z*=temp;
        dz*=temp;
    }
}

// vec3 color(vec3 pos,float sphereR)
// {
    //     vec3 surfaceColour1=vec3(.8,.0,0.);
    //     vec3 surfaceColour2=vec3(.4,.4,.5);
    //     vec3 surfaceColour3=vec3(.5,.3,0.);
    //     vec3 p=pos;
    //     vec3 p0=p;
    //     float trap=1.;
    
    //     for(int i=0;i<6;i++)
    //     {
        //         p.xyz=clamp(p.xyz,-1.,1.)*2.-p.xyz;
        //         float r2=dot(p.xyz,p.xyz);
        //         p*=clamp(max(.25/r2,.25),0.,1.);
        //         p=p*vec3(scale)+p0.xyz;
        //         trap=min(trap,r2);
    //     }
    //     // |c.x|: log final distance (fractional iteration count)
    //     // |c.y|: spherical orbit trap at (0,0,0)
    //     vec2 c=clamp(vec2(.3333*log(dot(p,p))-1.,sqrt(trap)),0.,1.);
    
    //     float t=mod(length(pos),16.);
    //     surfaceColour1=mix(surfaceColour1,vec3(.4,3.,5.),pow(smoothstep(0.,.3,t)*smoothstep(.6,.3,t),10.));
    //     return mix(mix(surfaceColour1,surfaceColour2,c.y),surfaceColour3,c.x);
// }

void box_fold(inout vec3 z,inout float dz){
    z=clamp(z,-folding_limit,folding_limit)*2.-z;
}

vec2 mb(vec3 z){
    vec3 offset=z;
    float dr=1.;
    float minDist = 10000.;
    for(int n=0;n<10;++n){
        box_fold(z,dr);
        sphere_fold(z,dr);
        
        z=scale*z+offset;
        dr=dr*abs(scale)+1.;
        minDist = min(minDist, length(z));
        //scale = -2.8 - 0.2 * stime;
    }
    float r=length(z);
    return vec2(r/abs(dr),minDist);
}

vec2 map(in vec3 rayP)
{
    return mb(rayP);
}

vec3 intersect(in vec3 ro,in vec3 rd,in float maxdist)
{
    float dist=0.;
    for(int i=0;i<100;i++)
    {
        vec3 rayP=ro+dist*rd;
        vec2 mapData=map(rayP);
        if(mapData.x<(scaleEpsilon*dist+detail)||dist>maxdist){
            return vec3(dist,i,mapData.y);
        }
        dist+=mapData.x;
    }
    return vec3(dist,100,-1);
}

vec3 colorRamp(float c)
{
    float f=2.*c-1.;
    if(c<.5){
        f=2.*c;
        return(1.-f)*vec3(.843,.098,.1098)+f*vec3(.996,.988,.737);
    }
    return(1.-f)*vec3(.996,.988,.737)+f*vec3(.192,.529,.721);
}

//not mine------
float softshadow(vec3 ro,vec3 rd,float k){
    float akuma=1.,h=0.;
    float t=.01;
    for(int i=0;i<50;++i){
        if(i>=shadow_count){
            return akuma;
        }
        h=map(ro+rd*t).x;
        if(h<.001)return.02;
        akuma=min(akuma,k*h/t);
        t+=clamp(h,.01,2.);
    }
    return akuma;
}
//---------------

vec3 calcNormal(vec3 p,float e){
    return normalize(vec3(
            map(vec3(p.x+e,p.y,p.z)).x-map(vec3(p.x-e,p.y,p.z)).x,
            map(vec3(p.x,p.y+e,p.z)).x-map(vec3(p.x,p.y-e,p.z)).x,
            map(vec3(p.x,p.y,p.z+e)).x-map(vec3(p.x,p.y,p.z-e)).x
        ));
    }
    
    vec3 render(in vec3 ro,in vec3 rd,bool effect)
    {
        vec3 light=vec3(1,-1,1);//point at player for backlit effect
        vec3 rayData=intersect(ro,rd,1024.);
        float dist=rayData.x;
        
        vec3 skyColor=vec3(.91,.91,.76);
        vec3 solidColor=pow(colorRamp(rayData.z/5.),vec3(1.5));
        //vec3 solidColor=color(ro+dist*rd,dist);
        if(rayData.x>1024.||rayData.z<0.){
            return skyColor;
        }
        
        vec3 normal=calcNormal(ro+dist*rd,scaleEpsilon*dist+detail);
        
        float distOcclusion=1.-rayData.y*2./200.;//hacky occlusion, more occlusion means higher number
        float diffuseLighting=1.-clamp(dot(light,normal),0.,1.);
        
        float shadow=softshadow(ro+dist*rd,-light,10.);
        float combinedShading=diffuseLighting*.3+distOcclusion*.2+.3*shadow+.2;
        return solidColor*combinedShading;
    }
    
    void main(void)
    {
        vec2 screenPos=-1.+2.*gl_FragCoord.xy/resolution.xy;// screenPos can range from -1 to 1
        screenPos.x*=resolution.x/resolution.y;
        
        vec3 ro=playerPos;
        vec3 rd=normalize(playerFwd+playerRight*screenPos.x+playerUp*-screenPos.y);
        vec3 col=render(ro,rd,screenPos.x>0.);
        gl_FragColor=vec4(col,1.);
    }