using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace QuartzTests.Listeners
{
    public class SchedulerListener /*: ISchedulerListener*/
    {
        private readonly ILogger<ISchedulerListener> _logger;
        private readonly IServiceProvider _serviceProvider = null;

        public SchedulerListener(ILogger<ISchedulerListener> logger,IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
    }
}
