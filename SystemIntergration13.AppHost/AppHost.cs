var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ResilienceTestApi>("resiliencetestapi");

builder.AddProject<Projects.MyResilientApi>("myresilientapi");

builder.Build().Run();
