using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 腳本功能：優化版 OAK-D 影片串流接收器。
///          1. ✨ 智慧判斷：滑鼠模式下自動休眠不佔用網路 Port。
///          2. 加入執行緒鎖定（lock），防止多執行緒資料衝突閃退。
///          3. 修正 LoadImage 記憶體洩漏與 GC 卡頓問題。
///          4. 新增 UV 鏡像翻轉魔法，完美對齊全域骨架座標！
/// </summary>
public class OAKVideoStream : MonoBehaviour
{
    private RawImage targetDisplay;
    private UdpClient udpClient;
    private Thread receiveThread;
    
    private byte[] latestFrameBytes;
    private bool hasNewFrame = false;
    private readonly object lockObject = new object(); 

    private Texture2D tex;

    [Header("=== 視覺設定 ===")]
    [Tooltip("開啟後會將畫面水平翻轉，變成真實的鏡子！")]
    public bool isMirrored = true;

    void Start()
    {
        targetDisplay = GetComponent<RawImage>();

        // 🌟 核心修改：向全域大腦確認目前的模式
        bool isCameraMode = true;
        if (OAKInputReceiver.Instance != null)
        {
            isCameraMode = (OAKInputReceiver.Instance.currentMode == OAKInputReceiver.InputMode.OAKCamera_相機偵測模式);
        }

        // 🖱️ 如果是滑鼠模式：不開網路、讓鏡子變暗、腳本直接休眠
        if (!isCameraMode)
        {
            Debug.Log("🖱️ [OAK Video] 目前為滑鼠測試模式，鏡子畫面已自動休眠。");
            if (targetDisplay != null)
            {
                // 將鏡子畫面變成深灰色，讓玩家知道現在沒有相機畫面
                targetDisplay.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            }
            this.enabled = false; // 徹底關閉 Update，節省遊戲效能
            return; // 提早結束，絕對不去開啟 UDP Thread！
        }

        // 📷 以下為相機模式專屬啟動流程：
        
        // 建立一張初始貼圖
        tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
        if (targetDisplay != null)
        {
            targetDisplay.color = Color.white; // 確保顏色是正常的
            targetDisplay.texture = tex;

            // 超級魔法：透過修改 UV 座標直接翻轉貼圖，0 效能損耗！
            if (isMirrored)
            {
                targetDisplay.uvRect = new Rect(1, 0, -1, 1);
            }
            else
            {
                targetDisplay.uvRect = new Rect(0, 0, 1, 1);
            }
        }

        // 開啟背景執行緒接收 Port 5006 的影像資料
        receiveThread = new Thread(ReceiveData) { IsBackground = true };
        receiveThread.Start();
    }

    void ReceiveData()
    {
        try
        {
            udpClient = new UdpClient(5006);
            var anyIP = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                byte[] data = udpClient.Receive(ref anyIP);
                
                lock (lockObject)
                {
                    latestFrameBytes = data;
                    hasNewFrame = true;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OAK Video] UDP 接收中斷: {e.Message}");
        }
    }

    void Update()
    {
        byte[] bytesToLoad = null;
        bool shouldUpdate = false;

        lock (lockObject)
        {
            if (hasNewFrame && latestFrameBytes != null)
            {
                bytesToLoad = latestFrameBytes;
                hasNewFrame = false;
                shouldUpdate = true;
            }
        }

        if (shouldUpdate && bytesToLoad != null)
        {
            tex.LoadImage(bytesToLoad);
            tex.Apply();
        }
    }

    void OnDestroy()
    {
        try
        {
            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Abort();
            }
        }
        catch { }

        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}