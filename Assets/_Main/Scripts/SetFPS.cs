using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    [Header("Cài đặt FPS")]
    [Tooltip("Số khung hình mục tiêu mỗi giây (Ví dụ: 30, 60, 120, hoặc -1 để không giới hạn)")]
    public int targetFPS = 60;

    void Start()
    {
        SetFPS();
    }

    // Tùy chọn: Hàm này giúp bạn cập nhật FPS ngay lập tức nếu bạn thay đổi giá trị 'targetFPS' trong Inspector khi game đang chạy
    void Update()
    {
        if (Application.targetFrameRate != targetFPS)
        {
            SetFPS();
        }
    }

    private void SetFPS()
    {
        // Tắt VSync là bắt buộc để Application.targetFrameRate có thể hoạt động
        QualitySettings.vSyncCount = 0;
        
        // Cố định mức FPS
        Application.targetFrameRate = targetFPS;
    }
}