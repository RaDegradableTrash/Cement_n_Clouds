using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Phone-shaped, clickable pause UI for the Voyage field terminal.</summary>
[DefaultExecutionOrder(8000)]
public sealed class VoyagePhonePauseMenu : MonoBehaviour
{
    const string PhoneResourcePath = "PhonePause/TerminalPhone";
    const float PhoneScale = 0.84f;
    const float MainScreenMaxYaw = 14f;
    const float OutsideScreenMaxYaw = 100f;
    const float MainScreenMaxPitch = 3.5f;
    const float OutsideScreenMaxPitch = 14f;
    const float PhoneRotationSmoothTime = 0.18f;
    const float PhoneEntryDuration = 0.95f;
    const float PauseDimAlpha = 0.58f;
    static readonly Color ScreenColor = Hex("080C11");
    static readonly Color Panel = Hex("101B22");
    static readonly Color PanelRaised = Hex("17272F");
    static readonly Color TextMain = Hex("EAF4F3");
    static readonly Color TextMuted = Hex("8BA4A7");
    static readonly Color Sea = Hex("56D6C0");
    static readonly Color Coral = Hex("EE926D");
    static readonly Color Gold = Hex("F0C47A");

    DrivingCore core;
    Canvas overlay;
    RectTransform overlayRect;
    Image dimOverlay;
    RectTransform mainScreenRect;
    RectTransform statusRect;
    Text timeText;
    Text modeValue;
    Text speedValue;
    Text fuelValue;
    Text mapXValue;
    Text mapZValue;
    Text audioValueText;
    Slider volumeSlider;
    Transform pageRoot;
    Button resumeButton;
    GameObject phoneInstance;
    Camera phoneCamera;
    Transform mainPlane;
    Transform statusPlane;
    MeshFilter mainPlaneMesh;
    MeshFilter statusPlaneMesh;
    Transform volumeUpHardwareButton;
    Transform volumeDownHardwareButton;
    Transform quitHardwareButton;
    Canvas mainScreenCanvas;
    Canvas statusScreenCanvas;
    Quaternion phoneBaseRotation = Quaternion.identity;
    Vector3 phoneScreenCenterLocal;
    Vector3 phoneTargetCenter = new Vector3(0f, 0f, 2.05f);
    Font runtimeFont;
    Sprite roundedIconSprite;
    Texture2D roundedIconTexture;
    float phoneYaw;
    float phoneYawVelocity;
    float phonePitch;
    float phonePitchVelocity;
    float tiltInfluence;
    bool mainScreenUiAttached;
    bool statusUiAttached;
    bool visible;
    bool entrancePlaying;
    bool quitPending;
    Coroutine entranceRoutine;
    bool cursorWasVisible;
    CursorLockMode previousCursorLock;
    float refreshAt;
    readonly List<Material> runtimeMaterials = new List<Material>();
    readonly Dictionary<Transform, Coroutine> hardwareButtonAnimations = new Dictionary<Transform, Coroutine>();
    readonly Dictionary<RectTransform, Coroutine> uiButtonAnimations = new Dictionary<RectTransform, Coroutine>();
    readonly Dictionary<RectTransform, Vector3> uiButtonBaseScales = new Dictionary<RectTransform, Vector3>();

    void Awake()
    {
        core = GetComponent<DrivingCore>();
        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildOverlay();
        EnsureEventSystem();
        overlay.gameObject.SetActive(false);
    }

    void Update()
    {
        if (core == null) core = GetComponent<DrivingCore>();
        bool shouldShow = core != null && core.HudPaused;
        if (shouldShow && !visible) Open();
        else if (!shouldShow && visible) Close();
        if (!visible) return;

        if (phoneCamera == null || !phoneCamera.isActiveAndEnabled)
            PreparePhone();
        UpdatePhoneRotation();
        HandleHardwareButtonClick();
        if (!mainScreenUiAttached)
            SyncScreenRect(mainScreenRect, mainPlane, mainPlaneMesh, new Vector2(Screen.width * 0.34f, Screen.height * 0.54f));
        if (!statusUiAttached)
            SyncScreenRect(statusRect, statusPlane, statusPlaneMesh, new Vector2(Screen.width * 0.34f, Screen.height * 0.07f));
        if (timeText != null) timeText.text = DateTime.Now.ToString("H:mm");
        if (Time.unscaledTime >= refreshAt)
        {
            RefreshLiveValues();
            refreshAt = Time.unscaledTime + 0.25f;
        }
    }

