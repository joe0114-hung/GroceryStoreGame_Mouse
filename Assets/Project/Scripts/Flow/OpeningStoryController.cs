using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project.Core;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.Flow
{
    /// <summary>
    /// 開場劇情與暖身/流程控制器
    /// 負責處理：故事劇情、任務總結、以及暖身流程（包含腳步定位、手部定位標靶、站力不動掃描與鐵捲門拉起等遊戲化邏輯）。
    /// </summary>
   
    public class OpeningStoryController : MonoBehaviour
    {
        private enum Stage { Story, TaskSummary, WarmUp, Done }
        private enum WarmUpStep { Step1_Slippers, Step2_LeftHand, Step3_RightHand, Step4_ShowOutfit, Step5_LockHandles, Step6_PullUp, Complete }


        [Header("=== 三個階段的畫面 Panel ===")]
        [SerializeField] private GameObject storyPanel;
        [SerializeField] private GameObject taskSummaryPanel;
        [SerializeField] private GameObject warmUpPanel;


        [Header("=== 舊有按鈕與開發跳過按鈕 ===")]
        [SerializeField] private Button storyContinueButton;
        [SerializeField] private Button taskSummaryContinueButton;
        [SerializeField] private Button devSkipButton;


        [Header("=== WarmUp 階段：場景與目標 UI ===")]
        [SerializeField] private GameObject mirrorObject;  
        [SerializeField] private Image slippersUI;          
        [SerializeField] private Image slippersProgressRing;
        [SerializeField] private Image vestUI;              
        [SerializeField] private Image hatUI;              
        [SerializeField] private RectTransform rollingDoorRect;
        [SerializeField] private Image cameraFrameUI;      
       
        [Header("=== 標靶 (手印) ===")]
        [SerializeField] private Image leftHandPromptUI;    
        [SerializeField] private Image rightHandPromptUI;  
       
        [Header("=== 標靶發亮進度條 ===")]
        [SerializeField] private Image leftHandPromptProgressRing;
        [SerializeField] private Image rightHandPromptProgressRing;
       
        [Header("=== 跟隨手部移動的游標元件 ===")]
        [SerializeField] private RectTransform leftHandCursor;        
        [SerializeField] private Image leftHandCursorProgressRing;    
        [SerializeField] private RectTransform rightHandCursor;      
        [SerializeField] private Image rightHandCursorProgressRing;  




        [Header("OAK 相機校正基準")]
        public float oakReferenceWidth = 1920f;
        public float oakReferenceHeight = 1080f;


        [Header("=== 掃描與蓋章特效 (Step 4) ===")]
        [SerializeField] private RectTransform scanLineRect;
        [SerializeField] private GameObject goldenStampUI;  


        [Header("=== 音效與語音素材 ===")]
        [SerializeField] private AudioSource voiceAudio;
        [SerializeField] private AudioSource sfxAudio;  
        [SerializeField] private AudioClip voiceSlippers;  
        [SerializeField] private AudioClip voiceVestHat;    
        [SerializeField] private AudioClip voiceRollingDoor;
        [SerializeField] private AudioClip rollingDoorSfx;
       
        [SerializeField] private AudioClip voiceScanPrompt;  
        [SerializeField] private AudioClip voiceScanSuccess;
        [SerializeField] private AudioClip sfxDingDong;      
        [SerializeField] private AudioClip sfxApplause;      


        /// <summary> 當前所處的主階段 </summary>
        private Stage _currentStage;
        private WarmUpStep _currentWarmUpStep;
        private OAKVideoStream _videoStream;


        // --- 時間累計與判定條件 ---
        private float _leftHandTimer = 0f;
        private float _rightHandTimer = 0f;
        private const float RequiredHoldTime = 2.0f;
       
        private float _footAlignmentTimer = 0f;
        private const float RequiredFootHoldTime = 2.0f;
       
        private bool _leftHandActive = false;
        private bool _rightHandActive = false;


        // --- 服裝掃描與姿勢判定 ---
        private float _poseTimer = 0f;
        private Vector2 _lastLeftHandPos;
        private Vector2 _lastRightHandPos;
        private const float PoseThreshold = 40f;
        private bool _isScanning = false;
        private bool _isOutfitFrozen = false;


        // --- UI 初始狀態記錄 ---
        private Transform _initialPromptParent;
        private Vector2 _initialLeftPromptPos;
        private Vector2 _initialRightPromptPos;


        private void Awake()
        {
            //綁定按鈕點擊事件
            if (storyContinueButton != null) storyContinueButton.onClick.AddListener(EnterTaskSummaryStage);
            if (taskSummaryContinueButton != null) taskSummaryContinueButton.onClick.AddListener(EnterWarmUpStage);
            if (devSkipButton != null) devSkipButton.onClick.AddListener(FinishOpeningStory);


            //獲取 OAKVideoStream
            _videoStream = FindAnyObjectByType<OAKVideoStream>();
            if (_videoStream == null && warmUpPanel != null)
            {
                var cameraFeed = warmUpPanel.transform.Find("CameraFeed");
                if (cameraFeed != null) _videoStream = cameraFeed.gameObject.AddComponent<OAKVideoStream>();
            }


            //記錄 UI 初始位置與父層階層
            if (leftHandPromptUI != null)
            {
                _initialPromptParent = leftHandPromptUI.transform.parent;
                _initialLeftPromptPos = leftHandPromptUI.rectTransform.anchoredPosition;
            }
            if (rightHandPromptUI != null)
            {
                _initialRightPromptPos = rightHandPromptUI.rectTransform.anchoredPosition;
            }
        }




        private void Start() { EnterStoryStage(); }




        private void EnterStoryStage()
        {
            _currentStage = Stage.Story;
            SetPanels(story: true, task: false, warmUp: false);
        }




        private void EnterTaskSummaryStage()
        {
            _currentStage = Stage.TaskSummary;
            SetPanels(story: false, task: true, warmUp: false);
        }




        private void EnterWarmUpStage()
        {
            _currentStage = Stage.WarmUp;
            SetPanels(story: false, task: false, warmUp: true);


            //UI 與 視訊/體感物件開關


            //關閉停留游標 (Dwell Cursor)
            if (Project.UI.DwellCursor.Instance != null)
                Project.UI.DwellCursor.Instance.SetCursorActive(false);
           
            //顯現熱身需要的相機畫面與框架
            if (mirrorObject != null) mirrorObject.SetActive(true);
            if (cameraFrameUI != null) cameraFrameUI.gameObject.SetActive(true);
            if (rollingDoorRect != null) rollingDoorRect.gameObject.SetActive(false);
            if (scanLineRect != null) scanLineRect.gameObject.SetActive(false);
            if (goldenStampUI != null) goldenStampUI.SetActive(false);
           
            SetImageAlpha(vestUI, 0f);
            SetImageAlpha(hatUI, 0f);  
           
            if (leftHandPromptUI != null)
            {
                leftHandPromptUI.transform.SetParent(_initialPromptParent);
                leftHandPromptUI.rectTransform.anchoredPosition = _initialLeftPromptPos;
                leftHandPromptUI.gameObject.SetActive(false);
            }
            if (rightHandPromptUI != null)
            {
                rightHandPromptUI.transform.SetParent(_initialPromptParent);
                rightHandPromptUI.rectTransform.anchoredPosition = _initialRightPromptPos;
                rightHandPromptUI.gameObject.SetActive(false);
            }
           
            SetCursorVisibility(false, false);

            _leftHandActive = false;
            _rightHandActive = false;
            _isOutfitFrozen = false;

            StartWarmUpStep1();
        }


        //站在腳印上2秒(轉圈發亮)
        private void StartWarmUpStep1()
        {
            _currentWarmUpStep = WarmUpStep.Step1_Slippers;
            if (slippersUI != null) slippersUI.gameObject.SetActive(true);
            if (slippersProgressRing != null) slippersProgressRing.fillAmount = 0f;
            PlayVoice(voiceSlippers);
        }


        //左手放到手印框2秒，左手吸附鼠標
        private void StartWarmUpStep2_LeftHand()
        {
            _currentWarmUpStep = WarmUpStep.Step2_LeftHand;
            if (leftHandPromptUI != null) leftHandPromptUI.gameObject.SetActive(true);
            SetCursorVisibility(false, false);
            PlayVoice(voiceVestHat);
        }


        //右手放到手印框2秒，右手吸附鼠標
        private void StartWarmUpStep3_RightHand()
        {
            _currentWarmUpStep = WarmUpStep.Step3_RightHand;
            if (leftHandPromptUI != null) leftHandPromptUI.gameObject.SetActive(false);
            if (rightHandPromptUI != null) rightHandPromptUI.gameObject.SetActive(true);
            SetCursorVisibility(true, false);
        }


        //左右手都吸附後穿上背心和帽子進行掃描
        private void StartWarmUpStep4_ShowOutfit()
        {
            _currentWarmUpStep = WarmUpStep.Step4_ShowOutfit;
            _isScanning = false;
            _poseTimer = 0f;
           
            if (rightHandPromptUI != null) rightHandPromptUI.gameObject.SetActive(false);
            if (slippersUI != null) slippersUI.gameObject.SetActive(false);
           
            // 穿上衣服開始掃描時，隱藏雙手游標
            SetCursorVisibility(false, false);
            SetImageAlpha(vestUI, 1.0f);
            SetImageAlpha(hatUI, 1.0f);
           


            if (scanLineRect != null)
            {
                scanLineRect.gameObject.SetActive(true);
                scanLineRect.anchoredPosition = new Vector2(0, 500f);
            }
           
            PlayVoice(voiceScanPrompt);
           //紀錄當前雙手在螢幕上的鏡像座標 (用於後續判定玩家是否維持姿勢/凍結畫面)
            if (OAKInputReceiver.Instance != null) {
                _lastLeftHandPos = GetMirroredScreenPos(OAKInputReceiver.Instance.LeftHandScreenPos);
                _lastRightHandPos = GetMirroredScreenPos(OAKInputReceiver.Instance.RightHandScreenPos);
            }
        }


        private void StartWarmUpStep5_LockHandles()
        {
            _currentWarmUpStep = WarmUpStep.Step5_LockHandles;
           
            if (mirrorObject != null) mirrorObject.SetActive(false);
            if (cameraFrameUI != null) cameraFrameUI.gameObject.SetActive(false);
           
            SetImageAlpha(vestUI, 0f);
            SetImageAlpha(hatUI, 0f);
            if (goldenStampUI != null) goldenStampUI.SetActive(false);




            if (rollingDoorRect != null)
            {
                rollingDoorRect.gameObject.SetActive(true);
                rollingDoorRect.anchoredPosition = new Vector2(rollingDoorRect.anchoredPosition.x, -100f);
            }




            if (leftHandPromptUI != null && rollingDoorRect != null)
            {
                leftHandPromptUI.gameObject.SetActive(true);
                leftHandPromptUI.transform.SetParent(rollingDoorRect, false);
                leftHandPromptUI.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                leftHandPromptUI.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                leftHandPromptUI.rectTransform.anchoredPosition = new Vector2(-280f, 100f);
            }
            if (rightHandPromptUI != null && rollingDoorRect != null)
            {
                rightHandPromptUI.gameObject.SetActive(true);
                rightHandPromptUI.transform.SetParent(rollingDoorRect, false);
                rightHandPromptUI.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rightHandPromptUI.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rightHandPromptUI.rectTransform.anchoredPosition = new Vector2(280f, 100f);
            }




            // 鐵門降下來後，恢復顯示雙手游標，讓玩家知道在哪裡
            SetCursorVisibility(true, true);
            PlayVoice(voiceRollingDoor);
        }


        //把鐵門往上拉準備進入關卡前情提要
        private void StartWarmUpStep6_PullUp()
        {
            _currentWarmUpStep = WarmUpStep.Step6_PullUp;
            SetImageAlpha(leftHandPromptUI, 1.0f);
            SetImageAlpha(rightHandPromptUI, 1.0f);
        }


        private void Update()
        {
            if (_currentStage != Stage.WarmUp) return;

            UpdateCursorPositions();

            if (!_isOutfitFrozen && vestUI != null && vestUI.color.a > 0 && hatUI != null && hatUI.color.a > 0)
            {
                UpdateOutfitPositions();
            }

            float pulse = 0.4f + Mathf.PingPong(Time.time * 2f, 0.6f);
            if (_currentWarmUpStep == WarmUpStep.Step2_LeftHand) SetImageAlpha(leftHandPromptUI, pulse);
            if (_currentWarmUpStep == WarmUpStep.Step3_RightHand) SetImageAlpha(rightHandPromptUI, pulse);
            if (_currentWarmUpStep == WarmUpStep.Step5_LockHandles)
            {
                SetImageAlpha(leftHandPromptUI, pulse);
                SetImageAlpha(rightHandPromptUI, pulse);
            }


            switch (_currentWarmUpStep)
            {
                case WarmUpStep.Step1_Slippers:
                    if (CheckPlayerOnSlippers()) StartWarmUpStep2_LeftHand();
                    break;
                case WarmUpStep.Step2_LeftHand:
                    if (CheckHandAlignment(leftHandPromptUI, leftHandPromptProgressRing, ref _leftHandTimer, ref _leftHandActive, true))
                        StartWarmUpStep3_RightHand();
                    break;
                case WarmUpStep.Step3_RightHand:
                    if (CheckHandAlignment(rightHandPromptUI, rightHandPromptProgressRing, ref _rightHandTimer, ref _rightHandActive, false))
                        StartWarmUpStep4_ShowOutfit();
                    break;
                case WarmUpStep.Step4_ShowOutfit:
                    if (!_isScanning && CheckPoseStillness())
                    {
                        StartCoroutine(ScanSuccessRoutine());
                    }
                    break;
                case WarmUpStep.Step5_LockHandles:
                    if (CheckDualHandsLockedOnHandles()) StartWarmUpStep6_PullUp();
                    break;
                case WarmUpStep.Step6_PullUp:
                    if (CheckDualHandsPulledUp()) StartCoroutine(AnimateRollingDoorOpen());
                    break;
            }
        }


        //判定玩家是否保持姿勢不動
        private bool CheckPoseStillness()
        {
            if (OAKInputReceiver.Instance == null) return false;


            Vector2 currentLeft = GetMirroredScreenPos(OAKInputReceiver.Instance.LeftHandScreenPos);
            Vector2 currentRight = GetMirroredScreenPos(OAKInputReceiver.Instance.RightHandScreenPos);


            if (OAKInputReceiver.Instance.currentMode == OAKInputReceiver.InputMode.MouseOnly_滑鼠測試模式)
            {
                currentLeft = OAKInputReceiver.Instance.GeneratedScreenPosition;
                currentRight = OAKInputReceiver.Instance.GeneratedScreenPosition;
            }


            float leftMove = Vector2.Distance(currentLeft, _lastLeftHandPos);
            float rightMove = Vector2.Distance(currentRight, _lastRightHandPos);


            if (leftMove < PoseThreshold && rightMove < PoseThreshold)
            {
                _poseTimer += Time.deltaTime;
               
                if (scanLineRect != null)
                {
                    float progress = _poseTimer / 3.0f;
                    scanLineRect.anchoredPosition = Vector2.Lerp(new Vector2(0, 500f), new Vector2(0, -500f), progress);
                }



                if (_poseTimer >= 3.0f) return true;
            }
            else
            {
                _poseTimer = 0f;
                if (scanLineRect != null) scanLineRect.anchoredPosition = new Vector2(0, 500f);
            }




            _lastLeftHandPos = currentLeft;
            _lastRightHandPos = currentRight;
            return false;
        }


        //負責處理當玩家保持姿勢 2 秒掃描成功後的獎勵特效與過場動畫
        private IEnumerator ScanSuccessRoutine()
        {
            _isScanning = true;
            _isOutfitFrozen = true;


            // --- 凍結/暫停相機串流，防止畫面繼續更新 ---
            if (_videoStream != null)
            {
                _videoStream.enabled = false; // 暫停影片串流組件，畫面會定格在最後一幀
            }

            if (scanLineRect != null) scanLineRect.gameObject.SetActive(false);




            // 音效與語音同步啟動
            if (sfxAudio != null) {
                if (sfxDingDong != null) sfxAudio.PlayOneShot(sfxDingDong);
                if (sfxApplause != null) sfxAudio.PlayOneShot(sfxApplause);
            }
            PlayVoice(voiceScanSuccess); // 移至此處，讓語音與音效/蓋章同步


            // 蓋章動畫 (耗時 0.5 秒)
            if (goldenStampUI != null)
            {
                goldenStampUI.SetActive(true);
                goldenStampUI.transform.localScale = Vector3.one * 2.5f; // 初始放大為 2.5 倍
               
                float duration = 0.5f;
                float t = 0f;
               
                // 蓋章動畫：前 0.3 秒重重蓋下，後 0.2 秒微微回彈
                while (t < duration)
                {
                    t += Time.deltaTime;
                    float progress = Mathf.Clamp01(t / duration);
                    float smoothProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);
                   
                    goldenStampUI.transform.localScale = Vector3.Lerp(Vector3.one * 2.5f, Vector3.one, smoothProgress);
                    yield return null;
                }


                // 強制校正，確保尺寸精準為 1 倍
                goldenStampUI.transform.localScale = Vector3.one;
            }


            // --- 總定格 3 秒 (扣除動畫 0.5 秒，這裡等 2.5 秒即可) ---
            yield return new WaitForSeconds(2.5f);
           
            // 進入 Step 5（關閉鏡頭與服裝、下拉鐵門）
            StartWarmUpStep5_LockHandles();
        }


        //把 OAK 相機傳進來的原始畫面座標，轉換成目前遊戲畫面上正確對應的像素座標
        private Vector2 GetMirroredScreenPos(Vector2 originalPos)
        {
            if (OAKInputReceiver.Instance == null) return originalPos;
            float normX = originalPos.x / oakReferenceWidth;
            float normY = originalPos.y / oakReferenceHeight;
            if (OAKInputReceiver.Instance.currentMode == OAKInputReceiver.InputMode.OAKCamera_相機偵測模式)
            {
                normX = 1f - normX;
            }
            return new Vector2(normX * Screen.width, normY * Screen.height);
        }



        //  實時取得頭部與身體的螢幕座標，並將「帽子 UI (hatUI)」與「背心 UI (vestUI)」精準貼合到對應的身體位置上
        private void UpdateOutfitPositions()
        {
            if (OAKInputReceiver.Instance == null || _videoStream == null) return;


            Vector2 headPos = GetMirroredScreenPos(OAKInputReceiver.Instance.HeadScreenPos);
            Vector2 torsoPos = GetMirroredScreenPos(OAKInputReceiver.Instance.TorsoScreenPos);


            if (hatUI != null)
            {
                RectTransform parentRect = hatUI.transform.parent as RectTransform;
                if (parentRect != null) {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, headPos, null, out Vector2 localHead);
                    hatUI.rectTransform.anchoredPosition = localHead;
                }
            }
            if (vestUI != null)
            {
                RectTransform parentRect = vestUI.transform.parent as RectTransform;
                if (parentRect != null) {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, torsoPos, null, out Vector2 localTorso);
                    vestUI.rectTransform.anchoredPosition = localTorso;
                }
            }
        }


        //控制左右手游標 (Cursor) 的顯示與隱藏，並在切換顯示狀態時自動重置相關的計時器與進度環 UI
        private void SetCursorVisibility(bool showLeft, bool showRight)
        {
            if (leftHandCursor != null) leftHandCursor.gameObject.SetActive(showLeft);
            if (rightHandCursor != null) rightHandCursor.gameObject.SetActive(showRight);
           
            if (leftHandCursorProgressRing != null) leftHandCursorProgressRing.fillAmount = 0f;
            if (rightHandCursorProgressRing != null) rightHandCursorProgressRing.fillAmount = 0f;
            _leftHandTimer = 0f;
            _rightHandTimer = 0f;
        }



        //  即時讀取相機偵測到的雙手位置
        private void UpdateCursorPositions()
        {
            if (OAKInputReceiver.Instance == null || _videoStream == null) return;
           
            Vector2 leftPos = GetMirroredScreenPos(OAKInputReceiver.Instance.LeftHandScreenPos);
            Vector2 rightPos = GetMirroredScreenPos(OAKInputReceiver.Instance.RightHandScreenPos);


            if (leftHandCursor != null && leftHandCursor.gameObject.activeSelf)
            {
                RectTransform parentRect = leftHandCursor.parent as RectTransform;
                if (parentRect != null) {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, leftPos, null, out Vector2 localLeft);
                    leftHandCursor.anchoredPosition = localLeft;
                }
            }
            if (rightHandCursor != null && rightHandCursor.gameObject.activeSelf)
            {
                RectTransform parentRect = rightHandCursor.parent as RectTransform;
                if (parentRect != null) {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, rightPos, null, out Vector2 localRight);
                    rightHandCursor.anchoredPosition = localRight;
                }
            }
        }
       
       //判斷玩家是否「站在指定的鞋子區域（slippersUI）上」，並進行時間累積與進度條更新
        private bool CheckPlayerOnSlippers()
        {
            bool isStanding = false;
            if (OAKInputReceiver.Instance != null && OAKInputReceiver.Instance.currentMode == OAKInputReceiver.InputMode.OAKCamera_相機偵測模式)
            {
                if (slippersUI != null)
                {
                    Vector2 leftFootPos = GetMirroredScreenPos(OAKInputReceiver.Instance.LeftFootScreenPos);
                    Vector2 rightFootPos = GetMirroredScreenPos(OAKInputReceiver.Instance.RightFootScreenPos);
                    bool leftFootIn = RectTransformUtility.RectangleContainsScreenPoint(slippersUI.rectTransform, leftFootPos);
                    bool rightFootIn = RectTransformUtility.RectangleContainsScreenPoint(slippersUI.rectTransform, rightFootPos);
                    //兩隻腳都站在裡面isStanding 才會被設為 true
                    if (leftFootIn && rightFootIn) isStanding = true;
                }
            }
           
            if (IsKeyPressed(1)) isStanding = true;




            if (isStanding)
            {
                _footAlignmentTimer += Time.deltaTime;
                if (slippersProgressRing != null) slippersProgressRing.fillAmount = _footAlignmentTimer / RequiredFootHoldTime;




                if (_footAlignmentTimer >= RequiredFootHoldTime)
                {
                    _footAlignmentTimer = 0f;
                    return true;
                }
            }
            else
            {
                _footAlignmentTimer = 0f;
                if (slippersProgressRing != null) slippersProgressRing.fillAmount = 0f;
            }
            return false;
        }


        //檢查玩家的「左手」或「右手」是否移到了特定的 UI 提示圖標 (targetPrompt) 上，並且停留滿指定時間
        private bool CheckHandAlignment(Image targetPrompt, Image promptProgressRing, ref float timer, ref bool isActive, bool isLeft)
        {
            if (isActive) return true;


            bool isTouching = false;
            if (OAKInputReceiver.Instance != null && targetPrompt != null)
            {
                Vector2 handScreenPos = isLeft ? OAKInputReceiver.Instance.LeftHandScreenPos : OAKInputReceiver.Instance.RightHandScreenPos;
                handScreenPos = GetMirroredScreenPos(handScreenPos);


                if (OAKInputReceiver.Instance.currentMode == OAKInputReceiver.InputMode.MouseOnly_滑鼠測試模式)
                {
                    handScreenPos = OAKInputReceiver.Instance.GeneratedScreenPosition;
                }


                Camera uiCam = targetPrompt.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetPrompt.canvas.worldCamera;
                Vector2 targetScreenPos = RectTransformUtility.WorldToScreenPoint(uiCam, targetPrompt.rectTransform.position);


                float distance = Vector2.Distance(handScreenPos, targetScreenPos);
                float lockRadius = 200f;
                isTouching = distance < lockRadius;
            }




            if (isTouching || IsKeyPressed(2))
            {
                timer += Time.deltaTime;
                if (promptProgressRing != null) promptProgressRing.fillAmount = timer / RequiredHoldTime;
               
                if (timer >= RequiredHoldTime) {
                    timer = 0f;
                    isActive = true;
                    SetCursorVisibility(isLeft, !isLeft);
                    return true;
                }
            }
            else
            {
                timer = 0f;
                if (promptProgressRing != null) promptProgressRing.fillAmount = 0f;
            }
            return false;
        }




        private bool CheckDualHandsLockedOnHandles()
        {
            bool isTouching = false;
           
            // 進入把手階段，確保手部游標是顯示狀態
            if (!leftHandCursor.gameObject.activeSelf || !rightHandCursor.gameObject.activeSelf)
            {
                SetCursorVisibility(true, true);
            }


            if (OAKInputReceiver.Instance != null && leftHandPromptUI != null && rightHandPromptUI != null)
            {
                Vector2 leftHandPos = GetMirroredScreenPos(OAKInputReceiver.Instance.LeftHandScreenPos);
                Vector2 rightHandPos = GetMirroredScreenPos(OAKInputReceiver.Instance.RightHandScreenPos);


                if (OAKInputReceiver.Instance.currentMode == OAKInputReceiver.InputMode.MouseOnly_滑鼠測試模式)
                {
                    leftHandPos = OAKInputReceiver.Instance.GeneratedScreenPosition;
                    rightHandPos = OAKInputReceiver.Instance.GeneratedScreenPosition;
                }


                Camera uiCamLeft = leftHandPromptUI.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : leftHandPromptUI.canvas.worldCamera;
                Camera uiCamRight = rightHandPromptUI.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rightHandPromptUI.canvas.worldCamera;
                Vector2 targetLeftScreen = RectTransformUtility.WorldToScreenPoint(uiCamLeft, leftHandPromptUI.rectTransform.position);
                Vector2 targetRightScreen = RectTransformUtility.WorldToScreenPoint(uiCamRight, rightHandPromptUI.rectTransform.position);


                float lockRadius = 120f;


                // 左手摸左把手 + 右手摸右把手
                bool normalMatch = Vector2.Distance(leftHandPos, targetLeftScreen) < lockRadius &&
                                Vector2.Distance(rightHandPos, targetRightScreen) < lockRadius;


                if (normalMatch) isTouching = true;
            }
           
            if (isTouching || IsKeyPressed(3))
            {
                _leftHandTimer += Time.deltaTime;
                float progress = _leftHandTimer / RequiredHoldTime;
               
                // --- 轉動跟隨手的游標進度條 (Cursor Progress Ring) ---
                if (leftHandCursorProgressRing != null) leftHandCursorProgressRing.fillAmount = progress;
                if (rightHandCursorProgressRing != null) rightHandCursorProgressRing.fillAmount = progress;


                // --- 確保把手 UI 本身的進度條保持關閉/歸零 ---
                if (leftHandPromptProgressRing != null) leftHandPromptProgressRing.fillAmount = 0f;
                if (rightHandPromptProgressRing != null) rightHandPromptProgressRing.fillAmount = 0f;


                if (_leftHandTimer >= RequiredHoldTime)
                {
                    _leftHandTimer = 0f;
                    return true;
                }
            }
            else
            {
                _leftHandTimer = 0f;
                // 未觸碰時，重置游標與把手的進度條
                if (leftHandCursorProgressRing != null) leftHandCursorProgressRing.fillAmount = 0f;
                if (rightHandCursorProgressRing != null) rightHandCursorProgressRing.fillAmount = 0f;
               
                if (leftHandPromptProgressRing != null) leftHandPromptProgressRing.fillAmount = 0f;
                if (rightHandPromptProgressRing != null) rightHandPromptProgressRing.fillAmount = 0f;
            }
            return false;
        }


        //判斷玩家是否「雙手向上抬起/拉起（超過螢幕高度的一半）」
        private bool CheckDualHandsPulledUp()
        {
            if (OAKInputReceiver.Instance != null && OAKInputReceiver.Instance.currentMode == OAKInputReceiver.InputMode.OAKCamera_相機偵測模式)
            {
                if (OAKInputReceiver.Instance.LeftHandScreenPos.y > Screen.height * 0.7f &&
                    OAKInputReceiver.Instance.RightHandScreenPos.y > Screen.height * 0.7f)
                {
                    SetCursorVisibility(false, false);
                    return true;
                }
            }
           
            if (IsKeyDown(4))
            {
                SetCursorVisibility(false, false);
                return true;
            }
            return false;
        }



        private bool IsKeyPressed(int keyNumber)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            switch(keyNumber) {
                case 1: return Keyboard.current.digit1Key.isPressed;
                case 2: return Keyboard.current.digit2Key.isPressed;
                case 3: return Keyboard.current.digit3Key.isPressed;
                case 4: return Keyboard.current.digit4Key.isPressed;
                default: return false;
            }
