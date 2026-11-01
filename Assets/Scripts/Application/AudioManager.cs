using System;
using UnityEngine;

namespace GameUp.App
{
    public enum GameAudioCue { Button, ProductPickup, BookOpen, BookClose, BookPage, Newspaper, Disposal, Fax, Alert }

    [Serializable]
    public struct AudioCueBinding
    {
        public GameAudioCue Cue;
        public AudioClip Clip;
        [Range(0, 1)] public float Volume;
    }

    /// <summary>Authored application audio channels. The First scene owns the only instance.</summary>
    [DefaultExecutionOrder(-9000), DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource music, ambience, conveyor, analyzer, effects, printer, narration;
        [SerializeField] private AudioClip menuMusic, gameMusic;
        [SerializeField] private AudioCueBinding[] cues;
        [SerializeField, Range(0, 1)] private float menuOceanVolume = .12f, openingOceanVolume = .22f, workstationOceanVolume = .2f;
        [SerializeField, Range(0, 1)] private float openingMusicVolume = .06f;
        private AudioCueBinding[] cueIndex;
        private bool inGame, beltRunning, analyzing, paused, opening, narrationSuspended, narrationFinished;
        public bool IsBeltRunning => beltRunning && !paused;
        public float FaxDuration => cueIndex[(int)GameAudioCue.Fax].Clip.length;

        private void Awake()
        {
            if (!music || !ambience || !conveyor || !analyzer || !effects || !printer || !narration || !menuMusic || !gameMusic || !ambience.clip || !conveyor.clip || !analyzer.clip || !narration.clip)
                throw new InvalidOperationException("First scene requires all authored audio channels and music.");
            if (printer.loop || effects.loop || analyzer.loop || narration.loop)
                throw new InvalidOperationException("Printer, effects, analyzer and narration must not loop.");
            int count = Enum.GetValues(typeof(GameAudioCue)).Length;
            cueIndex = new AudioCueBinding[count];
            var assigned = new bool[count];
            foreach (var binding in cues)
            {
                int id = (int)binding.Cue;
                if (!binding.Clip || assigned[id]) throw new InvalidOperationException("Missing or duplicate audio cue: " + binding.Cue);
                cueIndex[id] = binding; assigned[id] = true;
            }
            foreach (bool valid in assigned) if (!valid) throw new InvalidOperationException("Audio cue catalog is incomplete.");
        }

        public void PlayCue(GameAudioCue cue)
        {
            var binding = cueIndex[(int)cue];
            if (cue == GameAudioCue.Fax)
            {
                // One physical printer: replace its previous job instead of stacking one-shots.
                printer.Stop(); printer.PlayOneShot(binding.Clip, binding.Volume); return;
            }
            effects.PlayOneShot(binding.Clip, binding.Volume);
        }

        public void StopFax() => printer.Stop();

        public void EnterMenu()
        {
            EndOpening(); inGame = false; paused = false; ResetWorkstation();
            SetOcean(menuOceanVolume);
            SetMusic(menuMusic, .3f);
        }

        public void BeginOpening()
        {
            inGame = true; paused = false; opening = true; narrationSuspended = false; narrationFinished = false;
            ResetWorkstation(); SetMusic(gameMusic, openingMusicVolume); SetOcean(openingOceanVolume);
            narration.Stop(); narration.Play();
        }

        public void SampleOpening(double time, bool advancing, float interiorBlend)
        {
            if (!opening) return;
            ambience.volume = Mathf.Lerp(openingOceanVolume, workstationOceanVolume, Mathf.Clamp01(interiorBlend));
            if (narrationFinished) return;
            if (time >= narration.clip.length) { narration.Stop(); narrationFinished = true; return; }
            int sample = Mathf.Clamp((int)(Math.Max(0, time) * narration.clip.frequency), 0, narration.clip.samples - 1);
            if (Math.Abs(narration.timeSamples - sample) > narration.clip.frequency * .12f) narration.timeSamples = sample;
            bool suspend = !advancing;
            if (narrationSuspended == suspend) return;
            narrationSuspended = suspend;
            if (suspend) narration.Pause(); else narration.UnPause();
        }

        public void EndOpening()
        {
            opening = false; narrationSuspended = false; narrationFinished = true; narration.Stop();
        }
        public void EnterWorkstation()
        {
            EndOpening(); SetOcean(workstationOceanVolume); SetMusic(gameMusic, .2f);
        }

        public void SetBeltRunning(bool active)
        {
            if (beltRunning == active) return;
            beltRunning = active;
            if (active) conveyor.UnPause(); else conveyor.Pause();
            if (active && !conveyor.isPlaying) conveyor.Play();
        }

        public void SetAnalysis(bool active, bool suspend)
        {
            if (active != analyzing)
            {
                analyzing = active;
                if (active) analyzer.Play(); else analyzer.Stop();
            }
            if (!active) return;
            if (suspend) analyzer.Pause(); else analyzer.UnPause();
        }

        public void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            music.volume = opening ? openingMusicVolume : inGame ? (value ? .08f : .2f) : .3f;
        }

        public void ResetWorkstation()
        {
            SetBeltRunning(false); SetAnalysis(false, false); effects.Stop(); StopFax();
        }

        private void SetOcean(float volume)
        { ambience.volume = volume; if (!ambience.isPlaying) ambience.Play(); }

        private void SetMusic(AudioClip clip, float volume)
        {
            music.volume = volume;
            if (music.clip == clip && music.isPlaying) return;
            music.clip = clip; music.Play();
        }
    }
}
