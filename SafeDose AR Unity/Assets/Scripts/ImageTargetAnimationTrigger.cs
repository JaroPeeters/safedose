using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vuforia;

public class ImageTargetAnimationTrigger : MonoBehaviour
{
    [System.Serializable]
    struct TargetMessage
    {
        public int id;
        public string name;
        public string eventType;
    }

    public CharacterSequenceController character;
    public AppearScale appearScale;
    public ParticleTrigger particleTrigger;
    public Button playButton;
    public Text buttonText;           // legacy uGUI text (optional)
    public TMP_Text buttonTMPText;    // TMP text (auto-found if left empty)
    [Header("Labels")]
    public string playLabel = "Luister!";
    public string stopLabel = "Stop!";
    [Header("Flutter Messaging")]
    public int targetId = 1;
    public string targetName = "nurofen";
    public bool sendOnRecognized = true;
    public bool sendOnListenClick = true;
    [Header("Ragdoll")]
    public float ragdollHideDelay = 3f;
    [Tooltip("Override gravity used while ragdoll is active (e.g. 0,0,-9.81 for characters rotated 90 deg on X).")]
    public Vector3 ragdollGravity = new Vector3(0f, 0f, -9.81f);
    [Tooltip("How long the button stays hidden after triggering ragdoll.")]
    public float buttonHideSeconds = 3f;
    [Tooltip("Extra time to keep the character active so the ragdoll disappear FX can play.")]
    public float ragdollDisappearFxDuration = 0.5f;
    [Tooltip("Extra time to keep the character active so the normal disappear FX can play.")]
    public float sequenceDisappearFxDuration = 0.5f;

    bool isTracked;
    ObserverBehaviour observer;
    bool isSequencePlaying;
    bool ragdollActive;
    Coroutine ragdollRoutine;
    Coroutine sequenceFinishRoutine;
    bool gravityOverridden;
    Vector3 originalGravity;
    bool hasSentOnTrack;

    readonly List<TransformPose> originalPose = new List<TransformPose>();
    Rigidbody[] ragdollBodies = new Rigidbody[0];

    struct TransformPose
    {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    void Awake()
    {
        observer = GetComponent<ObserverBehaviour>();

        if (playButton != null)
        {
            playButton.gameObject.SetActive(false);
            playButton.onClick.AddListener(OnPlayButtonClicked);
            EnsureButtonLabelReference();
        }

        if (observer != null)
            observer.OnTargetStatusChanged += OnTargetStatusChanged;

        CacheRagdollData();
        SetButtonLabel(playLabel);
        originalGravity = Physics.gravity;

        if (character != null)
            character.onSequenceFinished += OnSequenceFinished;
    }

    void OnDestroy()
    {
        if (observer != null)
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;

        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayButtonClicked);

        if (character != null)
            character.onSequenceFinished -= OnSequenceFinished;

        RestoreGravity();
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        isTracked =
            status.Status == Status.TRACKED ||
            status.Status == Status.EXTENDED_TRACKED;

        if (playButton != null)
            playButton.gameObject.SetActive(isTracked);

        if (!isTracked)
        {
            ResetCharacterState();
            hasSentOnTrack = false;
        }
        else
        {
            ShowButtonIfTracked();
            if (sendOnRecognized && !hasSentOnTrack)
            {
                SendTargetData("recognized");
                hasSentOnTrack = true;
            }
        }
    }

    void OnPlayButtonClicked()
    {
        if (!isTracked)
            return;

        if (ragdollActive || isSequencePlaying)
        {
            // Treat click as "Stop" when sequence is running or ragdoll is active.
            StartRagdoll();
        }
        else
        {
            StartSequence();
        }
    }

    void StartSequence()
    {
        if (character == null)
            return;

        CancelRagdollRoutine();
        PrepareForPlay();
        character.PlaySequence();
        isSequencePlaying = true;
        SetButtonLabel(stopLabel);

        if (sendOnListenClick)
            SendTargetData("listen");
    }

    void OnSequenceFinished()
    {
        isSequencePlaying = false;
        if (sequenceFinishRoutine != null)
            StopCoroutine(sequenceFinishRoutine);

        sequenceFinishRoutine = StartCoroutine(SequenceFinishRoutine());
    }

    System.Collections.IEnumerator SequenceFinishRoutine()
    {
        if (sequenceDisappearFxDuration > 0f)
            yield return new WaitForSeconds(sequenceDisappearFxDuration);

        if (character != null)
            character.gameObject.SetActive(false);

        SetButtonLabel(playLabel);
        ShowButtonIfTracked();
        sequenceFinishRoutine = null;
    }

