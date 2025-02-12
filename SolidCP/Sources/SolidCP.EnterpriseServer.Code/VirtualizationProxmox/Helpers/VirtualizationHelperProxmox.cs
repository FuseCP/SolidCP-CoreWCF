﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolidCP.Server.Client;
//using SolidCP.Providers.VirtualizationProxmox;

namespace SolidCP.EnterpriseServer.Code.VirtualizationProxmox
{
    public class VirtualizationHelperProxmox: ControllerBase
    {
        public VirtualizationHelperProxmox(ControllerBase provider) : base(provider) { }

        public VirtualizationServerProxmox GetVirtualizationProxy(int serviceId)
        {
            VirtualizationServerProxmox ws = new VirtualizationServerProxmox();
            ServiceProviderProxy.Init(ws, serviceId);
            return ws;
        }
    }
}
