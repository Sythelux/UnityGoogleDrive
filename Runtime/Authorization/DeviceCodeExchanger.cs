using System;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityGoogleDrive
{
    /// <summary>
    /// Retrieves access and refresh tokens using provided authorization code.
    /// Protocol: https://developers.google.com/identity/protocols/OAuth2WebServer#exchange-authorization-code.
    /// </summary>
    public class DeviceCodeExchanger
    {
        private const string AuthorizationPending = "authorization_pending";
#pragma warning disable 0649
        [Serializable]
        struct ExchangeResponse
        {
            public string error, error_description, access_token, refresh_token, expires_in, scope, token_type;
        }
#pragma warning restore 0649

        public event Action<DeviceCodeExchanger> OnDone;

        public bool IsDone { get; private set; }
        public bool IsError { get; private set; }
        public string AccesToken { get; private set; }
        public string RefreshToken { get; private set; }
        public bool IsPending { get; private set; } = true;

        private GoogleDriveSettings settings;
        private IClientCredentials credentials;
        private UnityWebRequest exchangeRequest;

        public DeviceCodeExchanger(GoogleDriveSettings googleDriveSettings, IClientCredentials clientCredentials)
        {
            settings = googleDriveSettings;
            credentials = clientCredentials;
        }

        public void ExchangeAuthCode(string deviceCode)
        {
            var tokenRequestForm = new WWWForm();
            tokenRequestForm.AddField("client_id", credentials.ClientId);
            tokenRequestForm.AddField("device_code", deviceCode);
            if (!string.IsNullOrEmpty(credentials.ClientSecret))
                tokenRequestForm.AddField("client_secret", credentials.ClientSecret);
            tokenRequestForm.AddField("grant_type", "urn:ietf:params:oauth:grant-type:device_code");

            exchangeRequest = UnityWebRequest.Post(credentials.TokenUri, tokenRequestForm);
            exchangeRequest.SetRequestHeader("Content-Type", GoogleDriveSettings.RequestContentType);
            exchangeRequest.SetRequestHeader("Accept", "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            exchangeRequest.SendWebRequest().completed += HandleRequestComplete;
        }

        private void HandleExchangeComplete(bool error = false)
        {
            IsError = error;
            IsDone = true;
            OnDone?.Invoke(this);
        }

        private void HandleRequestComplete(AsyncOperation requestYield)
        {
            IsPending = CheckIsPending(exchangeRequest);
            if (IsPending)
                return;

            if (CheckRequestErrors(exchangeRequest))
            {
                HandleExchangeComplete(true);
                return;
            }

            var response = JsonUtility.FromJson<ExchangeResponse>(exchangeRequest.downloadHandler.text);
            AccesToken = response.access_token;
            RefreshToken = response.refresh_token;
            HandleExchangeComplete();
        }

        private bool CheckIsPending(UnityWebRequest request)
        {
            if (request == null)
            {
                Debug.LogError("UnityGoogleDrive: Exchange auth code request failed. Request object is null.");
                return false;
            }

            if (!string.IsNullOrEmpty(request.error))
                if (request.error.Contains("428"))
                    return true;
            return false;
        }

        private static bool CheckRequestErrors(UnityWebRequest request)
        {
            if (request == null)
            {
                Debug.LogError("UnityGoogleDrive: Exchange auth code request failed. Request object is null.");
                return true;
            }

            var errorDescription = string.Empty;

            if (!string.IsNullOrEmpty(request.error))
                errorDescription += " HTTP Error: " + request.error;

            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                var response = JsonUtility.FromJson<ExchangeResponse>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(response.error))
                    errorDescription += " API Error: " + response.error + " API Error Description: " + response.error_description;
            }

            var isError = errorDescription.Length > 0;
            if (isError) Debug.LogError("UnityGoogleDrive: Exchange auth code request failed." + errorDescription);
            return isError;
        }
    }
}