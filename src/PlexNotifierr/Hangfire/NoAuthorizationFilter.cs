using Hangfire.Dashboard;

namespace PlexNotifierr.Api.Hangfire;

public class NoAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}