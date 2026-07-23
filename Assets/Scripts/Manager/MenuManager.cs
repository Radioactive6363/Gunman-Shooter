using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    
    [Header("Stats UI")]
    [SerializeField] private TextMeshProUGUI winsDisplay;
    
    [Header("Avatar UI")]
    [SerializeField] private AvatarDatabase avatarDatabase;
    [SerializeField] private Image avatarPreviewImage;
    [SerializeField] private TextMeshProUGUI avatarNameDisplay;
    [SerializeField] private TextMeshProUGUI avatarConfirmationText;
    private Coroutine _confirmationCoroutine;
    
    [Header("Color UI")]
    [SerializeField] private FlexibleColorPicker fcp; 
    private string _currentColorHex = "#FFFFFF";
    
    private int _currentAvatarIndex = 0;
    
    private enum ErrorContext { None, Disconnected, RoomJoinFailed, RoomCreateFailed }
    private ErrorContext _currentErrorContext = ErrorContext.None;

    private string playerName = "Default Name";
    private bool firstLoad = true;

    private void Start()
    {
        PhotonManager.Instance.OnConnectedToMasterEvent += OnFinishedLoading;
        PhotonManager.Instance.OnJoinRoomFailedEvent += OnJoinRoomError;
        PhotonManager.Instance.OnDisconnectedEvent += OnDisconnectedError;
        
        if (PhotonManager.Instance != null && PhotonManager.Instance.LocalProfile != null)
        {
            playerName = PhotonManager.Instance.LocalProfile.nickname;
            _currentColorHex = PhotonManager.Instance.LocalProfile.colorHex;
            _currentAvatarIndex = PhotonManager.Instance.LocalProfile.avatarId;
            
            if (ColorUtility.TryParseHtmlString(_currentColorHex, out Color parsedColor))
            {
                if (fcp != null) fcp.color = parsedColor; 
            }
            
            if (winsDisplay != null)
                winsDisplay.text = $"Wins: {PhotonManager.Instance.LocalProfile.wins}";
            
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["AvatarID"] = _currentAvatarIndex;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        else
        {
            playerName = PhotonNetwork.NickName;
        }
        
        UpdateName();

        HideAllScreens();

        if (PhotonManager.ShowDisconnectErrorOnLoad)
        {
            Log.Info("[UI] Critical Network Error Received, Reinitializing UI.");
            PhotonManager.ShowDisconnectErrorOnLoad = false;
            ShowError(PhotonManager.LastDisconnectErrorMessage, ErrorContext.Disconnected);
        }
        else if (PhotonNetwork.InLobby || PhotonNetwork.IsConnectedAndReady)
        {
            Log.Info("[UI] Already connected, showing menu.");
            firstLoad = false;
            menuScreen.SetActive(true);
        }
        else
        {
            Log.Info("[UI] Loading Regularly.");
            loadingScreen.SetActive(true);
        }
        
        if (avatarConfirmationText != null) 
            avatarConfirmationText.text = "";
        UpdateAvatarUI();
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
        
        if (string.IsNullOrEmpty(inputText)) 
        {
            Log.Warning("Name cant be Empty");
            return;
        }

        playerName = inputText;
        
        PhotonManager.Instance.LocalProfile.nickname = playerName;
        PhotonManager.Instance.SaveCurrentProfile();
        
        UpdateName();
    }
    
    private void UpdateName()
    {
        nameTool.text = playerName;
        
        if (nameInputField != null && string.IsNullOrEmpty(nameInputField.text))
        {
            nameInputField.text = playerName; 
        }
    }
    
    public void NextAvatar()
    {
        if (avatarDatabase == null || avatarDatabase.availableAvatars.Length == 0) return;
        
        _currentAvatarIndex++;
        if (_currentAvatarIndex >= avatarDatabase.availableAvatars.Length)
        {
            _currentAvatarIndex = 0;
        }
        UpdateAvatarUI();
    }
    
    public void PreviousAvatar()
    {
        if (avatarDatabase == null || avatarDatabase.availableAvatars.Length == 0) return;
        
        _currentAvatarIndex--;
        if (_currentAvatarIndex < 0)
        {
            _currentAvatarIndex = avatarDatabase.availableAvatars.Length - 1;
        }
        UpdateAvatarUI();
    }
    
    private void UpdateAvatarUI()
    {
        AvatarData currentData = avatarDatabase.availableAvatars[_currentAvatarIndex];
        
        if (avatarPreviewImage != null)
            avatarPreviewImage.sprite = currentData.avatarIcon;
            
        if (avatarNameDisplay != null)
            avatarNameDisplay.text = currentData.avatarName;
    }

    public void ConfirmAvatarSelection()
    {
        PhotonManager.Instance.LocalProfile.avatarId = _currentAvatarIndex;
        PhotonManager.Instance.LocalProfile.colorHex = _currentColorHex;
        PhotonManager.Instance.SaveCurrentProfile();
        
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["AvatarID"] = _currentAvatarIndex;
        props["ColorHex"] = _currentColorHex;   
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        
        if (avatarConfirmationText != null)
        {
            if (_confirmationCoroutine != null) StopCoroutine(_confirmationCoroutine);
            _confirmationCoroutine = StartCoroutine(ShowConfirmationRoutine());
        }
    }
    
    private IEnumerator ShowConfirmationRoutine()
    {
        avatarConfirmationText.text = "Avatar Confirmed!";
        avatarConfirmationText.color = Color.green; 
        
        yield return new WaitForSeconds(2.5f);
        
        if (avatarConfirmationText != null)
            avatarConfirmationText.text = "";
    }
    
    public void OnColorWheelChanged(Color newColor)
    {
        _currentColorHex = "#" + ColorUtility.ToHtmlStringRGB(newColor);
    }
}
