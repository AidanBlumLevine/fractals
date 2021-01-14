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

void box_fold(inout vec3 z,inout float dz){
    z=clamp(z,-folding_limit,folding_limit)*2.-z;
}

vec2 mb(vec3 z){
    vec3 offset=z;
    float dr=1.;
    int surface = 0;
    float oldDist = dot(z,z);
    for(int n=0;n<10;++n){
        box_fold(z,dr);
        sphere_fold(z,dr);

        if(oldDist >= dot(z,z)){
            surface = n;
        }
        
        z=scale*z+offset;
        dr=dr*abs(scale)+1.;
    }
    float r=length(z);
    return vec2(r/abs(dr),surface);
}

vec3 HsvToRgb (vec3 c)
{
  return c.z * mix (vec3 (1.), clamp (abs (fract (c.xxx + vec3 (1., 2./3., 1./3.)) * 6. - 3.) - 1., 0., 1.), c.y);
}

vec4 ObjCol (vec3 p)
{
  vec3 p3, col;
  float pp, ppMin, cn, s;
  p = mod (p + 3., 6.) - 3.;
  p3 = p;
  cn = 0.;
  ppMin = 1.;
  for (float j = 0.; j < 10.; j ++) {
    p3 = 2. * clamp (p3, -1., 1.) - p3;
    pp = dot (p3, p3);
    if (pp < ppMin) {
      cn = j;
      ppMin = pp;
    }
    p3 = 2.8 * p3 / clamp (pp, 0.25, 1.) + p;
  }
  s = mod (cn, 2.);
  col = HsvToRgb (vec3 (mod (0.6 + 1.5 * cn / 10., 1.), mix (0.6, 0., s), 1.));
  return vec4 (col, 0.05 + 0.4 * s);
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
        vec3 solidColor=pow(colorRamp(rayData.z/10.),vec3(1.5));
        //vec3 solidColor=color(ro+dist*rd,dist);
       // vec3 solidColor = ObjCol(ro + dist*rd).xyz;
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