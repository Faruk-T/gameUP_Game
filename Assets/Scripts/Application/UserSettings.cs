using System;
using UnityEngine;

namespace GameUp.App
{
    public sealed class UserSettings
    {
        private const string Prefix = "GameUP32.";
        public float Volume { get; private set; }
        public float Sensitivity { get; private set; }
        public float FieldOfView { get; private set; }
        public bool Fullscreen { get; private set; }
        public event Action Changed;

        public UserSettings(UserSettingsDefaults defaults)
        {
            if (!defaults) throw new ArgumentNullException(nameof(defaults));
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Volume", defaults.MasterVolume));
            Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "Sensitivity", defaults.Sensitivity), 0.03f, 0.3f);
            FieldOfView = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "FOV", defaults.FieldOfView), 45, 80);
            Fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", defaults.Fullscreen ? 1 : 0) == 1;
            AudioListener.volume = Volume;
            Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
        public void SetVolume(float value) { Volume = Mathf.Clamp01(value); AudioListener.volume = Volume; Changed?.Invoke(); }
        public void SetSensitivity(float value) { Sensitivity = Mathf.Clamp(value, 0.03f, 0.3f); Changed?.Invoke(); }
        public void SetFieldOfView(float value) { FieldOfView = Mathf.Clamp(value, 45, 80); Changed?.Invoke(); }
        public void SetFullscreen(bool value)
        {
            Fullscreen = value;
            Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Changed?.Invoke();
        }
        public void Save()
        {
            PlayerPrefs.SetFloat(Prefix + "Volume", Volume); PlayerPrefs.SetFloat(Prefix + "Sensitivity", Sensitivity);
            PlayerPrefs.SetFloat(Prefix + "FOV", FieldOfView); PlayerPrefs.SetInt(Prefix + "Fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
