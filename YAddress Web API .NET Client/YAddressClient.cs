using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Web;
using System.Reflection;

namespace YAddress
{
    /// <summary>
    /// Client for making calls into YAddress Web API.
    /// </summary>
    public class WebApiClient : IDisposable
    {
        /// <summary>
        /// Stores a processed address, parsed into fields.
        /// </summary>
        public class Address
        {
            public int ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public string AddressLine1 { get; set; }
            public string AddressLine2 { get; set; }
            public string Number { get; set; }
            public string PreDir { get; set; }
            public string Street { get; set; }
            public string Suffix { get; set; }
            public string PostDir { get; set; }
            public string Sec { get; set; }
            public string SecNumber { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
            public string Zip4 { get; set; }
            public string County { get; set; }
            public string StateFP { get; set; }
            public string CountyFP { get; set; }
            public string CensusTract { get; set; }
            public string CensusBlock { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public int GeoPrecision { get; set; }
            public int? TimeZoneOffset { get; set; }
            public bool? DstObserved { get; set; }
            public decimal? SalesTaxRate { get; set; }
            public int? SalesTaxJurisdiction { get; set; }
        }

        /// Private variables
        HttpClient _http = null;

        /// Constructor
        public WebApiClient()
        {
            // Instantiate Http client 
            _http = new HttpClient();
            _http.BaseAddress = new Uri("https://www.yaddress.net/api/");
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
            Version v = typeof(WebApiClient).Assembly.GetName().Version;
            _http.DefaultRequestHeaders.Add("User-Agent", 
                $"YAddressWebApiDotNetClient/{v.Major}.{v.Minor}.{v.Revision}");
        }

        /// Implementation of IDisposable
        public void Dispose()
        {
            _http?.Dispose();
        }

        /// <summary>
        /// Calls YAddress Web API to process a postal address.
        /// </summary>
        /// <param name="AddressLine1">First line of the address, i.e., street address.</param>
        /// <param name="AddressLine2">Second line of the address, i.e., city, state, zip.</param>
        /// <param name="UserKey">Your YAddress Web API user key. Use null if you do not have a YAddress account.</param>
        public async Task<Address> ProcessAddressAsync(
            string AddressLine1, string AddressLine2, string UserKey = null)
        {
            // Call Web API
            HttpResponseMessage res = await _http.GetAsync(
                $"Address?AddressLine1={HttpUtility.UrlEncode(AddressLine1)}" +
                    $"&AddressLine2={HttpUtility.UrlEncode(AddressLine2)}" +
                    $"&UserKey={UserKey}");
            Stream st = await res.Content.ReadAsStreamAsync();

            // Deserialize JSON
            var serializer = new DataContractJsonSerializer(typeof(Address));
            Address adr;
            try
            {
                adr = (Address)serializer.ReadObject(st);
            }
            catch(Exception ex)
            {
                throw new Exception("Error parsing response from server", ex);
            }
            st.Close();

            return adr;
        }

        /// <summary>
        /// Calls YAddress Web API to process a postal address.
        /// </summary>
        /// <param name="AddressLine1">First line of the address, i.e., street address.</param>
        /// <param name="AddressLine2">Second line of the address, i.e., city, state, zip.</param>
        /// <param name="UserKey">Your YAddress Web API user key. Use null if you do not have a YAddress account.</param>
        public Address ProcessAddress(
            string AddressLine1, string AddressLine2, string UserKey = null)
        {
            Task<Address> tsk = ProcessAddressAsync(AddressLine1, AddressLine2, UserKey);
            tsk.Wait();
            return tsk.Result;
        }
    }
}
