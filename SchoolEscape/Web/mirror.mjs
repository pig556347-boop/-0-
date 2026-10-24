import * as T from './vendor/three.module.min.js';

// An orthographic planar mirror. A second camera renders the active scene,
// clipped at the mirror plane, into a projectively sampled texture. The general
// oblique projection works with the game's orthographic camera as well as a
// perspective camera. No room image or prerecorded character is substituted.
export class SchoolMirror extends T.Mesh {
 constructor(item,spaceId,readPose){
  const target=new T.WebGLRenderTarget(512,512,{depthBuffer:true});
  const textureMatrix=new T.Matrix4();
  super(new T.PlaneGeometry(item.width,item.height),new T.ShaderMaterial({
   uniforms:{reflection:{value:target.texture},textureMatrix:{value:textureMatrix}},
   vertexShader:`uniform mat4 textureMatrix;varying vec4 mirrorUv;
    void main(){mirrorUv=textureMatrix*vec4(position,1.0);gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.0);}`,
   fragmentShader:`uniform sampler2D reflection;varying vec4 mirrorUv;
    void main(){vec3 image=texture2DProj(reflection,mirrorUv).rgb;
     gl_FragColor=vec4(mix(image,vec3(.30,.39,.37),.055),1.0);
     #include <tonemapping_fragment>
     #include <colorspace_fragment>
    }`
  }));
  this.name='Live mirror · '+spaceId;this.userData.fixtureRoomId=spaceId;
  this.spaceId=spaceId;this.width=item.width;this.height=item.height;
  this.target=target;this.reflectionCamera=new T.OrthographicCamera();
  this.renderedFrames=0;this.signature='';this.drawing=false;
  this.onBeforeRender=(renderer,scene,source)=>{
   if(this.drawing)return;
   const pose=readPose(),normal=new T.Vector3(0,0,1).transformDirection(this.matrixWorld);
   const origin=new T.Vector3().setFromMatrixPosition(this.matrixWorld);
   const eye=new T.Vector3().setFromMatrixPosition(source.matrixWorld);
   if(eye.clone().sub(origin).dot(normal)<=0)return;
   const key=[...source.matrixWorld.elements,...source.projectionMatrix.elements,pose.actor.position.x,pose.actor.position.z,
    pose.angle,pose.speed>.01?pose.phase:0,pose.visual.visible?1:0,...(pose.extraSignature||[])].map(v=>Math.round(v*10000)).join(',');
   if(this.signature===key)return;
   const plane=new T.Plane().setFromNormalAndCoplanarPoint(normal,origin);
   const view=new T.Vector3();source.getWorldDirection(view);view.reflect(normal);
   const reflectedEye=eye.clone().addScaledVector(normal,-2*plane.distanceToPoint(eye));
   const virtual=this.reflectionCamera;
   virtual.position.copy(reflectedEye);virtual.up.set(0,1,0).applyQuaternion(source.quaternion).reflect(normal);
   virtual.lookAt(reflectedEye.clone().add(view));virtual.near=source.near;virtual.far=source.far;
   virtual.updateMatrixWorld();virtual.projectionMatrix.copy(source.projectionMatrix);
   textureMatrix.set(.5,0,0,.5,0,.5,0,.5,0,0,.5,.5,0,0,0,1)
    .multiply(virtual.projectionMatrix).multiply(virtual.matrixWorldInverse).multiply(this.matrixWorld);
   const clip=plane.clone().applyMatrix4(virtual.matrixWorldInverse);
   const c=new T.Vector4(clip.normal.x,clip.normal.y,clip.normal.z,clip.constant);
   const q=new T.Vector4(Math.sign(c.x),Math.sign(c.y),1,1)
    .applyMatrix4(virtual.projectionMatrix.clone().invert());
   c.multiplyScalar(2/c.dot(q));const p=virtual.projectionMatrix.elements;
   p[2]=c.x-p[3];p[6]=c.y-p[7];p[10]=c.z-p[11];p[14]=c.w-p[15];
   virtual.projectionMatrixInverse.copy(virtual.projectionMatrix).invert();
   const oldTarget=renderer.getRenderTarget(),oldShadow=renderer.shadowMap.autoUpdate,oldXr=renderer.xr.enabled;
   this.drawing=true;this.visible=false;
   try{
    renderer.xr.enabled=false;renderer.shadowMap.autoUpdate=false;renderer.setRenderTarget(target);
    renderer.render(scene,virtual);this.renderedFrames++;this.signature=key;
   }finally{
    renderer.setRenderTarget(oldTarget);renderer.shadowMap.autoUpdate=oldShadow;renderer.xr.enabled=oldXr;
    this.visible=true;this.drawing=false;
   }
  };
 }
 invalidate(){this.signature=''}
 inspect(actor){
  const origin=new T.Vector3().setFromMatrixPosition(this.matrixWorld);
  const normal=new T.Vector3(0,0,1).transformDirection(this.matrixWorld);
  const direction=new T.Vector3();this.reflectionCamera.getWorldDirection(direction);
  const denominator=direction.dot(normal);let inside=false,inView=false;
  for(const height of [.25,.65,1.05,1.45,1.65]){
   const point=actor.position.clone().add(new T.Vector3(0,height,0));
   const distance=point.clone().sub(origin).dot(normal);
   if(distance<=0||Math.abs(denominator)<1e-6)continue;
   const at=this.worldToLocal(point.clone().addScaledVector(direction,-distance/denominator));
   const projected=point.clone().project(this.reflectionCamera);
   if(Math.abs(at.x)<this.width/2&&Math.abs(at.y)<this.height/2){
    inside=true;
    if(Math.abs(projected.x)<1&&Math.abs(projected.y)<1&&Math.abs(projected.z)<=1)inView=true;
   }
  }
  return {roomId:this.spaceId,visible:this.visible,width:this.width,height:this.height,
   renderedFrames:this.renderedFrames,actorInsideSurface:inside,
   actorInReflection:this.renderedFrames>0&&actor.visible&&inView,
   textureWidth:this.target.width,textureHeight:this.target.height};
 }
 dispose(){this.target.dispose();this.material.dispose();this.geometry.dispose()}
}
