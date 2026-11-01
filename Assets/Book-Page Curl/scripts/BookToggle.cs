using UnityEngine;
using UnityEngine.InputSystem;

// Place on an always-active object outside the book canvas hierarchy.
public class BookToggle : MonoBehaviour
{
    [SerializeField] GameObject bookCanvas;
    [SerializeField] bool startOpen = false;

    void Awake()
    {
        if (bookCanvas == null || transform.IsChildOf(bookCanvas.transform))
        {
            Debug.LogError("BookToggle requires a book canvas and must be outside its hierarchy.", this);
            enabled = false;
            return;
        }

        bookCanvas.SetActive(startOpen);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
            ToggleBook();
    }

    public void ToggleBook()
    {
        if (bookCanvas != null)
            bookCanvas.SetActive(!bookCanvas.activeSelf);
    }
}
