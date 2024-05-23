using BenchmarkDotNet.Running;
using Core.Application.BenchmarkTests.Pipelines.Authorization;

BenchmarkRunner.Run<AuthorizationBehaviorBenchmark>();
