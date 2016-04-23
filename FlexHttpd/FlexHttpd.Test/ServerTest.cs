using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace FlexHttpd.Test
{
    [TestClass]
    public class ServerTest
    {
        //[TestInitialize]
        //public void TestInitialize()
        //{
            
        //}

        [TestMethod]
        public async Task Start()
        {
            var server = new FlexServer();

            var completionSource = new TaskCompletionSource<FlexRequest>();

            server.Get["/{action}/{state}"] = (request) =>
            {
                completionSource.SetResult(request);
                return Task.FromResult(new FlexResponse(FlexHttpStatus.Ok));
            };

            await server.Start(55555);

            var flexRequest = await completionSource.Task;
            Assert.IsNotNull(flexRequest);
        }
    }
}
