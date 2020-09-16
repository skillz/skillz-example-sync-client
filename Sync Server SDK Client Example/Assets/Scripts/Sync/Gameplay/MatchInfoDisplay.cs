using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchInfoDisplay : MonoBehaviour
{
    [SerializeField]
    private Text _gameStateText;

    [SerializeField]
    private Text _userIdText;
    [SerializeField]
    private Text _opponentIdText;

    [SerializeField]
    private Text _playerScoreText;
    [SerializeField]
    private Text _opponentScoreText;

    [SerializeField]
    private Text _currentGameTickText;

    [SerializeField]
    private Text _currentTickText;
    [SerializeField]
    private Text _tickRateText;


    [SerializeField]
    private Button scoreUp;
    [SerializeField]
    private Button scoreDown;

    public string GameState { 
        get {
            return _gameStateText.text;
        }
        set  {
            _gameStateText.text = value;
        }
    }
    public string UserId { 
        get {
            return _userIdText.text;
        }
        set  {
            _userIdText.text = value;
        }
    }
    public string OpponentId { 
        get {
            return _opponentIdText.text;
        }
        set  {
            _opponentIdText.text = value;
        }
    }
    public int PlayerScore { 
        get {
            return int.Parse(_playerScoreText.text);
        }
        set  {
            _playerScoreText.text = value.ToString();
        }
    }
    public int OpponentScore { 
        get {
            return int.Parse(_opponentScoreText.text);
        }
        set  {
            _opponentScoreText.text = value.ToString();
        }
    }
    public int CurrentGameTick { 
        get {
            return int.Parse(_currentGameTickText.text);
        }
        set  {
            _currentGameTickText.text = value.ToString();
        }
    }
    public int CurrentTick { 
        get {
            return int.Parse(_currentTickText.text);
        }
        set  {
            _currentTickText.text = value.ToString();
        }
    }
    public int TickRate { 
        get {
            return int.Parse(_tickRateText.text);
        }
        set  {
            _tickRateText.text = value.ToString() + "ms";
        }
    }

    public void SetInputAllowed(bool inputAllowed)
    {
        scoreDown.interactable = inputAllowed;
        scoreUp.interactable = inputAllowed;
    }
}
