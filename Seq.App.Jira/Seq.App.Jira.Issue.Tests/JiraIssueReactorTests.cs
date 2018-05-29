using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public void Given_Story_Issue_Type_When_Reacting_On_Seq_Event_Then_Dont_throw()
        {
            _reactor.JiraIssueType = "Story";
            
            var @event = EventHelper.CreateEventFromFile("event-9ebc39fec59e08d5f9cf690000000000.json");

            _reactor.On(@event);
        }
    }
}