﻿using CaptivePortal.Helpers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CaptivePortal.Services.Outer
{
    public partial class IronNacConfiguration
        : DotEnvConfiguration<IronNacConfiguration>
    {
        public IronNacConfiguration() : base() { }

        [EnvList("IRONNAC_WEB_LISTEN_ADDRESSES", defaultValue: "0.0.0.0")]
        public List<string> WebListenAddresses { get; private set; } = null!;

        [Env("IRONNAC_WEB_HOSTNAME", optional: true)]
        public string? WebHostname { get; private set; }

        [EnvList("IRONNAC_WEB_REDIRECT_BYPASS_HOSTS", defaultValue: "")]
        public List<string> WebRedirectBypassHosts { get; private set; } = null!;

        [Env<int>("IRONNAC_WEB_HTTP_PORT", defaultValue: "80")]
        public int WebHttpPort { get; private set; }

        [Env<bool>("IRONNAC_WEB_USE_HTTPS", defaultValue: "true")]
        public bool WebUseHttps { get; set; }

        [Env<int>("IRONNAC_WEB_HTTPS_PORT", defaultValue: "443")]
        public int WebHttpsPort { get; set; }

        [Env("IRONNAC_WEB_HTTPS_CERT_EMAIL", optional: true)]
        public string? WebHttpsCertEmail { get; set; }

        [Env<bool>("IRONNAC_WEB_HTTPS_CERT_STAGING", defaultValue: "false")]
        public bool WebHttpsCertStaging { get; set; }

        [Env("IRONNAC_DATABASE_HOSTNAME")]
        public string DatabaseHostname { get; set; } = null!;

        [Env<int>("IRONNAC_DATABASE_PORT", defaultValue: "5432")]
        public int DatabasePort { get; set; }

        [Env("IRONNAC_DATABASE_DBNAME")]
        public string DatabaseDbName { get; set; } = null!;

        [Env("IRONNAC_DATABASE_USERNAME")]
        public string DatabaseUsername { get; set; } = null!;

        [Env("IRONNAC_DATABASE_PASSWORD")]
        public string DatabasePassword { get; set; } = null!;

        [Env("IRONNAC_RADIUS_AUTHORIZATION_LISTEN_ADDRESS")]
        public string RadiusAuthorizationListenAddress { get; set; } = null!;

        [Env<int>("IRONNAC_RADIUS_AUTHORIZATION_PORT", defaultValue: "1812")]
        public int RadiusAuthorizationPort { get; set; }

        [Env("IRONNAC_RADIUS_AUTHORIZATION_SECRET")]
        public string RadiusAuthorizationSecret { get; set; } = null!;

        [Env("IRONNAC_RADIUS_ACCOUNTING_LISTEN_ADDRESS")]
        public string RadiusAccountingListenAddress { get; set; } = null!;

        [Env<int>("IRONNAC_RADIUS_ACCOUNTING_PORT", defaultValue: "1813")]
        public int RadiusAccountingPort { get; set; }

        [Env("IRONNAC_RADIUS_ACCOUNTING_SECRET")]
        public string RadiusAccountingSecret { get; set; } = null!;

        [Env("IRONNAC_AWS_ACCESS_TOKEN")]
        public string? AwsAccessToken { get; set; }

        [Env("IRONNAC_AWS_SECRET_KEY")]
        public string? AwsSecretKey { get; set; }

        [Env("IRONNAC_AWS_REGION")]
        public string? AwsRegion { get; set; }

        [Env("IRONNAC_AWS_HOSTED_ZONE_ID")]
        public string? AwsHostedZoneId { get; set; }

        [Env("IRONNAC_DNS_LISTEN_ADDRESS")]
        public string DnsListenAddress { get; set; } = null!;

        [Env<int>("IRONNAC_DNS_PORT", defaultValue: "53")]
        public int DnsPort { get; set; }

        [Env("IRONNAC_DNS_REDIRECT_ADDRESS")]
        public string DnsRedirectAddress { get; set; } = null!;
    }
}