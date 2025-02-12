﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;
using SolidCP.Providers.OS;

namespace SolidCP.UniversalInstaller.Core.DotnetHost
{
	public class CertificateStoreInfo
	{
		public class StoreId
		{
			public StoreName Name;
			public StoreLocation Location;
		}

		public static IEnumerable<StoreId> GetStores()
		{
			var names = new StoreName[] { StoreName.My, StoreName.Root, StoreName.TrustedPeople };
			var locations = new StoreLocation[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine };

			foreach (var name in names)
			{
				foreach (var loc in locations)
				{
					StoreId storeId = null;
					try
					{
						var store = new X509Store(name, loc);
						store.Open(OpenFlags.ReadOnly);
						storeId = new StoreId() { Name = name, Location = loc };
					}
					catch { }

					if (storeId != null) yield return storeId;
				}
			}
		}

		public static bool ExistsDirect(StoreLocation location, StoreName name, X509FindType findType, object findValue)
		{
			try
			{
				var store = new X509Store(name, location);
				store.Open(OpenFlags.ReadOnly);
				return store.Certificates.Find(findType, findValue, true)
					.OfType<X509Certificate2>()
					.Any();
			}
			catch
			{
				return false;
			}
		}

		public static string GetDotnetHost(Assembly assembly = null)
		{
			assembly = assembly ?? Assembly.GetExecutingAssembly();
			var res = assembly.GetManifestResourceNames()
				.FirstOrDefault(name => name.EndsWith("SolidCP.Setup.DotnetHost.dll"));
			var fileName = $"{Path.GetTempFileName()}.SolidCP.Setup.DotnetHost.dll";
			using (var stream = assembly.GetManifestResourceStream(res))
			using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
			{
				stream.CopyTo(file);
			}
			File.WriteAllText(fileName + ".runtimeconfig.json",
@"{
	""runtimeOptions"": {
		""tfm"": ""net8.0"",
		""framework"": {
			""name"": ""Microsoft.NETCore.App"",
			""version"": ""8.0.0""
		}
	}
}", Encoding.UTF8);
			return fileName;
		}

		public static void GetStoreNames(out string[] names, out string[] locations)
		{
			if (!OSInfo.IsWindows && OSInfo.IsMono)
			{
				var dll = GetDotnetHost();
				var output = new StringReader(Shell.Default.Exec($"dotnet \"{dll}\"").Output().Result);
				var line = output.ReadLine();
				var namesList = new List<string>();
				var locList = new List<string>();
				while (line != null)
				{
					namesList.Add(line);
					line = output.ReadLine();
					if (line != null) locList.Add(line);
				}

				names = namesList.Distinct().ToArray();
				locations = locList.Distinct().ToArray();
			}
			else
			{
				var stores = GetStores();
				names = stores.Select(s => s.Name.ToString()).Distinct().ToArray();
				locations = stores.Select(s => s.Location.ToString()).Distinct().ToArray();
			}
		}
		public static bool Exists(StoreLocation location, StoreName name, X509FindType findType, object findValue)
		{
			if (!OSInfo.IsWindows && OSInfo.IsMono)
			{
				var dll = GetDotnetHost();
				var code = Shell.Default.Exec($"dotnet \"{dll}\" {name} {location} {findType} {findValue}").ExitCode().Result;
				return code == 0;
			}
			else return ExistsDirect(location, name, findType, findValue);
		}

	}
}
