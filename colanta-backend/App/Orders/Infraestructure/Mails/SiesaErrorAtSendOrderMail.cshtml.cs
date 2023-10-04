using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace colanta_backend.App.Orders.Infraestructure
{
    using Shared.Domain;
    public class SiesaErrorAtSendOrderMailModel : PageModel
    {
        public SiesaException exception;
        public string vtexOrderId;
        public string requestBody;
        public string responseBody;
        public string status;

        public SiesaErrorAtSendOrderMailModel(SiesaException exception, string vtexOrderId)
        {
            this.exception = exception;
            this.vtexOrderId = vtexOrderId;
            this.status = exception.status.ToString();

            var requestBodyObject = Newtonsoft.Json.JsonConvert.DeserializeObject(exception.requestBody);
            this.requestBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBodyObject, Newtonsoft.Json.Formatting.Indented);
            
            var responseBodyObject = Newtonsoft.Json.JsonConvert.DeserializeObject(exception.responseBody);
            this.responseBody = Newtonsoft.Json.JsonConvert.SerializeObject(responseBodyObject, Newtonsoft.Json.Formatting.Indented);
        }

        public void OnGet()
        {
        }
    }
}
