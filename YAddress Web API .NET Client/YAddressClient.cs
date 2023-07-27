using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using SimpleJSON;

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
            public bool? SecValidated { get; set; }
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
            public string UspsCarrierRoute { get; set; }
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
                    $"YAddressWebApiDotNetClient/{v.Major}.{v.Minor}.{v.Build}");
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
            string sResponse;
            try
            {
                sResponse = await _http.GetStringAsync(
                    $"{_sBaseUrl}/Address" +
                    $"?AddressLine1={HttpUtility.UrlEncode(AddressLine1)}" +
                    $"&AddressLine2={HttpUtility.UrlEncode(AddressLine2)}" +
                    $"&UserKey={_sUserKey}");
            }
            catch (Exception ex)
            {
                throw new Exception("YAddress Web API call failed.", ex);
            }

            // Deserialize JSON
            try
            {
                JSONNode nd = JSONNode.Parse(sResponse);
                Address adr = new Address();

                adr.ErrorCode = nd["ErrorCode"].AsInt;
                adr.ErrorMessage = nd["ErrorMessage"].AsString;
                adr.AddressLine1 = nd["AddressLine1"].AsString;
                adr.AddressLine2 = nd["AddressLine2"].AsString;
                adr.Number = nd["Number"].AsString;
                adr.PreDir = nd["PreDir"].AsString;
                adr.Street = nd["Street"].AsString;
                adr.Suffix = nd["Suffix"].AsString;
                adr.PostDir = nd["PostDir"].AsString;
                adr.Sec = nd["Sec"].AsString;
                adr.SecNumber = nd["SecNumber"].AsString;
                adr.SecValidated = nd["SecValidated"].AsNullableBool;
                adr.City = nd["City"].AsString;
                adr.State = nd["State"].AsString;
                adr.Zip = nd["Zip"].AsString;
                adr.Zip4 = nd["Zip4"].AsString;
                adr.County = nd["County"].AsString;
                adr.StateFP = nd["StateFP"].AsString;
                adr.CountyFP = nd["CountyFP"].AsString;
                adr.CensusBlock = nd["CensusBlock"].AsString;
                adr.CensusTract = nd["CensusTract"].AsString;
                adr.Latitude = nd["Latitude"].AsDouble;
                adr.Longitude = nd["Longitude"].AsDouble;
                adr.GeoPrecision = nd["GeoPrecision"].AsInt;
                adr.TimeZoneOffset = nd["TimeZoneOffset"].AsNullableInt;
                adr.DstObserved = nd["DstObserved"].AsNullableBool;
                adr.PlaceFP = nd["PlaceFP"].AsNullableInt;
                adr.CityMunicipality = nd["CityMunicipality"].AsString;
                adr.SalesTaxRate = nd["SalesTaxRate"].AsNullableDecimal;
                adr.SalesTaxJurisdiction = nd["SalesTaxJurisdiction"].AsNullableInt;
                adr.UspsCarrierRoute = nd["UspsCarrierRoute"].AsString;

                return adr;
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing response from server", ex);
            }
        }

        /// <summary>
        /// Calls YAddress Web API to process a postal address.
        /// </summary>
        /// <param name="AddressLine1">First line of the address -- street address.</param>
        /// <param name="AddressLine2">Second line of the address -- city, state, zip.</param>
        public Address ProcessAddress(
            string AddressLine1, string AddressLine2)
        {
            return ProcessAddressAsync(AddressLine1, AddressLine2).Result;
        }
    }
}
