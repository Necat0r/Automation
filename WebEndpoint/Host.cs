namespace WebEndpoint
{
    using System;
    using Nancy.Hosting.Self;
    using Microsoft.AspNet.SignalR;
    using Owin;
    using Microsoft.Owin.Hosting;
    using Logging;

    public class Host : IDisposable
    {
        public class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                //app.MapConnection<RawConnection>("/raw", new ConnectionConfiguration { EnableJSONP = true });
                //app.MapHubs();
                app.MapSignalR();
            }
        }

        public Host(UInt16 port, Module.ServiceManager serviceManager, Module.DeviceManager deviceManager)
        {
            var uri =
                new Uri("http://localhost:" + port);

            var bootstrapper = new Bootstrapper(serviceManager, deviceManager);

            mHost = new NancyHost(bootstrapper, uri);

            try
            {
                mHost.Start();
            }
            catch (Nancy.Hosting.Self.AutomaticUrlReservationCreationFailureException)
            {
                // Need to add acl for that address.
                // netsh http add urlacl url=http://+:<port>/automation/ user=<machine>\<user>
                Log.Fatal("Probably an ACL issue.\nRun the following in an elevated command prompt:\nnetsh http add urlacl url=http://+:" + port + "/ user=<machine>\\<user>");

                /*
                netsh http add urlacl url=https://+:4443/ user=<your user name>”

                netsh http add sslcert ipport=0.0.0.0:4443 certhash=thumbprint appid={app-guid} 
                b227d46d-4a39-45af-b5f0-ab2091a4e4bf
                 * 
                 * 
                 * */

                throw;
            }

            Log.Info("Your application is running on " + uri);

            //using (Microsoft.Owin.Hosting.WebApp.Start("http://localhost:8080"))
            //{
            //    Log.Info("Server running at http://localhost:8080/");
            //    Console.ReadLine();
            //}

            //// SignalR server for WebSockets
            //using (WebApplication.Start<Startup>("http://localhost:8080/"))
            //{
            //    Log.Info("Server running at http://localhost:8080/");
            //    Console.ReadLine();
            //}
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (mHost != null)
                {
                    mHost.Stop();
                    mHost.Dispose();
                }
                mHost = null;
            }

        }

        public void Stop()
        {
            mHost.Stop();
        }

        private NancyHost mHost;
    }
}
