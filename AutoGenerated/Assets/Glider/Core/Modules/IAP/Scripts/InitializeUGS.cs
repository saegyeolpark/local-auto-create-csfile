using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace Glider.Core.IAP
{
    public class InitializeUGS : MonoBehaviour
    {
        public string environment = "production";

        async void Start()
        {
            try
            {
                var options = new InitializationOptions()
                    .SetEnvironmentName(environment);

                await UnityServices.InitializeAsync(options);
            }
            catch (Exception exception)
            {
                // An error occurred during initialization.
            }
        }
    }
}