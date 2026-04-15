var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ResilienceTestApi>("resiliencetestapi");

builder.AddProject<Projects.ResilientApi>("resilientapi");

builder.Build().Run();
