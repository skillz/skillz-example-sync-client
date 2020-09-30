using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
	public static ChatManager Instance { set; get; }

    [SerializeField] List<Sprite> chatSprites;
    [SerializeField] List<ChatObject> chats;
    [SerializeField] ChatDisplay playerChat;
    [SerializeField] ChatDisplay opponentChat;
    [SerializeField] Button menuButton;
    [SerializeField] private float chatCooldownSeconds = .5f;
    public bool chatsShown { private set; get; }
    private bool shouldHideChats = false;
    private bool ignoreNextTouchFrame = false;
    private bool canChat = false;

    void Awake()
    {
        if (Instance != null){
			Destroy(Instance);
		}
		Instance = this;
    }

    void Update()
    {
        // The touch to show and hide the menu conflict based on Unity's ordering. If we just showed the menu, ignore the touch this frame
        if (ignoreNextTouchFrame)
        {
            ignoreNextTouchFrame = false;
            return;
        }

        if (shouldHideChats)
        {
            HideChatOptions();
            shouldHideChats = false;
            return;
        }

        // Using mouse button and touch events for editor and non-editor support
        if ((Input.GetMouseButtonDown(0) || Input.touchCount > 0) && chatsShown)
        {
            // Only care about touch start events, dismiss the menu, let individual chats send the message thru event detection
            if (Input.GetMouseButtonDown(0) || Input.GetTouch(0).phase == TouchPhase.Began)
            {
                // Hide the chat options next frame, don't hide immediately so we don't mess with the event system
                Debug.Log("Touch start detected, hiding chat menu");
                shouldHideChats = true;
            }
        }
    }
    
    public void HandleChatMenuButtonPress()
    {
        // If currently shown or can't chat, hide options
        if (chatsShown || !canChat)
        {
            HideChatOptions();
            return;
        }
        ShowChatOptions();
    }

    public void ShowChatOptions()
    {
        chatsShown = true;
        ignoreNextTouchFrame = true;
        foreach (ChatObject chat in chats)
        {
            chat.Show(chatSprites[chat.chatId]);
        }
    }

    public void HideChatOptions()
    {
        chatsShown = false;
        foreach (ChatObject chat in chats)
        {
            chat.Hide();
        }
    }

    public void ShowPlayerChat(int chatId, bool currentPlayer)
    {
        if (chatId < 0 || chatId >= chatSprites.Count)
        {
            if (currentPlayer)
            {
                playerChat.HideChat();
                return;
            }
            opponentChat.HideChat();
            return;
        }
        
        if (currentPlayer)
        {
            playerChat.ShowChat(chatSprites[chatId]);
            return;
        }
        opponentChat.ShowChat(chatSprites[chatId]);
        return;
    }

    public void HandleChatCooldown()
    {
        StartCoroutine(HandleChatCooldownWithDelay(chatCooldownSeconds));
    }

    public void SetChatEnabled(bool enabled) 
    {
        canChat = enabled;
        menuButton.interactable = enabled;
    }

    public IEnumerator HandleChatCooldownWithDelay(float seconds)
    {
        canChat = false;
        menuButton.interactable = false;
        yield return new WaitForSeconds(seconds);
        canChat = true;
        menuButton.interactable = true;
    }
}
