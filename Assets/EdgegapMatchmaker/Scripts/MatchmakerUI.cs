using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MatchmakerManager;

public class MatchmakerUI : MonoBehaviour
{
    public GameObject onlineUI;
    public GameObject offlineUI;

    public Button createBtn;
    public Button deleteBtn;
    public Button exitMatchBtn;

    public TMP_InputField gameModeField;

    private MatchmakerManager _matchmaker;

    private Ticket currentTicket;

    private bool isOnline = false;
    private bool isWaiting = true;
    private bool isReady = false;
    private float waitingTimeSec = 5;

    // Start is called before the first frame update
    void Start()
    {
        _matchmaker = (MatchmakerManager)ScriptableObject.CreateInstance("MatchmakerManager");

        createBtn.onClick.AddListener(CreateTicketClick);
        deleteBtn.onClick.AddListener(DeleteTicketClick);
        exitMatchBtn.onClick.AddListener(DisconnectMatch);
    }

    // Update is called once per frame
    void Update()
    {
        if (isOnline)
        {
            onlineUI.SetActive(true);
            offlineUI.SetActive(false);
        }
        else
        {
            onlineUI.SetActive(false);
            offlineUI.SetActive(true);

            if (currentTicket is null)
            {
                deleteBtn.interactable = false;

                if (string.IsNullOrEmpty(gameModeField.text))
                {
                    createBtn.interactable = false;
                }
                else
                {
                    createBtn.interactable = true;
                }

                gameModeField.interactable = true;
            }
            else
            {
                deleteBtn.interactable = true;
                createBtn.interactable = false;
                gameModeField.interactable = false;

                if (currentTicket.assignment is not null && !isReady)
                {
                    isReady = true;
                    ConnectMatch();
                }

                if (!isWaiting && !isReady)
                {
                    RefreshTicket();
                    StartCoroutine(Waiting());
                }
            }
        }
    }

    public async void CreateTicketClick()
    {
        try
        {
            Debug.Log("create");
            string mode = gameModeField.text;
            currentTicket = await _matchmaker.CreateTicket(mode);
            isWaiting = false;
        }
        catch (HttpRequestException httpEx)
        {
            Debug.Log($"Request failed;\n{httpEx.InnerException}");
            currentTicket = null;
        }
    }

    public async void DeleteTicketClick()
    {
        try
        {
            await _matchmaker.DeleteTicket(currentTicket.id);
        }
        catch(HttpRequestException httpEx)
        {
            Debug.Log($"Request failed;\n{httpEx.Message}");
        }
        finally
        {
            currentTicket = null;
        }
    }

    public void ConnectMatch()
    {
        try
        {
            _matchmaker.ConnectPlayer(currentTicket.assignment);
            isOnline = true;
        }
        catch (Exception e)
        {
            Debug.Log($"Failed to connect;\n{e.Message}");
        }
        finally
        {
            currentTicket = null;
            isWaiting = true;
            isReady = false;
        }
    }

    public void DisconnectMatch()
    {
        _matchmaker.DisconnectPlayer();
        isOnline = false;
        currentTicket = null;
        isWaiting = true;
        isReady = false;
    }

    public async void RefreshTicket()
    {
        try
        {
            Debug.Log("refresh");
            currentTicket = await _matchmaker.GetTicket(currentTicket.id);
        }
        catch (HttpRequestException httpEx)
        {
            Debug.Log($"Request failed;\n{httpEx.Message}");
            currentTicket = null;
        }
    }

    IEnumerator Waiting()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitingTimeSec);
        isWaiting = false;
    }
}
