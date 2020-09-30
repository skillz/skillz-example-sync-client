using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ChatDisplay : MonoBehaviour
{
    [SerializeField] Image chatImage;
    [SerializeField] int displaySeconds = 2;

    public void ShowChat(Sprite chatSprite)
    {
        chatImage.sprite = chatSprite;
        transform.localScale = new Vector3(0, 0, 1);
        gameObject.SetActive(true);
        ScaleChatSelector(0, 1, true);
    }

    public void Start()
    {
        HideChat();
    }


    public void HideChat()
    {
        gameObject.SetActive(false);
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
            Invoke("HideChat", displaySeconds);
        });
    }
}
