using System;
using System.Linq;
using System.Threading;
using UnityEngine.Networking;
#if UNITY_LUMIN
using System.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace UnityGoogleDrive
{
    /// <summary>
    /// Provides access token using OAuth Example of Lumin: https://developer.magicleap.com/en-us/learn/guides/sdk-oauth-windows-for-unity
    /// this is an implementation using the google TV and limited input device workflow: https://developers.google.com/identity/protocols/oauth2/limited-input-device
    /// </summary>
    public class LuminAccessTokenProvider : IAccessTokenProvider
    {
        private const string tokenArgName = "code";


        public event Action<IAccessTokenProvider> OnDone;
        public bool IsDone { get; private set; }
        public bool IsError { get; private set; }
        public event MLDispatch.OAuthHandler oAuthEvent;

        private SynchronizationContext unitySyncContext;
        private readonly GoogleDriveSettings settings;
        private readonly AccessTokenRefresher accessTokenRefresher;
        private readonly DeviceCodeExchanger deviceCodeExchanger;
        private readonly LimitedDeviceExchanger limitedDeviceExchanger;
        private string expectedState;
        private string codeVerifier;
        private string redirectUri;
        private UnityWebRequest _deviceInitialExchangeRequest;

        public LuminAccessTokenProvider(GoogleDriveSettings googleDriveSettings)
        {
            settings = googleDriveSettings;
            unitySyncContext = SynchronizationContext.Current;

            accessTokenRefresher = new AccessTokenRefresher(settings.GenericClientCredentials);
            accessTokenRefresher.OnDone += HandleAccessTokenRefreshed;

            limitedDeviceExchanger = new LimitedDeviceExchanger(settings, settings.GenericClientCredentials);
            limitedDeviceExchanger.OnDone += HandleLimitedDeviceExchanged;

            deviceCodeExchanger = new DeviceCodeExchanger(settings, settings.GenericClientCredentials);
            deviceCodeExchanger.OnDone += HandleDeviceCodeExchanged;
        }

        public void ProvideAccessToken()
        {
            if (string.IsNullOrEmpty(settings.CachedRefreshToken))
                ExecuteFullAuth();
            else accessTokenRefresher.RefreshAccessToken(settings.CachedRefreshToken);
        }

        private void HandleProvideAccessTokenComplete(bool error = false)
        {
            IsError = error;
            IsDone = true;
            OnDone?.Invoke(this);
        }


        private void HandleAccessTokenRefreshed(AccessTokenRefresher refresher)
        {
            if (refresher.IsError)
            {
                if (Debug.isDebugBuild)
                {
                    var message = "UnityGoogleDrive: Failed to refresh access token; executing full auth procedure.";
                    if (!string.IsNullOrEmpty(refresher.Error))
                        message += $"\nDetails: {refresher.Error}";
                    Debug.Log(message);
                }

                ExecuteFullAuth();
            }
            else
            {
                settings.CachedAccessToken = refresher.AccesToken;
                HandleProvideAccessTokenComplete();
            }
        }

        private void HandleLimitedDeviceExchanged(LimitedDeviceExchanger obj)
        {
            if (limitedDeviceExchanger.IsError)
            {
                Debug.LogError("UnityGoogleDrive: Failed to open device Portal");
                HandleProvideAccessTokenComplete(true);
            }
            else
            {
                UserDeviceAuthorizationHandler.Open(limitedDeviceExchanger, PollCoroutine()); //Step 2
            }
        }

        private IEnumerator PollCoroutine() //Step 3
        {
            do
            {
                deviceCodeExchanger.ExchangeAuthCode(limitedDeviceExchanger.DeviceCode);
                yield return new WaitForSeconds(Convert.ToSingle(limitedDeviceExchanger.Interval));
            } while (deviceCodeExchanger.IsPending);
        }

        private void HandleDeviceCodeExchanged(DeviceCodeExchanger exchanger)
        {
            if (deviceCodeExchanger.IsError)
            {
                if (!deviceCodeExchanger.IsPending)
                {
                    Debug.LogError("UnityGoogleDrive: Failed to exchange code Portal");
                    HandleProvideAccessTokenComplete(true);
                }
            }
            else
            {
                settings.CachedAccessToken = exchanger.AccesToken;
                HandleProvideAccessTokenComplete();
            }
        }


        private void ExecuteFullAuth()
        {
            limitedDeviceExchanger.ExchangeDeviceCode(); // Step 1
        }
    }
}
#endif