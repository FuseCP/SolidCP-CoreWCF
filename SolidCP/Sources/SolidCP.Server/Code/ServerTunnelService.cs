﻿using System.Net;
using System.Threading.Tasks;
using SolidCP.Web.Services;
using SolidCP.Providers;
using SolidCP.Providers.OS;
using SolidCP.Providers.Virtualization;

[assembly:TunnelService(typeof(SolidCP.Server.ServerTunnelService))]

namespace SolidCP.Server
{
    public class ServerTunnelService: ServerTunnelServiceBase
    {
        public override string CryptoKey => Settings.CryptoKey;
        public override void Authenticate(string user, string password)
        {
            PasswordValidator.Validate(password);
        }
        public override async Task<TunnelSocket> GetPveVncWebSocketAsync(string vmId, VncCredentials credentials, RemoteServerSettings serverSettings, ServiceProviderSettings providerSettings)
        {
            using (var proxmox = new VirtualizationServerProxmox())
            {
                proxmox.ProviderSettings = providerSettings;
                proxmox.ServerSettings = serverSettings;
                return await proxmox.GetPveVncWebSocketAsync(vmId, credentials);
            }
        }
    }
}
