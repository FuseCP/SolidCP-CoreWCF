// Copyright (c) 2016, SolidCP
// SolidCP is distributed under the Creative Commons Share-alike license
// 
// SolidCP is a fork of WebsitePanel:
// Copyright (c) 2015, Outercurve Foundation.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must  retain  the  above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form  must  reproduce the  above  copyright  notice,
//   this list of conditions  and  the  following  disclaimer in  the documentation
//   and/or other materials provided with the distribution.
//
// - Neither  the  name  of  the  Outercurve Foundation  nor   the   names  of  its
//   contributors may be used to endorse or  promote  products  derived  from  this
//   software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING,  BUT  NOT  LIMITED TO, THE IMPLIED
// WARRANTIES  OF  MERCHANTABILITY   AND  FITNESS  FOR  A  PARTICULAR  PURPOSE  ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL,  SPECIAL,  EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO,  PROCUREMENT  OF  SUBSTITUTE  GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)  HOWEVER  CAUSED AND ON
// ANY  THEORY  OF  LIABILITY,  WHETHER  IN  CONTRACT,  STRICT  LIABILITY,  OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE)  ARISING  IN  ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#if NETFRAMEWORK
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Net;
using System.Timers;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using SolidCP.Web.Clients;

namespace SolidCP.EnterpriseServer
{
	public class Global : System.Web.HttpApplication
	{
		private int keepAliveMinutes = 10;
		private static string keepAliveUrl = "";
		private static System.Timers.Timer timer = null;

		protected void Application_Start(object sender, EventArgs e)
		{
			//if (!Debugger.IsAttached) Debugger.Launch();
			UsernamePasswordValidator.Init();
			Web.Clients.CertificateValidator.Init();
			Web.Services.StartupNetFX.Start();
			Web.Clients.AssemblyLoader.Init(null, null, false);
		}

		protected void Application_End(object sender, EventArgs e)
		{
			ClientBase.DisposeAllSshTunnels();
			Web.Clients.AssemblyLoader.Dispose();
		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{
			// ASP.NET Integration Mode workaround
			if (String.IsNullOrEmpty(keepAliveUrl))
			{
				// init keep-alive
				keepAliveUrl = HttpContext.Current.Request.Url.ToString();
				if (this.keepAliveMinutes > 0)
				{
					timer = new System.Timers.Timer(60000 * this.keepAliveMinutes);
					timer.Elapsed += new ElapsedEventHandler(KeepAlive);
					timer.AutoReset = true;
					timer.Start();
				}
			}
		}

		public override void Init()
		{

		}

		private void KeepAlive(Object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				using (HttpWebRequest.Create(keepAliveUrl).GetResponse()) { }
			}
			catch { }
		}
	}
}
#endif