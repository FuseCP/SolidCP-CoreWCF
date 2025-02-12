﻿using SolidCP.Providers.OS;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SolidCP.EnterpriseServer;
using System.Text.RegularExpressions;

namespace SolidCP.UniversalInstaller
{
	public abstract class UnixInstaller : Installer
	{
		public override string InstallExeRootPath { get => base.InstallExeRootPath ?? $"/usr/local/{SolidCP}"; set => base.InstallExeRootPath = value; }
		public override string InstallWebRootPath { get => base.InstallWebRootPath ?? $"/var/www/{SolidCP}"; set => base.InstallWebRootPath = value; }
		public override string WebsiteLogsPath => $"/var/log/{SolidCP}";
		public virtual string UnixServiceId => "solidcp-server";
		public virtual string CertificateFolder => "Certificates";
		public UnixInstaller() : base() { }

		public override void InstallServerWebsite()
		{
			// Run SolidCP.Server as a service on Unix

			var websitePath = Path.Combine(InstallWebRootPath, ServerFolder, "bin_dotnet");
			var dll = Path.Combine(websitePath, "SolidCP.Server.dll");

			if (!File.Exists(dll) && !Debugger.IsAttached)
			{
				throw new FileNotFoundException($"The service executable {dll} was not found.");
			}

			var service = new ServiceDescription()
			{
				ServiceId = UnixServiceId,
				Directory = websitePath,
				Description = "SolidCP.Server service, the server management service for the SolidCP control panel.",
				Executable = $"dotnet {dll}",
				DependsOn = new List<string>() { "network-online.target" },
				EnvironmentVariables = new Dictionary<string, string>(),
				Restart = "on-failure",
				RestartSec = "1s",
				StartLimitBurst = "5",
				StartLimitIntervalSec = "500",
				SyslogIdentifier = UnixServiceId
			};
			service.EnvironmentVariables.Add("ASPNETCORE_ENVIRONMENT", "Production");

			ServiceController.Install(service);
			ServiceController.Enable(service.ServiceId);
			var status = ServiceController.Info(service.ServiceId);
			if (status != null && status.Status == OSServiceStatus.Running) ServiceController.Stop(service.ServiceId);
			ServiceController.Start(service.ServiceId);

			OpenFirewall(ServerSettings.Urls);
		}

		public override void RemoveServerWebsite()
		{
			var serviceId = UnixServiceId;

			if (ServiceController.Info(serviceId) != null)
			{
				ServiceController.Stop(serviceId);
				ServiceController.Disable(serviceId);
				ServiceController.Remove(serviceId);

				RemoveFirewallRule(ServerSettings.Urls);
			}
		}

		public override void OpenFirewall(int port)
		{
			if (Shell.Default.Find("ufw") != null)
			{
				Shell.Default.Exec($"ufw allow {port}/tcp");
			}
		}

		public override void RemoveFirewallRule(int port)
		{
			if (Shell.Default.Find("ufw") != null)
			{
				Shell.Default.Exec($"ufw delete allow {port}/tcp");
			}
		}

		public override void ReadServerConfiguration()
		{
			var appsettingsfile = Path.Combine(InstallWebRootPath, ServerFolder, "bin_dotnet", "appsettings.json");
			if (File.Exists(appsettingsfile))
			{
				var appsettings = JsonConvert.DeserializeObject<ServerAppSettings>(File.ReadAllText(appsettingsfile)) ?? new ServerAppSettings();
				ServerSettings.Urls = appsettings.applicationUrls;
				ServerSettings.ServerPasswordSHA = appsettings.Server?.Password ?? "";
				ServerSettings.ServerPassword = "";
				ServerSettings.LetsEncryptCertificateEmail = appsettings.LettuceEncrypt?.EmailAddress;
				ServerSettings.LetsEncryptCertificateDomains = (appsettings.LettuceEncrypt != null && appsettings.LettuceEncrypt.DomainNames != null) ? string.Join(", ", appsettings.LettuceEncrypt.DomainNames) : "";
				ServerSettings.CertificateFile = appsettings.Certificate?.File;
				ServerSettings.CertificatePassword = appsettings.Certificate?.Password;
				ServerSettings.CertificateStoreLocation = appsettings.Certificate?.StoreLocation.ToString();
				ServerSettings.CertificateStoreName = appsettings.Certificate?.StoreName.ToString();
				ServerSettings.CertificateFindType = appsettings.Certificate?.FindType.ToString();
				ServerSettings.CertificateFindValue = appsettings.Certificate?.FindValue;
			}
		}

