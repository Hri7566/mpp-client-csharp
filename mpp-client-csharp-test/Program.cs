using mpp_client_csharp;

namespace mpp_client_csharp_test {
    public class Program {
        public static void Main()
        {
            /*
            EventEmitter events = new EventEmitter();

            Action<dynamic> callback = delegate (dynamic args)
            {
                Console.WriteLine(args[0]);
            };

            events.On("test", callback);

            events.Emit("test", "Hello, world!");

            events.Emit("test", "Hello, 2");
            events.Off("test", callback);
            events.Emit("test", "you won't see this");
            */

            Client cl = new Client("wss://mppclone.com:8443", "");
            cl.On("status", delegate (dynamic txt)
            {
                Console.WriteLine(Convert.ToString(txt));
            });

            cl.start();
            cl.setChannel("✧𝓓𝓔𝓥 𝓡𝓸𝓸𝓶✧", new mpp_client_csharp.Channel.ChannelSettings {
                lobby = false,
                chat = true,
                color = "#054805",
                color2 = "#000000",
                crownsolo = false,
                visible = true
            });

            Console.ReadLine();
        }
    }
}
