using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Web;

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
            public int? PlaceFP { get; set; }
            public string CityMunicipality { get; set; }
            public decimal? SalesTaxRate { get; set; }
            public int? SalesTaxJurisdiction { get; set; }
        }

        /// Private variables
        static readonly HttpClient _http = new HttpClient();
        string _sBaseUrl, _sUserKey;

        /// <summary>
        /// Constructor
        /// Initializes a new WebApiClient instance.
        /// </summary>
        /// <param name="UserKey">Your YAddress Web API user key. Use null if you do not have a YAddress account.</param>
        /// <param name="BaseUrl">Optional. Base URL for API calls if different than http://www.yaddress.net/api.</param>
        public WebApiClient(string UserKey, 
            string BaseUrl = "http://www.yaddress.net/api")
        {
            // Initialize Http client headers
            if (_http.DefaultRequestHeaders.Accept.Count == 0)
            {
                _http.DefaultRequestHeaders.Add("Accept", "application/json");
                Version v = typeof(WebApiClient).Assembly.GetName().Version;
                _http.DefaultRequestHeaders.Add("User-Agent",
                    $"YAddressWebApiDotNetClient/{v.Major}.{v.Minor}.{v.Revision}");
            }

            // Save vars
            _sUserKey = UserKey;
            _sBaseUrl = BaseUrl?.TrimEnd('/');
        }

        /// Implementation of IDisposable
        public void Dispose()
        {
        }

        /// <summary>
        /// Calls YAddress Web API to process a postal address.
        /// </summary>
        /// <param name="AddressLine1">First line of the address -- street address.</param>
        /// <param name="AddressLine2">Second line of the address -- city, state, zip.</param>
        public async Task<Address> ProcessAddressAsync(
            string AddressLine1, string AddressLine2)
        {
            // Call Web API
            HttpResponseMessage res;
            try
            {
                res = await _http.GetAsync(
                    $"{_sBaseUrl}/Address" +
                    $"?AddressLine1={HttpUtility.UrlEncode(AddressLine1)}" +
                    $"&AddressLine2={HttpUtility.UrlEncode(AddressLine2)}" +
                    $"&UserKey={_sUserKey}");
            }
            catch (Exception ex)
            {
                throw new Exception("YAddress Web API call failed.", ex);
            }
            if (res.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"YAddress Web API call failed. HTTP response code: {(int)res.StatusCode}");
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
        /// <param name="AddressLine1">First line of the address -- street address.</param>
        /// <param name="AddressLine2">Second line of the address -- city, state, zip.</param>
        public Address ProcessAddress(
            string AddressLine1, string AddressLine2)
        {
            Task<Address> tsk = ProcessAddressAsync(AddressLine1, AddressLine2);
            tsk.Wait();
            return tsk.Result;
        }
    }
}
