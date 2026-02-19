using UnityEngine;
using UnityEngine.Video;

public class StreamingVideoPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    [Tooltip("Only write file name. Example: gif.mp4")]
    public string fileName;

    public bool playOnStart = true;

    void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer not assigned.");
            return;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("File name is empty.");
            return;
        }

        videoPlayer.source = VideoSource.Url;

#if UNITY_WEBGL && !UNITY_EDITOR
        videoPlayer.url = Application.streamingAssetsPath + "/" + fileName;
#else
        videoPlayer.url = "file://" + Application.streamingAssetsPath + "/" + fileName;
#endif

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.Prepare();
    }

    void OnPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnPrepared;

        if (playOnStart)
            vp.Play();
    }
}
