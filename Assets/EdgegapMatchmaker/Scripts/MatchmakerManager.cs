using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using kcp2k;
using System;

public class MatchmakerManager : ScriptableObject
{
    //when testing the Matchmaker locally, this value should be http://localhost:51504
    //when testing the Matchmaker's online release, you do not need to specify the port at the end of the URL
    public const string MATCHMAKER_URL = "<FRONTEND_COMPONENT_URL>";
    private readonly HttpClient _httpClient = new();
    public KcpTransport transport = (KcpTransport)NetworkManager.singleton.transport;

    /// <summary>
    /// Get a ticket's data from the Matchmaker
    /// </summary>
    /// <param name="ticketId">Ticket's ID</param>
    /// <returns>Ticket data</returns>
    public async Task<Ticket> GetTicket(string ticketId)
    {
        var response = await _httpClient.GetAsync($"{MATCHMAKER_URL}/v1/tickets/{ticketId}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error code: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        Ticket content = JsonConvert.DeserializeObject<Ticket>(responseContent);

        return content;
    }

    /// <summary>
    /// Create a new ticket for the Matchmaker
    /// </summary>
    /// <param name="modeTag">What game mode the players wants to be in</param>
    /// <returns>Ticket data</returns>
    public async Task<Ticket> CreateTicket(string modeTag)
    {
        CreateTicketPayload objectToSerialize = new()
        {
            mode = modeTag
        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(objectToSerialize), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{MATCHMAKER_URL}/v1/tickets", jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error code: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        Ticket content = JsonConvert.DeserializeObject<Ticket>(responseContent);  

        return content;
    }

    /// <summary>
    /// Delete a ticket from the Matchmaker
    /// </summary>
    /// <param name="ticketId">Ticket's ID</param>
    public async Task DeleteTicket(string ticketId)
    {
        var response = await _httpClient.DeleteAsync($"{MATCHMAKER_URL}/v1/tickets/{ticketId}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error code: {response.StatusCode}");
        }
    }

    /// <summary>
    /// Connect the player to a match
    /// </summary>
    /// <param name="assignment">Ticket's assignment data</param>
    public void ConnectPlayer(Assignment assignment)
    {
        string[] networkComponents = assignment.connection.Split(':');
        NetworkManager.singleton.networkAddress = networkComponents[0];

        if (ushort.TryParse(networkComponents[1], out ushort port))
        {
            transport.port = port;
        }
        else
        {
            throw new Exception("port couldn't be parsed");
        }

        NetworkManager.singleton.StartClient();
    }

    /// <summary>
    /// Disconnect the player from a match
    /// </summary>
    public void DisconnectPlayer()
    {
        NetworkManager.singleton.StopClient();
    }


    public class CreateTicketPayload
    {
        public string mode { get; set; }
    }

    public class Ticket
    {
        public string id { get; set; }
        public Assignment assignment { get; set; }
        public SearchFields search_fields { get; set; }
        public Dictionary<string, Extension> extensions { get; set; }
        public string create_time { get; set; }
    }

    public class SearchFields
    {
        public string[] tags { get; set; }
    }

    public class Extension
    {
        public string @type { get; set; }
        public byte[] value { get; set; }
    }

    public class Assignment
    {
        public string connection { get; set; }
        public Dictionary<string, Extension> extensions { get; set; }
    }
}
