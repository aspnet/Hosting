namespace Microsoft.AspNetCore.Server.IntegrationTesting.Common
{
    // Public for use in other test projects
    public static class TestUrlHelper
    {
        public static string BuildTestUrl(ServerType serverType)
        {
            return TestUriHelper.BuildTestUri(serverType).ToString();
        }
    }
}
