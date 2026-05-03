using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private CampusApiClient m_ApiClient;
    [SerializeField] private NavigationFlowController m_NavigationFlow;

    [HideInInspector] public TMP_InputField inputField;
    [HideInInspector] public Transform chatContainer;
    [HideInInspector] public TextMeshProUGUI statusText;
    [HideInInspector] public ScrollRect chatScrollRect;

    private readonly List<CampusApiClient.ChatHistoryMessage> m_History = new List<CampusApiClient.ChatHistoryMessage>();

    public void Configure(
        CampusApiClient apiClient,
        NavigationFlowController navigationFlow,
        TMP_InputField chatInput,
        Transform contentRoot,
        TextMeshProUGUI status,
        ScrollRect scrollRect)
    {
        m_ApiClient = apiClient;
        m_NavigationFlow = navigationFlow;
        inputField = chatInput;
        chatContainer = contentRoot;
        statusText = status;
        chatScrollRect = scrollRect;
    }

    public void SendChatMessage(string userText)
    {
        userText = (userText ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(userText))
            return;

        SpawnBubble(userText, true);
        m_History.Add(new CampusApiClient.ChatHistoryMessage { role = "user", content = userText });

        if (inputField != null)
            inputField.text = string.Empty;
        if (statusText != null)
            statusText.text = "Thinking...";

        if (m_ApiClient == null || m_NavigationFlow == null)
        {
            SpawnBubble("Chat is not configured yet.", false);
            return;
        }

        StartCoroutine(SendToBackend(userText));
    }

    private IEnumerator SendToBackend(string query)
    {
        yield return m_ApiClient.ResolveDestination(
            query,
            m_History,
            response =>
            {
                if (statusText != null)
                    statusText.text = string.Empty;

                string answer = response != null && !string.IsNullOrEmpty(response.answer)
                    ? response.answer
                    : "I could not resolve that destination.";

                m_History.Add(new CampusApiClient.ChatHistoryMessage { role = "assistant", content = answer });
                SpawnBubble(answer, false);

                if (response != null && !string.IsNullOrEmpty(response.destination))
                    m_NavigationFlow.NavigateToDestination(response.destination);
            },
            error =>
            {
                if (statusText != null)
                    statusText.text = "Error";

                SpawnBubble($"Could not reach the backend. {error}", false);
            });

        yield return new WaitForEndOfFrame();
        if (chatScrollRect != null)
            chatScrollRect.verticalNormalizedPosition = 0f;
    }

    private void SpawnBubble(string text, bool isUser)
    {
        if (chatContainer == null)
            return;

        GameObject row = new GameObject(isUser ? "UserRow" : "AssistantRow");
        row.transform.SetParent(chatContainer, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.sizeDelta = Vector2.zero;

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        rowLayout.childForceExpandWidth = false;
        rowLayout.padding = new RectOffset(12, 12, 6, 6);

        ContentSizeFitter rowFitter = row.AddComponent<ContentSizeFitter>();
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject bubble = new GameObject("Bubble");
        bubble.transform.SetParent(row.transform, false);
        Image bubbleImage = bubble.AddComponent<Image>();
        bubbleImage.color = isUser
            ? new Color(0.0f, 0.66f, 0.72f, 0.96f)
            : new Color(0.1f, 0.12f, 0.18f, 0.98f);

        LayoutElement layoutElement = bubble.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = Mathf.Min(Mathf.Max(260f, text.Length * 12f), 760f);

        ContentSizeFitter bubbleFitter = bubble.AddComponent<ContentSizeFitter>();
        bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(bubble.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(18, 12);
        textRT.offsetMax = new Vector2(-18, -12);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 26;
        tmp.color = Color.white;
        tmp.alignment = isUser ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true;

        ContentSizeFitter textFitter = textGO.AddComponent<ContentSizeFitter>();
        textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
}
