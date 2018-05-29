using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;
using System.Web.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.Jira.Issue
{
    [SeqApp("Jira Issue",
        Description = "Creates an issue in Jira for the selected event.")]
    public class JiraIssueReactor : Reactor, ISubscribeTo<LogEventData>
    {
        [SeqAppSetting(
            DisplayName = "Jira Url",
            IsOptional = false,
            HelpText = "Jira server URL.")]
        public string JiraServerUrl { get; set; }

        [SeqAppSetting(
            DisplayName = "Issue Type",
            IsOptional = false,
            HelpText = "Jira issue type (ex: Bug, Story, etc.)")]
        public string JiraIssueType { get; set; }

        [SeqAppSetting(
            DisplayName = "Project Key",
            IsOptional = false,
            HelpText = "Jira project key.")]
        public string JiraProjectKey { get; set; }

        [SeqAppSetting(
            DisplayName = "Jira Username",
            IsOptional = false)]
        public string JiraUsername { get; set; }

        [SeqAppSetting(
            DisplayName = "Jira Password",
            IsOptional = false,
            InputType = SettingInputType.Password)]
        public string JiraPassword { get; set; }

        private Uri JiraApiUrl
        {
            get
            {
                if (!Uri.IsWellFormedUriString(JiraServerUrl, UriKind.Absolute))
                    throw new ArgumentException("Jira Server URL must be a valid URL.", nameof(JiraServerUrl));

                var baseUri = new Uri(JiraServerUrl);
                return new Uri(baseUri, "/rest/api/latest/");
            }
        }

        public void On(Event<LogEventData> @event)
        {
            var client = new RestClient(JiraApiUrl)
            {
                Authenticator = new HttpBasicAuthenticator(JiraUsername, JiraPassword),
            };

            var response = CreateIssue(client, JiraProjectKey.Trim(), JiraIssueType.Trim(), @event);
            if (!response.IsSuccessful) throw new Exception(response.ErrorMessage);

            JToken issueKey;
            var responseContent = JsonConvert.DeserializeObject<JObject>(response.Content);
            if (responseContent.TryGetValue("key", out issueKey))
            {
                AddEventAttachment(client, issueKey.ToString(), @event);
            }
        }

        private static IRestResponse CreateIssue(IRestClient client, string projectKey, string issueType, Event<LogEventData> @event)
        {
            var issueRequest = new RestRequest("issue", Method.POST);
            issueRequest.Parameters.Clear();
            issueRequest.Parameters.AddRange(new[]
            {
                new Parameter
                {
                    Type = ParameterType.HttpHeader,
                    Name = "Content-Type",
                    Value = "application/json"
                },
                new Parameter
                {
                    Type = ParameterType.RequestBody,
                    Name = "fields",
                    Value = JsonConvert.SerializeObject(CreateIssuePayload(@event, projectKey, issueType))
                }
            });

            var response = client.Execute(issueRequest);
            return response;
        }

        private static IRestResponse AddEventAttachment(IRestClient client, string issueKey, Event<LogEventData> @event)
        {
            var request = new RestRequest($"issue/{issueKey}/attachments", Method.POST)
            {
                AlwaysMultipartFormData = true,
            };

            request.Parameters.Clear();
            request.Parameters.AddRange(new[]
            {
                new Parameter
                {
                    Type = ParameterType.HttpHeader,
                    Name = "X-Atlassian-Token",
                    Value = "nocheck"
                },
            });

            using (var stream = new MemoryStream())
            using (TextWriter writer = new StreamWriter(stream))
            {
                var eventJson = JsonConvert.SerializeObject(
                    @event,
                    new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                    });

                writer.Write(eventJson);
                writer.Flush();

                request.AddFileBytes("file", stream.ToArray(), $"{@event.Id}.json");

                return client.Execute(request);
            }
        }

        private static dynamic CreateIssuePayload(Event<LogEventData> @event, string projectKey, string issueType)
        {
            var fields = new Dictionary<string, object>
            {
                {"project", new {key = projectKey}},
                {"issuetype", new {name = issueType}},
                {"summary", @event.Data.RenderedMessage},
                {"description", @event.Data.Exception}
            };

            return new {fields};
        }
    }
}