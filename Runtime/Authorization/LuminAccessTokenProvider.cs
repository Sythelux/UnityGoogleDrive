using System;
using System.Linq;
#if PLATFORM_LUMIN
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
        private string redirectUri = "redirecturi://";
        private string cancelUri = "canceluri://";
        private MLResult redirectDispatcher;
        private MLResult cancelDispatcher;

        public LuminAccessTokenProvider(GoogleDriveSettings googleDriveSettings)
        {
            settings = googleDriveSettings;
            oAuthEvent += OnAuthentication;
        }

        public void ProvideAccessToken()
        {
            if (string.IsNullOrEmpty(access_token)) // Access token isn't available; retrieve it.
            {
                if (redirectDispatcher == default)
                    redirectDispatcher = MLDispatch.OAuthRegisterSchema(redirectUri, ref oAuthEvent);
                if (cancelDispatcher == default)
                    cancelDispatcher = MLDispatch.OAuthRegisterSchema(cancelUri, ref oAuthEvent);

                var authRequest = string.Format("{0}?response_type=token&scope={1}&redirect_uri={2}&client_id={3}",
                    settings.GenericClientCredentials.AuthUri,
                    settings.AccessScope,
                    redirectUri,
                    settings.GenericClientCredentials.ClientId);

                MLDispatch.OAuthOpenWindow(authRequest, cancelUri);
            }
            else
            {
                settings.CachedAccessToken = access_token;
            }
        }

        private void OnAuthentication(string response, string schema)
        {
            Debug.Log("OnAuthentication: " + response + ", " + schema);
            var arguments = response.Substring(response.IndexOf(tokenArgName, StringComparison.InvariantCultureIgnoreCase)).Split('&').Select(q => q.Split('=')).ToDictionary(q => q.FirstOrDefault(), q => q.Skip(1).FirstOrDefault());
            if (arguments.ContainsKey(tokenArgName))
                access_token = arguments[tokenArgName];
        }
    }
}
#endif