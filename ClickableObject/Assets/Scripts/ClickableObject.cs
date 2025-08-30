using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Reflection;

public class ClickableObject : MonoBehaviour
{
    #region Fields
    private static ClickableObject s_currentHover;

    public static readonly List<ClickableObject> All = new();

    [Header("ビジュアルフィードバック")]
    [SerializeField] private bool enableHighlight;
    [SerializeField] private bool enableAnimation;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private bool applyHighlightChild;
    [SerializeField] private string objectDescription;

    [Header("イベント")]
    [SerializeField] private UnityEvent[] onClick;
    [SerializeField] private UnityEvent[] onMouseEnter;
    [SerializeField] private UnityEvent[] onMouseExit;

    [Header("アニメーター")]
    [SerializeField] private string isMouseOverParameter = "isMouseOver";
    [SerializeField] private string isClickedParameter = "isClicked";

    [Header("デバッグ")]
    [SerializeField] private bool showDebugGUI;
    [SerializeField] private bool enableDebugLog;
    private bool activeClickable = true;
    private bool isGloballyBlocked;

    private Vector2 debugGUIPosition = new Vector2(10, 10);
    private int debugGUIWidth = 300;

    private Camera mainCamera;
    private Renderer objectRenderer;
    private Material originalMaterial;
    private bool isHighlighted = false;

    private bool isMouseOver = false;
    private bool wasClicked = false;
    private float clickTime = 0f;
    private int totalClickCount = 0;
    private Vector3 lastClickPosition;

    private bool raycastHit = false;
    private string lastHitObjectName = "";
    private float raycastDistance = 0f;
    private bool hasCollider = false;
    
    private Animator animator;

    public static bool AnyHovered => s_currentHover != null;

    #endregion


