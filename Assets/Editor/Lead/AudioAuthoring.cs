using System;
using System.Linq;
using GameUp.App;
using UnityEditor;
using UnityEngine;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class AudioAuthoring
    {
        internal static void Configure(Transform application)
        {
            foreach(string path in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Audio"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);
                var sample=importer.defaultSampleSettings;
                sample.loadType=path.Contains("/Music/")||path.EndsWith("ocean_wave_background.ogg",StringComparison.Ordinal)
                    ?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
                sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.7f;
                sample.preloadAudioData=sample.loadType!=AudioClipLoadType.Streaming;importer.defaultSampleSettings=sample;
                importer.SaveAndReimport();
            }
            var old=application.Find("Audio");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Audio").transform;root.SetParent(application,false);
            root.gameObject.AddComponent<AudioListener>();
            var manager=root.gameObject.AddComponent<AudioManager>();
            Set(application.GetComponent<ApplicationRoot>(),"audioManager",manager);
            Set(manager,"music",Channel(root,"Music",true,.2f));
            Set(manager,"ambience",Channel(root,"Ocean",true,.45f,Clip("ocean_wave_background.ogg")));
            Set(manager,"conveyor",Channel(root,"Conveyor",true,.2f,Clip("conveyor.mp3")));
            Set(manager,"analyzer",Channel(root,"Analyzer",false,.4f,Clip("analyzer_full.mp3")));
            Set(manager,"effects",Channel(root,"Effects",false,.65f));
            var music=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Audio/Music"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Set(manager,"menuMusic",AssetDatabase.LoadAssetAtPath<AudioClip>(music.Single(p=>p.Contains("Herbal Tea"))));
            Set(manager,"gameMusic",AssetDatabase.LoadAssetAtPath<AudioClip>(music.Single(p=>p.Contains("Way Home"))));
            var bindings=new[]{
                (GameAudioCue.Button,"approve_reject_button.mp3",.75f),(GameAudioCue.ProductPickup,"item_pick_up.mp3",.65f),
                (GameAudioCue.BookOpen,"book_pick_up.mp3",.65f),(GameAudioCue.BookClose,"book_drop.mp3",.6f),
                (GameAudioCue.BookPage,"book_flip_page.mp3",.4f),(GameAudioCue.Newspaper,"newspaper.mp3",.7f),
                (GameAudioCue.Disposal,"dissolve_garbage.mp3",.55f),(GameAudioCue.Fax,"fax_machine.mp3",.55f),
                (GameAudioCue.Alert,"FaxSignal.wav",.65f)};
            var so=new SerializedObject(manager);var cues=so.FindProperty("cues");cues.arraySize=bindings.Length;
            for(int i=0;i<bindings.Length;i++)
            {
                var item=cues.GetArrayElementAtIndex(i);item.FindPropertyRelative("Cue").enumValueIndex=(int)bindings[i].Item1;
                item.FindPropertyRelative("Clip").objectReferenceValue=Clip(bindings[i].Item2);item.FindPropertyRelative("Volume").floatValue=bindings[i].Item3;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("AUDIO_CATALOG_OK / source chore/audio-assets 10bb33f / 9 effects, 2 music, ocean, conveyor, analyzer");
        }
        internal static AudioClip Clip(string name)
        {
            string path="Assets/Audio/"+name;
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path)??throw new InvalidOperationException("Missing audio: "+path);
        }
        private static AudioSource Channel(Transform parent,string name,bool loop,float volume,AudioClip clip=null)
        {
            var source=new GameObject(name).AddComponent<AudioSource>();source.transform.SetParent(parent,false);
            source.playOnAwake=false;source.loop=loop;source.volume=volume;source.spatialBlend=0;source.clip=clip;
            return source;
        }
    }
}
