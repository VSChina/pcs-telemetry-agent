// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
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

            var allRules = ruleService.GetAllAsync().Result;
            allRules = allRules.Where(r => r.GroupId.Equals("toilet", StringComparison.OrdinalIgnoreCase));

            RuleApiModel heatingOnRule;
            RuleApiModel heatingOffRule;

            var result = allRules.Where(r => r.Name.Equals("turn-on-heating", StringComparison.OrdinalIgnoreCase));
            if (result != null && result.Count() > 0)
            {
                // heating-on rule already exists
                heatingOnRule = result.Last();
            }
            else
            {
                // Create new rules
                heatingOnRule = new RuleApiModel
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

                RuleApiModel heatingOnRuleResponse = null;

                try
                {
                    heatingOnRuleResponse = await ruleService.CreateAsync(heatingOnRule);
                    logger.Info($"Create heating-on rule succeeded, rule ID: {heatingOnRuleResponse.Id}", () => { });
                }
                catch (Exception e)
                {
                    logger.Error($"Create heating-on rule failed, error details: {e.Message}", () => { });
                }
            }

            result = allRules.Where(r => r.Name.Equals("turn-off-heating", StringComparison.OrdinalIgnoreCase));
            if (result != null && result.Count() > 0)
            {
                // heating-off rule already exists
                heatingOffRule = result.Last();
            }
            else
            {
                heatingOffRule = new RuleApiModel
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

                RuleApiModel heatingOffRuleResponse = null;

                try
                {
                    heatingOffRuleResponse = await ruleService.CreateAsync(heatingOffRule);
                    logger.Info($"Create heating-off rule succeeded, rule ID: {heatingOffRuleResponse.Id}", () => { });
                }
                catch (Exception e)
                {
                    logger.Error($"Create heating-off rule failed, error details: {e.Message}", () => { });
                }
            }
        }
    }
}
