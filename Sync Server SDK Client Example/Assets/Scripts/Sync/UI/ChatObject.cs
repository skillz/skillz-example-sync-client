using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class ChatObject : MonoBehaviour, IPointerDownHandler
{
    public int chatId;
    [SerializeField] Image chatImage;

    //Do this when the mouse is clicked over the selectable object this script is attached to.
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log(this.gameObject.name + " was selected, sending chat");
        SyncGameController.Instance.SendChatForId(chatId);
        ChatManager.Instance.HandleChatCooldown();
        ChatManager.Instance.ShowPlayerChat(chatId, true);
    }

    public void Start()
    {
        gameObject.SetActive(false);
    }

    public void Show(Sprite chatSprite)
    {
        chatImage.sprite = chatSprite;
        transform.localScale = new Vector3(0, 0, 1);
        gameObject.SetActive(true);
        ScaleChatSelector(0, 1, true);
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            ScaleChatSelector(transform.localScale.x, 0, false);
        }
    }

    private void ScaleChatSelector(float start, float end, bool activeAtEnd)
    {
        float currentScale = start;
        float lastTime = Time.time;
        DOTween.To(() => currentScale, newScale => currentScale = newScale, end, .25f).OnUpdate(() =>
        {
            if (Time.time - lastTime > 0.05f)
            {
                transform.localScale = new Vector3(currentScale, currentScale, 1);
                lastTime = Time.time;
            }
        }).OnComplete(() =>
        {
            transform.localScale = new Vector3(end, end, 1);
            gameObject.SetActive(activeAtEnd);
        });
    }
}
