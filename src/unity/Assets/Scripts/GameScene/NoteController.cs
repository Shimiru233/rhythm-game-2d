using UnityEngine;

/// <summary>
/// High-performance Note Controller
/// Optimized for 120fps+ gameplay
/// </summary>
public class NoteController : MonoBehaviour
{
    [Header("Visual Settings")]
    public SpriteRenderer spriteRenderer;
    public ParticleSystem hitEffect;

    [Header("Runtime Data")]
    public Note noteData;
    public GameManager gameManager;
    public double targetTime; // Precise target hit time
    public float speed;

    private Transform cachedTransform;
    private bool isInitialized = false;

    void Awake()
    {
        cachedTransform = transform;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(Note note, GameManager manager, float noteSpeed)
    {
        noteData = note;
        gameManager = manager;
        targetTime = note.time;
        speed = noteSpeed;
        isInitialized = true;

        // Reset visual state
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;

            // Color based on note type if applicable
            if (note.type == NoteType.Normal)
                spriteRenderer.color = Color.white;
            else if (note.type == NoteType.Hold)
                spriteRenderer.color = Color.yellow;
            else if (note.type == NoteType.Flick)
                spriteRenderer.color = Color.cyan;
        }
    }

    void OnDisable()
    {
        isInitialized = false;
    }

    public void PlayHitEffect()
    {
        if (hitEffect != null)
        {
            hitEffect.Play();
        }
    }
}

/// <summary>
/// Note data structure
/// </summary>
[System.Serializable]
public class Note
{
    public double time; // Time in seconds when note should be hit
    public int lane; // Lane number (0-3 for 4-lane game)
    public NoteType type;

    public Note(double hitTime, int noteLane = 0, NoteType noteType = NoteType.Normal)
    {
        time = hitTime;
        lane = noteLane;
        type = noteType;
    }
}

public enum NoteType
{
    Normal,
    Hold,
    Flick,
    Slide
}