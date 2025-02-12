﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using RestSharp;
using System.Threading;
using SolidCP.Providers.Virtualization.Proxmox;
using SolidCP.Providers.HostedSolution;
using System.Security.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Shared;

namespace SolidCP.Providers.Virtualization
{
	public class ApiClient: PveClient, IDisposable
	{
		private string baseUrl;
		//private string node;
		private ApiTicket apiTicket;
		private Proxmoxvps Provider { get; set; }

		private const string TaskOk = "TASK OK";
		private const string RequestRootElement = "data";
		public ApiClient(Proxmoxvps provider): base(provider.Server.Ip, int.Parse(provider.Server.Port))
		{
			this.baseUrl = "https://" + provider.Server.Ip + ":" + provider.Server.Port + "/api2/json";
			Provider = provider;
			this.ValidateCertificate = provider.Server.ValidateCertificate;
			//this.node = node;
			//HostedSolutionLog.DebugInfo("APIClient: {0}", this.baseUrl);
		}

		RestClient GetRestClient(int timeout = 0)
		{
			var options = new RestClientOptions(baseUrl)
			{
				RemoteCertificateValidationCallback = Provider.Server.ValidateCertificate ? null :
				(sender, certificate, chain, sslPolicyErrors) =>
					true
			};
			if (timeout <= 0) options.Timeout = null;
			else options.Timeout = TimeSpan.FromMilliseconds(timeout);
			return new RestClient(options);
		}
		public Result Login(User user)
		{
			/* //HostedSolutionLog.DebugInfo("Login"); 
			var restClient = GetRestClient();
			//HostedSolutionLog.DebugInfo("Login - baseUrl: {0}", baseUrl);
			var request = new RestRequest("access/ticket", Method.Post);
			//request.AddHeader("content-type", "application/json");
			request.RequestFormat = DataFormat.Json;
			request.AddParameter("username", user.Username);
			request.AddParameter("password", user.Password);
			request.AddParameter("realm", user.Realm);
			//request.RootElement = RequestRootElement;
			//HostedSolutionLog.DebugInfo("Login - request: {0}", request.ToString());
			var response = restClient.Execute(request);
			if (response == null || response.Content == null || response.StatusCode == HttpStatusCode.ServiceUnavailable) throw new Exception($"Proxmox Server API Service at {baseUrl} unavaliable.\n{response.ErrorMessage}\n{response.ErrorException}");
			//HostedSolutionLog.DebugInfo("Login - response Content: {0}", response.Content.ToString());
			JObject json = JObject.Parse(response.Content);

			var data = json["data"];
			ApiTicket apiTicketdata = new ApiTicket();

			apiTicketdata.ticket = (string)data["ticket"];
			apiTicketdata.username = (string)data["username"];
			apiTicketdata.CSRFPreventionToken = (string)data["CSRFPreventionToken"];
			apiTicket = apiTicketdata;
			//HostedSolutionLog.DebugInfo("Login - apiTicket: {0}", apiTicket.ticket);
			return response; */

			try
			{
				if (!LoginAsync($"{user.Username}@{(string.IsNullOrEmpty(user.Realm) ? "pam" : user.Realm)}", user.Password).Result)
					throw new Exception($"Proxmox Server API Service at {baseUrl} unavaliable.\n{LastResult.ReasonPhrase}");
			} catch (Exception ex)
			{
				throw new AuthenticationException(ex.Message, ex);
			}
			ApiTicket apiTicketdata = new ApiTicket();
			dynamic data = LastResult.ToData();
			apiTicketdata.ticket = data.ticket;
            apiTicketdata.username = data.username;
            apiTicketdata.CSRFPreventionToken = data.CSRFPreventionToken;
            apiTicket = apiTicketdata;

			return LastResult;
        }


        public dynamic Status(string vmId)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PrepareGetRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/status/current");
			var response = client.Execute<VMStatusInfo>(request);
			
			//HostedSolutionLog.DebugInfo("Status - response Content: {0}", response.Content.ToString());
			dynamic json = JObject.Parse(response.Content);
			return json;
		}

