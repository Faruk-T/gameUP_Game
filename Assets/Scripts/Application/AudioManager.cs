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
        [SerializeField] private AudioSource music, ambience, conveyor, analyzer, effects;
        [SerializeField] private AudioClip menuMusic, gameMusic;
        [SerializeField] private AudioCueBinding[] cues;
        private AudioCueBinding[] cueIndex;
        private bool inGame, beltRunning, analyzing, paused;
        public bool IsBeltRunning => beltRunning && !paused;

        private void Awake()
        {
            if (!music || !ambience || !conveyor || !analyzer || !effects || !menuMusic || !gameMusic || !ambience.clip || !conveyor.clip || !analyzer.clip)
                throw new InvalidOperationException("First scene requires all authored audio channels and music.");
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
            effects.PlayOneShot(binding.Clip, binding.Volume);
        }

        public void EnterMenu()
        {
            inGame = false; SetBeltRunning(false); SetAnalysis(false, false);
            ambience.Stop(); effects.Stop();
            SetMusic(menuMusic, .3f);
        }

        public void BeginOpening()
        {
            inGame = true; paused = false;
            SetMusic(gameMusic, .2f);
            ambience.volume = .45f; ambience.Play();
        }

        public void EnterWorkstation() => ambience.volume = .065f;

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
            music.volume = inGame ? (value ? .08f : .2f) : .3f;
        }

        public void ResetWorkstation()
        {
            SetBeltRunning(false); SetAnalysis(false, false); effects.Stop();
        }

        private void SetMusic(AudioClip clip, float volume)
        {
            music.volume = volume;
            if (music.clip == clip && music.isPlaying) return;
            music.clip = clip; music.Play();
        }
    }
}
