#ifdef GL_ES
precision highp float;
#endif

uniform vec2 resolution;

float sphere(vec3 pos,float radius,vec3 translation)
{
    return length(translation+pos)-radius;
}

float box(vec3 pos,vec3 size,vec3 translation)
{
    return length(max(abs(translation+pos)-size,0.));
}

//http://www.fractalforums.com/ifs-iterated-function-systems/revenge-of-the-half-eaten-menger-sponge/15/
float mengersponge_de(vec3 pos){//by recursively digging a box
    float x=pos.x,y=pos.y,z=pos.z;
    x=x*.5+.5;y=y*.5+.5;z=z*.5+.5;//center it by changing position and scale
    
    float xx=abs(x-.5)-.5,yy=abs(y-.5)-.5,zz=abs(z-.5)-.5;
    float d1=max(xx,max(yy,zz));//distance to the box
    float d=d1;//current computed distance
    float p=1.;
    for(int i=1;i<=10;++i){
        float xa=mod(3.*x*p,3.);
        float ya=mod(3.*y*p,3.);
        float za=mod(3.*z*p,3.);
        p*=3.;
        
        //we can also translate/rotate (xa,ya,za) without affecting the DE estimate
        
        float xx=.5-abs(xa-1.5),yy=.5-abs(ya-1.5),zz=.5-abs(za-1.5);
        d1=min(max(xx,zz),min(max(xx,yy),max(yy,zz)))/p;//distance inside the 3 axis-aligned square tubes
        
        d=max(d,d1);//intersection
    }
    //return d*2.0; //the distance estimate. The *2 is because of the scaling we did at the beginning of the function
    return d;
}

float distfunc(vec3 pos)
{
    //eturn max(-sphere(pos, 1, vec3()),box(pos,vec3(1),vec3(1)));
    return max(-sphere(pos,1.3,vec3(-1)),mengersponge_de(pos));
    //return max(-box(pos,vec3(10),vec3(0)),mengersponge_de(3,pos));
}

void main()
{
    vec3 cameraOrigin=vec3(2.2,0.,0.);
    vec3 cameraTarget=vec3(0.,0.,0.);
    vec3 upDirection=vec3(0.,1.,0.);
    vec3 cameraDir=normalize(cameraTarget-cameraOrigin);
    vec3 cameraRight=normalize(cross(upDirection,cameraOrigin));
    vec3 cameraUp=cross(cameraDir,cameraRight);
    
    vec2 screenPos=-1.+2.*gl_FragCoord.xy/resolution.xy;// screenPos can range from -1 to 1
    screenPos.x*=resolution.x/resolution.y;// Correct aspect ratio
    vec3 rayDir=normalize(cameraRight*screenPos.x+cameraUp*screenPos.y+cameraDir);
    
    const int MAX_ITER=100;// 100 is a safe number to use, it won't produce too many artifacts and still be quite fast
    const float MAX_DIST=20.;// Make sure you change this if you have objects farther than 20 units away from the camera
    const float EPSILON=.0001;// At this distance we are close enough to the object that we have essentially hit it
    
    float totalDist=0.;
    vec3 pos=cameraOrigin;
    float dist=EPSILON;
    
    for(int i=0;i<MAX_ITER;i++)
    {
        // Either we've hit the object or hit nothing at all, either way we should break out of the loop
        if(dist<EPSILON||totalDist>MAX_DIST){
            if(dist<EPSILON)
            {
                vec2 eps=vec2(0.,EPSILON);
                vec3 normal=normalize(vec3(distfunc(pos+eps.yxx)-distfunc(pos-eps.yxx),distfunc(pos+eps.xyx)-distfunc(pos-eps.xyx),distfunc(pos+eps.xxy)-distfunc(pos-eps.xxy)));
                
                float diffuse=max(0.,dot(-rayDir,normal));
                float specular=pow(diffuse,32.);
                vec3 color=vec3(diffuse+specular);
                
                gl_FragColor=vec4(color,1.);
                break;
            }
            else
            {
                gl_FragColor=vec4(0);
                break;
            }
        }
        
        dist=distfunc(pos);// Evalulate the distance at the current point
        totalDist+=dist;
        pos+=dist*rayDir;// Advance the point forwards in the ray direction by the distance
    }
    
}