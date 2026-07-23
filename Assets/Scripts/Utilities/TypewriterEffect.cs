using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class TypewriterEffect : MonoBehaviour
{
    [Header("文本组件")]
    [SerializeField] private TextMeshProUGUI textComponent;

    [Header("打字机参数")]
    [SerializeField] private float normalSpeed = 0.2f;      // 正常打字间隔（秒）
    [SerializeField] private float punctuationSpeed = 0.3f;  // 遇到标点符号时的停顿（秒），制造节奏感
    [SerializeField] private bool autoStart = false;         // 是否自动开始（一般手动调用）

    [Header("打字过程中的输入控制")]
    [SerializeField] private bool lockInputWhileTyping = true; // 打字时是否锁住玩家拖拽

    // 私有状态
    private string fullText;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Action onCompleteCallback;

    private void Awake()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
            textComponent.text = string.Empty;
    }

    /// <summary>
    /// 外部调用：开始打印一段文字
    /// </summary>
    /// <param name="text">要打印的文本</param>
    /// <param name="onComplete">打印完成时的回调</param>
    public void StartTyping(string text, Action onComplete = null)
    {
        // 如果正在打字，强制停止之前的
        if (isTyping && typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;
        }

        fullText = text;
        onCompleteCallback = onComplete;

        // 如果文字为空，直接完成
        if (string.IsNullOrEmpty(fullText))
        {
            textComponent.text = "";
            onCompleteCallback?.Invoke();
            return;
        }

        // 开始打字协程
        typingCoroutine = StartCoroutine(TypewriterRoutine());
    }

    /// <summary>
    /// 立即跳过打字，直接显示全文
    /// </summary>
    public void SkipTyping()
    {
        if (isTyping && typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;

            // 显示全文
            if (textComponent != null)
                textComponent.text = fullText;

            // 解锁输入
            if (lockInputWhileTyping && InteractionController.Instance != null)
                InputManager.Instance.SetInputEnabled(true);

            // 执行回调
            onCompleteCallback?.Invoke();
        }
    }

    private IEnumerator TypewriterRoutine()
    {
        isTyping = true;

        if (lockInputWhileTyping && InteractionController.Instance != null)
        {
            InputManager.Instance.SetInputEnabled(false);
            Debug.Log("【打字机】输入已锁定，玩家无法拖拽");
        }

        textComponent.text = "";
        int totalChars = fullText.Length;
        int currentIndex = 0;

        while (currentIndex < totalChars)
        {
            // 追加下一个字符
            char c = fullText[currentIndex];
            textComponent.text += c;
            currentIndex++;

            // TODO:播放打字音效

            float waitTime = normalSpeed;
            if (c == '。' || c == '！' || c == '？' || c == '.' || c == '!' || c == '?' || c == '，' || c == ',')
            {
                waitTime = punctuationSpeed;
            }

            yield return new WaitForSeconds(waitTime);
        }

        // 打字结束
        isTyping = false;
        typingCoroutine = null;

        if (lockInputWhileTyping && InteractionController.Instance != null)
        {
            InputManager.Instance.SetInputEnabled(true);
            Debug.Log("【打字机】输入已解锁，玩家可以继续拖拽");
        }

        // 执行回调（比如弹出下一句或关闭对话框）
        onCompleteCallback?.Invoke();
    }

    /// <summary>
    /// 强制清空文本（用于切换对话时重置）
    /// </summary>
    public void ClearText()
    {
        if (isTyping && typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;
            if (lockInputWhileTyping && InteractionController.Instance != null)
                InputManager.Instance.SetInputEnabled(true);
        }
        textComponent.text = string.Empty;
        fullText = string.Empty;
    }

    /// <summary>
    /// 判断是否正在打字
    /// </summary>
    public bool IsTyping => isTyping;
}
