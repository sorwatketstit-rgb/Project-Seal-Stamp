using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class NPCInteractable : Interactable
{
    public enum DialogueAction
    {
        Continue,       // Go to the next message
        WaitForAnswer,  // Same as Continue for now
        Stop            // End the interaction (counts as finished)
    }

    [System.Serializable]
    public class DialogueEntry
    {
        [TextArea(2, 5)] public string message;
        public DialogueAction onAction = DialogueAction.Continue;
        [Tooltip("Optional voice clips for this line. Empty = use default talking sounds.")]
        public List<AudioClip> customTalkingSounds;
    }

    [Header("UI")]
    [Tooltip("NPC talk canvas. Enabled by the button's UnityEvent, hidden again when dialogue ends.")]
    [SerializeField] private GameObject npcTalkCanvas;
    [Tooltip("The button UI script (E to interact). Disabled during dialogue, re-enabled after.")]
    [SerializeField] private Interactable buttonInteractable;
    [Tooltip("Shown only after the line finishes typing (e.g. '[E] Continue').")]
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private TMP_Text dialogueText;
    [Tooltip("Optional dialogue box panel.")]
    [SerializeField] private GameObject dialogueContainer;

    [Header("Dialogue Lists")]
    [SerializeField] private List<DialogueEntry> dialogueList = new List<DialogueEntry>();
    [Tooltip("Played instead of the first list once hasInteracted is true.")]
    [SerializeField] private List<DialogueEntry> onInteractAgain = new List<DialogueEntry>();

    [Header("Typewriter")]
    [SerializeField] private float typingSpeed = 0.035f;
    [Tooltip("Pressing the key while typing shows the full line instantly.")]
    [SerializeField] private bool allowFastForward = true;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Voice Audio (optional, everything works with none assigned)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<AudioClip> talkingSounds = new List<AudioClip>();
    [Range(0.5f, 2f)] [SerializeField] private float minPitch = 0.95f;
    [Range(0.5f, 2f)] [SerializeField] private float maxPitch = 1.05f;
    [Range(1, 5)] [SerializeField] private int soundFrequency = 1;

    [Header("Runtime State")]
    [SerializeField] private bool hasInteracted = false;

    [Header("Re-enable Delay")]
    [Tooltip("Delay in seconds before re-enabling the button prompt UI after dialogue ends with Stop.")]
    [SerializeField] private float promptReenableDelay = 1f;

    [Header("Events")]
    public UnityEvent onDialogueStart;
    public UnityEvent onDialogueEnd;
    public UnityEvent<int> onDialogueLineChanged;
    public UnityEvent onBeginDialogue; // Renamed to avoid name collision with BeginDialogue() method

    private List<DialogueEntry> activeList;
    private Coroutine typingCoroutine;
    private Transform playerTransform;
    private int currentLine;
    private int startFrame = -1;
    private bool isTyping;
    private bool isInteracting;
    private bool delayReenableActive;

    public bool IsInteracting => isInteracting;
    public bool HasInteracted { get => hasInteracted; set => hasInteracted = value; }

    protected override void Awake()
    {
        base.Awake();

        if (audioSource == null) audioSource = GetComponent<AudioSource>(); // may stay null, that's fine

        if (npcTalkCanvas == null && promptUI != null) npcTalkCanvas = promptUI;

        if (buttonInteractable == null)
        {
            foreach (Interactable it in GetComponentsInChildren<Interactable>(true))
            {
                if (it != this) { buttonInteractable = it; break; }
            }
        }

        ClearDialogueVisuals();
    }

    protected virtual void OnEnable()
    {
        StartDialogue();
    }

    /// <summary>The ONLY cleanup path. Runs for finish, leaving range, or external disable.</summary>
    protected virtual void OnDisable()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        bool wasInteracting = isInteracting;
        isTyping = false;
        isInteracting = false;
        currentLine = 0;

        ClearDialogueVisuals();

        if (npcTalkCanvas != null) npcTalkCanvas.SetActive(false);
        if (buttonInteractable != null && !delayReenableActive)
        {
            buttonInteractable.enabled = true;
            buttonInteractable.ResyncRange();
        }
        if (wasInteracting) onDialogueEnd?.Invoke();
    }

    private void StartDialogue()
    {
        if (isInteracting) return;
        if (buttonInteractable != null) buttonInteractable.enabled = false;

        activeList = (hasInteracted && onInteractAgain != null && onInteractAgain.Count > 0)
            ? onInteractAgain
            : dialogueList;

        if (activeList == null || activeList.Count == 0)
        {
            if (debugLogs) Debug.LogWarning($"[NPCInteractable:{name}] Dialogue list is empty.", this);
            enabled = false;
            return;
        }

        isInteracting = true;
        currentLine = 0;
        startFrame = Time.frameCount; // ignore the same key press that opened the dialogue

        if (dialogueContainer != null) dialogueContainer.SetActive(true);

        onDialogueStart?.Invoke();
        onBeginDialogue?.Invoke();
        DisplayLine(0);
    }

    private void Update()
    {
        if (!isInteracting) return;

        if (PlayerOutOfRange())
        {
            EndDialogue(false); // hasInteracted stays unchanged
            return;
        }

        if (Time.frameCount == startFrame || !Input.GetKeyDown(interactKey)) return;

        if (isTyping)
        {
            if (allowFastForward) FinishTyping();
        }
        else
        {
            Advance();
        }
    }

    public void BeginDialogue()
    {
        if (!enabled) enabled = true;   // OnEnable -> StartDialogue
        else StartDialogue();          // already enabled but idle
    }

    private bool PlayerOutOfRange()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else
            {
                SM64.SM64PlayerController c = FindFirstObjectByType<SM64.SM64PlayerController>();
                if (c != null) playerTransform = c.transform;
            }
            if (playerTransform == null) return false;
        }

        return Vector3.Distance(transform.position, playerTransform.position) > interactRadius;
    }

    private void Advance()
    {
        DialogueEntry entry = activeList[currentLine];

        if (entry.onAction == DialogueAction.Stop || currentLine + 1 >= activeList.Count)
        {
            hasInteracted = true;
            EndDialogue(true);
            return;
        }

        currentLine++;
        DisplayLine(currentLine);
    }

    private void EndDialogue(bool withDelay)
    {
        delayReenableActive = withDelay && promptReenableDelay > 0f;

        if (buttonInteractable != null && delayReenableActive)
        {
            buttonInteractable.EnableAfterDelay(promptReenableDelay);
        }

        enabled = false;
        delayReenableActive = false;
    }

    private void DisplayLine(int index)
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (continuePrompt != null) continuePrompt.SetActive(false);

        DialogueEntry entry = activeList[index];
        typingCoroutine = StartCoroutine(TypeText(entry.message, entry.customTalkingSounds));
        onDialogueLineChanged?.Invoke(index);
    }

    private IEnumerator TypeText(string fullText, List<AudioClip> customSounds)
    {
        isTyping = true;

        if (dialogueText != null)
        {
            dialogueText.text = fullText ?? string.Empty;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();

            int total = dialogueText.textInfo.characterCount;
            int counter = 0;

            for (int i = 0; i < total; i++)
            {
                dialogueText.maxVisibleCharacters = i + 1;

                if (!char.IsWhiteSpace(dialogueText.textInfo.characterInfo[i].character))
                {
                    counter++;
                    if (counter % soundFrequency == 0) PlayTalkingSound(customSounds);
                }

                if (typingSpeed > 0f) yield return new WaitForSeconds(typingSpeed);
            }
        }

        FinishTyping();
    }

    private void FinishTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        if (dialogueText != null) dialogueText.maxVisibleCharacters = int.MaxValue;
        if (continuePrompt != null) continuePrompt.SetActive(true);
    }

    private void PlayTalkingSound(List<AudioClip> customSounds)
    {
        if (audioSource == null) return;

        List<AudioClip> pool = (customSounds != null && customSounds.Count > 0) ? customSounds : talkingSounds;
        if (pool == null || pool.Count == 0) return;

        AudioClip clip = pool[Random.Range(0, pool.Count)];
        if (clip == null) return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip);
    }

    private void ClearDialogueVisuals()
    {
        if (continuePrompt != null) continuePrompt.SetActive(false);
        if (dialogueContainer != null) dialogueContainer.SetActive(false);
        if (dialogueText != null) dialogueText.text = string.Empty;
    }

    /// <summary>Makes the first dialogue list play again next time.</summary>
    public void ResetInteractionState()
    {
        hasInteracted = false;
    }
}