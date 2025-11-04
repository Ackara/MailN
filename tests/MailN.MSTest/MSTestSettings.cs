[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
[assembly: ApprovalTests.Namers.UseApprovalSubdirectory("approved-results")]
[assembly: ApprovalTests.Reporters.UseReporter(typeof(ApprovalTests.Reporters.DiffReporter))]