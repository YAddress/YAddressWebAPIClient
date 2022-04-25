using System;

namespace TestApp
{
    class Program
    {
        static void Main()
        {
            YAddress.WebApiClient client = new YAddress.WebApiClient(UserKey: null);
            YAddress.WebApiClient.Address adr = 
                client.ProcessAddress("506 Fourth Avenue # 1", "Asbury Prk, NJ");
        }
    }
}
