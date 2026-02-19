using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public enum RocketType
{
    Stage1_4,
    Stage2_7_Large,
    Booster_10
}

public class Launch : MonoBehaviour
{
    [Header("Launch Permission")]
    public bool canLaunch = true;

    [Header("Idle Reset")]
    //public GameObject SimulationScreen;
    public float idleLimit = 30f;   // seconds
    private float lastInputTime;
    [Header("Result Idle Reset")]
    public float resultIdleLimit = 40f;



    [Header("Lift Off")]
    public float initialLiftDistance = 0f;
    public float launchAcceleration = 6f;

    [Header("Timing")]
    public float launchDuration = 8f;

    [Header("Camera Focus")]
    public CameraFocus cameraFocus;

    [Header("Post-Assembly Systems")]
    public RocketLockOnLaunch rocketLock;

    [Header("Error Objects")]
    [SerializeField] private GameObject ErrorBox;
    [SerializeField] private GameObject ErrorPanel;
    [SerializeField] private GameObject ErrorPan;

    [Header("UI Screens")]
    [SerializeField] private GameObject assemblyScreen;
    [SerializeField] private GameObject ResultScreen;
    [SerializeField] private GameObject StartScreen;
    [SerializeField] private GameObject VideoLayer;

    [Header("Result Screen Pages")]
    [SerializeField] private GameObject SuccessPage;
    [SerializeField] private GameObject ErrorMessage;
    //[SerializeField] private GameObject BlackScreen;

    [Header("Reset Audio")]
    public AudioSource clickSound;
    public AudioClip clickClip;
    public AudioClip LaunchClip;

    [Header("Validation State")]
    private bool rocketValidated = false;

    private Coroutine launchCoroutine;
    private Vector3 initialPosition;

    [Header("Launch Video")]
    public VideoPlayer videoPlayer;

    public VideoClip video4Part;
    public VideoClip video7Large;
    public VideoClip video10Part;

    [Header("Video Pause Control")]
    private long pauseBeforeFrame;    // <-- your X frame here
    private bool pauseTriggered = false;
    public ErrorDeispaly errorDisplay;


    void Start()
    {
        initialPosition = transform.position;
        lastInputTime = Time.time;

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;

            videoPlayer.errorReceived += (vp, msg) =>
            {
                Debug.LogError("🎬 VIDEO ERROR: " + msg);
            };

            videoPlayer.started += _ =>
            {
                Debug.Log("🎬 Video started successfully");
            };
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }
    void Update()
    {
        DetectUserActivity();

        // ----- RESULT SCREEN IDLE -----
        if (ResultScreen.activeSelf &&
            Time.time - lastInputTime >= resultIdleLimit)
        {
            Debug.Log("⏳ Result idle timeout → StartScreen");
            ResetGame();
            ResultScreen.SetActive(false);
            StartScreen.SetActive(true);
            
            lastInputTime = Time.time;
        }

        // ----- ASSEMBLY SCREEN IDLE -----
        if (assemblyScreen.activeSelf &&
            Time.time - lastInputTime >= idleLimit)
        {
            Debug.Log("⏳ Assembly idle timeout → ResetGame()");
            ResetGame();
            lastInputTime = Time.time;
        }

        // ----- VIDEO LOGIC -----
        if (videoPlayer == null) return;
        if (!videoPlayer.isPlaying) return;
        if (pauseTriggered) return;

        if (videoPlayer.frame >= pauseBeforeFrame)
        {
            videoPlayer.Pause();
            pauseTriggered = true;
            ShowResultAfterPause();
        }
    }



    void DetectUserActivity()
    {
        // Only REAL interaction should reset idle timer
        if (Input.anyKeyDown ||
            Input.GetMouseButtonDown(0) ||
            Input.touchCount > 0)
        {
            lastInputTime = Time.time;
        }
    }






