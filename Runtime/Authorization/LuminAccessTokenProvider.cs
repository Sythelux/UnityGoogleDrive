using System;
using System.Linq;
#if UNITY_LUMIN
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace UnityGoogleDrive
{
    /// <summary>
    /// Provides access token using OAuth Example of Lumin: https://developer.magicleap.com/en-us/learn/guides/sdk-oauth-windows-for-unity
    /// </summary>
    public class LuminAccessTokenProvider : IAccessTokenProvider
    {
        private const string tokenArgName = "access_token";

        public event Action<IAccessTokenProvider> OnDone;
        public bool IsDone { get; private set; }
        public bool IsError { get; private set; }
        public event MLDispatch.OAuthHandler oAuthEvent;

        private GoogleDriveSettings settings;
        private string access_token;
        private string redirectUri;
        private string cancelUri = "canceluri://";
        private MLResult redirectDispatcher;
        private MLResult cancelDispatcher;

        public LuminAccessTokenProvider(GoogleDriveSettings googleDriveSettings)
        {
            settings = googleDriveSettings;
            oAuthEvent += OnAuthentication;
            redirectUri = settings.LoopbackUri;
            redirectDispatcher = MLDispatch.OAuthRegisterSchema(redirectUri, ref oAuthEvent);
            cancelDispatcher = MLDispatch.OAuthRegisterSchema(cancelUri, ref oAuthEvent);
        }

        public void ProvideAccessToken()
        {
            if (string.IsNullOrEmpty(access_token)) // Access token isn't available; retrieve it.
            {
                var authRequest = string.Format("{0}?response_type=token&scope={1}&redirect_uri={2}&client_id={3}",
                    settings.GenericClientCredentials.AuthUri,
                    settings.AccessScope,
                    Uri.EscapeDataString(redirectUri),
                    settings.GenericClientCredentials.ClientId);

                Debug.Log("Requesting: " + authRequest);
                MLDispatch.OAuthOpenWindow(authRequest, cancelUri);
            }
            else
            {
                Debug.Log("AccessToken: " + access_token);
                settings.CachedAccessToken = access_token;
            }
        }

        private void OnAuthentication(string response, string schema)
        {
            Debug.Log("OnAuthentication: " + response + ", " + schema);
            var arguments = response.Substring(response.IndexOf(tokenArgName, StringComparison.InvariantCultureIgnoreCase)).Split('&').Select(q => q.Split('=')).ToDictionary(q => q.FirstOrDefault(), q => q.Skip(1).FirstOrDefault());
            if (arguments.ContainsKey(tokenArgName))
                access_token = arguments[tokenArgName];
            OnDone?.Invoke(this);
            IsDone = true;
            IsError = schema.Contains(cancelUri);
        }
    }
}
#endif