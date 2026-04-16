var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ResilienceTestApi>("resiliencetestapi");

builder.Build().Run();
