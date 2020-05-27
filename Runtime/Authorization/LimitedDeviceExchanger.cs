using System;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityGoogleDrive
{
    /// <summary>
    /// Retrieves access and refresh tokens using provided authorization code.
    /// Protocol: https://developers.google.com/identity/protocols/OAuth2WebServer#exchange-authorization-code.
    /// </summary>
    public class LimitedDeviceExchanger
    {
        private const string googleDevicesUrl = "https://oauth2.googleapis.com/device/code";

#pragma warning disable 0649
        [Serializable]
        struct DevicesExchangeResponse
        {
            public string error, error_description, device_code, user_code, expires_in, interval, verification_url;
        }
#pragma warning restore 0649

        public event Action<LimitedDeviceExchanger> OnDone;

        public bool IsDone { get; private set; }
        public bool IsError { get; private set; }
        public string DeviceCode { get; private set; }
        public string ExpiresIn { get; private set; }
        public string Interval { get; private set; }
        public string UserCode { get; private set; }
        public string VerificationUrl { get; private set; }

        private GoogleDriveSettings settings;
        private IClientCredentials credentials;
        private UnityWebRequest exchangeRequest;

        public LimitedDeviceExchanger(GoogleDriveSettings googleDriveSettings, IClientCredentials clientCredentials)
        {
            settings = googleDriveSettings;
            credentials = clientCredentials;
        }

        public void ExchangeDeviceCode()
        {
            var tokenRequestForm = new WWWForm();
            tokenRequestForm.AddField("client_id", settings.GenericClientCredentials.ClientId);
            tokenRequestForm.AddField("scope", settings.AccessScope);

            exchangeRequest = UnityWebRequest.Post(googleDevicesUrl, tokenRequestForm);
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
            if (CheckRequestErrors(exchangeRequest))
            {
                HandleExchangeComplete(true);
                return;
            }

            var response = JsonUtility.FromJson<DevicesExchangeResponse>(exchangeRequest.downloadHandler.text);
            DeviceCode = response.device_code;
            ExpiresIn = response.expires_in;
            Interval = response.interval;
            UserCode = response.user_code;
            VerificationUrl = response.verification_url;
            HandleExchangeComplete();
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
                var response = JsonUtility.FromJson<DevicesExchangeResponse>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(response.error))
                    errorDescription += " API Error: " + response.error + " API Error Description: " + response.error_description;
            }

            var isError = errorDescription.Length > 0;
            if (isError) Debug.LogError("UnityGoogleDrive: Exchange auth code request failed." + errorDescription);
            return isError;
        }
    }
}