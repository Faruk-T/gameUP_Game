using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using GameUp.Gameplay;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class OpeningEffectsAuthoring
    {
        private static Material ParticleMaterial(string name,bool additive)
        {
            string path=CoastalAssetSetup.Root+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("GameUP/COP31/ParticleGlow"));AssetDatabase.CreateAsset(material,path);}
            material.shader=Shader.Find("GameUP/COP31/ParticleGlow");material.SetFloat("_DstBlend",additive?1:10);material.renderQueue=3000;
            EditorUtility.SetDirty(material);return material;
        }
        internal static ParticleSystem Wake(Transform parent)
        {
            var go=new GameObject("SwimmingWake",typeof(ParticleSystem));go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,.3f,-.35f);
            var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.duration=2;main.startLifetime=1.3f;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.2f,.35f);
            main.startColor=new Color(.9f,1,1,.35f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=16;
            var emission=ps.emission;emission.rateOverTime=5;emission.enabled=false;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=.13f;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,2));
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.4f,0),new GradientAlphaKey(0,1)});color.color=gradient;
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.HorizontalBillboard;renderer.sharedMaterial=ParticleMaterial("TurtleWake",false);
            return ps;
        }
        internal static void Create(TimelineAsset timeline,PlayableDirector director)
        {
            var root=new GameObject("COP31RisingLogo",typeof(Animator),typeof(CanvasGroup));
            var canvas=new GameObject("OfficialLogo",typeof(RectTransform),typeof(Canvas)).GetComponent<RectTransform>();canvas.SetParent(root.transform,false);
            canvas.localScale=Vector3.one*(38f/1200);canvas.sizeDelta=new Vector2(1200,675);canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var image=Image("OriginalCOP31Artwork",canvas,0,0,1200,675,Color.white,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Menu/COP31Horizontal.png"));image.preserveAspect=true;
            var clip=new AnimationClip{name="COP31 rises above the Mediterranean",frameRate=30};
            clip.SetCurve("",typeof(Transform),"localPosition.x",AnimationCurve.Constant(0,OpeningTiming.End,-8));
            clip.SetCurve("",typeof(Transform),"localPosition.z",AnimationCurve.Constant(0,OpeningTiming.End,50));
            clip.SetCurve("",typeof(Transform),"localPosition.y",new AnimationCurve(new Keyframe(0,-9),new Keyframe(OpeningTiming.LogoStart,-9),new Keyframe(OpeningTiming.LogoRaised,12),new Keyframe(OpeningTiming.End,12)));
            clip.SetCurve("",typeof(CanvasGroup),"m_Alpha",new AnimationCurve(new Keyframe(0,0),new Keyframe(OpeningTiming.LogoStart,0),new Keyframe(OpeningTiming.LogoStart+1,1),new Keyframe(OpeningTiming.FireworksEnd,1),new Keyframe(OpeningTiming.WalkStart,0),new Keyframe(OpeningTiming.End,0)));
            string path="Assets/Data/Cinematic/RisingLogo.anim";var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(saved){EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);}else{AssetDatabase.CreateAsset(clip,path);saved=clip;}
            var track=timeline.CreateTrack<AnimationTrack>(null,"COP31 / logo rise after swimming");track.trackOffset=TrackOffset.ApplyTransformOffsets;
            var item=track.CreateClip<AnimationPlayableAsset>();item.start=0;item.duration=OpeningTiming.End;((AnimationPlayableAsset)item.asset).clip=saved;((AnimationPlayableAsset)item.asset).removeStartOffset=false;
            director.SetGenericBinding(track,root.GetComponent<Animator>());root.transform.position=new Vector3(-8,-9,50);root.GetComponent<CanvasGroup>().alpha=0;
            var colors=new[]{new Color(.1f,.7f,1),new Color(.2f,1,.55f),new Color(1,.25f,.18f),new Color(1,.8f,.2f),new Color(.4f,.8f,1)};
            for(int i=0;i<5;i++)
            {
                var go=new GameObject("COP31Firework_"+i,typeof(ParticleSystem));go.transform.position=new Vector3(-18+i*5,16+i%2*4,55+i%2*4);
                var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(311+i);
                var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=2.8f;main.startLifetime=new ParticleSystem.MinMaxCurve(1.6f,2.4f);main.startSpeed=new ParticleSystem.MinMaxCurve(2.5f,4.5f);
                main.startSize=new ParticleSystem.MinMaxCurve(.14f,.22f);main.startColor=colors[i]*3;main.gravityModifier=.16f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=100;
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.06f;
                var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,80)});
                var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(colors[i],.6f)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
                var trails=ps.trails;trails.enabled=true;trails.ratio=.7f;trails.lifetime=.26f;trails.worldSpace=true;trails.inheritParticleColor=true;
                var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=.2f;renderer.velocityScale=.04f;
                renderer.sharedMaterial=ParticleMaterial("COP31Firework",true);renderer.trailMaterial=renderer.sharedMaterial;
                var controlTrack=timeline.CreateTrack<ControlTrack>(null,"Fireworks / "+i);var controlClip=controlTrack.CreateClip<ControlPlayableAsset>();controlClip.start=OpeningTiming.LogoRaised+i*.9f;controlClip.duration=2.8;
                var control=(ControlPlayableAsset)controlClip.asset;control.active=true;control.postPlayback=ActivationControlPlayable.PostPlaybackState.Inactive;control.updateParticle=true;control.updateDirector=false;control.updateITimeControl=false;control.particleRandomSeed=(uint)(311+i);
                var key=new PropertyName("COP31Firework_"+i);control.sourceGameObject=new ExposedReference<GameObject>{exposedName=key};director.SetReferenceValue(key,go);
                go.SetActive(false);
            }
        }
    }
}
