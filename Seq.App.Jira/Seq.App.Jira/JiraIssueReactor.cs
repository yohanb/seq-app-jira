using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.Jira
{
    [SeqApp("JiraIssue",
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

            var request = new RestRequest("issue", Method.POST);
            request.Parameters.Clear();
            request.Parameters.AddRange(new[]
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
                    Value = JsonConvert.SerializeObject(CreateIssuePayload(@event))
                }
            });

            var response = client.Execute(request);
            if (!response.IsSuccessful) throw new Exception(response.ErrorMessage);
        }

        private dynamic CreateIssuePayload(Event<LogEventData> @event)
        {
            var fields = new Dictionary<string, object>
            {
                {"project", new {key = JiraProjectKey.Trim()}},
                {"issuetype", new {name = JiraIssueType.Trim()}},
                {"summary", @event.Data.RenderedMessage},
                {"description", @event.Data.Exception}
            };

            return new {fields};
        }
    }
}