#else
            switch(keyNumber) {
                case 1: return Input.GetKey(KeyCode.Alpha1);
                case 2: return Input.GetKey(KeyCode.Alpha2);
                case 3: return Input.GetKey(KeyCode.Alpha3);
                case 4: return Input.GetKey(KeyCode.Alpha4);
                default: return false;
            }
#endif
        }




        private bool IsKeyDown(int keyNumber)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            switch(keyNumber) {
                case 1: return Keyboard.current.digit1Key.wasPressedThisFrame;
                case 2: return Keyboard.current.digit2Key.wasPressedThisFrame;
                case 3: return Keyboard.current.digit3Key.wasPressedThisFrame;
                case 4: return Keyboard.current.digit4Key.wasPressedThisFrame;
                default: return false;
            }
#else
            switch(keyNumber) {
                case 1: return Input.GetKeyDown(KeyCode.Alpha1);
                case 2: return Input.GetKeyDown(KeyCode.Alpha2);
                case 3: return Input.GetKeyDown(KeyCode.Alpha3);
                case 4: return Input.GetKeyDown(KeyCode.Alpha4);
                default: return false;
            }
#endif
        }


        //鐵捲門開啓過場動畫
        private IEnumerator AnimateRollingDoorOpen()
        {
            _currentWarmUpStep = WarmUpStep.Complete;
            if (sfxAudio != null && rollingDoorSfx != null) sfxAudio.PlayOneShot(rollingDoorSfx);


            if (leftHandPromptUI != null) leftHandPromptUI.gameObject.SetActive(false);
            if (rightHandPromptUI != null) rightHandPromptUI.gameObject.SetActive(false);


            if (rollingDoorRect != null)
            {
                float duration = 2.0f;
                float elapsed = 0;
                Vector2 startPos = rollingDoorRect.anchoredPosition;
                Vector2 targetPos = new Vector2(startPos.x, startPos.y + 1100);


                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    t = t * t * (3f - 2f * t);
                    rollingDoorRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                    yield return null;
                }
            }



            FinishOpeningStory();
        }


        //完成開場故事與啟動通用游標
        private void FinishOpeningStory()
        {
            if (_currentStage == Stage.Done) return;
            _currentStage = Stage.Done;
           
            if (Project.UI.DwellCursor.Instance != null)
            {
                Project.UI.DwellCursor.Instance.SetCursorActive(true);
            }
           
            GameManager.Instance.NotifyOpeningStoryCompleted();
        }




        private void SetPanels(bool story, bool task, bool warmUp)
        {
            if (storyPanel != null) storyPanel.SetActive(story);
            if (taskSummaryPanel != null) taskSummaryPanel.SetActive(task);
            if (warmUpPanel != null) warmUpPanel.SetActive(warmUp);
        }


        private void SetImageAlpha(Image img, float alpha)
        {
            if (img == null) return;
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }


        private void PlayVoice(AudioClip clip)
        {
            if (voiceAudio != null && clip != null)
            {
                voiceAudio.Stop();
                voiceAudio.clip = clip;
                voiceAudio.Play();
            }
        }
    }
}









