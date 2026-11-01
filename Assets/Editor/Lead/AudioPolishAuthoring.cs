using System;
using System.IO;
using System.Linq;
using System.Text;
using GameUp.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameUp.Editor
{
    public static class AudioPolishAuthoring
    {
        internal const string NarrationPath = "Assets/Audio/Cinematic/sinema_senkron_ses.mp3";
        [MenuItem("GameUP32/Authoring/Apply cinematic and workstation audio mix")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before audio authoring.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            var application = scene.GetRootGameObjects().Single(r => r.GetComponent<ApplicationRoot>());
            AudioAuthoring.Configure(application.transform);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            var narration = AssetDatabase.LoadAssetAtPath<AudioClip>(NarrationPath);
            if (narration.length > GameUp.Gameplay.OpeningTiming.End) throw new InvalidOperationException("Narration exceeds the authored opening duration.");
            Debug.Log($"AUDIO_POLISH_OK narration={narration.length:F3}s opening={GameUp.Gameplay.OpeningTiming.End}s analyzer=0.95 ocean_workstation=0.20 printer_loop=false");
        }
        public static void Inspect()
        {
            Directory.CreateDirectory("Logs/AudioPolish");
            var text = new StringBuilder();
            foreach (var path in new[] { NarrationPath, "Assets/Audio/fax_machine.mp3", "Assets/Audio/analyzer_full.mp3", "Assets/Audio/ocean_wave_background.ogg" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (!clip) throw new InvalidOperationException("Missing audio: " + path);
                text.AppendLine($"{path} duration={clip.length:F4} channels={clip.channels} rate={clip.frequency} load={clip.loadType}");
                if (clip.loadType == AudioClipLoadType.Streaming) continue;
                clip.LoadAudioData(); var samples = new float[clip.samples * clip.channels];
                if (!clip.GetData(samples, 0)) throw new InvalidOperationException("Cannot inspect PCM: " + path);
                double energy = 0; float peak = 0;
                foreach (float value in samples) { peak = Mathf.Max(peak, Mathf.Abs(value)); energy += value * value; }
                text.AppendLine($"  peak={peak:F4} rms={Math.Sqrt(energy / samples.Length):F4}");
                for (int second = 0; second < Mathf.CeilToInt(clip.length); second++)
                {
                    int start = second * clip.frequency * clip.channels, end = Math.Min(samples.Length, start + clip.frequency * clip.channels);
                    double segment = 0; for (int i = start; i < end; i++) segment += samples[i] * samples[i];
                    text.AppendLine($"  second={second} rms={Math.Sqrt(segment / (end - start)):F4}");
                }
            }
            File.WriteAllText("Logs/AudioPolish/source-audit.txt", text.ToString(), new UTF8Encoding(false));
            Debug.Log("AUDIO_SOURCE_AUDIT_OK\n" + string.Join("\n", text.ToString().Split('\n').Where(line => line.StartsWith("Assets/", StringComparison.Ordinal))));
        }
    }
}