    void StartRagdoll()
    {
        if (ragdollActive)
            return;

        isSequencePlaying = false;
        ragdollActive = true;
        SetButtonLabel(playLabel);

        if (character != null)
        {
            character.StopAudio();
            character.StopAndReset();
        }

        EnableRagdoll();
        ApplyRagdollGravity();
        if (playButton != null)
            playButton.gameObject.SetActive(false);
        if (character != null)
            character.PlayRagdollSound();
        CancelRagdollRoutine();
        ragdollRoutine = StartCoroutine(RagdollHideRoutine());
    }

    System.Collections.IEnumerator RagdollHideRoutine()
    {
        float waitTime = Mathf.Max(ragdollHideDelay, buttonHideSeconds);
        yield return new WaitForSeconds(waitTime);

        if (character != null)
        {
            character.PlayRagdollDisappearFX();
            if (ragdollDisappearFxDuration > 0f)
                yield return new WaitForSeconds(ragdollDisappearFxDuration);

            DisableRagdollAndRestore();
            character.gameObject.SetActive(false);
        }

        ragdollActive = false;
        ShowButtonIfTracked();
        ragdollRoutine = null;
    }

    void PrepareForPlay()
    {
        if (character != null)
        {
            character.gameObject.SetActive(true);
            character.StopAndReset();
        }

        DisableRagdollAndRestore();
        ragdollActive = false;
    }

    void ResetCharacterState()
    {
        CancelRagdollRoutine();
        DisableRagdollAndRestore();
        ragdollActive = false;
        isSequencePlaying = false;

        if (character != null)
        {
            character.StopAndReset();
            character.gameObject.SetActive(false);
        }

        SetButtonLabel(playLabel);
    }

    void CacheRagdollData()
    {
        if (character == null)
            return;

        ragdollBodies = character.GetComponentsInChildren<Rigidbody>(true);
        originalPose.Clear();

        // Store unique transforms so we can restore the pose later.
        HashSet<Transform> visited = new HashSet<Transform>();
        foreach (var rb in ragdollBodies)
        {
            Transform t = rb.transform;
            if (t != null && visited.Add(t))
            {
                originalPose.Add(new TransformPose
                {
                    transform = t,
                    localPosition = t.localPosition,
                    localRotation = t.localRotation
                });
            }
        }

        DisableRagdollAndRestore();
    }

    void EnableRagdoll()
    {
        // Turn off animator so physics can drive the pose.
        if (character != null && character.animator != null)
            character.animator.enabled = false;

        foreach (var rb in ragdollBodies)
        {
            if (rb == null) continue;
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }
    }

    void DisableRagdollAndRestore()
    {
        foreach (var rb in ragdollBodies)
        {
            if (rb == null) continue;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (var pose in originalPose)
        {
            if (pose.transform == null) continue;
            pose.transform.localPosition = pose.localPosition;
            pose.transform.localRotation = pose.localRotation;
        }

        if (character != null && character.animator != null)
            character.animator.enabled = true;

        RestoreGravity();
    }

    void CancelRagdollRoutine()
    {
        if (ragdollRoutine != null)
        {
            StopCoroutine(ragdollRoutine);
            ragdollRoutine = null;
        }

        ShowButtonIfTracked();
    }

    void SetButtonLabel(string label)
    {
        EnsureButtonLabelReference();

        if (buttonText != null)
            buttonText.text = label;

        if (buttonTMPText != null)
            buttonTMPText.text = label;
    }

    void EnsureButtonLabelReference()
    {
        if (buttonTMPText == null && playButton != null)
            buttonTMPText = playButton.GetComponentInChildren<TMP_Text>(true);
    }

    void ApplyRagdollGravity()
    {
        if (!gravityOverridden)
        {
            originalGravity = Physics.gravity;
            gravityOverridden = true;
        }

        Physics.gravity = ragdollGravity;
    }

    void RestoreGravity()
    {
        if (!gravityOverridden)
            return;

        Physics.gravity = originalGravity;
        gravityOverridden = false;
    }

    void ShowButtonIfTracked()
    {
        if (playButton != null)
            playButton.gameObject.SetActive(isTracked);
    }

    void SendTargetData(string eventType)
    {
        TargetMessage message = new TargetMessage
        {
            id = targetId,
            name = targetName,
            eventType = eventType
        };

        string payload = JsonUtility.ToJson(message);
        SendToFlutter.Send(payload);
    }
}
