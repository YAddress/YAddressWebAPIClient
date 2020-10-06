using System;

namespace TestApp
{
    class Program
    {
        static void Main()
        {
            YAddress.WebApiClient client = new YAddress.WebApiClient();
            YAddress.WebApiClient.Address adr = 
                client.ProcessAddress("506 Fourth Avenue # 1", "Asbury Prk, NJ",
                    "3DE15352-4B97-4E34-AA7C-570105C1C68B");
        }
    }
}
