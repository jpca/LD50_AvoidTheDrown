using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;


/// <summary>
/// Manage Game life cycle : menu/start/gameover 
/// Player wins if the PlayerWinScore is reached.
/// Game over is triggered if player wins or if Player.Damage.health is 0 
/// Player.Damage.Health and Score are managed by other game objects in the scene
/// </summary>
public class GameLoop : MonoBehaviour
{
    public Text PlayerMessageText;

    public Text PlayerScoreText;

    protected const string PLAYPREFS_HIGHSCORE_KEY = "HighScore";

    public GameObject Player;

    public float PlayerStartHealth;

    public Transform PlayerRestartLocation;

    public AudioClip GameOverLostSound;

    public AudioClip GameStartSound;

    protected AudioSource audioSource;


    [Tooltip("Objects to active / disactivate at GameOver / Restart")]
    public List<GameObject> GameObjects = new List<GameObject>();

    [Tooltip("Timer used")]
    public Timer Timer;

    [Tooltip("Score to achieve for player to win")]
    [Range(0, 1000)]
    public int PlayerWinScore = 100;

    [Tooltip("If checked, the game ends when the win score is reached")]
    public bool EndGameWhenWinScoreReached = false;

    // Player current score
    public int PlayerCurrentScore { get; protected set; } = 0;

    // Player first icefiemd
    public GameObject startIceField;

    public enum GameState
    {
        Menu,
        Playing,
        Gameover
    }
    public GameState State { get; protected set; } = GameState.Menu;

    protected Damageable playerDamageable;

    public void Start()
    {
        playerDamageable = Player.GetComponent<Damageable>();
        audioSource = gameObject.GetComponent<AudioSource>();
        Menu();
    }

    public void Update()
    {
        if (State == GameState.Playing)
        {
            /* used for High Score
            if (PlayerScoreText)
                PlayerScoreText.text = $"Score: {PlayerCurrentScore:0}";
            */

            if ((playerDamageable.Health <= 0) || (EndGameWhenWinScoreReached && PlayerWin()))
            {
                Gameover();
            }

            // NB : GameOver for TimeOut is triggered by the Timer
        }
        else
        {
            // Menu or GameOver
            if ((Input.GetButtonDown("Jump")))
            {
                RestartGame();
            }

        }

#if UNITY_STANDALONE
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
#endif

        if (Input.GetKey(KeyCode.P))
        {
            string screenshotFileName = $"{Application.companyName}-{Application.productName}-{Application.version}";
            screenshotFileName += "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            ScreenCapture.CaptureScreenshot(screenshotFileName);
        }


    }

    private void RestartGame()
    {
        State = GameState.Playing;

        playerDamageable.Health = PlayerStartHealth;
        Player.transform.position = PlayerRestartLocation.position;
        Player.transform.rotation = PlayerRestartLocation.rotation;
        PlayerCurrentScore = 0;
        //activated in next line: Player.SetActive(true);
        GameObjects.ForEach(go => go.SetActive(true)); 

        DisplayMessage("");

        // Play score used for high score
        if (PlayerScoreText)
            PlayerScoreText.text = $"High Score: {PlayerPrefs.GetInt("HighScore", 0):0}";

        Timer.RestartTimer();

        Resizer startRs = startIceField.GetComponentInChildren<Resizer>();
        startRs.enabled = true;
        startRs.ResetSize();

        // Play restart sound
        if (audioSource && GameStartSound)
            audioSource.PlayOneShot(GameStartSound);

    }

    public void Gameover()
    {
        State = GameState.Gameover;
        if (PlayerWin())
        {
            float bonus = Timer.TimeCount + playerDamageable.Health;
            DisplayMessage($"You Win\nScore {PlayerCurrentScore:0} - Bonus {bonus:0}");
        }
        else
        {
            if (playerDamageable.Health > 0)
            {
                // Play does not died but miss required score in allowed time
                DisplayMessage($"Game Over\nMissed {PlayerWinScore - PlayerCurrentScore} points");
            }
            else
            {
                // Player died
                DisplayMessage($"Game Over\nDied in {Timer.TimeCount:0} seconds");

                // Play lost sound
                if (audioSource && GameOverLostSound)
                    audioSource.PlayOneShot(GameOverLostSound);

            }
        }

        // manage high score
        PlayerCurrentScore = Mathf.FloorToInt(Timer.TimeCount);
        if (PlayerCurrentScore > PlayerPrefs.GetInt("HighScore", 0))
        {
            PlayerPrefs.SetInt(PLAYPREFS_HIGHSCORE_KEY, PlayerCurrentScore);
            PlayerPrefs.Save();
        }

        //StartCoroutine(LaunchMenuInSeconds(2));
    }

    public void AddPlayerScore(int value)
    {
        PlayerCurrentScore += value;
        Debug.Log($"Player Score changed: {PlayerCurrentScore}");
    }

    public void IncrementPlayerGoalScore()
    {
        AddPlayerScore(1);
    }


    public void DecrementPlayerGoalScore()
    {
        AddPlayerScore(-1);
    }

    public bool PlayerWin()
    {
        return (PlayerCurrentScore >= PlayerWinScore && playerDamageable.Health > 0);
    }

    private void Menu()
    {
        State = GameState.Menu;
        GameObjects.ForEach(go => go.SetActive(false));
        DisplayMessage("Press SpaceBar to Start");
    }

    protected virtual IEnumerator LaunchMenuInSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Menu();
    }

    public void DisplayMessage(string message)
    {
        PlayerMessageText.text = message;
    }

    public void DisplayTempMessage(string message)
    {
        PlayerMessageText.text = message;
        StartCoroutine(ClearMessageInSeconds(2));
    }
    protected virtual IEnumerator ClearMessageInSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        DisplayMessage("");
    }
}
