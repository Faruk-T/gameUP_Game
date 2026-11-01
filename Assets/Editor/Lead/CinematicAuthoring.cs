using System;
using System.IO;
using GameUp.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    public static class CinematicAuthoring
    {
        private const string Data="Assets/Data/Cinematic";
        private readonly struct Pose
        {
            internal readonly float Time;
            internal readonly Vector3 Position, Look;
            internal Pose(float time,Vector3 position,Vector3 look){Time=time;Position=position;Look=look;}
        }
        [MenuItem("GameUP32/Authoring/Rebuild coastal facility and opening")]
        public static void Author()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring.");
            Directory.CreateDirectory(Data);AssetDatabase.Refresh();
            WorkstationAuthoring.Game();
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            Debug.Log("COASTAL_AUTHORING_OK / First -> Menu -> Game / unskippable opening");
        }
        internal static void Create(GameSessionController session,Camera gameplayCamera,Canvas gameplayInterface)
        {
            Directory.CreateDirectory(Data);AssetDatabase.Refresh();
            gameplayCamera.farClipPlane=260;gameplayCamera.clearFlags=CameraClearFlags.Skybox;
            CoastalAssetSetup.ConfigureCamera(gameplayCamera);
            var cameraObject=new GameObject("OpeningCamera",typeof(Camera),typeof(AudioListener),typeof(Animator));
            var camera=cameraObject.GetComponent<Camera>();camera.nearClipPlane=.08f;camera.farClipPlane=260;camera.fieldOfView=60;camera.clearFlags=CameraClearFlags.Skybox;
            cameraObject.GetComponent<Animator>().applyRootMotion=false;
            CoastalAssetSetup.ConfigureCamera(camera);
            var root=new GameObject("OpeningSequence");var director=root.AddComponent<PlayableDirector>();director.playOnAwake=false;
            director.extrapolationMode=DirectorWrapMode.Hold;director.timeUpdateMode=DirectorUpdateMode.Manual;
            var intro=root.AddComponent<IntroCinematic>();Set(session,"intro",intro);Set(intro,"director",director);
            Set(intro,"cinematicCamera",cameraObject);Set(intro,"gameplayCamera",gameplayCamera.gameObject);Set(intro,"gameplayInterface",gameplayInterface.gameObject);
            var canvas=Canvas("OpeningCanvas",null,70);var overlay=canvas.gameObject.AddComponent<CanvasGroup>();overlay.blocksRaycasts=true;Set(intro,"overlay",overlay);
            var black=Overlay("BlackBackground",canvas.transform,Color.black).AddComponent<CanvasGroup>();Set(intro,"blackout",black);
            var quote=Rect("OpeningQuote",canvas.transform,0,0,1920,1080).gameObject.AddComponent<CanvasGroup>();Set(intro,"quote",quote);
            Text("Eyebrow",quote.transform,"COP31  /  ORTAK GELECEK",0,385,1400,55,24,new Color(.48f,.73f,.69f),TextAnchor.MiddleCenter);
            Text("QuotationMark",quote.transform,"“",0,280,1400,140,108,new Color(.48f,.73f,.69f),TextAnchor.MiddleCenter);
            Text("QuoteText",quote.transform,"İklim krizi yalnızca çevresel değil, aynı zamanda ahlaki bir sorumluluk alanıdır. Gelecek nesillere daha adil, yaşanabilir ve sürdürülebilir bir dünya bırakmak; tüketim alışkanlıklarımızı, üretim modellerimizi ve doğayla kurduğumuz ilişkiyi yeniden düşünmeyi gerektirmektedir.",0,15,1320,400,43,Color.white,TextAnchor.MiddleCenter);
            Text("Attribution",quote.transform,"Emine ERDOĞAN",0,-280,1300,70,29,Color.white,TextAnchor.MiddleCenter);
            Text("AttributionRole",quote.transform,"Türkiye Cumhuriyeti Cumhurbaşkanı Eşi\nBirleşmiş Milletler (BM) Sıfır Atık Yüksek Düzeyli Şahsiyetler Danışma Kurulu Başkanı",0,-358,1400,110,21,new Color(.68f,.7f,.72f),TextAnchor.MiddleCenter);
            Image("TopLetterbox",canvas.transform,0,500,1920,80,Color.black);
            Image("BottomLetterbox",canvas.transform,0,-500,1920,80,Color.black);
            Set(intro,"caption",Text("SceneCaption",canvas.transform,"",0,-438,1500,80,25,Color.white,TextAnchor.MiddleCenter));
            var so=new SerializedObject(intro);var captions=so.FindProperty("captions");captions.arraySize=4;
            Caption(captions,0,20.5f,28,"ANTALYA  /  AKDENİZ KIYISI\nBir kıyıyı korumak, bir yaşamı korumaktır.");
            Caption(captions,1,30,40,"Temiz su, sağlıklı ekosistemler ve güvenli gıda aynı geleceğe bağlı.");
            Caption(captions,2,44,56,"COP31 hazırlıklarında görevli bir ürün denetçisisin.\nBugünün kararları, yarının sofrasını korur.");
            Caption(captions,3,60,70,"Kaynağı izle. Riski belirle. İsrafı azalt.\nAntalya Gıda Güvenliği Tesisi — ilk vardiya.");
            so.ApplyModifiedPropertiesWithoutUndo();
            var poses=new[]{
                new Pose(0,new Vector3(-16,1.65f,22),new Vector3(-10,-.15f,28)),
                new Pose(19,new Vector3(-16,1.65f,22),new Vector3(-10,-.15f,28)),
                new Pose(25,new Vector3(-13.8f,1.45f,23.2f),new Vector3(-10,-.35f,29)),
                new Pose(31,new Vector3(-12.6f,1.55f,23.5f),new Vector3(-8,-.45f,32)),
                new Pose(33,new Vector3(-12.8f,1.55f,23.5f),new Vector3(-8,-.45f,32)),
                new Pose(36,new Vector3(-20,1.7f,21),new Vector3(-23,1.8f,10)),
                new Pose(41,new Vector3(-23,1.7f,13),new Vector3(-20,2.1f,-4)),
                new Pose(47,new Vector3(-23,1.7f,2),new Vector3(-20,2.1f,-4)),
                new Pose(49,new Vector3(-23,1.7f,-4),new Vector3(-14,1.9f,-4)),
                new Pose(51,new Vector3(-19.3f,1.7f,-4),new Vector3(-11,1.8f,-4)),
                new Pose(56,new Vector3(-15.7f,1.7f,-4),new Vector3(-15.3f,1.95f,-2.1f)),
                new Pose(60,new Vector3(-13.2f,1.7f,-4),new Vector3(-11.1f,1.95f,-5.87f)),
                new Pose(65,new Vector3(-9.2f,1.7f,-4),new Vector3(0,1.5f,.25f)),
                new Pose(69,new Vector3(-3.8f,1.9f,-3.7f),new Vector3(0,1.2f,.25f)),
                new Pose(72,gameplayCamera.transform.position,gameplayCamera.transform.position+gameplayCamera.transform.forward*5)
            };
            var clip=new AnimationClip{name="Beach to inspection desk",frameRate=30};
            var positions=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
            var rotations=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
            Quaternion previous=Quaternion.identity;
            for(int i=0;i<poses.Length;i++)
            {
                var pose=poses[i];var rotation=Quaternion.LookRotation(pose.Look-pose.Position);
                if(i>0&&Quaternion.Dot(previous,rotation)<0)rotation=new Quaternion(-rotation.x,-rotation.y,-rotation.z,-rotation.w);
                previous=rotation;
                for(int axis=0;axis<3;axis++)positions[axis].AddKey(pose.Time,pose.Position[axis]);
                for(int axis=0;axis<4;axis++)rotations[axis].AddKey(pose.Time,rotation[axis]);
            }
            for(int i=0;i<3;i++){Smooth(positions[i]);clip.SetCurve("",typeof(Transform),"localPosition."+"xyz"[i],positions[i]);}
            for(int i=0;i<4;i++){Smooth(rotations[i]);clip.SetCurve("",typeof(Transform),"localRotation."+"xyzw"[i],rotations[i]);}
            clip.EnsureQuaternionContinuity();
            string clipPath=Data+"/ArrivalCamera.anim";var savedClip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(savedClip){EditorUtility.CopySerialized(clip,savedClip);UnityEngine.Object.DestroyImmediate(clip);}else{AssetDatabase.CreateAsset(clip,clipPath);savedClip=clip;}
            string timelinePath=Data+"/Arrival.playable";var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            if(!timeline){timeline=ScriptableObject.CreateInstance<TimelineAsset>();AssetDatabase.CreateAsset(timeline,timelinePath);}
            foreach(var track in System.Linq.Enumerable.ToArray(timeline.GetRootTracks()))timeline.DeleteTrack(track);
            var cameraTrack=timeline.CreateTrack<AnimationTrack>(null,"Coast -> corridor -> workstation");cameraTrack.trackOffset=TrackOffset.ApplyTransformOffsets;
            var cameraClip=cameraTrack.CreateClip<AnimationPlayableAsset>();cameraClip.start=0;cameraClip.duration=72;cameraClip.displayName="Unskippable arrival";
            var playable=(AnimationPlayableAsset)cameraClip.asset;playable.clip=savedClip;playable.removeStartOffset=false;
            director.playableAsset=timeline;director.SetGenericBinding(cameraTrack,cameraObject.GetComponent<Animator>());
            cameraObject.transform.position=poses[0].Position;cameraObject.transform.rotation=Quaternion.LookRotation(poses[0].Look-poses[0].Position);
            cameraObject.SetActive(false);canvas.gameObject.SetActive(false);
            EditorUtility.SetDirty(timeline);AssetDatabase.SaveAssets();
        }
        private static void Smooth(AnimationCurve curve)
        {
            for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
        }
        private static void Caption(SerializedProperty array,int i,float start,float end,string text)
        {var item=array.GetArrayElementAtIndex(i);item.FindPropertyRelative("Start").floatValue=start;item.FindPropertyRelative("End").floatValue=end;item.FindPropertyRelative("Text").stringValue=text;}
    }
}
