using GameUp.Gameplay;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GameUp.Editor
{
    internal static class EntranceDoorAuthoring
    {
        internal static void Create(Transform entrance)
        {
            var root=new GameObject("FacilityEntranceDoors",typeof(Animator)).transform;root.SetParent(entrance,false);
            root.GetComponent<Animator>().applyRootMotion=false;
            Leaf(root,"NorthHinge","SM_Bld_Door_Large_01_R",-2.1f,1);
            Leaf(root,"SouthHinge","SM_Bld_Door_Large_01_L",-5.9f,-1);
        }
        private static void Leaf(Transform parent,string name,string prefab,float z,float direction)
        {
            var hinge=new GameObject(name).transform;hinge.SetParent(parent,false);
            hinge.localPosition=new Vector3(-20,0,z);hinge.localRotation=Quaternion.Euler(0,90,0);
            CoastalEnvironmentAuthoring.Fitted("Assets/PolygonOffice/Prefabs/Buildings/"+prefab+".prefab",hinge,
                new Vector3(direction*.93f,1.64f,0),new Vector3(1.86f,3.24f,.12f));
        }
        internal static void Bind(TimelineAsset timeline,PlayableDirector director)
        {
            var animator=GameObject.Find("FacilityEntranceDoors").GetComponent<Animator>();
            var clip=new AnimationClip{name="Entrance double doors open as the inspector arrives",frameRate=30};
            Curve(clip,"NorthHinge",0);Curve(clip,"SouthHinge",180);
            var saved=CinematicAuthoring.SaveClip(clip,"Assets/Data/Cinematic/EntranceDoors.anim");
            var track=timeline.CreateTrack<AnimationTrack>(null,"Entrance / closed then open on arrival");track.trackOffset=TrackOffset.ApplyTransformOffsets;
            var item=track.CreateClip<AnimationPlayableAsset>();item.start=0;item.duration=OpeningTiming.End;
            var playable=(AnimationPlayableAsset)item.asset;playable.clip=saved;playable.removeStartOffset=false;
            director.SetGenericBinding(track,animator);
        }
        private static void Curve(AnimationClip clip,string hinge,float opened)
            =>clip.SetCurve(hinge,typeof(Transform),"localEulerAnglesRaw.y",new AnimationCurve(
                new Keyframe(0,90),new Keyframe(OpeningTiming.DoorsOpenStart,90),
                new Keyframe(OpeningTiming.DoorsOpenEnd,opened),new Keyframe(OpeningTiming.End,opened)));
    }
}
