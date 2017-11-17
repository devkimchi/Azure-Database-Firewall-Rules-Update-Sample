using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace FirewallRules.FunctionApp
{
    /// <summary>
    /// This represents the timer trigger entity to update firewall rules for Azure SQL Servers.
    /// </summary>
    public static class UpdateFirewallRulesTimerTrigger
    {
        /// <summary>
        /// Invokes the timer trigger
        /// </summary>
        /// <param name="myTimer"><see cref="TimerInfo"/> instance.</param>
        /// <param name="log"><see cref="TraceWriter"/> instance.</param>
        [FunctionName("UpdateFirewallRulesTimerTrigger")]
        public static async Task Run(
            [TimerTrigger("0 0 0 * * *")]TimerInfo myTimer,
            TraceWriter log)
        {
            var tenantId = Config.TenantId;
            var subscriptionId = Config.SubscriptionId;
            var clientId = Config.ClientId;
            var clientSecret = Config.ClientSecret;

            var credentials =
                SdkContext.AzureCredentialsFactory
                          .FromServicePrincipal(
                                                clientId,
                                                clientSecret,
                                                tenantId,
                                                AzureEnvironment.AzureGlobalCloud);

            var logLevel = HttpLoggingDelegatingHandler.Level.Basic;
            var azure = Azure.Configure()
                             .WithLogLevel(logLevel)
                             .Authenticate(credentials)
                             .WithSubscription(subscriptionId);

            var resourceGroupName = Config.ResourceGroupName;

            log.Info($"Firewall rules on database servers in {resourceGroupName} are updating...");

            var res = await azure.WebApps
                                 .Inner
                                 .ListByResourceGroupWithHttpMessagesAsync(resourceGroupName)
                                 .ConfigureAwait(false);
            var webapps = res.Body.ToList();
            var outboundIps = webapps.SelectMany(p => p.OutboundIpAddresses.Split(','))
                                     .Distinct()
                                     .ToList();

            var tasks = new List<Task>();

            var servers = await azure.SqlServers
                                     .ListByResourceGroupAsync(resourceGroupName)
                                     .ConfigureAwait(false);
            foreach (var server in servers)
            {
                var registeredIps = server.FirewallRules
                                          .List()
                                          .ToDictionary(p => p.Name, p => p.StartIPAddress);
                var ipsToExclude = registeredIps.Where(p => !outboundIps.Contains(p.Value))
                                                .Select(p => p.Key)
                                                .ToList();
                var IpsToInclude = outboundIps.Where(p => !registeredIps.ContainsValue(p))
                                              .ToList();

                var tasksToExclude = ipsToExclude.Select(ip => server.FirewallRules
                                                                     .DeleteAsync(ip));
                var tasksToInclude = IpsToInclude.Select(ip => server.FirewallRules
                                                                     .Define($"webapp-{ip.Replace(".", "-")}")
                                                                     .WithIPAddressRange(ip, ip)
                                                                     .CreateAsync());
                tasks.AddRange(tasksToExclude);
                tasks.AddRange(tasksToInclude);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            log.Info($"Firewall rules on database servers in {resourceGroupName} have been updated.");
        }
    }
}
