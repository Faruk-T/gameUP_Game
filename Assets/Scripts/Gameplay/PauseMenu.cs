using System;
using GameUp.App;
using UnityEngine;
using UnityEngine.UI;

namespace GameUp.Gameplay
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject overlay, home;
        [SerializeField] private SettingsPanel settings;
        [SerializeField] private Button resume;
        public bool IsOpen { get; private set; }
        public event Action Changed;
        private void Awake() { settings.Closed += ReturnToPause; overlay.SetActive(false); }
        public void Open() { IsOpen = true; overlay.SetActive(true); home.SetActive(true); settings.gameObject.SetActive(false); Time.timeScale = 0; resume.Select(); Changed?.Invoke(); }
        public void Resume() { IsOpen = false; overlay.SetActive(false); Time.timeScale = 1; Changed?.Invoke(); }
        public void ShowSettings() { home.SetActive(false); settings.Show(); }
        private void ReturnToPause() { home.SetActive(true); resume.Select(); }
        public void Escape() { if (settings.IsOpen) settings.Close(); else Resume(); }
        public void ReturnToMenu() { Time.timeScale = 1; ApplicationRoot.Require().Settings.Save(); ApplicationRoot.Require().LoadMenu(); }
        private void OnDestroy() { settings.Closed -= ReturnToPause; Time.timeScale = 1; }
    }
}
