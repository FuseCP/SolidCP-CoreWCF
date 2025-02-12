﻿using System;
using System.Threading.Tasks;
using SolidCP.Providers.OS;
using SolidCP.Providers.Virtualization;

[assembly:TunnelClient(typeof(SolidCP.EnterpriseServer.Client.EnterpriseServerTunnelClient))]

namespace SolidCP.EnterpriseServer.Client
{
    public class EnterpriseServerTunnelClient : EnterpriseServerTunnelClientBase
    {
        public string GetCryptoKey()
        {
            var system = new esSystem() { Url = ServerUrl };
            system.Credentials.UserName = Username;
            system.Credentials.Password = Password;
            return system.GetCryptoKey();
        }
        public override string CryptoKey => GetCryptoKey();
        public override async Task<TunnelSocket> GetPveVncWebSocketAsync(int serviceItemId, VncCredentials credentials)
        {
            return await GetTunnel(nameof(GetPveVncWebSocketAsync), serviceItemId, credentials);
        }
    }
}
