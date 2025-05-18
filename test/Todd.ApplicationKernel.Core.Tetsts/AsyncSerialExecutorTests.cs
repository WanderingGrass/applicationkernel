using Todd.Applicationkernel.Core;
using Xunit;
using Xunit.Abstractions;

namespace Todd.ApplicationKernel.Core.Tests
{
    public class AsyncSerialExecutorTests
    {
        public ITestOutputHelper output;
        public int operationsInProgress;

        public AsyncSerialExecutorTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task AsyncSerialExecutorTests_Small()
        {
            AsyncSerialExecutor executor = new AsyncSerialExecutor();
            List<Task> tasks = new List<Task>();
            operationsInProgress = 0;

            tasks.Add(executor.AddNext(() => Operation(1)));
            tasks.Add(executor.AddNext(() => Operation(2)));
            tasks.Add(executor.AddNext(() => Operation(3)));

            await Task.WhenAll(tasks);
        }

        private async Task Operation(int opNumber)
        {
            if (operationsInProgress > 0) Assert.Fail($"1: Operation {opNumber} found {operationsInProgress} operationsInProgress.");
            operationsInProgress++;
            var delay = RandomTimeSpan.Next(TimeSpan.FromSeconds(2));

            output.WriteLine("Task {0} Staring", opNumber);
            await Task.Delay(delay);
            if (operationsInProgress != 1) Assert.Fail($"2: Operation {opNumber} found {operationsInProgress} operationsInProgress.");

            output.WriteLine("Task {0} after first delay", opNumber);
            await Task.Delay(delay);
            if (operationsInProgress != 1) Assert.Fail($"3: Operation {opNumber} found {operationsInProgress} operationsInProgress.");

            operationsInProgress--;
            output.WriteLine("Task {0} Done", opNumber);
        }
    }
}
