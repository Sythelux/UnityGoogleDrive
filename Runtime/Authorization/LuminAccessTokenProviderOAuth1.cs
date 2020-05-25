using System;
using System.Linq;
#if UNITY_LUMIN
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace UnityGoogleDrive
{
    /// <summary>
    /// Provides access token using OAuth Example of Lumin: https://developer.magicleap.com/en-us/learn/guides/sdk-oauth-windows-for-unity
    /// this is basically not working, as google doesn't allow custom redirect in OAuth2
    /// </summary>
    public class LuminAccessTokenProviderOAuth1 : IAccessTokenProvider
    {
        private const string tokenArgName = "code";

        public event Action<IAccessTokenProvider> OnDone;
        public bool IsDone { get; private set; }
        public bool IsError { get; private set; }
        public event MLDispatch.OAuthHandler oAuthEvent;

        private GoogleDriveSettings settings;
        private string authorizationCode;
        private string redirectUri = "https://accounts.google.com/o/oauth2/approval/v2/approvalnativeapp";
        private string cancelUri = "canceluri://";
        private MLResult redirectDispatcher;
        private MLResult cancelDispatcher;

        public LuminAccessTokenProviderOAuth1(GoogleDriveSettings googleDriveSettings)
        {
            settings = googleDriveSettings;
            oAuthEvent += OnAuthentication;
            // foreach (var uri in settings.GenericClientCredentials.RedirectUris.Where(uri => uri.Contains("urn")))
            //     redirectUri = uri;
            // if (string.IsNullOrEmpty(redirectUri))
            //     foreach (var uri in settings.GenericClientCredentials.RedirectUris.Where(uri => uri.Contains("localhost")))
            //         redirectUri = uri;
            redirectDispatcher = MLDispatch.OAuthRegisterSchema(redirectUri, ref oAuthEvent);
            cancelDispatcher = MLDispatch.OAuthRegisterSchema(cancelUri, ref oAuthEvent);
        }

        public void ProvideAccessToken()
        {
            if (string.IsNullOrEmpty(authorizationCode)) // Access token isn't available; retrieve it.
            {
                var authRequest = string.Format("{0}?response_type=code&scope={1}&redirect_uri={2}&client_id={3}",
                    settings.GenericClientCredentials.AuthUri,
                    Uri.EscapeDataString(settings.AccessScope),
                    Uri.EscapeDataString(redirectUri),
                    settings.GenericClientCredentials.ClientId);
                CheckAndRequestPrivilege(MLPrivileges.Id.Internet);
                CheckAndRequestPrivilege(MLPrivileges.Id.LocalAreaNetwork);
                CheckAndRequestPrivilege(MLPrivileges.Id.SecureBrowserWindow);
                Debug.Log("Requesting: " + authRequest);
                MLDispatch.OAuthOpenWindow(authRequest, cancelUri);
            }
            else
            {
                Debug.Log("AccessToken: " + authorizationCode);
                settings.CachedAccessToken = authorizationCode;
            }
        }

        private static void CheckAndRequestPrivilege(MLPrivileges.Id privilegeId)
        {
            if (MLPrivileges.CheckPrivilege(privilegeId) == MLResult.Code.PrivilegeNotGranted)
            {
                Debug.LogError(MLResult.Code.PrivilegeNotGranted + ": " + privilegeId);
                MLPrivileges.RequestPrivilege(privilegeId);
            }
        }

        private void OnAuthentication(string response, string schema)
        {
            Debug.Log("OnAuthentication: " + response + ", " + schema);
            var arguments = response.Substring(response.IndexOf(tokenArgName, StringComparison.InvariantCultureIgnoreCase)).Split('&').Select(q => q.Split('=')).ToDictionary(q => q.FirstOrDefault(), q => q.Skip(1).FirstOrDefault());
            if (arguments.ContainsKey(tokenArgName))
                authorizationCode = arguments[tokenArgName];
            OnDone?.Invoke(this);
            IsDone = true;
            IsError = schema.Contains(cancelUri);
        }
    }
}
#endif