    #region Unity
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null) mainCamera = FindFirstObjectByType<Camera>();
        if (animator == null) animator = GetComponent<Animator>();
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null && objectRenderer.material != null) originalMaterial = objectRenderer.material;

        hasCollider = GetComponent<Collider>() != null;
        if (!hasCollider) Debug.LogWarning($"{gameObject.name}: No Collider found! Raycast will not work.");
    }
    void Update()
    {
        if (isGloballyBlocked)
        {
            if (isHighlighted)
            {
                HighlightObject(false);
            }
            return;
        }
        HandleMouseInput();
    }
    #endregion


    #region Input
    private void HandleMouseInput()
    {
        if (mainCamera == null) return;
        if (!activeClickable) return;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        raycastHit = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity);
        ClickableObject hitClickable = null;
        if (raycastHit && hit.collider != null)
        {
            hitClickable = hit.collider.GetComponent<ClickableObject>();
        }
        if (raycastHit)
        {
            lastHitObjectName = hit.collider.gameObject.name;
            raycastDistance = hit.distance;
        }
        else
        {
            lastHitObjectName = "Nothing";
            raycastDistance = 0f;
        }
        bool hitThisObject = (hitClickable == this);
        // マウスが載っているかを更新
        isMouseOver = hitThisObject;
        if (isMouseOver)
        {
            if (onMouseEnter != null && onMouseEnter.Length > 0)
            {
                foreach (var evt in onMouseEnter)
                {
                    evt.Invoke();
                }
            }
            if (s_currentHover != this)
            {
                s_currentHover = this;
            }
        }
        else
        {
            if (s_currentHover == this)
            {
                if (hitClickable == null)
                {
                    if (onMouseExit != null && onMouseExit.Length > 0)
                    {
                        foreach (var evt in onMouseExit)
                        {
                            evt.Invoke();
                        }
                    }
                    s_currentHover = null;
                }
            }
        }
        // クリック処理
        if (Input.GetMouseButtonDown(0))
        {
            if (hitThisObject)
            {
                OnClick();
                wasClicked = true;
                clickTime = Time.time;
                totalClickCount++;
                lastClickPosition = hit.point;
            }
        }
        // クリックフラグをリセット
        if (wasClicked && Time.time - clickTime > 2f)
        {
            wasClicked = false;
        }
        // マウスが載っているときハイライト表示
        if (enableHighlight && objectRenderer != null)
        {
            if (hitThisObject && !isHighlighted)
            {
                HighlightObject(true);
            }
            else if (!hitThisObject && isHighlighted)
            {
                HighlightObject(false);
            }
        }
            
            // アニメーション制御
        if (enableAnimation)
        {
            if (hitThisObject)
            {
                OnMouseEnterAnimation();
            }
            else if (!hitThisObject)
            {
                OnMouseExitAnimation();
            }
        }
    }
    #endregion
    #region Animation
    public void OnMouseEnterAnimation()
    {
        if (enableDebugLog) Debug.Log($"ClickableObject '{gameObject.name}' OnMouseEnterAnimation called");
        animator.SetBool(isMouseOverParameter, true);
    }
    public void OnMouseExitAnimation()
    {
        if (enableDebugLog) Debug.Log($"ClickableObject '{gameObject.name}' OnMouseExitAnimation called");
        animator.SetBool(isMouseOverParameter, false);
    }
    public void OnClickAnimation()
    {
        animator.SetTrigger(isClickedParameter);
        animator.SetBool(isMouseOverParameter, false);
    }

    #endregion
    #region Click

    private void OnClick()
    {
        if (animator != null && enableAnimation) OnClickAnimation();
        if (onClick != null && onClick.Length > 0)
        {
            foreach (var clickEvent in onClick)
            {
                clickEvent?.Invoke();
            }
        }
    }
    #endregion


    #region Clean
    private void OnDestroy()
    {
        if (objectRenderer != null && objectRenderer.material != null)
        {
            objectRenderer.material = originalMaterial;
        }
        if (applyHighlightChild)
        {
            foreach (Transform child in transform)
            {
                var childRenderer = child.GetComponent<Renderer>();
                if (childRenderer != null && childRenderer.material != null)
                {
                    childRenderer.material = originalMaterial;
                }
            }
        }
    }

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }
    #endregion

    #region API
    public void ChangeClickable(bool isClickable)
    {
        activeClickable = isClickable;
    }

    public void HighlightObject(bool highlight)
    {
        if (objectRenderer == null || objectRenderer.material == null) return;
        isHighlighted = highlight;
        objectRenderer.material = highlight ? highlightMaterial : originalMaterial;
        if (applyHighlightChild)
        {
            foreach (Transform child in transform)
            {
                var childRenderer = child.GetComponent<Renderer>();
                if (childRenderer != null && childRenderer.material != null)
                {
                    childRenderer.material = highlight ? highlightMaterial : originalMaterial;
                }
            }
        }
    }
    public static void SetClickablesActive(bool isActive)
    {
        foreach (var obj in ClickableObject.All)
        {
            obj.ChangeClickable(isActive);
        }
    }
    public void TriggerClick()
    {
        OnClick();
    }

    #endregion
    #region GUI
    private void OnGUI()
    {
        if (!showDebugGUI) return;
        // Expand debug window for more information
        int expandedHeight = 320;
        // Create debug GUI window
        GUI.Box(new Rect(debugGUIPosition.x, debugGUIPosition.y, debugGUIWidth, expandedHeight),
              $"Debug Info - {gameObject.name}");
        float yOffset = debugGUIPosition.y + 25;
        float lineHeight = 18f;
        // Camera status
        GUI.color = mainCamera != null ? Color.green : Color.red;
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
               $"Camera: {(mainCamera != null ? mainCamera.name : "NULL")}");
        GUI.color = Color.white;
        yOffset += lineHeight;
        // Collider status
        GUI.color = hasCollider ? Color.green : Color.red;
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
               $"Has Collider: {hasCollider}");
        GUI.color = Color.white;
        yOffset += lineHeight;
        // Object status
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
               $"Object Active: {gameObject.activeInHierarchy}");
        yOffset += lineHeight;
        // Static flags that may cause blocking
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
            $"DialogueManager.IsDialogueOpen: {DialogueManager.IsDialogueOpen}");
        yOffset += lineHeight;
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
            $"ResultManager.IsResultsOpen: {ResultManager.IsResultsOpen}");
        yOffset += lineHeight;
        // Current hover owner info
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
            $"Hover Owner: {(s_currentHover != null ? s_currentHover.gameObject.name : "None")}");
        yOffset += lineHeight;
        // Raycast hit status
        GUI.color = raycastHit ? Color.green : Color.yellow;
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
               $"Raycast Hit: {raycastHit}");
        GUI.color = Color.white;
        yOffset += lineHeight;
        // Hit object name
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
               $"Hit Object: {lastHitObjectName}");
        yOffset += lineHeight;
        // Hit distance
        if (raycastHit)
        {
            GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
                   $"Hit Distance: {raycastDistance:F2}");
        }
        yOffset += lineHeight;
        // Mouse position
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
               $"Mouse Pos: {Input.mousePosition}");
        yOffset += lineHeight;
        // Mouse over status
        string mouseOverStatus = isMouseOver ? "ON OBJECT" : "NOT ON OBJECT";
        GUI.color = isMouseOver ? Color.green : Color.white;
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
               $"Mouse Over: {mouseOverStatus}");
        GUI.color = Color.white;
        yOffset += lineHeight;
        // Click status
        string clickStatus = wasClicked ? $"CLICKED ({Time.time - clickTime:F1}s ago)" : "NOT CLICKED";
        GUI.color = wasClicked ? Color.red : Color.white;
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
               $"Click Status: {clickStatus}");
        GUI.color = Color.white;
        yOffset += lineHeight;
        // Total clicks
        GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
               $"Total Clicks: {totalClickCount}");
        yOffset += lineHeight;
        // Last click position
        if (totalClickCount > 0)
        {
            GUI.Label(new Rect(debugGUIPosition.x + 10, yOffset, debugGUIWidth - 20, lineHeight),
                   $"Last Click Pos: {lastClickPosition:F2}");
        }
    }
#endregion
}
