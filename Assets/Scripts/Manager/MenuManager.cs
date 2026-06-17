using System;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Canvas Elements GO")]
    [SerializeField] private GameObject menuScreen;
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject joinRoomScreen;
    [SerializeField] private GameObject searchRoomScreen;
    [SerializeField] private GameObject createRoomScreen;
    [SerializeField] private GameObject errorScreen;

    [Header("Error Screen Elements")]
    [SerializeField] private TextMeshProUGUI errorDisplayText;
    [SerializeField] private UnityEngine.UI.Button errorActionButton;
    [SerializeField] private TextMeshProUGUI errorActionButtonText;
    
    [Header("Name Tools")]
    [SerializeField] private TextMeshProUGUI nameTool;
    [SerializeField] private TMP_InputField nameInputField;
    
    private enum ErrorContext { None, Disconnected, RoomJoinFailed, RoomCreateFailed }
    private ErrorContext _currentErrorContext = ErrorContext.None;

    private string playerName = "manaos";
    private bool firstLoad = true;

    private void Start()
    {
        PhotonManager.Instance.OnConnectedToMasterEvent += OnFinishedLoading;
        PhotonManager.Instance.OnJoinRoomFailedEvent += OnJoinRoomError;
        PhotonManager.Instance.OnDisconnectedEvent += OnDisconnectedError;
        LoadName();
        UpdateName();

        menuScreen.SetActive(false);
        joinRoomScreen.SetActive(false);
        searchRoomScreen.SetActive(false);
        createRoomScreen.SetActive(false);
        loadingScreen.SetActive(false);
        if (errorScreen != null) errorScreen.SetActive(false);

        if (PhotonManager.ShowDisconnectErrorOnLoad)
        {
            Log.Info("[UI] Critical Network Error Received, Reinitializing UI.");
            PhotonManager.ShowDisconnectErrorOnLoad = false;
            ShowError(PhotonManager.LastDisconnectErrorMessage, ErrorContext.Disconnected);
        }
        else
        {
            Log.Info("[UI] Loading Regularly.");
            loadingScreen.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (PhotonManager.Instance != null)
        {
            PhotonManager.Instance.OnConnectedToMasterEvent -= OnFinishedLoading;
            PhotonManager.Instance.OnJoinRoomFailedEvent -= OnJoinRoomError;
            PhotonManager.Instance.OnDisconnectedEvent -= OnDisconnectedError;
        }
    }
    
    private void ShowError(string message, ErrorContext context)
    {
        HideAllScreens();
        _currentErrorContext = context;

        if (errorScreen != null)
        {
            errorScreen.SetActive(true);

            if (errorDisplayText != null)
                errorDisplayText.text = message;
            if (errorActionButtonText != null)
            {
                errorActionButtonText.text = context == ErrorContext.Disconnected
                    ? "Reconnect"
                    : "Back to Menu";
            }
        }
    }
    
    private void HideAllScreens()
    {
        menuScreen.SetActive(false);
        loadingScreen.SetActive(false);
        joinRoomScreen.SetActive(false);
        searchRoomScreen.SetActive(false);
        createRoomScreen.SetActive(false);
        if (errorScreen != null) errorScreen.SetActive(false);
    }
    
    private void OnDisconnectedError(DisconnectCause cause)
    {
        ShowError($"Network Error: {cause}", ErrorContext.Disconnected);
    }

    private void OnFinishedLoading()
    {
        if (searchRoomScreen != null && searchRoomScreen.activeSelf) return;

        if (firstLoad)
        {
            firstLoad = false;
            loadingScreen.SetActive(false);
            if (errorScreen != null) errorScreen.SetActive(false);
            menuScreen.SetActive(true);
        }
    }

    private void OnJoinRoomError(short returnCode, string message)
    {
        bool isCreateError = returnCode == ErrorCode.GameIdAlreadyExists;
        ErrorContext context = isCreateError ? ErrorContext.RoomCreateFailed : ErrorContext.RoomJoinFailed;
        ShowError(message, context);
    }
    
    public void OnErrorActionButtonClicked()
    {
        switch (_currentErrorContext)
        {
            case ErrorContext.Disconnected:
                PhotonManager.ShowDisconnectErrorOnLoad = false;
                if (errorScreen != null) errorScreen.SetActive(false);
                
                if (errorDisplayText != null) 
                    errorDisplayText.text = "Attempting to reconnect...";
                loadingScreen.SetActive(true);
            
                PhotonManager.Instance.ManualReconnect();
                break;

            case ErrorContext.RoomJoinFailed:
            case ErrorContext.RoomCreateFailed:
                if (errorScreen != null) errorScreen.SetActive(false);
                menuScreen.SetActive(true);
                break;
        }

        _currentErrorContext = ErrorContext.None;
    }

    public void SetPlayerName()
    {
        string inputText = nameInputField.text;
        playerName = inputText;
        PlayerPrefs.SetString("PlayerName", inputText);
        PlayerPrefs.Save();
        PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName");
        UpdateName();
    }

    public void LoadName()
    {
        playerName = PlayerPrefs.GetString("PlayerName", "manaos");
    }
    
    private void UpdateName()
    {
        nameTool.text = playerName;
    }
}