    public void Abort()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        PlayResetSound();
    }

    public void ResetGame()
    {
        Debug.Log("🔄 Resetting game");
        PlayResetSound();

        if (launchCoroutine != null)
        {
            StopCoroutine(launchCoroutine);
            launchCoroutine = null;
        }

        transform.position = initialPosition;

        rocketValidated = false;
        canLaunch = true;

        cameraFocus?.StopFocus();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        Debug.Log("✅ Game reset complete");
        assemblyScreen?.SetActive(false);
        StartScreen?.SetActive(true);
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            //videoPlayer.gameObject.SetActive(false);
        }
    }

    public void PlayResetSound()
    {
        if (clickSound == null || clickClip == null)
        {
            Debug.LogWarning("Reset audio missing");
            return;
        }

        clickSound.PlayOneShot(clickClip);
    }

    public void StartLaunch()
    {
        pauseBeforeFrame = PartDrag.pauseBeforeFrame;

        if (!PartDrag.ValidateAssembly(out string error))
        {
            Debug.LogError("❌ Rocket INVALID: " + error);
            return;
        }

        Debug.Log("✅ Rocket VALID");

        RocketType type = GetRocketType();
        PlayLaunchVideo(type);
    }

    public void OnMoveToLaunchPadClicked()
    {
        if (rocketValidated)
            return;

        if (!PartDrag.ValidateAssembly(out string error))
        {
            ErrorPanel?.SetActive(true);
            ErrorBox.GetComponent<ErrorDisplay>()?.SetText(error);
            ErrorPan?.SetActive(true);
        }
        else
        {
            rocketValidated = true;
            Debug.Log("✅ Rocket VALID — moving to launch pad");

            rocketLock?.LockRocket();

            assemblyScreen?.SetActive(false);
            VideoLayer?.SetActive(true);
            StartLaunch();

            Debug.Log("🚀 Rocket moved to launch pad");
        }
    }

    private IEnumerator LaunchRoutine()
    {
        float elapsed = 0f;
        float velocity = 0f;

        Vector3 position = transform.position;

        while (elapsed < launchDuration)
        {
            elapsed += Time.deltaTime;

            velocity += launchAcceleration * Time.deltaTime;

            position.y += velocity * Time.deltaTime;
            transform.position = position;

            yield return null;
        }

        cameraFocus?.StopFocus();
        Debug.Log("🚀 Launch sequence completed");
    }

    RocketType GetRocketType()
    {
        int count = PartDrag.assemblyParts.Count;
        Debug.Log("🔍 Assembly part count = " + count);

        if (count == 4)
        {
            Debug.Log("🚀 Detected 4-part rocket");
            return RocketType.Stage1_4;
        }

        if (count == 7)
        {
            var chain = PartDrag.GetMainChain();
            PartRole last = chain[chain.Count - 1];

            Debug.Log("🔍 7-part final stage = " + last);

            Debug.Log("🚀 Detected 7-part LargeThruster rocket");
            return RocketType.Stage2_7_Large;
        }

        if (count == 10)
        {
            Debug.Log("🚀 Detected 10-part Booster rocket");
            return RocketType.Booster_10;
        }

        Debug.LogWarning("⚠ Unexpected rocket configuration");
        return RocketType.Stage1_4;
    }

    void PlayLaunchVideo(RocketType type)
    {
        if (videoPlayer == null)
        {
            Debug.LogError("❌ VideoPlayer is not assigned.");
            return;
        }

        string fileName = "";

        switch (type)
        {
            case RocketType.Stage1_4:
                fileName = "1.mp4";
                break;

            case RocketType.Stage2_7_Large:
                fileName = "2.mp4";
                break;

            case RocketType.Booster_10:
                fileName = "3.mp4";
                break;
        }

        videoPlayer.gameObject.SetActive(true);

#if UNITY_WEBGL && !UNITY_EDITOR
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = Application.streamingAssetsPath + "/" + fileName;
#else
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = "file://" + Application.streamingAssetsPath + "/" + fileName;
#endif

        Debug.Log("🎬 Playing video: " + fileName);

        videoPlayer.Stop();
        pauseTriggered = false;

        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;
        vp.loopPointReached += OnVideoFinished;
        vp.Play();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (pauseTriggered) return; // prevents duplicate result screen

        Debug.Log("🎬 Video finished");

        vp.loopPointReached -= OnVideoFinished;

        vp.Stop();
        vp.gameObject.SetActive(false);

        ResultScreen?.SetActive(true);
        ShowResultUI();
    }
    

    void ShowResultUI()
    {
        //BlackScreen.SetActive(true);
        if (SuccessPage) SuccessPage.SetActive(false);
        if (ErrorMessage) ErrorMessage.SetActive(false);

        switch (PartDrag.resultNumber)
        {
            case 3:
                SuccessPage?.SetActive(true);
                break;

            default:
                ErrorMessage?.SetActive(true);
                errorDisplay?.UpdateErrorUI();
                break;
        }
    }

    void ShowResultAfterPause()
    {
        //if (videoPlayer != null)
        //{
        //    videoPlayer.gameObject.SetActive(false);
        //}

        ResultScreen?.SetActive(true);
        ShowResultUI();
    }

}
