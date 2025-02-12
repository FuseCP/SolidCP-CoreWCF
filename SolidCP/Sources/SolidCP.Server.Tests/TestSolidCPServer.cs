namespace SolidCP.Server.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using SolidCP.Providers;
    using SolidCP.Server.Client;
    using SolidCP.Web.Clients;
    using System.ServiceModel;

    [TestClass]
    public class TestSolidCPServer
    {

        static readonly object Lock = new object();
        static Kestrel? KestrelServer = null;
        static IISExpress? IISExpressServer = null;
        public TestContext? TestContext { get; set; }

        [ClassInitialize]
        public static void InitTest(TestContext context)
        {
            lock (Lock)
            {
                if (KestrelServer == null) KestrelServer = new Kestrel();
                if (IISExpressServer == null) IISExpressServer = new IISExpress();
            }
        }

        [ClassCleanup]
        public static void Dispose()
        {
            lock (Lock)
            {
                //KestrelServer?.Dispose();
                KestrelServer = null;
                //IISExpressServer?.Dispose();
                IISExpressServer = null;
            }
        }

        [TestMethod]
        [DataRow(Protocols.BasicHttps)]
        [DataRow(Protocols.WSHttps)]
        [DataRow(Protocols.NetHttps)]

        public void AnonymousNet6(Protocols protocol)
        {
            using (var client = new Test() { Url = Kestrel.HttpsUrl })
            {
                try
                {
                    client.Protocol = protocol;
                    var msg = client.Echo("Hello");
                    Assert.AreEqual("Hello", msg);
                }
                catch (FaultException fex)
                {
                    TestContext.WriteLine($"Fault: {fex};{fex.InnerException}");
                }
                catch (Exception ex)
                {
                    throw;
                    Assert.Fail("Exception", ex);
                }
            }
        }

        [TestMethod]
        [DataRow(Protocols.BasicHttps)]
        [DataRow(Protocols.WSHttps)]
        [DataRow(Protocols.NetHttps)]

        public void AnonymousWithSoapHeaderNet6(Protocols protocol)
        {
            using (var client = new Test() { Url = Kestrel.HttpsUrl })
            {
                try
                {
                    client.Protocol = protocol;
                    var h = new ServiceProviderSettingsSoapHeader();
                    h.Settings = new string[] { "Hello from Settings!" };
                    client.SoapHeader = h;
                    var msg = client.EchoSettings();

                    Assert.AreEqual("Hello from Settings!", msg);
                }
                catch (FaultException fex)
                {
                    TestContext.WriteLine($"Fault: {fex};{fex.InnerException}");
                    Assert.Fail("FaultException", fex);
                }
                catch (Exception ex)
                {
                    throw;
                    Assert.Fail("Exception", ex);
                }
            }
        }

        [TestMethod]
        public void AnonymousNetTcp()
        {
            using (var client = new Test() { Url = "net.tcp://localhost:9022" })
            {
                try
                {
                    var msg = client.Echo("Hello");
                    Assert.AreEqual("Hello", msg);
                }
                catch (FaultException fex)
                {
                    TestContext.WriteLine($"Fault: {fex};{fex.InnerException}");
                }
                catch (Exception ex)
                {
                    throw;
                    Assert.Fail("Exception", ex);
                }
            }
        }

        [TestMethod]
        public async Task AnonymousNet6Async()
        {
            using (var client = new Test() { Url = Kestrel.HttpsUrl })
            {
                try
                {
                    var msg = await client.EchoAsync("Hello");
                    Assert.AreEqual("Hello", msg);
                }
                catch (Exception ex)
                {
                    throw;
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        [DataRow(Protocols.BasicHttps)]
        [DataRow(Protocols.WSHttps)]
        [DataRow(Protocols.NetHttps)]
        public void AuthenticatedNet6(Protocols protocol)
        {
            using (var client = new TestWithAuthentication() { Url = Kestrel.HttpsUrl })
            {
                try
                {
                    client.Credentials.Password = "uqCP8Qc3EjbAzzjpiSOu+Z+icAqYOtzM7Luy+OTIRZ8=";
                    client.Protocol = protocol;

                    // test echo method
                    var res = client.Echo("Hello");
                    Assert.AreEqual("Hello", res);

					// test method with soap header
					var header = new ServiceProviderSettingsSoapHeader()
					{
						Settings = new string[] { "Provider:ProviderType=SolidCP.Providers.OS.Windows2022, SolidCP.Providers.OS.Windows2022", "Provider:ProviderName=Windows2022" }
					};
                    client.SoapHeader = header;
					var settings = client.EchoSettings();
                    Assert.AreEqual(header.Settings[0], settings);

                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }
        [TestMethod]
        [DataRow(Protocols.BasicHttps)]
        [DataRow(Protocols.WSHttps)]
        [DataRow(Protocols.NetHttps)]
        // requires an IIS application on localhost/server pointing to the SolidCP.Server directory
        public void AnonymousNet48(Protocols protocol)
        {
            using (var client = new Test() { Url = IISExpress.HttpsUrl })
            {
                try
                {
                    client.Protocol = protocol;
                    var res = client.Echo("Hello");

                    Assert.AreEqual("Hello", res);
                }
                catch (FaultException fex)
                {
                    TestContext.WriteLine($"Fault: {fex};{fex.InnerException}");
                }
                catch (Exception ex)
                {
                    throw;
                    Assert.Fail("Exception", ex);
                }
            }
        }

        [TestMethod]
        [DataRow(Protocols.BasicHttps)]
        [DataRow(Protocols.WSHttps)]
        [DataRow(Protocols.NetHttps)]
        // requires an IIS application on localhost/server pointing to the SolidCP.Server directory
        public void AnonymousWithSoapHeaderNet48(Protocols protocol)
        {
            using (var client = new Test() { Url = IISExpress.HttpsUrl })
            {
                try
                {
                    client.Protocol = protocol;
                    var h = new ServiceProviderSettingsSoapHeader();
                    h.Settings = new string[] { "Hello from Settings!" };
                    client.SoapHeader = h;
                    var msg = client.EchoSettings();

                    Assert.AreEqual("Hello from Settings!", msg);
                }
                catch (FaultException fex)
                {
                    TestContext.WriteLine($"Fault: {fex};{fex.InnerException}");
                    Assert.Fail("FaultException", fex);
                }
                catch (Exception ex)
                {
                    throw;
                    Assert.Fail("Exception", ex);
                }
            }
        }

        [TestMethod]
        [DataRow(Protocols.BasicHttps)]
        [DataRow(Protocols.WSHttps)]
        [DataRow(Protocols.NetHttps)]
        public void AuthenticatedNet48(Protocols protocol)
        {
            using (var client = new TestWithAuthentication() { Url = IISExpress.HttpsUrl })
            {
                try
                {
                    client.SoapHeader = new ServiceProviderSettingsSoapHeader()
                    {
                        Settings = new string[] { "Provider:ProviderType=SolidCP.Providers.OS.Windows2022, SolidCP.Providers.OS.Windows2022", "Provider:ProviderName=Windows2022" }
                    };
                    client.Credentials.Password = "uqCP8Qc3EjbAzzjpiSOu+Z+icAqYOtzM7Luy+OTIRZ8=";
                    client.Protocol = protocol;

                    // test echo method
                    var res = client.Echo("Hello");
                    Assert.AreEqual("Hello", res);

                    // test method with soap header
                    var settings = client.EchoSettings();
                    Assert.AreEqual(((ServiceProviderSettingsSoapHeader)client.SoapHeader).Settings[0], settings);
                }
                catch (Exception ex)
                {
                    throw;
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        [DataRow(Protocols.BasicHttps)]
        [DataRow(Protocols.WSHttps)]
        [DataRow(Protocols.NetHttps)]
        public void WrongPasswordNet48(Protocols protocol)
        {
            using (var client = new TestWithAuthentication() { Url = IISExpress.HttpsUrl })
            {
                try
                {
                    client.SoapHeader = new ServiceProviderSettingsSoapHeader()
                    {
                        Settings = new string[] { "Provider:ProviderType=SolidCP.Providers.OS.Windows2022, SolidCP.Providers.OS.Windows2022", "Provider:ProviderName=Windows2022" }
                    };
                    client.Credentials.Password = "1234";
                    client.Protocol = protocol;

                    // test echo method
                    var res = client.Echo("Hello");
                    Assert.AreEqual("Hello", res);

                    // test method with soap header
                    var settings = client.EchoSettings();
                    Assert.AreEqual(((ServiceProviderSettingsSoapHeader)client.SoapHeader).Settings[0], settings);
                    Assert.Fail("Authorized with wrong password");
                }
                catch (FaultException fex)
                {
                    TestContext.WriteLine($"Access denied: {fex.StackTrace}");
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        [TestMethod]
        [DataRow(Protocols.BasicHttps)]
        [DataRow(Protocols.WSHttps)]
        [DataRow(Protocols.NetHttps)]
        public void WrongPasswordNet6(Protocols protocol)
        {
            using (var client = new TestWithAuthentication() { Url = Kestrel.HttpsUrl })
            {
                try
                {
                    client.SoapHeader = new ServiceProviderSettingsSoapHeader()
                    {
                        Settings = new string[] { "Provider:ProviderType=SolidCP.Providers.OS.Windows2022, SolidCP.Providers.OS.Windows2022", "Provider:ProviderName=Windows2022" }
                    };
                    client.Credentials.Password = "1234";
                    client.Protocol = protocol;

                    // test echo method
                    var res = client.Echo("Hello");
                    Assert.AreEqual("Hello", res);

                    // test method with soap header
                    var settings = client.EchoSettings();
                    Assert.AreEqual(((ServiceProviderSettingsSoapHeader)client.SoapHeader).Settings[0], settings);
                    Assert.Fail("Authorized with wrong password");
                }
                catch (FaultException fex)
                {
                    TestContext.WriteLine($"Access denied: {fex.StackTrace}");
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

    }
}