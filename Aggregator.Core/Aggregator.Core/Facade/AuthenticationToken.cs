namespace Aggregator.Core.Facade
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    using Microsoft.TeamFoundation.Client;
#if TFS2017
    using Microsoft.VisualStudio.Services.Common;
#endif

    using IdentityDescriptor = Microsoft.TeamFoundation.Framework.Client.IdentityDescriptor;

#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables

    public partial class WorkItemRepository
    {
        public abstract class AuthenticationToken
        {
            public abstract TfsTeamProjectCollection GetCollection(Uri tfsCollectionUri);

            public virtual HttpClientHandler CreateHttpClientHandler()
            {
                return new HttpClientHandler
                {
                    PreAuthenticate = true,
                    UseDefaultCredentials = true
                };
            }

            public virtual void Apply(HttpClient client)
            {
            }

            public override string ToString()
            {
                return this.GetType().Name;
            }
        }

        public class WindowsIntegratedAuthenticationToken : AuthenticationToken
        {
            public override TfsTeamProjectCollection GetCollection(Uri tfsCollectionUri)
            {
                return new TfsTeamProjectCollection(tfsCollectionUri);
            }
        }

        public class PersonalAuthenticationToken : AuthenticationToken
        {
            private readonly string personalToken;

            public PersonalAuthenticationToken(string personalToken)
            {
                this.personalToken = personalToken;
            }

            public override TfsTeamProjectCollection GetCollection(Uri tfsCollectionUri)
            {
                // username is not important, we use it to identify ourselves to callee
#if TFS2017
                var tfsCred = new VssCredentials(
                    new VssBasicCredential(
                        new System.Net.NetworkCredential(
                            "tfsaggregator2-webhooks", this.personalToken)));
#else
                var tfsCred = new TfsClientCredentials(
                    new BasicAuthCredential(
                        new System.Net.NetworkCredential(
                            "tfsaggregator2-webhooks", this.personalToken)));
                tfsCred.AllowInteractive = false;
#endif
                return new TfsTeamProjectCollection(tfsCollectionUri, tfsCred);
            }

            public override HttpClientHandler CreateHttpClientHandler()
            {
                return new HttpClientHandler
                {
                    PreAuthenticate = true,
                    UseDefaultCredentials = false
                };
            }

            public override void Apply(HttpClient client)
            {
                client.DefaultRequestHeaders.Authorization = CreateBasicHeader("tfsaggregator2-rest", this.personalToken);
            }
        }

        public class BasicAuthenticationToken : AuthenticationToken
        {
            private readonly string username;

            private readonly string password;

            public BasicAuthenticationToken(string username, string password)
            {
                this.username = username;
                this.password = password;
            }

            public override TfsTeamProjectCollection GetCollection(Uri tfsCollectionUri)
            {
#if TFS2017
                var tfsCred = new VssCredentials(
                    new VssBasicCredential(
                        new System.Net.NetworkCredential(this.username, this.password)));
#else
                var tfsCred = new TfsClientCredentials(
                    new BasicAuthCredential(
                        new System.Net.NetworkCredential(this.username, this.password)));
                tfsCred.AllowInteractive = false;
#endif
                return new TfsTeamProjectCollection(tfsCollectionUri, tfsCred);
            }

            public override HttpClientHandler CreateHttpClientHandler()
            {
                return new HttpClientHandler
                {
                    PreAuthenticate = true,
                    UseDefaultCredentials = false
                };
            }

            public override void Apply(HttpClient client)
            {
                client.DefaultRequestHeaders.Authorization = CreateBasicHeader(this.username, this.password);
            }

            public override string ToString()
            {
                return $"{base.ToString()}({this.username})";
            }
        }

        public class ImpersonateAuthenticationToken : AuthenticationToken
        {
            private readonly IdentityDescriptor identityDescriptor;

            public ImpersonateAuthenticationToken(IdentityDescriptor identityDescriptor)
            {
                this.identityDescriptor = identityDescriptor;
            }

            public override TfsTeamProjectCollection GetCollection(Uri tfsCollectionUri)
            {
                return new TfsTeamProjectCollection(tfsCollectionUri, this.identityDescriptor);
            }

            public override string ToString()
            {
                return $"{base.ToString()}({this.identityDescriptor.Identifier})";
            }
        }

        private static AuthenticationHeaderValue CreateBasicHeader(string username, string password)
        {
            string rawValue = $"{username ?? string.Empty}:{password ?? string.Empty}";
            string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(rawValue));
            return new AuthenticationHeaderValue("Basic", encoded);
        }
    }
}
