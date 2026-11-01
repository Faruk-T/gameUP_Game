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
        private static Material ParticleMaterial(string name,bool additive,float intensity=1)
        {
            string path=CoastalAssetSetup.Root+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("GameUP/COP31/ParticleGlow"));AssetDatabase.CreateAsset(material,path);}
            material.shader=Shader.Find("GameUP/COP31/ParticleGlow");material.SetFloat("_DstBlend",additive?1:10);material.renderQueue=3000;
            material.SetFloat("_Intensity",intensity);
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
        internal static void Create(TimelineAsset timeline,PlayableDirector director,Transform openingCanvas,Vector3 celebrationPosition,Vector3 celebrationLook)
        {
            // The original PNG has symmetric transparent padding. A centered overlay preserves
            // its aspect and visible center at every camera angle without a runtime billboard.
            var root=Rect("COP31RisingLogo",openingCanvas,0,-850,1800,1012.5f);
            root.gameObject.AddComponent<Animator>();root.gameObject.AddComponent<CanvasGroup>();
            var image=Image("OriginalCOP31Artwork",root,0,0,1800,1012.5f,Color.white,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Menu/COP31Horizontal.png"));image.preserveAspect=true;
            var clip=new AnimationClip{name="COP31 rises above the Mediterranean",frameRate=30};
            clip.SetCurve("",typeof(RectTransform),"m_AnchoredPosition.x",AnimationCurve.Constant(0,OpeningTiming.End,0));
            clip.SetCurve("",typeof(RectTransform),"m_AnchoredPosition.y",new AnimationCurve(new Keyframe(0,-850),new Keyframe(OpeningTiming.LogoStart,-850),new Keyframe(OpeningTiming.LogoRaised,65),new Keyframe(OpeningTiming.End,65)));
            clip.SetCurve("",typeof(CanvasGroup),"m_Alpha",new AnimationCurve(new Keyframe(0,0),new Keyframe(OpeningTiming.LogoStart,0),new Keyframe(OpeningTiming.LogoStart+1,1),new Keyframe(OpeningTiming.FireworksEnd,1),new Keyframe(OpeningTiming.WalkStart,0),new Keyframe(OpeningTiming.End,0)));
            var saved=CinematicAuthoring.SaveClip(clip,"Assets/Data/Cinematic/RisingLogo.anim");
            var track=timeline.CreateTrack<AnimationTrack>(null,"COP31 / logo rise after swimming");track.trackOffset=TrackOffset.ApplyTransformOffsets;
            var item=track.CreateClip<AnimationPlayableAsset>();item.start=0;item.duration=OpeningTiming.End;((AnimationPlayableAsset)item.asset).clip=saved;((AnimationPlayableAsset)item.asset).removeStartOffset=false;
            director.SetGenericBinding(track,root.GetComponent<Animator>());root.GetComponent<CanvasGroup>().alpha=0;
            var sky=new GameObject("COP31CelebrationSky").transform;
            sky.SetPositionAndRotation(celebrationPosition,Quaternion.LookRotation(celebrationLook-celebrationPosition));
            var colors=new[]{new Color(1,.04f,.12f),new Color(.08f,.35f,1),new Color(.04f,1,.3f),new Color(1,.65f,.02f),new Color(.72f,.04f,1),new Color(.02f,.95f,1),new Color(1,.12f,.55f),new Color(1,.9f,.28f)};
            var order=new[]{0,7,2,5,1,6,3,4};
            for(int i=0;i<24;i++)
            {
                int row=i/8,slot=i%8,column=order[slot];var tint=colors[(column+row*3)%colors.Length];
                var go=new GameObject("COP31Firework_"+i,typeof(ParticleSystem));go.transform.SetParent(sky,false);
                go.transform.localPosition=new Vector3(-18+column*5.15f+Mathf.Sin(i*2.3f)*1.1f,-5+row*8.5f,48+row*5);
                var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(311+i);
                var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=3;main.startLifetime=new ParticleSystem.MinMaxCurve(2.1f,2.8f);main.startSpeed=new ParticleSystem.MinMaxCurve(5,8.5f);
                main.startSize=new ParticleSystem.MinMaxCurve(.26f,.44f);main.startColor=Color.white;main.gravityModifier=.12f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=180;
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.06f;
                var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,150)});
                var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.Lerp(tint,Color.white,.15f),0),new GradientColorKey(tint,.18f),new GradientColorKey(tint*.65f,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.95f,.6f),new GradientAlphaKey(0,1)});fade.color=gradient;
                var trails=ps.trails;trails.enabled=true;trails.ratio=.92f;trails.lifetime=.65f;trails.worldSpace=true;trails.inheritParticleColor=true;
                trails.widthOverTrail=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
                var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Billboard;
                renderer.sharedMaterial=ParticleMaterial("COP31Firework",false,2.2f);renderer.trailMaterial=ParticleMaterial("COP31FireworkTrails",true,1.7f);
                var controlTrack=timeline.CreateTrack<ControlTrack>(null,"Fireworks / "+i);var controlClip=controlTrack.CreateClip<ControlPlayableAsset>();controlClip.start=OpeningTiming.LogoRaised+slot*.64f+row*.18f;controlClip.duration=3;
                var control=(ControlPlayableAsset)controlClip.asset;control.active=true;control.postPlayback=ActivationControlPlayable.PostPlaybackState.Inactive;control.updateParticle=true;control.updateDirector=false;control.updateITimeControl=false;control.particleRandomSeed=(uint)(311+i);
                var key=new PropertyName("COP31Firework_"+i);control.sourceGameObject=new ExposedReference<GameObject>{exposedName=key};director.SetReferenceValue(key,go);
                go.SetActive(false);
            }
        }
    }
}
