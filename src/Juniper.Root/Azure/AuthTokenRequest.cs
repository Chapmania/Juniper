using System.Net;

using Juniper.HTTP;

namespace Juniper.Azure
{
    public class AuthTokenRequest : AbstractAzureRequest<MediaType.Text>
    {
        private readonly string subscriptionKey;

        public AuthTokenRequest(string region, string subscriptionKey)
            : base(HttpMethod.POST, region, "api.cognitive", "sts/v1.0/issueToken", Juniper.MediaType.Text.Plain)
        {
            this.subscriptionKey = subscriptionKey;
        }

        protected override void ModifyRequest(HttpWebRequest request)
        {
            base.ModifyRequest(request);
            request.Header("Ocp-Apim-Subscription-Key", subscriptionKey);
        }

        protected override BodyInfo GetBodyInfo()
        {
            return new BodyInfo(Juniper.MediaType.Application.X_Www_Form_Urlencoded, 0);
        }
    }
}
