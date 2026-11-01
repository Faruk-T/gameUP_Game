using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameUp.App
{
    [RequireComponent(typeof(Button)), DisallowMultipleComponent]
    public sealed class UIButtonSound : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        private Button button;
        private void Awake() => button=GetComponent<Button>();
        public void OnPointerClick(PointerEventData data)
        { if(data.button==PointerEventData.InputButton.Left)Play(); }
        public void OnSubmit(BaseEventData data) => Play();
        private void Play()
        { if(button.IsInteractable())ApplicationRoot.Require().Audio.PlayCue(GameAudioCue.Button); }
    }
}