    void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("VOYAGE // PHONE PAUSE UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        overlay = canvasObject.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceCamera;
        overlay.worldCamera = Camera.main;
        overlay.planeDistance = 1f;
        overlay.overrideSorting = true;
        overlay.sortingOrder = 0;
        overlayRect = canvasObject.GetComponent<RectTransform>();
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;

        dimOverlay = MakeImage("Ocean Dim", overlayRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0f));
        dimOverlay.raycastTarget = false;
        mainScreenRect = MakeRect("Main OLED UI", overlayRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460f, 640f));
        mainScreenRect.gameObject.AddComponent<Image>().color = ScreenColor;
        mainScreenRect.gameObject.AddComponent<RectMask2D>();
        pageRoot = MakeRect("Phone App Page", mainScreenRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        statusRect = MakeRect("Transparent Status Strip", overlayRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460f, 70f));
        timeText = AddText("Status Time", statusRect, new Vector2(0.06f, 0.08f), new Vector2(0.30f, 0.92f), "9:41", TextMain, 14f, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateSignalBars(statusRect);
        AddText("WiFi Status", statusRect, new Vector2(0.69f, 0.08f), new Vector2(0.83f, 0.92f), "WiFi", TextMain, 11f, FontStyle.Bold, TextAnchor.MiddleCenter);
        CreateBattery(statusRect);

        BuildHomePage();
    }

    void Open()
    {
        visible = true;
        cursorWasVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        BuildHomePage();
        overlay.gameObject.SetActive(true);
        PreparePhone();
        phoneYaw = 0f;
        phoneYawVelocity = 0f;
        phonePitch = 0f;
        phonePitchVelocity = 0f;
        tiltInfluence = IsPointerOverMainScreen() ? 0f : 1f;
        entrancePlaying = true;
        if (dimOverlay != null) dimOverlay.color = new Color(0f, 0f, 0f, 0f);
        ApplyPhonePose(phoneYaw);
        if (entranceRoutine != null) StopCoroutine(entranceRoutine);
        entranceRoutine = StartCoroutine(PlayEntrance());
        if (!mainScreenUiAttached)
            SyncScreenRect(mainScreenRect, mainPlane, mainPlaneMesh, new Vector2(Screen.width * 0.34f, Screen.height * 0.54f));
        if (!statusUiAttached)
            SyncScreenRect(statusRect, statusPlane, statusPlaneMesh, new Vector2(Screen.width * 0.34f, Screen.height * 0.07f));
        EventSystem.current?.SetSelectedGameObject(resumeButton != null ? resumeButton.gameObject : null);
    }

    void Close()
    {
        visible = false;
        entrancePlaying = false;
        quitPending = false;
        if (entranceRoutine != null)
        {
            StopCoroutine(entranceRoutine);
            entranceRoutine = null;
        }
        if (dimOverlay != null) dimOverlay.color = new Color(0f, 0f, 0f, 0f);
        if (phoneInstance != null) phoneInstance.SetActive(false);
        if (overlay != null) overlay.gameObject.SetActive(false);
        Cursor.visible = cursorWasVisible;
        Cursor.lockState = previousCursorLock;
    }

    void PreparePhone()
    {
        if (phoneInstance != null && phoneCamera != null)
        {
            phoneInstance.SetActive(true);
            return;
        }
        phoneCamera = Camera.main;
        if (phoneCamera == null) return;
        overlay.worldCamera = phoneCamera;

        GameObject prefab = Resources.Load<GameObject>(PhoneResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("VOYAGE PHONE // PhonePause/TerminalPhone FBX was not found in Resources.", this);
            return;
        }

        phoneInstance = Instantiate(prefab, phoneCamera.transform, false);
        phoneInstance.name = "VOYAGE // Pause Terminal";
        phoneInstance.transform.localPosition = Vector3.zero;
        phoneInstance.transform.localRotation = Quaternion.identity;
        phoneInstance.transform.localScale = Vector3.one * PhoneScale;
        mainPlane = FindChild(phoneInstance.transform, "Plane");
        statusPlane = FindChild(phoneInstance.transform, "Plane.001");
        mainPlaneMesh = mainPlane != null ? mainPlane.GetComponent<MeshFilter>() : null;
        statusPlaneMesh = statusPlane != null ? statusPlane.GetComponent<MeshFilter>() : null;

        if (mainPlane != null)
        {
            Vector3 localNormal = phoneInstance.transform.InverseTransformDirection(mainPlane.forward);
            Vector3 localUp = phoneInstance.transform.InverseTransformDirection(mainPlane.up);
            Quaternion sourceBasis = Quaternion.LookRotation(localNormal, localUp);
            Quaternion cameraBasis = Quaternion.LookRotation(Vector3.back, Vector3.up);
            phoneBaseRotation = cameraBasis * Quaternion.Inverse(sourceBasis);
            Vector3 screenCenter = mainPlaneMesh != null && mainPlaneMesh.sharedMesh != null
                ? mainPlane.TransformPoint(mainPlaneMesh.sharedMesh.bounds.center)
                : mainPlane.position;
            phoneScreenCenterLocal = phoneInstance.transform.InverseTransformPoint(screenCenter);
            ApplyPhonePose(0f);
        }
        else
        {
            phoneBaseRotation = Quaternion.identity;
            phoneScreenCenterLocal = Vector3.zero;
            ApplyPhonePose(0f);
        }

        ConfigureHardwareButtons();
        AttachScreenUiToPhonePlanes();
        ApplyMaterials(phoneInstance);
        phoneInstance.SetActive(true);
    }

    void AttachScreenUiToPhonePlanes()
    {
        if (mainPlane != null && mainPlaneMesh != null && mainPlaneMesh.sharedMesh != null && mainScreenRect != null)
        {
            Vector2 pixelSize = new Vector2(460f, 640f);
            mainScreenCanvas = CreatePlaneCanvas("VOYAGE // Main Screen Canvas", mainPlane, mainPlaneMesh.sharedMesh, pixelSize, 80);
            if (mainScreenCanvas == null) return;
            FitScreenRectToCanvas(mainScreenRect, pixelSize);
            mainScreenRect.SetParent(mainScreenCanvas.transform, false);
            FitScreenRectToCanvas(mainScreenRect, pixelSize);
            mainScreenUiAttached = true;
        }

        if (statusPlane != null && statusPlaneMesh != null && statusPlaneMesh.sharedMesh != null && statusRect != null)
        {
            Vector2 pixelSize = new Vector2(460f, 70f);
            statusScreenCanvas = CreatePlaneCanvas("VOYAGE // Status Screen Canvas", statusPlane, statusPlaneMesh.sharedMesh, pixelSize, 81);
            if (statusScreenCanvas == null) return;
            statusRect.SetParent(statusScreenCanvas.transform, false);
            FitScreenRectToCanvas(statusRect, pixelSize);
            statusUiAttached = true;
        }
    }

    Canvas CreatePlaneCanvas(string canvasName, Transform plane, Mesh mesh, Vector2 pixelSize, int sortingOrder)
    {
        Bounds meshBounds = mesh.bounds;
        Vector3 dimensions = meshBounds.size;
        int[] axes = { 0, 1, 2 };
        Array.Sort(axes, (a, b) => GetComponentValue(dimensions, b).CompareTo(GetComponentValue(dimensions, a)));
        bool canvasIsLandscape = pixelSize.x >= pixelSize.y;
        int horizontalAxis = canvasIsLandscape ? axes[0] : axes[1];
        int verticalAxis = canvasIsLandscape ? axes[1] : axes[0];
        float width = GetComponentValue(dimensions, horizontalAxis);
        float height = GetComponentValue(dimensions, verticalAxis);
        if (width <= 0.0001f || height <= 0.0001f)
        {
            Debug.LogWarning("VOYAGE PHONE // Screen mesh has no usable surface area; leaving its UI in screen space.", plane);
            return null;
        }

        Vector3 localRight = AxisVector(horizontalAxis);
        Vector3 localUp = AxisVector(verticalAxis);
        Vector3 localNormal = Vector3.Cross(localRight, localUp).normalized;
        Vector3[] normals = mesh.normals;
        if (normals != null && normals.Length > 0 && Vector3.Dot(localNormal, normals[0]) < 0f)
        {
            localUp = -localUp;
            localNormal = -localNormal;
        }

        GameObject canvasObject = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(plane, false);
        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = pixelSize;
        rect.localPosition = meshBounds.center + localNormal * Mathf.Max(0.003f, Mathf.Min(width, height) * 0.01f);
        rect.localRotation = Quaternion.LookRotation(localNormal, localUp);
        // The phone's front normal is turned toward the camera by the base
        // rotation, which reverses its horizontal axis. Counter that here so
        // labels read left-to-right from the viewer's side of the display.
        rect.localScale = new Vector3(
            -width / Mathf.Max(1f, pixelSize.x),
            height / Mathf.Max(1f, pixelSize.y),
            1f);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = phoneCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
        // World-space Text scales against this value. Unity's default of 1
        // keeps the glyphs the same design size as their RectTransforms.
        scaler.dynamicPixelsPerUnit = 1f;
        return canvas;
    }

    static float GetComponentValue(Vector3 value, int axis)
    {
        return axis == 0 ? value.x : axis == 1 ? value.y : value.z;
    }

    static Vector3 AxisVector(int axis)
    {
        return axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
    }

    void FitScreenRectToCanvas(RectTransform rect, Vector2 pixelSize)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = pixelSize;
        rect.localScale = Vector3.one;
    }

    void UpdatePhoneRotation()
    {
        if (phoneInstance == null || phoneCamera == null || entrancePlaying) return;
        float targetInfluence = IsPointerOverMainScreen() ? 0f : 1f;
        tiltInfluence = Mathf.MoveTowards(tiltInfluence, targetInfluence, Time.unscaledDeltaTime * 9f);
        float mouseX = Mathf.Clamp((Input.mousePosition.x / Mathf.Max(1f, Screen.width) - 0.5f) * 2f, -1f, 1f);
        float exponent = Mathf.Lerp(1.15f, 0.65f, tiltInfluence);
        float response = Mathf.Sign(mouseX) * Mathf.Pow(Mathf.Abs(mouseX), exponent);
        float maxYaw = Mathf.Lerp(MainScreenMaxYaw, OutsideScreenMaxYaw, tiltInfluence);
        float targetYaw = response * maxYaw;
        float mouseY = Mathf.Clamp((Input.mousePosition.y / Mathf.Max(1f, Screen.height) - 0.5f) * 2f, -1f, 1f);
        float verticalResponse = Mathf.Sign(mouseY) * Mathf.Pow(Mathf.Abs(mouseY), exponent);
        float maxPitch = Mathf.Lerp(MainScreenMaxPitch, OutsideScreenMaxPitch, tiltInfluence);
        float targetPitch = -verticalResponse * maxPitch;
        float yawSpeed = VariableTurnSpeed(phoneYaw, targetYaw, OutsideScreenMaxYaw);
        float pitchSpeed = VariableTurnSpeed(phonePitch, targetPitch, OutsideScreenMaxPitch);
        phoneYaw = Mathf.SmoothDampAngle(phoneYaw, targetYaw, ref phoneYawVelocity, PhoneRotationSmoothTime, yawSpeed, Time.unscaledDeltaTime);
        phonePitch = Mathf.SmoothDampAngle(phonePitch, targetPitch, ref phonePitchVelocity, PhoneRotationSmoothTime, pitchSpeed, Time.unscaledDeltaTime);
        ApplyPhonePose(phoneYaw, phonePitch);
    }

    float VariableTurnSpeed(float current, float target, float maxAngle)
    {
        float distance = Mathf.Abs(Mathf.DeltaAngle(current, target));
        return Mathf.Lerp(24f, 420f, Mathf.Clamp01(distance / Mathf.Max(1f, maxAngle)));
    }

    bool IsPointerOverMainScreen()
    {
        if (mainScreenRect == null) return false;
        Camera eventCamera = mainScreenUiAttached ? phoneCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(mainScreenRect, Input.mousePosition, eventCamera);
    }

    void ApplyPhonePose(float yaw, float pitch = 0f, Vector3 positionOffset = default(Vector3), float roll = 0f)
    {
        if (phoneInstance == null) return;
        Vector3 desiredScale = Vector3.one * PhoneScale;
        if ((phoneInstance.transform.localScale - desiredScale).sqrMagnitude > 0.000001f)
            phoneInstance.transform.localScale = desiredScale;
        // Both axes are in camera-parent space: yaw swings like a door, while
        // pitch tips the top/bottom edge toward or away from the viewer.
        Quaternion rotation = Quaternion.AngleAxis(yaw, Vector3.up)
            * Quaternion.AngleAxis(pitch, Vector3.right)
            * Quaternion.AngleAxis(roll, Vector3.forward)
            * phoneBaseRotation;
        phoneInstance.transform.localRotation = rotation;
        Vector3 scaledScreenCenter = Vector3.Scale(phoneInstance.transform.localScale, phoneScreenCenterLocal);
        phoneInstance.transform.localPosition = phoneTargetCenter + positionOffset - rotation * scaledScreenCenter;
    }

    System.Collections.IEnumerator PlayEntrance()
    {
        float elapsed = 0f;
        while (elapsed < PhoneEntryDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PhoneEntryDuration);
            float eased = EaseOutBack(t);
            if (dimOverlay != null)
            {
                Color color = dimOverlay.color;
                color.a = PauseDimAlpha * Mathf.SmoothStep(0f, 1f, t);
                dimOverlay.color = color;
            }
            if (phoneInstance != null)
            {
                float remaining = 1f - eased;
                ApplyPhonePose(-32f * remaining, 24f * remaining, new Vector3(0f, -3.15f * remaining, 0f), 18f * remaining);
            }
            yield return null;
        }
        if (dimOverlay != null) dimOverlay.color = new Color(0f, 0f, 0f, PauseDimAlpha);
        phoneYaw = phonePitch = phoneYawVelocity = phonePitchVelocity = 0f;
        ApplyPhonePose(0f, 0f);
        entrancePlaying = false;
        entranceRoutine = null;
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float shifted = t - 1f;
        return 1f + c3 * shifted * shifted * shifted + c1 * shifted * shifted;
    }

    void ConfigureHardwareButtons()
    {
        List<Renderer> buttons = new List<Renderer>();
        string[] names = { "Cube.004", "Cube.005", "Cube.006" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform button = FindChild(phoneInstance.transform, names[i]);
            Renderer renderer = button != null ? button.GetComponent<Renderer>() : null;
            if (renderer == null) continue;
            if (renderer.GetComponent<Collider>() == null)
            {
                BoxCollider collider = renderer.gameObject.AddComponent<BoxCollider>();
                Bounds localBounds = renderer.localBounds;
                collider.center = localBounds.center;
                collider.size = localBounds.size;
            }
            buttons.Add(renderer);
        }

        buttons.Sort((a, b) =>
            phoneCamera.WorldToScreenPoint(a.bounds.center).x.CompareTo(phoneCamera.WorldToScreenPoint(b.bounds.center).x));
        if (buttons.Count < 3)
        {
            Debug.LogWarning("VOYAGE PHONE // Expected three hardware button meshes (Cube.004, Cube.005, Cube.006).", phoneInstance);
            return;
        }

        Renderer leftA = buttons[0];
        Renderer leftB = buttons[1];
        quitHardwareButton = buttons[2].transform;
        float leftAY = phoneCamera.WorldToScreenPoint(leftA.bounds.center).y;
        float leftBY = phoneCamera.WorldToScreenPoint(leftB.bounds.center).y;
        volumeUpHardwareButton = leftAY >= leftBY ? leftA.transform : leftB.transform;
        volumeDownHardwareButton = leftAY >= leftBY ? leftB.transform : leftA.transform;
    }

    void HandleHardwareButtonClick()
    {
        if (entrancePlaying || !Input.GetMouseButtonDown(0) || phoneCamera == null || PointerIsOverUi()) return;

        Physics.SyncTransforms();
        Ray ray = phoneCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hit = hits[i].collider.transform;
            if (IsButtonHit(hit, volumeUpHardwareButton))
            {
                AnimateHardwareButton(volumeUpHardwareButton);
                AdjustMasterVolume(0.1f);
                return;
            }
            if (IsButtonHit(hit, volumeDownHardwareButton))
            {
                AnimateHardwareButton(volumeDownHardwareButton);
                AdjustMasterVolume(-0.1f);
                return;
            }
            if (IsButtonHit(hit, quitHardwareButton))
            {
                if (!quitPending)
                {
                    quitPending = true;
                    AnimateHardwareButton(quitHardwareButton);
                    StartCoroutine(QuitAfterButtonPress());
                }
                return;
            }
        }
    }

    bool PointerIsOverUi()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;
        PointerEventData pointer = new PointerEventData(eventSystem) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointer, results);
        return results.Count > 0;
    }

    bool IsButtonHit(Transform hit, Transform button)
    {
        return button != null && (hit == button || hit.IsChildOf(button));
    }

    void AdjustMasterVolume(float delta)
    {
        float value = Mathf.Clamp01(AudioListener.volume + delta);
        AudioListener.volume = value;
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(value);
        if (audioValueText != null) audioValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    void AnimateHardwareButton(Transform button)
    {
        if (button == null || phoneInstance == null) return;
        if (hardwareButtonAnimations.TryGetValue(button, out Coroutine active) && active != null)
            StopCoroutine(active);
        hardwareButtonAnimations[button] = StartCoroutine(PlayHardwareButtonPress(button));
    }

    System.Collections.IEnumerator PlayHardwareButtonPress(Transform button)
    {
        Transform parent = button.parent;
        if (parent == null || phoneInstance == null) yield break;
        Vector3 original = button.localPosition;
        Bounds modelBounds = new Bounds(phoneInstance.transform.position, Vector3.zero);
        Renderer[] renderers = phoneInstance.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++) modelBounds.Encapsulate(renderers[i].bounds);
        Vector3 centerLocal = phoneInstance.transform.InverseTransformPoint(modelBounds.center);
        Vector3 buttonLocal = phoneInstance.transform.InverseTransformPoint(button.position);
        Vector3 inwardWorld = phoneInstance.transform.TransformDirection(centerLocal - buttonLocal).normalized;
        Vector3 inwardParent = parent.InverseTransformDirection(inwardWorld).normalized;
        Vector3 travel = inwardParent * 0.035f;
        const float pressDuration = 0.075f;
        const float reboundDuration = 0.34f;
        float elapsed = 0f;
        while (elapsed < pressDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / pressDuration);
            float eased = t * t;
            button.localPosition = original + travel * eased;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < reboundDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / reboundDuration);
            float spring = Mathf.Exp(-6.5f * t) * Mathf.Cos(t * Mathf.PI * 3.2f);
            button.localPosition = original + travel * spring;
            yield return null;
        }
        button.localPosition = original;
        hardwareButtonAnimations.Remove(button);
    }

    System.Collections.IEnumerator QuitAfterButtonPress()
    {
        yield return new WaitForSecondsRealtime(0.43f);
        QuitGame();
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ApplyMaterials(GameObject model)
    {
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            string objectName = renderer.gameObject.name;
            Material material;
            if (objectName == "Plane.001" || objectName == "Plane.002")
                material = MakeRuntimeMaterial("Phone | Transparent status surface", Color.clear, 0f, 0.4f, true, false);
            else if (objectName == "Plane")
                material = MakeRuntimeMaterial("Phone | OLED black", new Color(0.018f, 0.024f, 0.031f, 1f), 0.12f, 0.8f, false, true);
            else if (objectName == "Cube.004" || objectName == "Cube.006")
                material = MakeRuntimeMaterial("Phone | Signal coral", Coral, 0.45f, 0.6f, false, false);
            else if (objectName == "Cube.005")
                material = MakeRuntimeMaterial("Phone | Sea glass", Sea, 0.42f, 0.64f, false, false);
            else if (objectName == "Cube.001")
                material = MakeRuntimeMaterial("Phone | Brushed alloy", Hex("344D56"), 0.78f, 0.72f, false, false);
            else if (objectName == "Cube.002")
                material = MakeRuntimeMaterial("Phone | Graphite ceramic", Hex("111A21"), 0.4f, 0.73f, false, false);
            else if (objectName == "Cube.003")
                material = MakeRuntimeMaterial("Phone | Slate polymer", Hex("243740"), 0.18f, 0.48f, false, false);
            else
                material = MakeRuntimeMaterial("Phone | Deep sea titanium", Hex("182833"), 0.62f, 0.62f, false, false);

            Material[] assigned = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
            for (int i = 0; i < assigned.Length; i++) assigned[i] = material;
            renderer.sharedMaterials = assigned;
        }
    }

    Material MakeRuntimeMaterial(string name, Color color, float metallic, float smoothness, bool transparent, bool emissive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (emissive && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.008f, 0.012f, 0.018f, 1f));
        }
        if (transparent)
        {
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("DepthNormals", false);
        }
        runtimeMaterials.Add(material);
        return material;
    }

    void SyncScreenRect(RectTransform rect, Transform plane, MeshFilter mesh, Vector2 fallbackSize)
    {
        if (rect == null) return;
        if (plane == null || mesh == null || phoneCamera == null)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = fallbackSize;
            return;
        }

        Bounds bounds = mesh.sharedMesh.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, max.z), new Vector3(max.x, max.y, max.z)
        };
        float left = float.MaxValue, right = float.MinValue, bottom = float.MaxValue, top = float.MinValue;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 point = phoneCamera.WorldToScreenPoint(plane.TransformPoint(corners[i]));
            left = Mathf.Min(left, point.x);
            right = Mathf.Max(right, point.x);
            bottom = Mathf.Min(bottom, point.y);
            top = Mathf.Max(top, point.y);
        }
        float width = Mathf.Max(1f, right - left);
        float height = Mathf.Max(1f, top - bottom);
        rect.anchoredPosition = new Vector2((left + right) * 0.5f - Screen.width * 0.5f, (bottom + top) * 0.5f - Screen.height * 0.5f);
        rect.sizeDelta = new Vector2(width, height);
    }

    void BuildHomePage()
    {
        ClearPage();
        AddText("Pause Title", pageRoot, new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.96f), "PAUSED", TextMain, 22f, FontStyle.Bold, TextAnchor.MiddleCenter);

        CreateAppTile("MAP", "M", Hex("2D7F83"), 0.20f, 0.615f, "MAP");
        CreateAppTile("GEAR", "G", Hex("668B63"), 0.50f, 0.615f, "GEAR");
        CreateAppTile("LOG", "L", Hex("C27C5B"), 0.80f, 0.615f, "LOG");
        CreateAppTile("AUDIO", "A", Hex("7469A2"), 0.20f, 0.345f, "AUDIO");
        CreateAppTile("CREW", "C", Hex("4082A3"), 0.50f, 0.345f, "CREW");
        CreateAppTile("SETUP", "O", Hex("8E7850"), 0.80f, 0.345f, "SETTINGS");

        resumeButton = CreateActionButton("Resume Dive", pageRoot, new Vector2(0.09f, 0.075f), new Vector2(0.91f, 0.185f), "RESUME   ›", Sea, () => core?.SetPaused(false));
    }

    void CreateAppTile(string label, string glyph, Color accent, float centerX, float centerY, string page)
    {
        RectTransform tile = MakeRect(label + " App", pageRoot,
            new Vector2(centerX - 0.135f, centerY - 0.105f), new Vector2(centerX + 0.135f, centerY + 0.105f),
            new Vector2(0.5f, 0.5f), Vector2.zero);
        Image tileImage = tile.gameObject.AddComponent<Image>();
        tileImage.color = Panel;
        tileImage.sprite = GetRoundedIconSprite();
        tileImage.type = Image.Type.Sliced;
        Button button = tile.gameObject.AddComponent<Button>();
        button.targetGraphic = tileImage;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = ButtonColors(Panel, PanelRaised, accent);
        AddUiButtonFeedback(button);
        button.onClick.AddListener(() => BuildAppPage(page));

        RectTransform icon = MakeRect(label + " Icon", tile, new Vector2(0.32f, 0.49f), new Vector2(0.68f, 0.91f), new Vector2(0.5f, 0.5f), Vector2.zero);
        Image iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.color = accent;
        iconImage.sprite = GetRoundedIconSprite();
        iconImage.type = Image.Type.Sliced;
        AddText(label + " Glyph", icon, Vector2.zero, Vector2.one, glyph, Color.white, 34f, FontStyle.Bold, TextAnchor.MiddleCenter);
        AddText(label + " Label", tile, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.40f), label, TextMain, 17f, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    void BuildAppPage(string page)
    {
        ClearPage();
        Button back = CreateActionButton("Back", pageRoot, new Vector2(0.06f, 0.895f), new Vector2(0.36f, 0.96f), "‹  HOME", TextMuted, BuildHomePage);
        back.colors = ButtonColors(Panel, PanelRaised, Sea);
        AddText(page + " Title", pageRoot, new Vector2(0.36f, 0.865f), new Vector2(0.94f, 0.96f), AppTitle(page), TextMain, 26f, FontStyle.Bold, TextAnchor.MiddleRight);
        AddText(page + " Subtitle", pageRoot, new Vector2(0.08f, 0.815f), new Vector2(0.92f, 0.86f), AppSubtitle(page), Sea, 14f, FontStyle.Bold, TextAnchor.MiddleLeft);

        switch (page)
        {
            case "MAP": BuildMapPage(); break;
            case "GEAR": BuildGearPage(); break;
            case "LOG": BuildLogPage(); break;
            case "AUDIO": BuildAudioPage(); break;
            case "CREW": BuildCrewPage(); break;
            case "SETTINGS": BuildSettingsPage(); break;
        }
        CreateActionButton("Resume from App", pageRoot, new Vector2(0.09f, 0.075f), new Vector2(0.91f, 0.16f), "RESUME DIVE   ›", Sea, () => core?.SetPaused(false));
        AddText("App Home Hint", pageRoot, new Vector2(0.08f, 0.012f), new Vector2(0.92f, 0.06f), "‹ HOME TO RETURN TO APPS", TextMuted, 11f, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    void BuildMapPage()
    {
        RectTransform card = MakeCard("Chart Card", new Vector2(0.09f, 0.30f), new Vector2(0.91f, 0.76f));
        AddText("Chart Status", card, new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.97f), "LOCAL POSITION  /  LIVE", TextMuted, 13f, FontStyle.Bold, TextAnchor.MiddleLeft);
        for (int i = 1; i < 5; i++)
        {
            float fraction = i / 5f;
            MakeImage("Chart Grid Vertical", card, new Vector2(fraction, 0.12f), new Vector2(fraction + 0.002f, 0.78f), new Color(Sea.r, Sea.g, Sea.b, 0.20f));
            MakeImage("Chart Grid Horizontal", card, new Vector2(0.06f, 0.12f + fraction * 0.66f), new Vector2(0.94f, 0.122f + fraction * 0.66f), new Color(Sea.r, Sea.g, Sea.b, 0.20f));
        }
        MakeImage("Chart Position", card, new Vector2(0.47f, 0.43f), new Vector2(0.53f, 0.49f), Coral);
        mapXValue = AddText("Chart X", card, new Vector2(0.05f, 0.015f), new Vector2(0.46f, 0.12f), "X 0", TextMain, 14f, FontStyle.Bold, TextAnchor.MiddleCenter);
        mapZValue = AddText("Chart Z", card, new Vector2(0.54f, 0.015f), new Vector2(0.95f, 0.12f), "Z 0", TextMain, 14f, FontStyle.Bold, TextAnchor.MiddleCenter);
        AddText("Chart Footer", pageRoot, new Vector2(0.09f, 0.205f), new Vector2(0.91f, 0.28f), "NORTH  ↑       SIGNAL  ••••", TextMuted, 13f, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    void BuildGearPage()
    {
        AddMetricCard("Movement", "MODE", "VEHICLE", new Vector2(0.09f, 0.57f), new Vector2(0.47f, 0.75f), Sea, out modeValue);
        AddMetricCard("Velocity", "SPEED", "0 KM/H", new Vector2(0.53f, 0.57f), new Vector2(0.91f, 0.75f), Coral, out speedValue);
        AddMetricCard("Fuel", "FUEL", "-- / --", new Vector2(0.09f, 0.35f), new Vector2(0.47f, 0.53f), Gold, out fuelValue);
        AddMetricCard("Link", "LINK", "STABLE", new Vector2(0.53f, 0.35f), new Vector2(0.91f, 0.53f), Sea);
        AddText("Gear Help", pageRoot, new Vector2(0.10f, 0.22f), new Vector2(0.90f, 0.31f), "GEAR READOUT UPDATES WHILE PAUSED", TextMuted, 12f, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    void BuildLogPage()
    {
        RectTransform card = MakeCard("Field Log Card", new Vector2(0.09f, 0.30f), new Vector2(0.91f, 0.76f));
        AddText("Log Empty Icon", card, new Vector2(0.12f, 0.60f), new Vector2(0.88f, 0.86f), "—", Gold, 46f, FontStyle.Bold, TextAnchor.MiddleCenter);
        AddText("Log Empty", card, new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.62f), "NO ACTIVE FIELD NOTES", TextMain, 19f, FontStyle.Bold, TextAnchor.MiddleCenter);
        AddText("Log Controls", card, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.39f), "P / ESC   Resume\nH   Headlights\nR   Reset vehicle", TextMuted, 15f, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    void BuildAudioPage()
    {
        RectTransform card = MakeCard("Audio Card", new Vector2(0.09f, 0.32f), new Vector2(0.91f, 0.76f));
        AddText("Master Volume Label", card, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.88f), "MASTER VOLUME", TextMain, 19f, FontStyle.Bold, TextAnchor.MiddleLeft);
        AddText("Audio Hint", card, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.65f), "Adjust the game output level", TextMuted, 14f, FontStyle.Normal, TextAnchor.MiddleLeft);
        volumeSlider = CreateSlider(card, new Vector2(0.10f, 0.35f), new Vector2(0.90f, 0.48f), AudioListener.volume, value =>
        {
            AudioListener.volume = value;
            if (audioValueText != null) audioValueText.text = Mathf.RoundToInt(value * 100f) + "%";
        });
        audioValueText = AddText("Audio Value", card, new Vector2(0.10f, 0.12f), new Vector2(0.90f, 0.30f), Mathf.RoundToInt(AudioListener.volume * 100f) + "%", Sea, 22f, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    void BuildCrewPage()
    {
        RectTransform card = MakeCard("Crew Card", new Vector2(0.09f, 0.30f), new Vector2(0.91f, 0.76f));
        AddText("Crew Monogram", card, new Vector2(0.36f, 0.60f), new Vector2(0.64f, 0.88f), "V", Sea, 58f, FontStyle.Bold, TextAnchor.MiddleCenter);
        AddText("Crew Name", card, new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.60f), "VOYAGE OPERATOR", TextMain, 19f, FontStyle.Bold, TextAnchor.MiddleCenter);
        AddText("Crew Status", card, new Vector2(0.08f, 0.13f), new Vector2(0.92f, 0.40f), "FIELD LINK ACTIVE\nVEHICLE SYSTEMS READY\nSUPPORT CHANNEL STANDBY", TextMuted, 14f, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    void BuildSettingsPage()
    {
        RectTransform card = MakeCard("Settings Card", new Vector2(0.09f, 0.32f), new Vector2(0.91f, 0.76f));
        AddText("Display Setting Label", card, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.87f), "DISPLAY MODE", TextMain, 18f, FontStyle.Bold, TextAnchor.MiddleLeft);
        AddText("Display Setting Value", card, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.65f), Screen.fullScreen ? "FULLSCREEN" : "WINDOWED", TextMuted, 14f, FontStyle.Normal, TextAnchor.MiddleLeft);
        CreateActionButton("Toggle Display Mode", card, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.51f), "TOGGLE DISPLAY MODE", Sea,
            () =>
            {
                Screen.fullScreen = !Screen.fullScreen;
                Text mode = card.Find("Display Setting Value")?.GetComponent<Text>();
                if (mode != null) mode.text = Screen.fullScreen ? "FULLSCREEN" : "WINDOWED";
            });
        AddText("Settings Note", card, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.28f), "MORE FIELD OPTIONS WILL APPEAR HERE", TextMuted, 12f, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    void RefreshLiveValues()
    {
        if (core == null) return;
        var session = Voyage.Exploration.ExplorationSession.Instance;
        bool onFoot = session != null && session.OnFoot;
        if (modeValue != null) modeValue.text = onFoot ? "ON FOOT" : "VEHICLE";
        if (speedValue != null) speedValue.text = onFoot ? "ON FOOT" : Mathf.RoundToInt(core.HudSpeedKmh) + " KM/H";
        if (fuelValue != null) fuelValue.text = $"{FuelTank.SharedFuel:0.0} / {FuelTank.SharedCapacity:0}";
        Transform player = onFoot && session.Explorer != null ? session.Explorer.transform : core.Player != null ? core.Player.transform : null;
        if (player != null)
        {
            if (mapXValue != null) mapXValue.text = "X " + Mathf.RoundToInt(player.position.x);
            if (mapZValue != null) mapZValue.text = "Z " + Mathf.RoundToInt(player.position.z);
        }
    }

    void CreateSignalBars(Transform parent)
    {
        RectTransform group = MakeRect("Cell Signal", parent, new Vector2(0.51f, 0.16f), new Vector2(0.67f, 0.86f), new Vector2(0.5f, 0.5f), Vector2.zero);
        for (int i = 0; i < 4; i++)
        {
            float width = 0.16f;
            float gap = 0.05f;
            float x = i * (width + gap);
            float height = 0.24f + i * 0.20f;
            RectTransform bar = MakeRect("Signal Bar " + i, group, new Vector2(x, 0f), new Vector2(x + width, height), new Vector2(0f, 0f), Vector2.zero);
            bar.gameObject.AddComponent<Image>().color = TextMain;
        }
    }

    void CreateBattery(Transform parent)
    {
        RectTransform outline = MakeRect("Battery Outline", parent, new Vector2(0.86f, 0.26f), new Vector2(0.96f, 0.76f), new Vector2(0.5f, 0.5f), Vector2.zero);
        Image body = outline.gameObject.AddComponent<Image>();
        body.color = new Color(0f, 0f, 0f, 0f);
        Outline edge = outline.gameObject.AddComponent<Outline>();
        edge.effectColor = TextMain;
        edge.effectDistance = new Vector2(1.2f, 1.2f);
        RectTransform fill = MakeRect("Battery Charge", outline, new Vector2(0.10f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
        fill.gameObject.AddComponent<Image>().color = Sea;
        RectTransform cap = MakeRect("Battery Cap", parent, new Vector2(0.963f, 0.40f), new Vector2(0.98f, 0.62f), new Vector2(0f, 0.5f), Vector2.zero);
        cap.gameObject.AddComponent<Image>().color = TextMain;
    }

    RectTransform MakeCard(string name, Vector2 min, Vector2 max)
    {
        RectTransform rect = MakeRect(name, pageRoot, min, max, new Vector2(0.5f, 0.5f), Vector2.zero);
        rect.gameObject.AddComponent<Image>().color = Panel;
        return rect;
    }

    void AddMetricCard(string name, string label, string value, Vector2 min, Vector2 max, Color accent)
    {
        AddMetricCard(name, label, value, min, max, accent, out _);
    }

    void AddMetricCard(string name, string label, string value, Vector2 min, Vector2 max, Color accent, out Text valueText)
    {
        RectTransform card = MakeCard(name, min, max);
        MakeImage(name + " Accent", card, new Vector2(0.06f, 0.20f), new Vector2(0.075f, 0.80f), accent);
        AddText(name + " Label", card, new Vector2(0.13f, 0.58f), new Vector2(0.94f, 0.86f), label, TextMuted, 12f, FontStyle.Bold, TextAnchor.MiddleLeft);
        valueText = AddText(name + " Value", card, new Vector2(0.13f, 0.20f), new Vector2(0.94f, 0.59f), value, TextMain, 22f, FontStyle.Bold, TextAnchor.MiddleLeft);
    }

    Button CreateActionButton(string name, Transform parent, Vector2 min, Vector2 max, string label, Color accent, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = MakeRect(name, parent, min, max, new Vector2(0.5f, 0.5f), Vector2.zero);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = PanelRaised;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = ButtonColors(PanelRaised, Panel, accent);
        AddUiButtonFeedback(button);
        button.onClick.AddListener(action);
        AddText(name + " Label", rect, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), label, accent, 16f, FontStyle.Bold, TextAnchor.MiddleCenter);
        return button;
    }

    Slider CreateSlider(Transform parent, Vector2 min, Vector2 max, float initialValue, Action<float> onChange)
    {
        RectTransform root = MakeRect("Master Volume Slider", parent, min, max, new Vector2(0.5f, 0.5f), Vector2.zero);
        Image rootImage = root.gameObject.AddComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0f);
        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;
        slider.direction = Slider.Direction.LeftToRight;
        RectTransform track = MakeRect("Volume Track", root, new Vector2(0f, 0.36f), new Vector2(1f, 0.64f), new Vector2(0.5f, 0.5f), Vector2.zero);
        track.gameObject.AddComponent<Image>().color = PanelRaised;
        RectTransform fillArea = MakeRect("Fill Area", root, new Vector2(0.01f, 0.36f), new Vector2(0.99f, 0.64f), new Vector2(0f, 0.5f), Vector2.zero);
        RectTransform fill = MakeRect("Fill", fillArea, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero);
        fill.gameObject.AddComponent<Image>().color = Sea;
        RectTransform handleArea = MakeRect("Handle Slide Area", root, new Vector2(0.01f, 0.14f), new Vector2(0.99f, 0.86f), new Vector2(0f, 0.5f), Vector2.zero);
        RectTransform handle = MakeRect("Handle", handleArea, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));
        handle.gameObject.AddComponent<Image>().color = TextMain;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.onValueChanged.AddListener(value => onChange?.Invoke(value));
        return slider;
    }

    Text AddText(string name, Transform parent, Vector2 min, Vector2 max, string value, Color color, float fontSize, FontStyle style, TextAnchor alignment)
    {
        RectTransform rect = MakeRect(name, parent, min, max, new Vector2(0.5f, 0.5f), Vector2.zero);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = runtimeFont;
        text.text = value;
        text.color = color;
        text.fontSize = Mathf.RoundToInt(fontSize);
        text.fontStyle = style;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(8, Mathf.RoundToInt(fontSize * 0.65f));
        text.resizeTextMaxSize = Mathf.RoundToInt(fontSize * 1.1f);
        return text;
    }

    Image MakeImage(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        RectTransform rect = MakeRect(name, parent, min, max, new Vector2(0.5f, 0.5f), Vector2.zero);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    Sprite GetRoundedIconSprite()
    {
        if (roundedIconSprite != null) return roundedIconSprite;
        const int size = 64;
        const float radius = 15f;
        roundedIconTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "VOYAGE // Rounded App Icon",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new Vector2(x + 0.5f, y + 0.5f) - center;
            Vector2 q = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - new Vector2(half - radius, half - radius);
            float distance = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
            byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - distance) * 255f);
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        roundedIconTexture.SetPixels32(pixels);
        roundedIconTexture.Apply(false, false);
        roundedIconSprite = Sprite.Create(roundedIconTexture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(15f, 15f, 15f, 15f));
        roundedIconSprite.name = "VOYAGE // Rounded App Icon";
        return roundedIconSprite;
    }

    RectTransform MakeRect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pivot, Vector2 size)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        return rect;
    }

    void ClearPage()
    {
        ClearUiButtonFeedbackForPage();
        modeValue = null;
        speedValue = null;
        fuelValue = null;
        mapXValue = null;
        mapZValue = null;
        volumeSlider = null;
        audioValueText = null;
        if (pageRoot == null) return;
        for (int i = pageRoot.childCount - 1; i >= 0; i--)
            Destroy(pageRoot.GetChild(i).gameObject);
    }

    void ClearUiButtonFeedbackForPage()
    {
        List<RectTransform> stale = new List<RectTransform>();
        foreach (RectTransform button in uiButtonBaseScales.Keys)
            if (button == null || button == pageRoot || button.IsChildOf(pageRoot)) stale.Add(button);
        for (int i = 0; i < stale.Count; i++)
        {
            RectTransform button = stale[i];
            if (uiButtonAnimations.TryGetValue(button, out Coroutine active) && active != null)
                StopCoroutine(active);
            uiButtonAnimations.Remove(button);
            uiButtonBaseScales.Remove(button);
        }
    }

    string AppTitle(string page)
    {
        switch (page)
        {
            case "MAP": return "CHART";
            case "GEAR": return "EQUIPMENT";
            case "LOG": return "FIELD LOG";
            case "AUDIO": return "AUDIO";
            case "CREW": return "CREW";
            case "SETTINGS": return "SETTINGS";
            default: return page;
        }
    }

    string AppSubtitle(string page)
    {
        switch (page)
        {
            case "MAP": return "POSITION & NAVIGATION";
            case "GEAR": return "VEHICLE SYSTEMS";
            case "LOG": return "SESSION NOTES";
            case "AUDIO": return "OUTPUT CONTROL";
            case "CREW": return "FIELD CONNECTION";
            case "SETTINGS": return "DEVICE PREFERENCES";
            default: return "FIELD TERMINAL";
        }
    }

    ColorBlock ButtonColors(Color normal, Color highlighted, Color pressed)
    {
        return new ColorBlock
        {
            normalColor = normal,
            highlightedColor = highlighted,
            pressedColor = pressed,
            selectedColor = highlighted,
            disabledColor = new Color(normal.r, normal.g, normal.b, 0.4f),
            colorMultiplier = 1f,
            fadeDuration = 0.12f
        };
    }

    void AddUiButtonFeedback(Button button)
    {
        if (button == null) return;
        RectTransform rect = button.transform as RectTransform;
        if (rect == null) return;
        uiButtonBaseScales[rect] = rect.localScale;

        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
        if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();

        AddPointerTrigger(trigger, EventTriggerType.PointerDown, _ => AnimateUiButton(rect, true));
        AddPointerTrigger(trigger, EventTriggerType.PointerUp, _ => AnimateUiButton(rect, false));
        AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => AnimateUiButton(rect, false));
    }

    void AddPointerTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    void AnimateUiButton(RectTransform button, bool pressed)
    {
        if (button == null) return;
        if (uiButtonAnimations.TryGetValue(button, out Coroutine active) && active != null)
            StopCoroutine(active);
        uiButtonAnimations[button] = StartCoroutine(PlayUiButtonScale(button, pressed));
    }

    System.Collections.IEnumerator PlayUiButtonScale(RectTransform button, bool pressed)
    {
        if (button == null) yield break;
        if (!uiButtonBaseScales.TryGetValue(button, out Vector3 baseScale))
            baseScale = button.localScale;
        if (pressed)
        {
            Vector3 start = button.localScale;
            float elapsed = 0f;
            const float duration = 0.075f;
            while (elapsed < duration && button != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                button.localScale = Vector3.LerpUnclamped(start, baseScale * 0.92f, eased);
                yield return null;
            }
            if (button != null) button.localScale = baseScale * 0.92f;
        }
        else
        {
            float startRatio = baseScale.x > 0.0001f ? button.localScale.x / baseScale.x : 0.92f;
            const float duration = 0.32f;
            float elapsed = 0f;
            while (elapsed < duration && button != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float spring = Mathf.Exp(-6f * t) * Mathf.Cos(t * Mathf.PI * 2.5f);
                button.localScale = baseScale * (1f + (startRatio - 1f) * spring);
                yield return null;
            }
            if (button != null) button.localScale = baseScale;
        }
        if (button != null) uiButtonAnimations.Remove(button);
    }

    void EnsureEventSystem()
    {
        EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
            eventSystem = new GameObject("VOYAGE // UI Event System", typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
        else if (eventSystem.GetComponent<BaseInputModule>() == null)
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
    }

    Transform FindChild(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.name == childName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChild(root.GetChild(i), childName);
            if (found != null) return found;
        }
        return null;
    }

    void OnDestroy()
    {
        if (visible)
        {
            Cursor.visible = cursorWasVisible;
            Cursor.lockState = previousCursorLock;
        }
        for (int i = 0; i < runtimeMaterials.Count; i++)
            if (runtimeMaterials[i] != null) Destroy(runtimeMaterials[i]);
        if (roundedIconSprite != null) Destroy(roundedIconSprite);
        if (roundedIconTexture != null) Destroy(roundedIconTexture);
    }

    static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        return color;
    }
}
