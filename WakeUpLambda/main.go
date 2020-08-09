package main

import (
	"context"
	"github.com/aws/aws-lambda-go/lambda"
	"github.com/aws/aws-sdk-go/aws"
	"github.com/aws/aws-sdk-go/aws/session"
	"github.com/aws/aws-sdk-go/service/gamelift"
)

const aliasId = "alias-860fde8f-5f40-45bb-8f17-2b10a8ed1f34"

func HandleRequest(ctx context.Context, _ struct{}) (string, error) {
	cfg := session.Must(session.NewSession())
	svc := gamelift.New(cfg)
	capacity, err := svc.DescribeFleetCapacityWithContext(ctx, &gamelift.DescribeFleetCapacityInput{})
	if err != nil {
		return "Failed to describe fleet capacity", err
	}
	if *capacity.FleetCapacity[0].InstanceCounts.ACTIVE == 0 {
		fleet, err := svc.ResolveAliasWithContext(ctx, &gamelift.ResolveAliasInput{AliasId: aws.String(aliasId)})
		if err != nil {
			return "Failed to resolve alias", err
		}
		_, err = svc.UpdateFleetCapacityWithContext(ctx, &gamelift.UpdateFleetCapacityInput{FleetId: fleet.FleetId, DesiredInstances: aws.Int64(1)})
		if err != nil {
			return "Failed to update fleet capacity", err
		}
	}
	return "", nil
}

func main() {
	lambda.Start(HandleRequest)
}
