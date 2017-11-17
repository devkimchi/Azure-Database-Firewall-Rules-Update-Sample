using System;

namespace FirewallRules.FunctionApp
{
    /// <summary>
    /// This represents the config entity.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Gets the tenant Id (directory Id).
        /// </summary>
        public static string TenantId { get; } = Environment.GetEnvironmentVariable("Sp.DirectoryId");

        /// <summary>
        /// Gets the subscription Id.
        /// </summary>
        public static string SubscriptionId { get; } = Environment.GetEnvironmentVariable("Sp.SubscriptionId");

        /// <summary>
        /// Gets the client Id (application Id).
        /// </summary>
        public static string ClientId { get; } = Environment.GetEnvironmentVariable("Sp.ApplicationId");

        /// <summary>
        /// Gets the client secret (application key).
        /// </summary>
        public static string ClientSecret { get; } = Environment.GetEnvironmentVariable("Sp.ApplicationKey");

        /// <summary>
        /// Gets the resource group name.
        /// </summary>
        public static string ResourceGroupName { get; } = Environment.GetEnvironmentVariable("Rg.Name");
    }
}
