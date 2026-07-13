using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 腳本功能：全域大腦版 OAK-D 接收器完全體。
///          1. 解除開局執行緒鎖定，無條件啟動網路，防止換模式斷線。
///          2. 修正未收到相機封包時的 (0,0) 噴飛問題，強制錨定正中心。
///          3. ✨ 加入左右手獨立座標解析 (給鐵捲門雙手鎖定使用)
///          4. 👕 加入頭部與身體座標解析 (給虛擬服裝 AR 追蹤使用)
/// </summary>
public class OAKInputReceiver : MonoBehaviour
{
    public static OAKInputReceiver Instance;

    public enum InputMode
    {
        MouseOnly_滑鼠測試模式,
        OAKCamera_相機偵測模式
    }

    [Header("--- 模式切換一鍵開關 ---")]
    public InputMode currentMode = InputMode.MouseOnly_滑鼠測試模式;

    [Header("--- 核心元件自動綁定 ---")]
    [SerializeField] private ConstructionLevelManager levelManager;

    [Header("--- UDP 網路設定 ---")]
    public int port = 5005;

    private UdpClient udpClient;
    private Thread receiveThread;
    private string lastJsonMessage = "";
    private bool hasNewMessage = false;
    private readonly object lockObject = new object();

    private Vector2 smoothScreenPos;
    private bool hasReceivedFirstCameraPacket = false; 

    // ✨ 提供給 DwellCursor.cs 讀取的全域真理像素坐標 (單手游標)
    public Vector2 GeneratedScreenPosition { get; private set; }
    
    // ✨ 新增：儲存左右手真實螢幕座標，專門給鐵捲門雙手鎖定用
    public Vector2 LeftHandScreenPos { get; private set; }
    public Vector2 RightHandScreenPos { get; private set; }
    // 👣 新增：儲存左右腳真實螢幕座標
    public Vector2 LeftFootScreenPos { get; private set; }
    public Vector2 RightFootScreenPos { get; private set; }
    
    // 👕 新增：儲存頭部與身體螢幕座標，給衣服與帽子 AR 追蹤用
    public Vector2 HeadScreenPos { get; private set; }
    public Vector2 TorsoScreenPos { get; private set; }

    [System.Serializable]
    private class OAKPacket
    {
        public string @event;
        public int lane;
        public int hand_ratio_y;
        
        // 雙手獨立座標
        public int left_x;
        public int left_y;
        public int right_x;
        public int right_y;
        
        // 雙腳獨立座標
        public int left_foot_x;
        public int left_foot_y;
        public int right_foot_x;
        public int right_foot_y;
        
        // 👕 頭部與身體座標 (需配合 Python 端發送)
        public int head_x;
        public int head_y;
        public int torso_x;
        public int torso_y;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            ResetToCenter();

            // 讓大腦取得跨場景永生的特權
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (Project.Core.GameManager.Instance != null)
        {
            currentMode = Project.Core.GameManager.Instance.GlobalInputMode;
        }
        if (levelManager == null)
        {
            levelManager = FindAnyObjectByType<ConstructionLevelManager>();
        }
        
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log($"🚀 [OAK] UDP 網路接收中樞已成功在背景啟動，通訊埠：{port}");
    }

    private void ResetToCenter()
    {
        smoothScreenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        GeneratedScreenPosition = smoothScreenPos;
        LeftHandScreenPos = smoothScreenPos;
        RightHandScreenPos = smoothScreenPos;
        LeftFootScreenPos = smoothScreenPos;
        RightFootScreenPos = smoothScreenPos;
        HeadScreenPos = smoothScreenPos;
        TorsoScreenPos = smoothScreenPos;
    }

