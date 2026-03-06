using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(RawImage))]
[RequireComponent(typeof(VideoPlayer))]
public class UIVideoPlayer : MonoBehaviour
{
    private RawImage rawImage;
    private VideoPlayer videoPlayer;
    private RenderTexture videoRenderTexture;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        videoPlayer = GetComponent<VideoPlayer>();

        // 1. 初始状态隐藏 RawImage，防止加载时黑屏闪烁
        rawImage.color = new Color(1, 1, 1, 0); 

        // 2. 配置 VideoPlayer 基本参数
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.isLooping = true; // 根据需求设置
        
        // 3. 监听准备完成事件 (极度关键)
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    public void PlayVideo(string videoPathOrUrl)
    {
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoPathOrUrl; // 支持 StreamingAssets 路径或网络 URL
        videoPlayer.Prepare(); // 异步准备视频
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        // 4. 根据视频的真实分辨率，动态创建 RenderTexture
        int width = (int)vp.width;
        int height = (int)vp.height;

        // 核心参数：宽, 高, 深度(UI视频设为0极省内存), 颜色格式, 读写空间(解决发灰问题)
        videoRenderTexture = new RenderTexture(
            width, height, 0, 
            RenderTextureFormat.ARGB32, 
            RenderTextureReadWrite.sRGB // 如果项目是Linear空间且视频发白，这里必须是sRGB
        );
        videoRenderTexture.filterMode = FilterMode.Bilinear;

        // 5. 将生成的 RT 赋值给播放器和 UI
        vp.targetTexture = videoRenderTexture;
        rawImage.texture = videoRenderTexture;

        // 6. 播放并显示画面
        vp.Play();
        rawImage.color = Color.white; // 视频准备好后再显示，完美避开黑屏闪屏
    }

    void OnDestroy()
    {
        // 7. 终极防泄漏：彻底清理内存
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.Stop();
        }

        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release(); // 释放显存
            Destroy(videoRenderTexture);  // 销毁对象
            videoRenderTexture = null;
        }
    }
}