using Quartz;
using System.Threading.Tasks;

namespace CoreProxy
{
    [Quartz.DisallowConcurrentExecution]
    public class SysncActiveDirectoryJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            //lock (Local.TimeRecord)
            {
                //var x = (Local.TimeRecord.Total*8).Bytes().Humanize();
                //Console.WriteLine(x);
                // Local.TimeRecord.Total = 0;
            }

            return Task.CompletedTask;
        }
    }
}
