using System;
using System.Collections;
using UnityEngine;

namespace UnityGoogleDrive
{
    [RequireComponent(typeof(IShowDeviceCodeCallback))]
    public class UserDeviceAuthorizationHandler : MonoBehaviour
    {
        public static UserDeviceAuthorizationHandler instance;
        private LimitedDeviceExchanger limitedDeviceExchanger;

        private void OnEnable()
        {
            if (instance == default)
                instance = this;
            else
                enabled = false;
        }

        public static void Open(LimitedDeviceExchanger limitedDeviceExchanger, IEnumerator pollCoroutine)
        {
            instance.limitedDeviceExchanger = limitedDeviceExchanger;
            instance.GetComponent<IShowDeviceCodeCallback>().Show(limitedDeviceExchanger);
            instance.StartCoroutine(pollCoroutine);
        }
    }

    public interface IShowDeviceCodeCallback
    {
        void Show(LimitedDeviceExchanger limitedDeviceExchanger);
    }
}