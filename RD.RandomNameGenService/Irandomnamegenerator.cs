using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;

namespace RD.RandomNameGenService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "Irandomnamegenerator" in both code and config file together.
    [ServiceContract]
    public interface Irandomnamegenerator
    {
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "generate_names/{parameters}")]
        string generate_names(string parameters);
    }
}
