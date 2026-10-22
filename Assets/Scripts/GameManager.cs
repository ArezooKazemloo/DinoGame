using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Owns the run state, score, saved record, and runtime menus.</summary>
public class GameManager : MonoBehaviour
{
    public enum RunState { MainMenu, Playing, Paused, GameOver }

    public static GameManager Instance { get; private set; }

    [Header("HUD (optional; found by name when empty)")]
    public TMP_Text scoreText;
    public TMP_Text highScoreText;
    public TMP_Text healthText;

    [Header("Run settings")]
    [Min(0)] public int startingScore;
    [Min(0)] public int pointsPerSecond = 1;

    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }
    public RunState State { get; private set; } = RunState.MainMenu;

    private const string HighScoreKey = "HighScore";
    private Canvas canvas;
    private GameObject mainMenu;
    private GameObject pauseMenu;
    private GameObject gameOverMenu;
    private Button soundButton;
    private Image soundIcon;
    private TMP_Text gameOverScoreText;
    private InputAction pauseAction;
    private float scoreTimer;
    private static Sprite roundedPanelSprite;
    private static Sprite soundOnIconSprite;
    private static Sprite soundOffIconSprite;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (GetComponent<AudioManager>() == null) gameObject.AddComponent<AudioManager>();
        CurrentScore = startingScore;
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
    }

    private void OnEnable() => pauseAction?.Enable();

    private void Start()
    {
        FindHudReferences();
        BuildMenus();
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<ParallaxBackground>() == null)
            mainCamera.gameObject.AddComponent<ParallaxBackground>();
        SetState(RunState.MainMenu);
        RefreshHud();
    }

    private void Update()
    {
        if (State == RunState.Playing)
        {
            scoreTimer += Time.deltaTime * PlayerDash.GameSpeedMultiplier;
            if (scoreTimer >= 1f)
            {
                int earned = Mathf.FloorToInt(scoreTimer) * pointsPerSecond;
                scoreTimer -= Mathf.Floor(scoreTimer);
                AddScore(earned);
            }
        }

        if (pauseAction != null && pauseAction.WasPressedThisFrame())
        {
            if (State == RunState.Playing) PauseGame();
            else if (State == RunState.Paused) ResumeGame();
        }
    }

    public void AddScore(int amount)
    {
        if (amount <= 0 || State != RunState.Playing) return;
        CurrentScore += amount;
        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }
        RefreshHud();
    }

    public void StartGame()
    {
        ClearRunObjects();
        CurrentScore = startingScore;
        scoreTimer = 0f;
        if (PlayerHealth.Instance != null) PlayerHealth.Instance.ResetHealth();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.ResetForNewRun();
        ObstacleSpawner spawner = FindFirstObjectByType<ObstacleSpawner>();
        if (spawner != null) spawner.ResetForNewRun();
        SetState(RunState.Playing);
    }

    public void PauseGame()
    {
        if (State == RunState.Playing) SetState(RunState.Paused);
    }

    public void ResumeGame()
    {
        if (State == RunState.Paused) SetState(RunState.Playing);
    }

    public void EndGame()
    {
        if (State == RunState.GameOver) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGameOver();
        SetState(RunState.GameOver);
        if (gameOverScoreText != null) gameOverScoreText.text = $"SCORE  {CurrentScore:000000}";
        Time.timeScale = 1f;
        StartCoroutine(FreezeAfterDeathAnimation());
    }

    private IEnumerator FreezeAfterDeathAnimation()
    {
        yield return new WaitForSecondsRealtime(.4f);
        if (State == RunState.GameOver) Time.timeScale = 0f;
    }

    public void RestartGame() => StartGame();
    public void ReturnToMainMenu() => SetState(RunState.MainMenu);

    public void ExitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private static void ClearRunObjects()
    {
        ObstacleMovement[] movingObjects = FindObjectsByType<ObstacleMovement>(FindObjectsSortMode.None);
        foreach (ObstacleMovement movingObject in movingObjects) Destroy(movingObject.gameObject);
        Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
        foreach (Interactable interactable in interactables) Destroy(interactable.gameObject);
    }

    private void SetState(RunState next)
    {
        State = next;
        Time.timeScale = next == RunState.Playing ? 1f : 0f;
        if (mainMenu != null) mainMenu.SetActive(next == RunState.MainMenu);
        if (pauseMenu != null) pauseMenu.SetActive(next == RunState.Paused);
        if (gameOverMenu != null) gameOverMenu.SetActive(next == RunState.GameOver);
        if (soundButton != null) soundButton.gameObject.SetActive(next == RunState.Playing);
        RefreshHud();
    }

    private void FindHudReferences()
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (TMP_Text text in texts)
        {
            if (scoreText == null && text.name == "ScoreText") scoreText = text;
            if (highScoreText == null && text.name == "HighScoreText") highScoreText = text;
            if (healthText == null && text.name == "Health") healthText = text;
        }
        if (scoreText != null) scoreText.color = new Color(.98f, .92f, .78f);
        if (highScoreText != null) highScoreText.color = new Color(.63f, .84f, .88f);
    }

    private void RefreshHud()
    {
        if (scoreText != null) scoreText.text = $"SCORE  {CurrentScore:000000}";
        if (highScoreText != null) highScoreText.text = $"BEST  {HighScore:000000}";
    }

    private void BuildMenus()
    {
        EnsureInputSystemUI();
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        }

        GameObject scorePanel = CreateHudPanel("ScorePanel", new Vector2(48, -38), new Vector2(420, 126), false);
        PlaceHudText(scoreText, scorePanel.transform, new Vector2(22, -16), new Vector2(374, 46), TextAlignmentOptions.Left, 27);
        PlaceHudText(highScoreText, scorePanel.transform, new Vector2(22, -72), new Vector2(374, 38), TextAlignmentOptions.Left, 22);
        GameObject livesPanel = CreateHudPanel("LivesPanel", new Vector2(-48, -38), new Vector2(320, 110), true);
        PlaceHudText(healthText, livesPanel.transform, new Vector2(-22, -18), new Vector2(276, 48), TextAlignmentOptions.Right, 25);
        SetupHealthBar(livesPanel.transform);

        mainMenu = CreateMenu("MainMenu", "DINO RUN", "A tiny run through a big, strange world.", "START RUN", StartGame);
        AddButton(mainMenu.transform.Find("Card"), "EXIT", ExitGame, new Vector2(0, -175));
        pauseMenu = CreateMenu("PauseMenu", "PAUSED", "Catch your breath. The world is waiting.", "RESUME", ResumeGame);
        Transform pauseCard = pauseMenu.transform.Find("Card");
        AddButton(pauseCard, "RESTART", RestartGame, new Vector2(0, -25));
        AddButton(pauseCard, "MAIN MENU", ReturnToMainMenu, new Vector2(0, -100));
        AddButton(pauseCard, "EXIT", ExitGame, new Vector2(0, -175));
        gameOverMenu = CreateMenu("GameOverMenu", "RUN ENDED", "You made it further than last time.", "RUN AGAIN", RestartGame);
        Transform gameOverCard = gameOverMenu.transform.Find("Card");
        gameOverScoreText = CreateText(gameOverCard, "FinalScore", "SCORE  000000", 24, new Vector2(0, -15));
        AddButton(gameOverCard, "MAIN MENU", ReturnToMainMenu, new Vector2(0, -100));
        AddButton(gameOverCard, "EXIT", ExitGame, new Vector2(0, -175));
        CreateSoundHudControl();
    }

    private void CreateSoundHudControl()
    {
        GameObject buttonObject = new GameObject("SoundToggle", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-30f, 30f);
        rect.sizeDelta = new Vector2(68f, 68f);

        Image background = buttonObject.GetComponent<Image>();
        background.sprite = GetRoundedPanelSprite();
        background.type = Image.Type.Sliced;
        background.color = new Color(.055f, .105f, .19f, .94f);
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(.78f, .9f, 1f);
        colors.pressedColor = new Color(.62f, .77f, .9f);
        button.colors = colors;

        GameObject iconObject = new GameObject("SoundIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(buttonObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(9f, 9f);
        iconRect.offsetMax = new Vector2(-9f, -9f);
        soundIcon = iconObject.GetComponent<Image>();
        soundIcon.raycastTarget = false;

        button.onClick.AddListener(() =>
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.ToggleSound();
            RefreshSoundIcon();
            if (AudioManager.Instance.SoundEnabled) AudioManager.Instance.PlayScore();
        });
        soundButton = button;
        RefreshSoundIcon();
    }

    private void RefreshSoundIcon()
    {
        if (soundIcon == null) return;
        bool soundEnabled = AudioManager.Instance == null || AudioManager.Instance.SoundEnabled;
        if (soundEnabled)
        {
            if (soundOnIconSprite == null) soundOnIconSprite = CreateSoundIconSprite(true);
            soundIcon.sprite = soundOnIconSprite;
        }
        else
        {
            if (soundOffIconSprite == null) soundOffIconSprite = CreateSoundIconSprite(false);
            soundIcon.sprite = soundOffIconSprite;
        }
    }

    private static Sprite CreateSoundIconSprite(bool soundEnabled)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[size * size];
        Color speakerColor = new Color(.9f, .96f, 1f, 1f);
        Color muteColor = new Color(1f, .48f, .34f, 1f);

        bool InsidePolygon(Vector2 p, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                bool crosses = (a.y > p.y) != (b.y > p.y) &&
                    p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x;
                if (crosses) inside = !inside;
            }
            return inside;
        }

        Vector2[] speaker =
        {
            new Vector2(9, 24), new Vector2(20, 24), new Vector2(34, 13),
            new Vector2(34, 51), new Vector2(20, 40), new Vector2(9, 40)
        };

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2(x + .5f, y + .5f);
            bool muteSlash = !soundEnabled && DistanceToSegment(p, new Vector2(17, 16), new Vector2(49, 48)) < 3.2f;
            if (muteSlash)
            {
                pixels[y * size + x] = muteColor;
                continue;
            }
            if (InsidePolygon(p, speaker))
            {
                pixels[y * size + x] = speakerColor;
                continue;
            }

            float dx = p.x - 30f;
            float dy = p.y - 32f;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Abs(Mathf.Atan2(dy, dx));
            if (soundEnabled && dx > 0f && angle < .92f &&
                ((radius > 10f && radius < 13f) || (radius > 19f && radius < 22f)))
            {
                pixels[y * size + x] = speakerColor;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        sprite.name = soundEnabled ? "SoundOnIcon" : "SoundOffIcon";
        return sprite;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        float t = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared) : 0f;
        return Vector2.Distance(point, start + t * segment);
    }

    private static void SetupHealthBar(Transform parent)
    {
        GameObject background = GameObject.Find("HealthBarBackground");
        if (background == null) background = new GameObject("HealthBarBackground", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(parent, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = backgroundRect.anchorMax = Vector2.zero;
        backgroundRect.pivot = Vector2.zero;
        backgroundRect.anchoredPosition = new Vector2(22, 12);
        backgroundRect.sizeDelta = new Vector2(276, 14);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = GetRoundedPanelSprite();
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.color = new Color(.035f, .055f, .1f, .95f);
        backgroundImage.raycastTarget = false;

        Transform existingFill = background.transform.Find("HealthBarFill");
        GameObject fill = existingFill != null ? existingFill.gameObject : new GameObject("HealthBarFill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(background.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.sprite = GetRoundedPanelSprite();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = new Color(.49f, .84f, .55f);
        fillImage.raycastTarget = false;
        fill.SetActive(true);
        background.SetActive(true);
    }

    private static void EnsureInputSystemUI()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null) eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null) inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        foreach (BaseInputModule module in modules) if (module != inputModule) module.enabled = false;
        inputModule.enabled = true;
        if (inputModule.actionsAsset == null) inputModule.AssignDefaultActions();
    }

    private GameObject CreateMenu(string objectName, string title, string subtitle, string primaryButton, UnityEngine.Events.UnityAction action)
    {
        bool isMainMenu = objectName == "MainMenu";
        GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(.035f, .055f, .12f, .78f);

        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(root.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(.5f, .5f);
        cardRect.sizeDelta = isMainMenu ? new Vector2(760, 720) : new Vector2(610, 540);
        Image cardImage = card.GetComponent<Image>();
        cardImage.sprite = GetRoundedPanelSprite();
        cardImage.type = Image.Type.Sliced;
        cardImage.color = new Color(.075f, .11f, .2f, .98f);
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(.4f, .75f, .86f, .55f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        CreateText(card.transform, "Title", title, 48, new Vector2(0, isMainMenu ? 295 : 190));
        CreateText(card.transform, "Subtitle", subtitle, 19, new Vector2(0, isMainMenu ? 245 : 132));
        AddButton(card.transform, primaryButton, action, new Vector2(0, isMainMenu ? -100 : 55));
        if (isMainMenu)
        {
            CreateText(card.transform, "ItemGuideTitle", "ITEMS & HAZARDS", 20, new Vector2(0, 190));
            CreateItemGuideRow(card.transform, "SunstoneGuide", "Collect for +15 points", 146, GuideIconType.Sunstone);
            CreateItemGuideRow(card.transform, "LifeBlossomGuide", "Restore 1 heart; +5 points if health is full", 104, GuideIconType.LifeBlossom);
            CreateItemGuideRow(card.transform, "ShieldGuide", "4 sec protection; +10 points", 62, GuideIconType.AetherShield);
            CreateItemGuideRow(card.transform, "GroundHazardGuide", "Ground hazards: lose 1 heart on contact", 20, GuideIconType.Cactus, GuideIconType.SpikeRock);
            CreateItemGuideRow(card.transform, "AirHazardGuide", "Air hazards: lose 1 heart on contact", -22, GuideIconType.Bat, GuideIconType.StormCloud);
            CreateText(card.transform, "Controls", "SPACE  JUMP     HOLD SHIFT  SPEED UP     ESC  PAUSE", 15, new Vector2(0, -315));
        }
        return root;
    }

    private void CreateItemGuideRow(Transform parent, string objectName, string description, float y, params GuideIconType[] icons)
    {
        GameObject row = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = rowRect.anchorMax = new Vector2(.5f, .5f);
        rowRect.sizeDelta = new Vector2(620, 38);
        rowRect.anchoredPosition = new Vector2(0, y);
        Image rowImage = row.GetComponent<Image>();
        rowImage.sprite = GetRoundedPanelSprite();
        rowImage.type = Image.Type.Sliced;
        rowImage.color = new Color(.11f, .16f, .27f, .95f);

        for (int i = 0; i < icons.Length; i++)
        {
            GameObject icon = new GameObject($"ItemIcon{i + 1}", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(row.transform, false);
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, .5f);
            iconRect.pivot = new Vector2(0f, .5f);
            iconRect.sizeDelta = new Vector2(30, 30);
            iconRect.anchoredPosition = new Vector2(14 + i * 36, 0);
            Image iconImage = icon.GetComponent<Image>();
            iconImage.sprite = ObstacleSpawner.GetGuideIcon(icons[i]);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        TMP_Text text = CreateText(row.transform, "Description", description, 16, Vector2.zero);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = textRect.anchorMax = new Vector2(0f, .5f);
        textRect.pivot = new Vector2(0f, .5f);
        float textStart = 14 + icons.Length * 36 + 10;
        textRect.sizeDelta = new Vector2(620 - textStart - 12, 32);
        textRect.anchoredPosition = new Vector2(textStart, 0);
        text.alignment = TextAlignmentOptions.Left;
        text.enableAutoSizing = true;
        text.fontSizeMin = 13;
        text.fontSizeMax = 16;
        text.raycastTarget = false;
    }

    private TMP_Text CreateText(Transform parent, string objectName, string value, float size, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(500, 58);
        rect.anchoredPosition = anchoredPosition;
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.9f, .94f, 1f);
        text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(.015f, .03f, .08f, .85f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        return text;
    }

    private Button AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Vector2? position = null)
    {
        GameObject go = new GameObject(label.Replace(" ", "") + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(300, 58);
        rect.anchoredPosition = position ?? new Vector2(0, 5);
        Image image = go.GetComponent<Image>();
        image.sprite = GetRoundedPanelSprite(); image.type = Image.Type.Sliced;
        image.color = new Color(.98f, .61f, .32f);
        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors; colors.normalColor = new Color(.98f, .61f, .32f); colors.highlightedColor = new Color(1f, .78f, .48f); colors.pressedColor = new Color(.78f, .34f, .28f); button.colors = colors;
        if (action != null) button.onClick.AddListener(action);
        TMP_Text text = CreateText(go.transform, "Label", label, 22, Vector2.zero);
        text.color = new Color(.035f, .065f, .09f);
        return button;
    }

    private GameObject CreateHudPanel(string objectName, Vector2 position, Vector2 size, bool rightAligned)
    {
        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.pivot = rightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = panel.GetComponent<Image>();
        image.sprite = GetRoundedPanelSprite(); image.type = Image.Type.Sliced;
        image.color = new Color(.055f, .105f, .19f, .88f);
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(.42f, .68f, .82f, .45f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        panel.transform.SetAsFirstSibling();
        return panel;
    }

    private static void PlaceHudText(TMP_Text text, Transform parent, Vector2 position, Vector2 size,
        TextAlignmentOptions alignment, float fontSize)
    {
        if (text == null) return;
        text.rectTransform.SetParent(parent, false);
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0f, 1f);
        text.rectTransform.pivot = new Vector2(0f, 1f);
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = size;
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
    }

    private static Sprite GetRoundedPanelSprite()
    {
        if (roundedPanelSprite != null) return roundedPanelSprite;
        const int size = 64;
        const float radius = 11f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float cx = x < radius ? radius : x >= size - radius ? size - radius - 1 : x;
            float cy = y < radius ? radius : y >= size - radius ? size - radius - 1 : y;
            bool inside = x >= radius && x < size - radius || y >= radius && y < size - radius ||
                          (new Vector2(x - cx, y - cy).sqrMagnitude <= radius * radius);
            pixels[y * size + x] = inside ? Color.white : new Color(1f, 1f, 1f, 0f);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        roundedPanelSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        return roundedPanelSprite;
    }

    private void OnDisable() => pauseAction?.Disable();
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
        pauseAction?.Dispose();
    }
}
