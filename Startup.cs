using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(NSH.Startup))] // Ensure namespace matches project

namespace NSH
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR(); // Enables SignalR
        }
    }
}