		public override void ConfigureServer()
		{
			ServerAppSettings appsettings = null;
            var appsettingsfile = Path.Combine(InstallWebRootPath, ServerFolder, "bin_dotnet", "appsettings.json");
			if (File.Exists(appsettingsfile))
			{
				appsettings = JsonConvert.DeserializeObject<ServerAppSettings>(File.ReadAllText(appsettingsfile)) ?? new ServerAppSettings();
			}
			else
			{
				appsettings = new ServerAppSettings();
			}

			appsettings.applicationUrls = ServerSettings.Urls;
			var allowedHosts = (ServerSettings?.Urls ?? "").Split(',', ';')
				.Select(url => new Uri(url.Trim()).Host)
				.ToList();
			if (allowedHosts.Any(host => host == "localhost"))
			{
				allowedHosts.Add("127.0.0.1");
				allowedHosts.Add("::1");
			}
			if (allowedHosts.Any(host => host == "*")) appsettings.AllowedHosts = null;
			else appsettings.AllowedHosts = string.Join(";", allowedHosts.Distinct());

			if (!string.IsNullOrEmpty(ServerSettings.ServerPassword) || !string.IsNullOrEmpty(ServerSettings.ServerPasswordSHA))
			{
				string pwsha1;
				if (!string.IsNullOrEmpty(ServerSettings.ServerPassword))
				{
					pwsha1 = Utils.ComputeSHAServerPassword(ServerSettings.ServerPassword);
				} else
				{
					pwsha1 = ServerSettings.ServerPasswordSHA;
				}
				appsettings.Server = new ServerAppSettings.ServerSetting() { Password = pwsha1 };
			}

			if (!string.IsNullOrEmpty(ServerSettings.LetsEncryptCertificateEmail) && !string.IsNullOrEmpty(ServerSettings.LetsEncryptCertificateDomains))
			{
				appsettings.LettuceEncrypt = new ServerAppSettings.LettuceEncryptSetting()
				{
					AcceptTermOfService = true,
					EmailAddress = ServerSettings.LetsEncryptCertificateEmail,
					DomainNames = ServerSettings.LetsEncryptCertificateDomains
						?.Split(',', ';')
						.Select(domain => domain.Trim())
						.ToArray() ?? new string[0]
				};
			}
			else if (!string.IsNullOrEmpty(ServerSettings.CertificateFile) && !string.IsNullOrEmpty(ServerSettings.CertificatePassword))
			{
				// create a local copy of the certificate file
				var certFile = ServerSettings.CertificateFile;
				var certFolder = Path.Combine(InstallWebRootPath, CertificateFolder);
				if (!Directory.Exists(certFolder)) Directory.CreateDirectory(certFolder);
				var shadowFileName = $"{Guid.NewGuid()}.{Path.GetFileName(certFile)}";
				var shadowFile = Path.Combine(certFolder, shadowFileName);
				File.Copy(certFile, shadowFile);
				OSInfo.Unix.GrantUnixPermissions(shadowFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);

				appsettings.Certificate = new ServerAppSettings.CertificateSetting()
				{
					File = shadowFile,
					Password = ServerSettings.CertificatePassword
				};
			}
			else if (!string.IsNullOrEmpty(ServerSettings.CertificateStoreLocation) && !string.IsNullOrEmpty(ServerSettings.CertificateStoreName))
			{
				appsettings.Certificate = new ServerAppSettings.CertificateSetting()
				{
					FindValue = ServerSettings.CertificateFindValue
				};
				Enum.TryParse<StoreLocation>(ServerSettings.CertificateStoreLocation, out appsettings.Certificate.StoreLocation);
				Enum.TryParse<StoreName>(ServerSettings.CertificateStoreName, out appsettings.Certificate.StoreName);
				Enum.TryParse<X509FindType>(ServerSettings.CertificateFindType, out appsettings.Certificate.FindType);
			}

			if (string.IsNullOrEmpty(appsettings.probingPaths)) appsettings.probingPaths = "..\\bin\\netstandard";

			var path = Path.GetDirectoryName(appsettingsfile);
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);
			File.WriteAllText(appsettingsfile, JsonConvert.SerializeObject(appsettings, Formatting.Indented, new JsonSerializerSettings()
			{
				ContractResolver = new ServerAppSettings.IgnoreAllowedHostsResolver()
			}));
		}
		public override void InstallServerPrerequisites()
		{
			InstallNet8Runtime();
		}
		public override Func<string, string> UnzipFilter => Net8UnzipFilter;
		public override bool IsRunningAsAdmin()
		{
			//var uid = Mono.Posix.Syscall.getuid();
			var euid = Mono.Unix.Native.Syscall.geteuid();
			return euid == 0;
		}

		public override bool CheckOSSupported() => CheckSystemdSupported();

		public override bool CheckIISVersionSupported() => false;

		public override bool CheckSystemdSupported() => new SystemdServiceController().IsInstalled;

		public override bool CheckNetVersionSupported() => OSInfo.IsMono || OSInfo.IsCore && int.Parse(Regex.Match(OSInfo.FrameworkDescription, "[0-9]+").Value) >= 8;

		public override void RestartAsAdmin()
		{
			var password = UI.GetRootPassword();
			var assembly = Assembly.GetEntryAssembly()?.Location;
			string arguments = null;
			arguments = Environment.CommandLine;
			Shell shell = null;
			arguments = string.IsNullOrEmpty(arguments) ? assembly : arguments;
			if (OSInfo.IsMono) shell = Shell.ExecScript($"echo {password} | sudo -S mono --debug {arguments}");
			else if (OSInfo.IsCore) shell = Shell.ExecScript($"echo {password} | sudo -S dotnet {arguments}");
			else throw new NotSupportedException();
			Environment.Exit(shell.ExitCode().Result -1);
		}
	}
}
