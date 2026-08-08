using System.Collections;
using UnityEngine;

public class Case1001TutorialController : MonoBehaviour
{
    private const int CaseId = 1001;
    private const int TargetItemId = 101;

    [SerializeField] private Item targetItem;
    [SerializeField] private string tutorialPanelName = "tutorial_guide_panel";
    [SerializeField] private string tutorialMessageKey = "case.1001.tutorial.drag_item_101";

    private TutorialGuidePanel _tutorialPanel;
    private bool _isStarting;
    private bool _isActive;

    private void Awake()
    {
        if (targetItem == null)
        {
            targetItem = GetComponent<Item>();
        }

        SetTutorialHighlight(false);
    }

    private void OnEnable()
    {
        EventManager.Instance.EventRegister<int>(
            GameEvents.ExplorationReady,
            HandleExplorationReady);
        EventManager.Instance.EventRegister<int>(
            GameEvents.ItemAccepted,
            HandleItemAccepted);
    }

    private void OnDisable()
    {
        EventManager.Instance.EventUnregister<int>(
            GameEvents.ExplorationReady,
            HandleExplorationReady);
        EventManager.Instance.EventUnregister<int>(
            GameEvents.ItemAccepted,
            HandleItemAccepted);

        _isStarting = false;
        _isActive = false;
        SetTutorialHighlight(false);
        HideTutorialPanel();
    }

    private void HandleExplorationReady(int caseId)
    {
        if (caseId != CaseId ||
            _isStarting ||
            _isActive ||
            !GameManager.Instance.ShouldRunCase1001Tutorial)
        {
            return;
        }

        StartCoroutine(BeginTutorial());
    }

    private IEnumerator BeginTutorial()
    {
        _isStarting = true;

        if (targetItem == null || targetItem.ItemID != TargetItemId)
        {
            AbortTutorial("Case1001 tutorial requires the Item component with ID 101.");
            yield break;
        }

        Collider2D targetCollider = targetItem.GetCollider();
        CaseBoardPanel caseBoardPanel =
            UIManager.Instance.GetPanel<CaseBoardPanel>("case_board_panel");
        RectTransform settingButtonRect =
            caseBoardPanel != null
                ? caseBoardPanel.GetSettingButtonRectTransform()
                : null;

        if (targetCollider == null || settingButtonRect == null)
        {
            AbortTutorial(
                "Case1001 tutorial requires the target collider and SettingButton.");
            yield break;
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetInputEnabled(false);
        }

        bool requestCompleted = false;
        UIManager.Instance.ShowPanel<TutorialGuidePanel>(
            tutorialPanelName,
            E_UILayer.TopLayer,
            panel =>
            {
                if (!isActiveAndEnabled)
                {
                    if (panel != null)
                    {
                        UIManager.Instance.HidePanel(tutorialPanelName);
                    }
                    return;
                }

                _tutorialPanel = panel;
                requestCompleted = true;
            },
            closeOnBack: false);

        yield return new WaitUntil(() => requestCompleted);

        if (_tutorialPanel == null)
        {
            AbortTutorial("Case1001 tutorial panel could not be loaded.");
            yield break;
        }

        _tutorialPanel.Configure(targetCollider, settingButtonRect);
        SetTutorialHighlight(true);

        string message = tutorialMessageKey;
        yield return DataManager.Instance.GetLocalizedTextAsync(
            tutorialMessageKey,
            value => message = value);

        _isStarting = false;
        _isActive = true;
        EventManager.Instance.EventTrigger(GameEvents.DemonSpeak, message);
        FlowController.Instance.RefreshGameplayInput();
    }

    private void HandleItemAccepted(int itemId)
    {
        if (!_isActive || itemId != TargetItemId)
        {
            return;
        }

        _isActive = false;
        SetTutorialHighlight(false);
        HideTutorialPanel();
        GameManager.Instance.CompleteCase1001Tutorial();
    }

    private void AbortTutorial(string reason)
    {
        Debug.LogWarning(reason, this);
        _isStarting = false;
        _isActive = false;
        SetTutorialHighlight(false);
        HideTutorialPanel();
        FlowController.Instance.RefreshGameplayInput();
    }

    private void SetTutorialHighlight(bool highlighted)
    {
        if (targetItem != null)
        {
            targetItem.SetTutorialHighlighted(highlighted);
        }
    }

    private void HideTutorialPanel()
    {
        if (_tutorialPanel == null)
        {
            return;
        }

        UIManager.Instance.HidePanel(tutorialPanelName);
        _tutorialPanel = null;
    }
}
