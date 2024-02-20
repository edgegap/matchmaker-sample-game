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

    private MatchmakerManager _matchmaker;
    private Ticket currentTicket;

    public List<Toggle> modeToggles;
    private bool isOnline = false;
    private bool isWaiting = true;
    private bool isReady = false;
    private float waitingTimeSec = 5;
    private string mode = "mode.casual";

    public void ToggleValueChanged(Toggle toggle)
    {
        if (toggle.isOn)
        {
            string value = toggle.gameObject.GetComponentInChildren<TextMeshProUGUI>().text;
            mode = value;
            Debug.Log($"Changed mode to {value};");
        }
    }

    public void changeTogglesInteractState(bool state)
    {
        foreach (Toggle t in modeToggles)
        {
            t.interactable = state;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _matchmaker = (MatchmakerManager)ScriptableObject.CreateInstance("MatchmakerManager");

        createBtn.onClick.AddListener(CreateTicketClick);
        deleteBtn.onClick.AddListener(DeleteTicketClick);
        exitMatchBtn.onClick.AddListener(DisconnectMatch);

        foreach (Toggle t in modeToggles)
        {
            ToggleValueChanged(t);

            t.onValueChanged.AddListener(delegate {
                ToggleValueChanged(t);
            });
        }
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

            if (currentTicket is not null)
            {
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
            createBtn.interactable = false;
            changeTogglesInteractState(false);

            currentTicket = await _matchmaker.CreateTicket(mode);
            isWaiting = false;
            deleteBtn.interactable = true;
        }
        catch (HttpRequestException httpEx)
        {
            Debug.Log($"Request failed;\n{httpEx.InnerException}");
            currentTicket = null;
            createBtn.interactable = true;
            deleteBtn.interactable = false;
            changeTogglesInteractState(true);
        }
    }

    public async void DeleteTicketClick()
    {
        try
        {
            deleteBtn.interactable = false;
            await _matchmaker.DeleteTicket(currentTicket.id);
        }
        catch(HttpRequestException httpEx)
        {
            Debug.Log($"Request failed;\n{httpEx.Message}");
        }
        finally
        {
            currentTicket = null;
            createBtn.interactable = true;
            changeTogglesInteractState(true);
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
        deleteBtn.interactable = false;
        createBtn.interactable = true;
        changeTogglesInteractState(true);
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