    private void ReceiveData()
    {
        try
        {
            udpClient = new UdpClient(port);
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                byte[] data = udpClient.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                lock (lockObject) { lastJsonMessage = text; hasNewMessage = true; }
            }
        }
        catch (Exception e) { Debug.LogWarning($"[OAK] UDP 錯誤: {e.Message}"); }
    }

    void Update()
    {
        // 💡 分流 A：滑鼠測試模式
        if (currentMode == InputMode.MouseOnly_滑鼠測試模式)
        {
            hasReceivedFirstCameraPacket = false; 
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                GeneratedScreenPosition = Mouse.current.position.ReadValue();
                LeftHandScreenPos = GeneratedScreenPosition;
                RightHandScreenPos = GeneratedScreenPosition;
                // 滑鼠模式下，假裝頭在滑鼠上方 150px，身體在滑鼠下方 100px
                HeadScreenPos = GeneratedScreenPosition + new Vector2(0, 150);
                TorsoScreenPos = GeneratedScreenPosition + new Vector2(0, -100);
            }
#else
            GeneratedScreenPosition = Input.mousePosition;
            LeftHandScreenPos = GeneratedScreenPosition;
            RightHandScreenPos = GeneratedScreenPosition;
            HeadScreenPos = GeneratedScreenPosition + new Vector2(0, 150);
            TorsoScreenPos = GeneratedScreenPosition + new Vector2(0, -100);
#endif
        }
        // 💡 分流 B：相機偵測模式
        else if (currentMode == InputMode.OAKCamera_相機偵測模式)
        {
            string jsonToParse = "";
            bool processData = false;
            lock (lockObject) { if (hasNewMessage) { jsonToParse = lastJsonMessage; hasNewMessage = false; processData = true; } }

            if (processData)
            {
                try
                {
                    OAKPacket packet = JsonUtility.FromJson<OAKPacket>(jsonToParse);

                    if (packet.@event == "SUBMIT_CONFIRM")
                    {
                        // 🌟 關鍵修正：不能依賴 Start() 找的舊變數，必須當下立刻在場景裡找！
                        ConstructionLevelManager currentLevelManager = FindAnyObjectByType<ConstructionLevelManager>();
                        
                        if (currentLevelManager != null)
                        {
                            Debug.Log("🙌 [OAK 網路指令] 收到雙手舉高事件，觸發第六關大腦 SubmitAnswer()！");
                            currentLevelManager.SubmitAnswer();
                        }
                        else
                        {
                            // 防呆：如果玩家在其他關卡舉起雙手，就忽略它
                            Debug.Log("⚠️ [OAK 網路指令] 收到雙手提交指令，但目前場景不在第六關。");
                        }
                    }
                
                    else if (packet.@event == "RAISE_HAND_BELL")
                    {
                        // 去場景中尋找等待畫面的管理器 (因為是跨場景，每次收到指令時即時找尋最安全)
                        WaitStartManager waitStartManager = FindAnyObjectByType<WaitStartManager>();
                        if (waitStartManager != null)
                        {
                            Debug.Log("🔔 [OAK 網路指令] 收到單手舉高事件，觸發鈴鐺！");
                            waitStartManager.OnHandRaisedDetected();
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ [OAK 網路指令] 收到敲鈴鐺指令，但當前場景不在等待畫面 (找不到 WaitStartManager)！");
                        }
                    }
                    else if (packet.@event == "UPDATE_LANE")
                    {
                        float rawPixelX = (packet.lane / 100f) * Screen.width;
                        float rawPixelY = (1f - (packet.hand_ratio_y / 100f)) * Screen.height;
                        
                        LeftHandScreenPos = new Vector2((packet.left_x / 100f) * Screen.width, (1f - (packet.left_y / 100f)) * Screen.height);
                        RightHandScreenPos = new Vector2((packet.right_x / 100f) * Screen.width, (1f - (packet.right_y / 100f)) * Screen.height);
                        
                        LeftFootScreenPos = new Vector2((packet.left_foot_x / 100f) * Screen.width, (1f - (packet.left_foot_y / 100f)) * Screen.height);
                        RightFootScreenPos = new Vector2((packet.right_foot_x / 100f) * Screen.width, (1f - (packet.right_foot_y / 100f)) * Screen.height);
                        
                        // 👕 轉換頭部與身體座標
                        HeadScreenPos = new Vector2((packet.head_x / 100f) * Screen.width, (1f - (packet.head_y / 100f)) * Screen.height);
                        TorsoScreenPos = new Vector2((packet.torso_x / 100f) * Screen.width, (1f - (packet.torso_y / 100f)) * Screen.height);

                        if (!hasReceivedFirstCameraPacket)
                        {
                            smoothScreenPos = new Vector2(rawPixelX, rawPixelY);
                            hasReceivedFirstCameraPacket = true;
                            Debug.Log("✨ [OAK] 成功與 Python 端握手！游標正式無縫同步相機座標！");
                        }
                        else
                        {
                            smoothScreenPos = Vector2.Lerp(smoothScreenPos, new Vector2(rawPixelX, rawPixelY), Time.deltaTime * 12f);
                        }
                    }
                }
                catch (Exception) { }
            }
            
            if (!hasReceivedFirstCameraPacket)
            {
                ResetToCenter();
            }
            else
            {
                GeneratedScreenPosition = smoothScreenPos;
            }
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
        if (udpClient != null) udpClient.Close();
    }
}