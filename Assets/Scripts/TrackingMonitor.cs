using UnityEngine;
using Vuforia;

// Pauses the game and shows a warning when the poster is lost.
public class TrackingMonitor : MonoBehaviour
{
    public GameObject warningUI;   // a UI object saying "Point the camera at the poster"
    public GameManager game;       // drag the GameManager object here

    ObserverBehaviour observer;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer != null)
            observer.OnTargetStatusChanged += OnStatusChanged;
    }

    void OnDestroy()
    {
        if (observer != null)
            observer.OnTargetStatusChanged -= OnStatusChanged;
    }

    void OnStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool ok = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;

        if (warningUI != null) warningUI.SetActive(!ok);
        if (game != null) game.SetPaused(!ok);
    }
}
