using UnityEngine;

namespace GameUp.App
{
    [CreateAssetMenu(menuName = "GameUP/Presentation/User settings")]
    public sealed class UserSettingsDefaults : ScriptableObject
    {
        [Range(0, 1)] public float MasterVolume = 0.8f;
        [Range(0.03f, 0.3f)] public float Sensitivity = 0.12f;
        [Range(45, 80)] public float FieldOfView = 55;
        public bool Fullscreen = true;
    }
}
