using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Find Forgy - game logic.
// Attach this to an empty GameObject called "GameManager".
// Needs: the AR Camera tagged "MainCamera", Forgy tagged "Forgy" with a Collider,
// and every wall/furniture piece with a Collider.
// Uses the NEW Input System (works with "Input System Package" or "Both").
public class GameManager : MonoBehaviour
{
    [Header("Scene objects")]
    public Transform forgy;          // the Forgy robot (tag: Forgy)
    public Transform[] hideSpots;    // empty objects placed around the level

    [Header("UI")]
    public GameObject howToPlayPanel;   // the panel shown before the game starts
    public GameObject hudGroup;
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text roundText;
    public TMP_Text messageText;

    [Header("Settings")]
    public float roundTime = 40f;        // seconds per round
    public int totalRounds = 3;
    public float wrongTapPenalty = 0f;   // seconds lost for a wrong tap (0 = no penalty)

    [Header("Sounds")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioSource tickSource;
    public AudioClip foundSound;
    public AudioClip clickSound;
    public AudioClip gameOverSound;
    
    public float tickStartTime = 5f;   // seconds left when the ticking sound starts

    float timeLeft;
    int score;
    int round;
    int lastSpot = -1;
    bool started;     // false until the player taps past the how-to-play panel
    bool playing;
    bool paused = true;
    bool gameOver;

    void Start()
    {
        // Show the how-to-play panel and wait for a tap. The game does NOT start yet.
        started = false;
        howToPlayPanel.SetActive(true);
        hudGroup.SetActive(false);
    }

    // Called by TrackingMonitor when the poster is lost or found again
    public void SetPaused(bool value)
    {
        paused = value;

        if (musicSource != null)
        {
            if (value) musicSource.Pause();
            else musicSource.UnPause();
        }
    }

    // Hides the panel and begins the first round
    public void StartGame()
    {
        Play(clickSound);
        if (musicSource != null) musicSource.Play();
        started = true;
        howToPlayPanel.SetActive(false);
        hudGroup.SetActive(true);
        Restart();
    }

    void Play(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    void Restart()
    {
        round = 0;
        score = 0;
        lastSpot = -1;
        gameOver = false;
        NextRound();
    }

    void NextRound()
    {
        if (hideSpots == null || hideSpots.Length == 0)
        {
            messageText.text = "No hide spots assigned!";
            return;
        }

        round++;
        if (round > totalRounds)
        {
            EndGame();
            return;
        }

        // pick a random hiding spot, different from the last one
        int i;
        do
        {
            i = Random.Range(0, hideSpots.Length);
        } while (i == lastSpot && hideSpots.Length > 1);
        lastSpot = i;

        forgy.position = hideSpots[i].position;
        forgy.rotation = hideSpots[i].rotation;

        timeLeft = roundTime;
        playing = true;
        messageText.text = "";
        roundText.text = "Round " + round + "/" + totalRounds;
        scoreText.text = "Score: " + score;
    }

    void EndGame()
    {
        playing = false;
        gameOver = true;
        messageText.text = "Game over! Score: " + score + "\nTap to play again";
        Play(gameOverSound);
        if (tickSource != null) tickSource.Stop();
    }

    void Update()
    {
        // Read a tap: phone touch, or mouse click when testing in the editor
        bool tapped = false;
        Vector2 tapPos = Vector2.zero;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            tapped = true;
            tapPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            tapped = true;
            tapPos = Mouse.current.position.ReadValue();
        }

        // Before the game starts: any tap closes the panel and starts round 1
        if (!started) return;

        if (gameOver)
        {
            if (tapped) Restart();
            return;
        }

        if (!playing || paused)
        {
            if (tickSource != null && tickSource.isPlaying) tickSource.Stop();
            return;
        }

        // countdown
        timeLeft -= Time.deltaTime;
        timerText.text = "0:" + Mathf.CeilToInt(Mathf.Max(timeLeft)).ToString();
        if (timeLeft <= tickStartTime && tickSource != null && !tickSource.isPlaying)
            tickSource.Play();
        if (timeLeft <= 0f)
        {
            playing = false;
            messageText.text = "Time's up!";
            Invoke(nameof(NextRound), 2f);
            return;
        }

        if (!tapped) return;

        // Shoot a ray from the tapped point on the screen into the 3D level
        Ray ray = Camera.main.ScreenPointToRay(tapPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.collider.CompareTag("Forgy"))
        {
            score += 100 + Mathf.CeilToInt(timeLeft) * 5;   // faster = more points
            scoreText.text = "Score: " + score;
            playing = false;
            messageText.text = "Found Forgy!";
            Play(foundSound);
            Invoke(nameof(NextRound), 2f);
        }
        else
        {
            timeLeft -= wrongTapPenalty;
            messageText.text = "Not here, keep looking!";
        }
    }
}