﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
#if NETFRAMEWORK
using System.ServiceModel;
#else
using CoreWCF;
#endif
using System.Text;
using System.Threading.Tasks;

namespace SolidCP.Web.Services
{
#if NETFRAMEWORK
	public class UserNamePasswordValidator: System.IdentityModel.Selectors.UserNamePasswordValidator
#else
	public class UserNamePasswordValidator : CoreWCF.IdentityModel.Selectors.UserNamePasswordValidator
#endif
	{

		public PolicyAttribute Policy;

#if NETFRAMEWORK
		public override void Validate(string userName, string password)
#else
		public override ValueTask ValidateAsync(string userName, string password)

#endif
		{
			if (Policy != null)
			{
				if (Policy.Policy == PolicyAttribute.ServerAuthenticated && ValidateServer != null)
				{
					if (ValidateServer != null && !ValidateServer(password)) throw new FaultException("Invalid server password");
				}
				else if (Policy.Policy == PolicyAttribute.EnterpriseServerAuthenticated && ValidateEnterpriseServer != null)
				{
					if (ValidateEnterpriseServer != null && !ValidateEnterpriseServer(userName, password)) throw new FaultException("Invalid user or password");
				}
				else if (Policy.Policy == PolicyAttribute.Encrypted) { } // do not require username & password
				else throw new NotSupportedException($"Unuspported policy {Policy.Policy} on service.");
				
			}

#if !NETFRAMEWORK
			return ValueTask.CompletedTask;
#endif
		}

		public static Func<string, bool> ValidateServer;
		public static Func<string, string, bool> ValidateEnterpriseServer;

	}
}
