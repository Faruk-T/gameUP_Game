using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace GameUp.App
{
    public sealed class MenuController : MonoBehaviour
    {
        [SerializeField] private GameObject home, help, about;
        [SerializeField] private SettingsPanel settings;
        [SerializeField] private Button startButton, helpBack, aboutBack;
        private void Awake()
        {
            if (!home || !help || !about || !settings || !startButton || !helpBack || !aboutBack)
                throw new InvalidOperationException("MenuController requires all authored panels and selections.");
            ApplicationRoot.Require().SetWorldInput(false);
            settings.Closed += ShowHome;
            ShowHome();
        }
        public void StartGame() { startButton.interactable = false; ApplicationRoot.Require().StartGame(); }
        public void ShowHome() { home.SetActive(true); help.SetActive(false); about.SetActive(false); settings.gameObject.SetActive(false); startButton.Select(); }
        public void ShowSettings() { home.SetActive(false); settings.Show(); }
        public void ShowHelp() { home.SetActive(false); help.SetActive(true); helpBack.Select(); }
        public void ShowAbout() { home.SetActive(false); about.SetActive(true); aboutBack.Select(); }
        public void OpenCOP31() => UnityEngine.Application.OpenURL("https://cop31.tr/tr");
        public void Quit() => ApplicationRoot.Require().Quit();
        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (settings.IsOpen) settings.Close(); else if (help.activeSelf || about.activeSelf) ShowHome();
        }
        private void OnDestroy() { if (settings) settings.Closed -= ShowHome; }
    }
}
