using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameUp.App
{
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider volume, sensitivity, fieldOfView;
        [SerializeField] private Toggle fullscreen;
        [SerializeField] private Text volumeValue, sensitivityValue, fieldOfViewValue;
        public event Action Closed;
        public bool IsOpen => gameObject.activeSelf;
        private UserSettings Settings => ApplicationRoot.Require().Settings;
        public void Show()
        {
            gameObject.SetActive(true);
            volume.SetValueWithoutNotify(Settings.Volume); sensitivity.SetValueWithoutNotify(Settings.Sensitivity);
            fieldOfView.SetValueWithoutNotify(Settings.FieldOfView); fullscreen.SetIsOnWithoutNotify(Settings.Fullscreen);
            UpdateLabels(); volume.Select();
        }
        public void Close() { Settings.Save(); gameObject.SetActive(false); Closed?.Invoke(); }
        public void SetVolume(float value) { Settings.SetVolume(value); UpdateLabels(); }
        public void SetSensitivity(float value) { Settings.SetSensitivity(value); UpdateLabels(); }
        public void SetFieldOfView(float value) { Settings.SetFieldOfView(value); UpdateLabels(); }
        public void SetFullscreen(bool value) => Settings.SetFullscreen(value);
        private void UpdateLabels()
        { volumeValue.text = Mathf.RoundToInt(Settings.Volume * 100) + "%"; sensitivityValue.text = Settings.Sensitivity.ToString("0.00"); fieldOfViewValue.text = Mathf.RoundToInt(Settings.FieldOfView) + "°"; }
    }
}