		public RestResponse<Upid> Start(string vmId)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PreparePostRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/status/start");
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> Stop(string vmId)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PreparePostRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/status/stop");
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> Shutdown(string vmId)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PreparePostRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/status/shutdown");
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> Reset(string vmId)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PreparePostRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/status/reset");
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> Suspend(string vmId)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PreparePostRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/status/suspend");
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> Resume(string vmId)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PreparePostRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/status/resume");
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> ChangeName(string vmId, string newhostname)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = new RestRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/config", Method.Post);
			request.RequestFormat = DataFormat.Json;
			request.AddHeader("CSRFPreventionToken", apiTicket.CSRFPreventionToken);
			request.AddCookie("PVEAuthCookie", apiTicket.ticket, new Uri(baseUrl).AbsolutePath, new Uri(baseUrl).Host);
			//request.RootElement = "root";
			request.AddParameter("name", newhostname);
			return client.Execute<Upid>(request);
		}


		public RestResponse<NodeStatus> NodeStatus(string node)
		{
			var client = GetRestClient();
			var request = PrepareGetRequest($"nodes/{node}/status");
			return client.Execute<NodeStatus>(request);
		}


		public RestResponse<VMConfig> VMConfig(string vmId)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PrepareGetRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/config");
			return client.Execute<VMConfig>(request);
		}

		public RestResponse<Upid> Delete(string vmId)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PrepareDeleteRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}");
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> Unlink(string vmId, string device)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = new RestRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/unlink", Method.Put);
			request.RequestFormat = DataFormat.Json;
			request.AddHeader("CSRFPreventionToken", apiTicket.CSRFPreventionToken);
			request.AddCookie("PVEAuthCookie", apiTicket.ticket, new Uri(baseUrl).AbsolutePath, new Uri(baseUrl).Host);
			//request.RootElement = "root";
			request.AddParameter("idlist", device);
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> CreateSnapshot(string vmId, string name, string description)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = new RestRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/snapshot", Method.Post);
			request.RequestFormat = DataFormat.Json;
			request.AddHeader("CSRFPreventionToken", apiTicket.CSRFPreventionToken);
			request.AddCookie("PVEAuthCookie", apiTicket.ticket, new Uri(baseUrl).AbsolutePath, new Uri(baseUrl).Host);
			//request.RootElement = "root";
			request.AddParameter("snapname", name);
			request.AddParameter("description", description);
			return client.Execute<Upid>(request);
		}

		public RestResponse<ListProxmoxSnapshots> ListSnapshots(string vmId)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PrepareGetRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/snapshot");
			return client.Execute<ListProxmoxSnapshots>(request);
		}

		public RestResponse<SnapshotConfig> GetSnapshot(string vmId, string snapshotid)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PrepareGetRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/snapshot/{snapshotid}/config");
			return client.Execute<SnapshotConfig>(request);
		}

		public RestResponse<Upid> RenameSnapshot(string vmId, string snapshotid, string description)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = new RestRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/snapshot/{snapshotid}/config", Method.Put);
			request.RequestFormat = DataFormat.Json;
			request.AddHeader("CSRFPreventionToken", apiTicket.CSRFPreventionToken);
			request.AddCookie("PVEAuthCookie", apiTicket.ticket, new Uri(baseUrl).AbsolutePath, new Uri(baseUrl).Host);
			//request.RootElement = "root";
			request.AddParameter("description", description);
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> DeleteSnapshot(string vmId, string snapshotid)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PrepareDeleteRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/snapshot/{snapshotid}");
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> Rollback(string vmId, string snapshotid)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PreparePostRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/snapshot/{snapshotid}/rollback");
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> CreateScreenshot(string vmId, string snapshotid)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = PreparePostRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/monitor");
			return client.Execute<Upid>(request);
		}

		public RestResponse ListISOs(string vmId, string storage)
		{
			var client = GetRestClient();
			var request = PrepareGetRequest($"nodes/{NodeId(vmId).Node}/storage/{storage}/content");
			return client.Execute(request);
		}


		public RestResponse<ProxmoxTaskStatus> TaskStatus(string upid)
		{
			var client = GetRestClient();
			var request = PrepareGetRequest($"nodes/{Upid2Node(upid)}/tasks/{upid}/status");
			return client.Execute<ProxmoxTaskStatus>(request);
		}

		public RestResponse<List<TaskLog>> TaskLog(string upid)
		{
			var client = GetRestClient();
			var request = PrepareGetRequest($"nodes/{Upid2Node(upid)}/tasks/{upid}/log");
			return client.Execute<List<TaskLog>>(request);
		}

		public bool TaskHasFinished(string upid, int seconds = 30)
		{
			var oneSecond = 1000;
			for (var i = 0; i < seconds * oneSecond;)
			{
				var logs = TaskLog(upid).Data;
				foreach (var log in logs)
				{
					if (log.t == TaskOk)
					{
						return true;
					}
				}
				i += oneSecond;
				Thread.Sleep(oneSecond);
			}
			return false;
		}

		/*
		public RestResponse<List<TaskStatus>> TaskStatusList()
		{
			 var client = GetRestClient();
			 var request = PrepareGetRequest($"nodes/{node}/tasks/");
			 return client.Execute<List<TaskStatus>>(request);
		}
		*/

		public RestResponse ClusterResources()
		{
			var client = GetRestClient();
			var request = PrepareGetRequest("cluster/resources");
			return client.Execute(request);
		}

		public RestResponse ClusterVMList()
		{
			//HostedSolutionLog.LogStart("ClusterVMList");
			var client = GetRestClient();
			var request = PrepareGetRequest("cluster/resources?type=vm");
			//HostedSolutionLog.LogEnd("ClusterVMList");
			return client.Execute(request);
		}

		public RestResponse<Upid> UpdateConfig(String vmId, UpdateConfiguration template)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = new RestRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/config", Method.Post);
			request.RequestFormat = DataFormat.Json;
			request.AddHeader("CSRFPreventionToken", apiTicket.CSRFPreventionToken);
			request.AddCookie("PVEAuthCookie", apiTicket.ticket, new Uri(baseUrl).AbsolutePath, new Uri(baseUrl).Host);
			//request.RootElement = "root";
			request.AddParameter("cores", template.cores);
			request.AddParameter("memory", template.memory);
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> UpdateDVD(String vmId, UpdateDVD template)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = new RestRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/config", Method.Post);
			request.RequestFormat = DataFormat.Json;
			request.AddHeader("CSRFPreventionToken", apiTicket.CSRFPreventionToken);
			request.AddCookie("PVEAuthCookie", apiTicket.ticket, new Uri(baseUrl).AbsolutePath, new Uri(baseUrl).Host);
			//request.RootElement = "root";
			request.AddParameter("ide2", template.ide2);
			return client.Execute<Upid>(request);
		}

		public RestResponse<Upid> ResizeDisk(string vmId, string disk, string size)
		{
			var client = GetRestClient();
			var nodeId = NodeId(vmId);
			var request = new RestRequest($"nodes/{nodeId.Node}/qemu/{nodeId.Id}/resize", Method.Put);
			request.RequestFormat = DataFormat.Json;
			request.AddHeader("CSRFPreventionToken", apiTicket.CSRFPreventionToken);
			request.AddCookie("PVEAuthCookie", apiTicket.ticket, new Uri(baseUrl).AbsolutePath, new Uri(baseUrl).Host);
			//request.RootElement = "root";
			request.AddParameter("disk", disk);
			request.AddParameter("size", size);
			return client.Execute<Upid>(request);
		}

        public virtual PpmImage GetScreenshot(string vmId)
		{
			var nodeId = NodeId(vmId);
			var remoteTmpFile = $"/tmp/screendump-{vmId.Replace(':','-')}-{DateTime.Now.Ticks}.ppm";
            //var remoteTmpFile = $"/tmp/screendump.ppm";
            var result = Nodes[nodeId.Node]?.Qemu[nodeId.Id]?.Monitor.Monitor($"screendump {remoteTmpFile}").Result;
            using (var file = Provider.GetFile(vmId, remoteTmpFile, true))
			{
				return PpmImage.FromStream(file);
			}
		}

		private RestRequest PreparePostRequest(string resource)
		{
			var request = new RestRequest(resource, Method.Post);
			request.RequestFormat = DataFormat.Json;
			request.AddHeader("CSRFPreventionToken", apiTicket.CSRFPreventionToken);
			request.AddCookie("PVEAuthCookie", apiTicket.ticket, new Uri(baseUrl).AbsolutePath, new Uri(baseUrl).Host);
			//request.RootElement = rootElement;
			return request;
		}

		private RestRequest PrepareGetRequest(string resource)
		{
			//HostedSolutionLog.DebugInfo("PrepareGetRequest");
			var request = new RestRequest(resource, Method.Get);
			//HostedSolutionLog.DebugInfo("PrepareGetRequest Request: {0}", request.ToString());
			request.RequestFormat = DataFormat.Json;
			//HostedSolutionLog.DebugInfo("PrepareGetRequest - PVEAuthCookie: {0)", apiTicket.ticket); 
			request.AddCookie("PVEAuthCookie", apiTicket.ticket, new Uri(baseUrl).AbsolutePath, new Uri(baseUrl).Host);
			//request.RootElement = rootElement;
			//HostedSolutionLog.DebugInfo("PrepareGetRequest Request2: {0}", request);
			return request;
		}

		private RestRequest PrepareDeleteRequest(string resource)
		{
			var request = new RestRequest(resource, Method.Delete);
			request.RequestFormat = DataFormat.Json;
			request.AddHeader("CSRFPreventionToken", apiTicket.CSRFPreventionToken);
			request.AddCookie("PVEAuthCookie", apiTicket.ticket, new Uri(baseUrl).AbsolutePath, new Uri(baseUrl).Host);
			//request.RootElement = rootElement;
			return request;
		}

		private string Upid2Node(string upid)
		{
			string node = upid.Split(':')[1];
			return node;
		}

		public ApiVM NodeId(string vmId)
		{
			ApiVM apivm = new ApiVM();
			apivm.Node = vmId.Split(':')[0];
			apivm.Id = vmId.Split(':')[1];

			var RestResponse = ClusterVMList();

			JToken jsonResponse = JToken.Parse(RestResponse.Content);
			JArray jsonResponsearray = (JArray)jsonResponse["data"];

			foreach (JObject resources in jsonResponsearray)
			{
				try
				{
					if (resources["type"].ToString().Equals("qemu") && resources["vmid"].ToString().Equals(apivm.Id))
					{
						apivm.Node = resources["node"].ToString();
						return apivm;
					}
				}
#pragma warning disable 0168
				catch (Exception ex)
#pragma warning restore 0168
				{
					apivm.Node = vmId.Split(':')[0];
				}
			}
			return apivm;
		}

		bool isDisposed = false;
		public void Dispose() {
			isDisposed = true;
		}
    }
}
