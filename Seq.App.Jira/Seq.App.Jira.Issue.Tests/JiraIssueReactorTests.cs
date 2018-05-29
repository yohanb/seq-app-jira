using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seq.Apps;
using Seq.Apps.LogEvents;
using Xunit;

namespace Seq.App.Jira.Issue.Tests
{
    public class JiraIssueReactorTests
    {
        public JiraIssueReactorTests()
        {
            var config = JsonConvert.DeserializeObject<JObject>(File.ReadAllText("config.json"));
            _reactor = new JiraIssueReactor
            {
                JiraUsername = config["username"].Value<string>(),
                JiraPassword = config["password"].Value<string>(),
                JiraProjectKey = config["projectKey"].Value<string>(),
                JiraServerUrl = config["serverUrl"].Value<string>()
            };
        }

        private readonly JiraIssueReactor _reactor;

        [Fact]
        public void Test1()
        {
            _reactor.JiraIssueType = "Story";
            _reactor.On(new Event<LogEventData>("eventid", 1, DateTime.UtcNow, new LogEventData
            {
                Id = "event_id",
                RenderedMessage = "This is the rendered message.",
                Exception = new Exception("1 2 1 2, this is just a test.").ToString()
            }));
        }
    }
}