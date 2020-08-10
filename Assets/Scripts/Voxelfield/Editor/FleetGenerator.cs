using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Amazon.Runtime;
using UnityEditor;
using UnityEngine;
using Voxelfield.Session;

namespace Voxelfield.Editor
{
    public static class FleetGenerator
    {
        private const string AccessKey = @"AKIA354QE4UEK73BYOGL", SecretKey = @"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"; // shaweewoo.jo
        // const string accessKey = @"AKIAWKQVDVRWSC42QICS", secretKey = @"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"; // shaweewoo.codes
        private static readonly BasicAWSCredentials Credentials = new BasicAWSCredentials(AccessKey, SecretKey);

        [MenuItem("Build/Create Game Lift Fleet", priority = 300)]
        private static async void CreateFleet()
        {
            // Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", accessKey);
            // Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", secretKey);
            // Debug.Log("Executing upload zsh script...");
            // string buildPath = $"{Application.dataPath}/../Builds",
            //        command = $"C:/msys64/usr/bin/sh.exe -c \"{buildPath}/gamelift_publish.zsh {Application.version}\"";
            // SessionExtensions.ExecuteProcess(command, buildPath);

            var config = new AmazonGameLiftConfig {RegionEndpoint = RegionEndpoint.USWest1};
            var client = new AmazonGameLiftClient(Credentials, config);

            // var buildRequest = new CreateBuildRequest
            // {
            //     Name = "voxelfield", OperatingSystem = OperatingSystem.AMAZON_LINUX_2, Version = Application.version,
            //     StorageLocation = new S3Location{}
            // };
            // CreateBuildResponse response = client.CreateBuild(buildRequest);

            ListBuildsResponse builds = await client.ListBuildsAsync(new ListBuildsRequest {Limit = 1, Status = BuildStatus.READY});
            Build build = builds.Builds.OrderByDescending(b => b.CreationTime).First();
            string buildId = build.BuildId;
            Debug.Log($"Using build: {build.Name} version: {build.Version} for new fleet");

            string activeFleetId = (await client.ListFleetsAsync(new ListFleetsRequest {Limit = 1})).FleetIds.FirstOrDefault();
            if (activeFleetId == null) Debug.Log("No active fleet to terminate");
            else
            {
                await client.DeleteFleetAsync(activeFleetId);
                Debug.Log("Marked active fleet for termination");
            }

            IpPermission CreateIpPermission(IpProtocol protocol) => new IpPermission
            {
                FromPort = SessionManager.DefaultPort, IpRange = "0.0.0.0/0", Protocol = protocol, ToPort = SessionManager.DefaultPort + 1
            };
            var fleetRequest = new CreateFleetRequest
            {
                Name = $"voxelfield_{build.Version}",
                FleetType = FleetType.ON_DEMAND,
                CertificateConfiguration = new CertificateConfiguration {CertificateType = CertificateType.DISABLED},
                BuildId = buildId,
                EC2InboundPermissions = new List<IpPermission> {CreateIpPermission(IpProtocol.TCP), CreateIpPermission(IpProtocol.UDP)},
                EC2InstanceType = EC2InstanceType.C5Large,
                RuntimeConfiguration = new RuntimeConfiguration
                {
                    GameSessionActivationTimeoutSeconds = 600, MaxConcurrentGameSessionActivations = 1,
                    ServerProcesses = new List<ServerProcess>
                        {new ServerProcess {ConcurrentExecutions = 1, LaunchPath = "/local/game/Voxelfield", Parameters = "-logFile /local/game/server.log"}}
                },
                NewGameSessionProtectionPolicy = ProtectionPolicy.FullProtection
            };
            CreateFleetResponse fleet = await client.CreateFleetAsync(fleetRequest);
            string fleetId = fleet.FleetAttributes.FleetId;
            Debug.Log($"Created fleet: {fleetId}");

            /* Request scaling down event to zero instances if no game sessions are present */
            var sleep = new PutScalingPolicyRequest
            {
                Name = "sleep", Threshold = 1, ComparisonOperator = ComparisonOperatorType.LessThanThreshold, EvaluationPeriods = 5, FleetId = fleetId,
                MetricName = MetricName.ActiveGameSessions, PolicyType = PolicyType.RuleBased, ScalingAdjustment = 0, ScalingAdjustmentType = ScalingAdjustmentType.ExactCapacity
            };
            // var scale = new PutScalingPolicyRequest
            // {
            //     Name = "scale", Threshold = 0, ComparisonOperator = ComparisonOperatorType.GreaterThanThreshold, EvaluationPeriods = 1, FleetId = fleetId,
            //     MetricName = MetricName.QueueDepth, PolicyType = PolicyType.RuleBased, ScalingAdjustment = 1, ScalingAdjustmentType = ScalingAdjustmentType.ChangeInCapacity
            // };
            await client.PutScalingPolicyAsync(sleep);
            // await client.PutScalingPolicyAsync(scale);
            Debug.Log("Created policy");

            /* Update fleet alias to point to new fleet */
            ListAliasesResponse aliases = await client.ListAliasesAsync(new ListAliasesRequest {Limit = 1, Name = "us-west-1"});
            Alias alias = aliases.Aliases.First();
            var routine = new RoutingStrategy {FleetId = fleetId, Type = RoutingStrategyType.SIMPLE};
            await client.UpdateAliasAsync(new UpdateAliasRequest {AliasId = alias.AliasId, RoutingStrategy = routine});
            Debug.Log($"Updated alias: {alias.AliasId}");
        }

        [MenuItem("Build/Put Custom Scaling Policies", priority = 301)]
        private static async void PutCustomScalingPolicies()
        {
            var config = new AmazonGameLiftConfig {RegionEndpoint = RegionEndpoint.USWest1};
            var client = new AmazonGameLiftClient(Credentials, config);
            string fleetId = (await client.ListFleetsAsync(new ListFleetsRequest {Limit = 1})).FleetIds.First();
            await client.DeleteScalingPolicyAsync(new DeleteScalingPolicyRequest {FleetId = fleetId, Name = "sleep"});
            await client.DeleteScalingPolicyAsync(new DeleteScalingPolicyRequest {FleetId = fleetId, Name = "scale"});
            Debug.Log("Deleted existing policies");

            await Task.Delay(TimeSpan.FromSeconds(2));

            var sleep = new PutScalingPolicyRequest
            {
                Name = "sleep", Threshold = 1, ComparisonOperator = ComparisonOperatorType.LessThanThreshold, EvaluationPeriods = 5, FleetId = fleetId,
                MetricName = MetricName.ActiveGameSessions, PolicyType = PolicyType.RuleBased, ScalingAdjustment = -1,
                ScalingAdjustmentType = ScalingAdjustmentType.ChangeInCapacity
            };
            var scale = new PutScalingPolicyRequest
            {
                Name = "scale", Threshold = 0, ComparisonOperator = ComparisonOperatorType.GreaterThanThreshold, EvaluationPeriods = 1, FleetId = fleetId,
                MetricName = MetricName.QueueDepth, PolicyType = PolicyType.RuleBased, ScalingAdjustment = 1, ScalingAdjustmentType = ScalingAdjustmentType.ChangeInCapacity
            };
            await client.PutScalingPolicyAsync(sleep);
            await client.PutScalingPolicyAsync(scale);
            Debug.Log("Put custom policies");
        }
    }
}