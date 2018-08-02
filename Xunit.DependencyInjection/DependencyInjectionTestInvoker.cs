using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit.DependencyInjection
{
    public class DependencyInjectionTestInvoker : TestInvoker<IXunitTestCase>
    {
        //testclass run one by one
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly TestOutputHelper _testOutputHelper;

        public DependencyInjectionTestInvoker(IServiceProvider provider, ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, aggregator, cancellationTokenSource)
        {
            try
            {
                _testOutputHelper = (TestOutputHelper)provider.GetRequiredService<ITestOutputHelper>();
            }
            catch (InvalidOperationException)
            {
                // ignored
            }
        }

        public string Output { get; set; }

        protected override async Task BeforeTestMethodInvokedAsync()
        {
            if (_testOutputHelper != null)
            {
                await SemaphoreSlim.WaitAsync();
                _testOutputHelper.Initialize(MessageBus, Test);
            }

            await  base.BeforeTestMethodInvokedAsync();
        }

        protected override Task AfterTestMethodInvokedAsync()
        {
            if (_testOutputHelper != null)
            {
                Output = _testOutputHelper.Output;
                _testOutputHelper.Uninitialize();
                SemaphoreSlim.Release();
            }
            
            return base.AfterTestMethodInvokedAsync();
        }
    }
}
