using GameUp.Contracts;
using UnityEngine;
namespace GameUp.Gameplay
{
    public sealed class WorkstationDocuments : MonoBehaviour
    {
        [SerializeField] private GameSessionController session;
        [SerializeField] private GameObject closedBook, openBook, newspaper;
        private void OnEnable(){session.Refreshed+=Refresh;Refresh();}
        private void OnDisable()=>session.Refreshed-=Refresh;
        private void Refresh()
        {
            var kind=session.Documents.Current?.Kind;
            bool handbook=kind==DocumentKind.Handbook;
            closedBook.SetActive(!handbook);openBook.SetActive(handbook);
            newspaper.SetActive(kind!=DocumentKind.Newspaper);
        }
    }
}
