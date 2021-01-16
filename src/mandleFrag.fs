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
uniform int recursions;

const float scaleEpsilon=.001;
const int steps=100;

float sdBox(vec3 p,vec3 b)
{
    vec3 q=abs(p)-b;
    return length(max(q,0.))+min(max(q.x,max(q.y,q.z)),0.);
}

void sphere_fold(inout vec4 z){
    z*=fixed_radius/clamp(dot(z.xyz,z.xyz),min_radius,fixed_radius);
}

void box_fold(inout vec4 z){
    z.xyz=clamp(z.xyz,-folding_limit,folding_limit)*2.-z.xyz;
}

void shiftXY(inout vec4 z,float angle,float radius){
    float c=cos(angle);
    float s=sin(angle);
    z=vec4(vec2(c,s)*radius+z.xy,z.zw);
}
void invertRadius(inout vec4 z,float radius2,float limit){
    float r2=dot(z.xyz,z.xyz);
    float f=clamp(radius2/r2,1.,limit);
    z*=f;
}
void rotateXZ(inout vec4 z,float angle){
    float c=cos(angle);
    float s=sin(angle);
    mat2 m=mat2(c,s,-s,c);
    vec2 r=m*z.xz;
    z=vec4(r.xyy,z.w);
}

float map(vec3 pos){
    vec4 z=vec4(pos,1.);
    for(int n=0;n<25;++n){
        if(n==recursions)break;
        box_fold(z);
        sphere_fold(z);
        z.xyz=scale*z.xyz+pos;
        z.w=z.w*abs(scale)+1.;
    }
    return sdBox(z.xyz,vec3(5))/abs(z.w);
}

vec2 intersect(in vec3 ro,in vec3 rd,in float maxdist)
{
    float dist=0.;
    for(int i=0;i<steps;i++)
    {
        vec3 rayP=ro+dist*rd;
        float mapDist=map(rayP);
        if(mapDist<(scaleEpsilon*dist+detail)||dist>maxdist){
            return vec2(dist,i);
        }
        dist+=mapDist;
    }
    return vec2(dist,steps);
}

//not mine------
float softshadow(vec3 ro,vec3 rd,float k){
    float akuma=1.,h=0.;
    float t=.001;
    for(int i=0;i<50;++i){
        if(i>=shadow_count){
            return akuma;
        }
        h=map(ro+rd*t);
        if(h<.0001)return.02;
        akuma=min(akuma,k*h/t);
        t+=h;
    }
    return akuma;
}
//---------------

vec3 calcNormal(vec3 p,float e){
    return normalize(vec3(map(vec3(p.x+e,p.y,p.z))-map(vec3(p.x-e,p.y,p.z)),map(vec3(p.x,p.y+e,p.z))-map(vec3(p.x,p.y-e,p.z)),map(vec3(p.x,p.y,p.z+e))-map(vec3(p.x,p.y,p.z-e))));
}

void sphere_fold_c(inout vec4 z,inout vec3 color){
    float r2=dot(z.xyz,z.xyz);
    color+=vec3(0,-.25,.15);
    if(r2<min_radius){
        z*=(fixed_radius/min_radius);
        color+=vec3(0,2.25,1.75);
    }else if(r2<fixed_radius){
        z*=(fixed_radius/r2);
        color+=vec3(.45,.6,2.2);
    }
}

void box_fold_c(inout vec4 z,inout vec3 color){
    vec3 pos=z.xyz;
    z.xyz=clamp(z.xyz,-folding_limit,folding_limit);
    color.x+=pos==z.xyz?float(recursions)/1.5:0.;
    z.xyz=z.xyz*2.-pos;
}

vec3 map_color(vec3 pos){
    vec3 surface=vec3(0,float(recursions)/2.,0);
    vec4 z=vec4(pos,1.);
    for(int n=0;n<25;++n){
        if(n==recursions)break;
        box_fold_c(z,surface);
        sphere_fold_c(z,surface);
        z.xyz=scale*z.xyz+pos;
        z.w=z.w*abs(scale)+1.;
    }
    surface/=float(recursions);
    //surface = sqrt(surface);
    return surface;
}

vec3 hsv2rgb(vec3 c){
    vec4 K=vec4(1.,2./3.,1./3.,3.);
    vec3 p=abs(fract(c.xxx+K.xyz)*6.-K.www);
    return c.z*mix(K.xxx,clamp(p-K.xxx,0.,1.),c.y);
}

vec3 orbit_trap(vec3 pos){
    float orbit=1000.;
    vec4 z=vec4(pos,1.);
    for(int n=0;n<25;++n){
        if(n==recursions)break;
        box_fold(z);
        sphere_fold(z);
        z.xyz=scale*z.xyz+pos;
        z.w=z.w*abs(scale)+1.;
        orbit=min(orbit,abs(length(z.xyz)/pow(scale,float(n))));
    }
    //orbit = floor(orbit);
    
    return hsv2rgb(vec3(orbit*10.,.5,.5));
}

vec3 render(in vec3 ro,in vec3 rd,bool effect)
{
    vec3 light=vec3(4,-2,1);
    vec3 lightColor=vec3(.5,.34,.26);
    
    vec2 rayData=intersect(ro,rd,1024.);
    float dist=rayData.x;
    
    vec3 color=map_color(ro+rd*dist);
    
    if(dist>1024.||int(rayData.y)==steps){
        return vec3(.91,.91,.76)+vec3(smoothstep(.98,1.,dot(rd,-normalize(light))));
    }
    
    vec3 normal=calcNormal(ro+dist*rd,scaleEpsilon*dist+detail);
    float specular=pow(dot(-normalize(rd),reflect(normalize(light),normal)),32.);
    
    float distOcclusion=1.-rayData.y/float(steps);//hacky occlusion, more occlusion means higher number
    float diffuseLighting=1.-clamp(dot(light,normal),0.,1.);
    
    float shadow=softshadow(ro+dist*rd,-light,10.);
    specular*=shadow;
    
    return color*(diffuseLighting*.3+distOcclusion*.2+.3*shadow)+vec3(.1+.5*clamp(specular,0.,1.));
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