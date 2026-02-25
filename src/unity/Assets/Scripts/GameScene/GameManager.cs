using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Profiling;

/// <summary>
/// High-Performance Game Manager for 120fps+ rhythm game
/// Optimized for mobile and high refresh rate displays
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Settings
    [Header("Performance Settings")]
    [Tooltip("Target frame rate (0 = unlimited)")]
    public int targetFrameRate = 120;

    [Tooltip("Enable VSync (disable for uncapped FPS)")]
    public bool enableVSync = false;

    [Tooltip("Use fixed timestep for physics-based note movement")]
    public bool useFixedTimestep = false;

    [Header("Game Settings")]
    [Range(0, 100)]
    [Tooltip("Audio latency compensation in milliseconds")]
    public float latencyOffset = 0f;

    [Tooltip("Selected BPM: 0=120, 1=150, 2=180")]
    public int selectedBPM = 0;

    [Range(1f, 20f)]
    [Tooltip("Note scroll speed (units per second)")]
    public float noteSpeed = 10f;

    [Header("Precision Settings")]
    [Tooltip("Perfect timing window (seconds)")]
    public float perfectWindow = 0.033f; // ~2 frames at 60fps

    [Tooltip("Great timing window (seconds)")]
    public float greatWindow = 0.067f; // ~4 frames at 60fps

    [Tooltip("Good timing window (seconds)")]
    public float goodWindow = 0.100f; // ~6 frames at 60fps

    [Header("UI References")]
    public Text scoreText;
    public Text comboText;
    public Text judgementText;
    public Text fpsText;
    public GameObject noteObjPrefab;
    public Transform noteSpawnPoint;
    public Transform judgementLine;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip[] bpmTracks;
    #endregion

    #region Private Variables
    // Gameplay state
    private BPMConfig currentConfig;
    private int currentNoteIndex = 0;
    private double gameTime = 0.0;
    private double audioStartTime = 0.0;
    private bool isPlaying = false;

    // Score tracking
    private int score = 0;
    private int combo = 0;
    private int maxCombo = 0;
    private int perfectCount = 0;
    private int greatCount = 0;
    private int goodCount = 0;
    private int missCount = 0;

    // Note pooling for performance
    private Queue<GameObject> notePool = new Queue<GameObject>();
    private List<NoteController> activeNotes = new List<NoteController>(256);
    private const int POOL_SIZE = 100;

    // Performance monitoring
    private float deltaTime = 0.0f;
    private int frameCount = 0;
    private float fpsUpdateInterval = 0.5f;
    private float fpsTimer = 0f;

    // Input buffering for high precision
    private Queue<InputEvent> inputBuffer = new Queue<InputEvent>();
    private const float INPUT_BUFFER_TIME = 0.100f; // 100ms buffer

    // Cached components
    private Transform myTransform;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        // Cache transform
        myTransform = transform;

        // Set target frame rate
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = enableVSync ? 1 : 0;

        // Initialize object pool
        InitializeNotePool();

        Debug.Log($"[GameManager] Initialized with target FPS: {targetFrameRate}");
    }

    void Start()
    {
        LoadBPMConfig();
    }

    void Update()
    {
        // FPS monitoring
        UpdateFPS();

        if (!isPlaying) return;

        // Update game time using high precision
        UpdateGameTime();

        // Process input with buffering
        ProcessInput();

        // Spawn notes
        SpawnNotes();

        // Check for misses
        CheckMisses();
    }

    void FixedUpdate()
    {
        if (!isPlaying || !useFixedTimestep) return;

        // Physics-based note movement in FixedUpdate for consistency
        MoveNotesFixed();
    }

    void LateUpdate()
    {
        if (!isPlaying || useFixedTimestep) return;

        // Frame-rate independent note movement in LateUpdate
        MoveNotes();
    }
    #endregion

    #region Performance Optimization
    private void InitializeNotePool()
    {
        Profiler.BeginSample("InitializeNotePool");

        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject noteObj = Instantiate(noteObjPrefab);
            noteObj.SetActive(false);
            notePool.Enqueue(noteObj);
        }

        Profiler.EndSample();

        Debug.Log($"[GameManager] Note pool initialized with {POOL_SIZE} objects");
    }

    private GameObject GetNoteFromPool()
    {
        GameObject noteObj;

        if (notePool.Count > 0)
        {
            noteObj = notePool.Dequeue();
        }
        else
        {
            // Pool exhausted, create new object
            noteObj = Instantiate(noteObjPrefab);
            Debug.LogWarning("[GameManager] Note pool exhausted, creating new object");
        }

        noteObj.SetActive(true);
        return noteObj;
    }

    private void ReturnNoteToPool(GameObject noteObj)
    {
        noteObj.SetActive(false);
        notePool.Enqueue(noteObj);
    }

    private void UpdateFPS()
    {
        fpsTimer += Time.unscaledDeltaTime;
        frameCount++;

        if (fpsTimer >= fpsUpdateInterval)
        {
            float fps = frameCount / fpsTimer;

            if (fpsText != null)
            {
                fpsText.text = $"FPS: {fps:F1}";

                // Color code FPS
                if (fps >= targetFrameRate * 0.95f)
                    fpsText.color = Color.green;
                else if (fps >= targetFrameRate * 0.80f)
                    fpsText.color = Color.yellow;
                else
                    fpsText.color = Color.red;
            }

            frameCount = 0;
            fpsTimer = 0f;
        }
    }
    #endregion

    #region Game Control
    public void StartGame()
    {
        Profiler.BeginSample("StartGame");

        LoadBPMConfig();
        ResetGame();

        // Start audio with precise timing
        if (musicSource != null && selectedBPM < bpmTracks.Length && bpmTracks[selectedBPM] != null)
        {
            musicSource.clip = bpmTracks[selectedBPM];
            musicSource.Play();
            audioStartTime = AudioSettings.dspTime;
        }

        isPlaying = true;

        Profiler.EndSample();

        Debug.Log($"[GameManager] Game started at BPM: {currentConfig?.bpm ?? 0}");
    }

    public void PauseGame()
    {
        isPlaying = false;

        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }

        Debug.Log("[GameManager] Game paused");
    }

    public void ResumeGame()
    {
        isPlaying = true;

        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.UnPause();
        }

        Debug.Log("[GameManager] Game resumed");
    }

    public void EndGame()
    {
        isPlaying = false;

        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        Debug.Log($"[GameManager] Game ended. Final score: {score}");
    }

    public void SetBPM(int index)
    {
        selectedBPM = Mathf.Clamp(index, 0, BPMConfigurations.configs.Length - 1);
        LoadBPMConfig();

        Debug.Log($"[GameManager] BPM set to: {currentConfig?.bpm ?? 0}");
    }

    public void SetLatency(float ms)
    {
        latencyOffset = Mathf.Clamp(ms, 0f, 100f);
        Debug.Log($"[GameManager] Latency offset set to: {latencyOffset}ms");
    }

    public void SetTargetFrameRate(int fps)
    {
        targetFrameRate = fps;
        Application.targetFrameRate = fps;
        Debug.Log($"[GameManager] Target frame rate set to: {fps}");
    }
    #endregion

    #region Game Logic
    private void LoadBPMConfig()
    {
        if (selectedBPM >= 0 && selectedBPM < BPMConfigurations.configs.Length)
        {
            currentConfig = BPMConfigurations.configs[selectedBPM];
            Debug.Log($"[GameManager] Loaded BPM config: {currentConfig.bpm} BPM, {currentConfig.notes.Count} notes");
        }
        else
        {
            Debug.LogError($"[GameManager] Invalid BPM index: {selectedBPM}");
        }
    }

    private void ResetGame()
    {
        Profiler.BeginSample("ResetGame");

        gameTime = 0.0;
        audioStartTime = 0.0;
        currentNoteIndex = 0;
        score = 0;
        combo = 0;
        maxCombo = 0;
        perfectCount = greatCount = goodCount = missCount = 0;

        // Return all active notes to pool
        foreach (var noteController in activeNotes)
        {
            if (noteController != null && noteController.gameObject != null)
            {
                ReturnNoteToPool(noteController.gameObject);
            }
        }
        activeNotes.Clear();

        // Clear input buffer
        inputBuffer.Clear();

        UpdateUI();

        Profiler.EndSample();
    }

    private void UpdateGameTime()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            // Use DSP time for precise audio synchronization
            gameTime = AudioSettings.dspTime - audioStartTime;
        }
        else
        {
            // Fallback to Time.time if no audio
            gameTime += Time.deltaTime;
        }

        // Apply latency compensation
        gameTime += latencyOffset / 1000.0;
    }

    private void SpawnNotes()
    {
        if (currentConfig == null || currentNoteIndex >= currentConfig.notes.Count)
            return;

        Profiler.BeginSample("SpawnNotes");

        Note nextNote = currentConfig.notes[currentNoteIndex];

        // Calculate spawn time based on distance and speed
        float travelDistance = Vector3.Distance(noteSpawnPoint.position, judgementLine.position);
        double spawnTime = nextNote.time - (travelDistance / noteSpeed);

        if (gameTime >= spawnTime)
        {
            // Get note from pool
            GameObject noteObj = GetNoteFromPool();
            noteObj.transform.position = noteSpawnPoint.position;
            noteObj.transform.rotation = Quaternion.identity;

            // Initialize note controller
            NoteController controller = noteObj.GetComponent<NoteController>();
            if (controller == null)
            {
                controller = noteObj.AddComponent<NoteController>();
            }

            controller.Initialize(nextNote, this, noteSpeed);
            activeNotes.Add(controller);

            currentNoteIndex++;
        }

        Profiler.EndSample();
    }

    private void MoveNotes()
    {
        if (activeNotes.Count == 0) return;

        Profiler.BeginSample("MoveNotes");

        float deltaMove = noteSpeed * Time.deltaTime;
        Vector3 moveVector = Vector3.down * deltaMove;

        // Use for loop for better performance
        for (int i = 0; i < activeNotes.Count; i++)
        {
            if (activeNotes[i] != null && activeNotes[i].gameObject != null)
            {
                activeNotes[i].transform.position += moveVector;
            }
        }

        Profiler.EndSample();
    }

    private void MoveNotesFixed()
    {
        if (activeNotes.Count == 0) return;

        float deltaMove = noteSpeed * Time.fixedDeltaTime;
        Vector3 moveVector = Vector3.down * deltaMove;

        for (int i = 0; i < activeNotes.Count; i++)
        {
            if (activeNotes[i] != null && activeNotes[i].gameObject != null)
            {
                activeNotes[i].transform.position += moveVector;
            }
        }
    }

    private void ProcessInput()
    {
        Profiler.BeginSample("ProcessInput");

        // Capture input with precise timing
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            double inputTime = AudioSettings.dspTime - audioStartTime;
            inputBuffer.Enqueue(new InputEvent { time = inputTime });

            // Process immediately for low latency
            HitNote(inputTime);
        }

        // Clean old inputs from buffer
        while (inputBuffer.Count > 0 && gameTime - inputBuffer.Peek().time > INPUT_BUFFER_TIME)
        {
            inputBuffer.Dequeue();
        }

        Profiler.EndSample();
    }

    private void HitNote(double inputTime)
    {
        if (activeNotes.Count == 0) return;

        Profiler.BeginSample("HitNote");

        NoteController closestNote = null;
        float closestDistance = float.MaxValue;
        float closestTimeDiff = float.MaxValue;

        // Find closest note by timing, not just position
        foreach (var noteController in activeNotes)
        {
            if (noteController == null || noteController.gameObject == null)
                continue;

            float timeDiff = Mathf.Abs((float)(noteController.targetTime - inputTime));

            if (timeDiff < closestTimeDiff && timeDiff < goodWindow)
            {
                closestTimeDiff = timeDiff;
                closestNote = noteController;
                closestDistance = Mathf.Abs(noteController.transform.position.y - judgementLine.position.y);
            }
        }

        if (closestNote != null)
        {
            JudgeHit(closestTimeDiff);
            activeNotes.Remove(closestNote);
            ReturnNoteToPool(closestNote.gameObject);
        }

        Profiler.EndSample();
    }

    private void JudgeHit(float timeDiff)
    {
        string judgement = "";
        int points = 0;
        bool incrementCombo = false;

        if (timeDiff < perfectWindow)
        {
            judgement = "PERFECT!";
            points = 100;
            perfectCount++;
            incrementCombo = true;
        }
        else if (timeDiff < greatWindow)
        {
            judgement = "Great";
            points = 80;
            greatCount++;
            incrementCombo = true;
        }
        else if (timeDiff < goodWindow)
        {
            judgement = "Good";
            points = 50;
            goodCount++;
            incrementCombo = true;
        }
        else
        {
            judgement = "Miss";
            points = 0;
            missCount++;
            combo = 0;
        }

        if (incrementCombo)
        {
            combo++;
            maxCombo = Mathf.Max(maxCombo, combo);
        }

        score += points;

        // Update judgement display
        if (judgementText != null)
        {
            judgementText.text = judgement;
            judgementText.color = GetJudgementColor(judgement);
        }

        UpdateUI();

        Debug.Log($"[GameManager] Hit: {judgement}, Time diff: {timeDiff:F4}s, Combo: {combo}");
    }

    private void CheckMisses()
    {
        if (activeNotes.Count == 0) return;

        Profiler.BeginSample("CheckMisses");

        List<NoteController> toRemove = new List<NoteController>();
        float missThreshold = judgementLine.position.y - 0.5f;

        foreach (var noteController in activeNotes)
        {
            if (noteController != null && noteController.gameObject != null &&
                noteController.transform.position.y < missThreshold)
            {
                toRemove.Add(noteController);
            }
        }

        if (toRemove.Count > 0)
        {
            missCount += toRemove.Count;
            combo = 0;

            foreach (var noteController in toRemove)
            {
                activeNotes.Remove(noteController);
                ReturnNoteToPool(noteController.gameObject);
            }

            if (judgementText != null)
            {
                judgementText.text = "Miss";
                judgementText.color = Color.red;
            }

            UpdateUI();
        }

        Profiler.EndSample();
    }
    #endregion

    #region UI
    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";

        if (comboText != null)
            comboText.text = $"Combo: {combo}";
    }

    private Color GetJudgementColor(string judgement)
    {
        switch (judgement)
        {
            case "PERFECT!": return new Color(1f, 0.84f, 0f); // Gold
            case "Great": return new Color(0f, 1f, 0.5f); // Bright green
            case "Good": return new Color(0.3f, 0.7f, 1f); // Sky blue
            default: return new Color(1f, 0.2f, 0.2f); // Bright red
        }
    }
    #endregion

    #region Public Accessors
    public GameResult GetGameResult()
    {
        return new GameResult
        {
            score = score,
            maxCombo = maxCombo,
            perfectCount = perfectCount,
            greatCount = greatCount,
            goodCount = goodCount,
            missCount = missCount,
            bpm = currentConfig?.bpm ?? 0,
            accuracy = CalculateAccuracy()
        };
    }

    private float CalculateAccuracy()
    {
        int totalNotes = perfectCount + greatCount + goodCount + missCount;
        if (totalNotes == 0) return 0f;

        int weightedScore = perfectCount * 100 + greatCount * 80 + goodCount * 50;
        return (float)weightedScore / (totalNotes * 100) * 100f;
    }

    public bool IsPlaying() => isPlaying;
    public int GetCurrentScore() => score;
    public int GetCurrentCombo() => combo;
    public float GetCurrentFPS() => 1f / Time.deltaTime;
    #endregion
}

#region Helper Classes
[System.Serializable]
public class GameResult
{
    public int score;
    public int maxCombo;
    public int perfectCount;
    public int greatCount;
    public int goodCount;
    public int missCount;
    public int bpm;
    public float accuracy;
}

public struct InputEvent
{
    public double time;
}
#endregion