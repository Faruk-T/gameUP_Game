using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameUp.App
{
    [DefaultExecutionOrder(-10000), DisallowMultipleComponent]
    public sealed class ApplicationRoot : MonoBehaviour
    {
        public const string FirstScene = "First", MenuScene = "Menu", GameScene = "Game";
        public static ApplicationRoot Current { get; private set; }
        [SerializeField] private UserSettingsDefaults defaults;
        [SerializeField] private CanvasGroup loadingScreen;
        [SerializeField] private AudioManager audioManager;
        public AudioManager Audio => audioManager;
        public UserSettings Settings { get; private set; }
        public bool IsLoading { get; private set; }
        public event Action<string> SceneReady;

        public static ApplicationRoot Require() => Current ? Current : throw new InvalidOperationException("Start the application from First.unity.");
        private void Awake()
        {
            if (Current) throw new InvalidOperationException("Only First.unity may own ApplicationRoot.");
            if (!defaults || !loadingScreen || !audioManager) throw new InvalidOperationException("ApplicationRoot has incomplete authored references.");
            Current = this;
            Settings = new UserSettings(defaults);
            DontDestroyOnLoad(gameObject);
        }
        private void Start() => LoadMenu();
        public void StartGame() => Load(GameScene);
        public void LoadMenu() => Load(MenuScene);
        private void Load(string scene)
        {
            if (IsLoading) return;
            StartCoroutine(LoadScene(scene));
        }
        private IEnumerator LoadScene(string scene)
        {
            IsLoading = true;
            Time.timeScale = 1;
            SetWorldInput(false);
            loadingScreen.alpha = 1; loadingScreen.blocksRaycasts = true;
            var operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            if (operation == null) throw new InvalidOperationException("Scene must be present in build settings: " + scene);
            yield return operation;
            IsLoading = false;
            loadingScreen.alpha = 0; loadingScreen.blocksRaycasts = false;
            if (scene == MenuScene) audioManager.EnterMenu();
            SceneReady?.Invoke(scene);
        }
        public void SetWorldInput(bool enabled)
        {
            bool active = enabled && !IsLoading && UnityEngine.Application.isFocused;
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }
        public void Quit() { Settings.Save(); UnityEngine.Application.Quit(); }
        private void OnApplicationQuit() => Settings?.Save();
        private void OnDestroy() { if (Current == this) Current = null; }
    }
}
