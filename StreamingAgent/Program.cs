// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Azure.IoTSolutions.IoTStreamAnalytics.Services;
using Microsoft.Azure.IoTSolutions.IoTStreamAnalytics.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.IoTStreamAnalytics.Services.Models;

namespace Microsoft.Azure.IoTSolutions.IoTStreamAnalytics.StreamingAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var container = DependencyResolution.Setup();

            SetupRules(container).Wait();

            var agent = container.Resolve<IAgent>();

            var cts = new CancellationTokenSource();
            var task = agent.RunAsync(cts.Token);

            // Wait until task completed or canceled
            task.Wait();
        }

        public static async Task SetupRules(IContainer container)
        {
            var ruleService = container.Resolve<IRules>();
            var logger = container.Resolve<ILogger>();

            // Create new rules
            RuleApiModel heatingOnRule = new RuleApiModel
            {
                Name = "turn-on-heating",
                Enabled = true,
                Description = "Time to turn on hearing for user's smart toilet",
                GroupId = "toilet",
                Severity = "info",
                Conditions = new List<ConditionApiModel>
                {
                    new ConditionApiModel
                    {
                        Field = "time",
                        Operator = "Equals",
                        Value = "5 PM"
                    },

                    new ConditionApiModel
                    {
                        Field = "heatingStatus",
                        Operator = "Equals",
                        Value = "off"
                    }
                }
            };

            RuleApiModel heatingOffRule = new RuleApiModel
            {
                Name = "turn-off-heating",
                Enabled = true,
                Description = "Time to turn off hearing for user's smart toilet",
                GroupId = "toilet",
                Severity = "info",
                Conditions = new List<ConditionApiModel>
                {
                    new ConditionApiModel
                    {
                        Field = "time",
                        Operator = "Equals",
                        Value = "5:15 PM"
                    },

                    new ConditionApiModel
                    {
                        Field = "heatingStatus",
                        Operator = "Equals",
                        Value = "on"
                    }
                }
            };

            RuleApiModel heatingOnRuleResponse = null;
            RuleApiModel heatingOffRuleResponse = null;

            try
            {
                heatingOnRuleResponse = await ruleService.CreateAsync(heatingOnRule);
                logger.Info($"Create heating-on rule succeeded, rule ID: {heatingOnRuleResponse.Id}", () => { });
            }
            catch (Exception e)
            {
                logger.Error($"Create heating-on rule failed, error details: {e.Message}", () => { });
            }

            try
            {
                heatingOffRuleResponse = await ruleService.CreateAsync(heatingOffRule);
                logger.Info($"Create heating-off rule succeeded, rule ID: {heatingOnRuleResponse.Id}", () => { });
            }
            catch (Exception e)
            {
                logger.Error($"Create heating-off rule failed, error details: {e.Message}", () => { });
            }
        }
    }
}
