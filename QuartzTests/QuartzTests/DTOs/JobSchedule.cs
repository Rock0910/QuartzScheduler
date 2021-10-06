#region DTO說明
//建立一個物件，傳這物件進去後能拿裡面的值自動產生Job 跟 Trigger
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuartzTests.DTOs
{
    public class JobSchedule
    {
        public JobSchedule(Type jobType, string cronExpression, string jobName, string jobGroup, List<string> excludedGroupNames = null)
        {
            JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
            CronExpression = cronExpression ?? throw new ArgumentNullException(nameof(cronExpression));
            JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
            JobGroup = jobGroup ?? throw new ArgumentNullException(nameof(jobGroup));
            ExcludedGroupNames = excludedGroupNames;
        }

        public JobSchedule(Type jobType, string jobName, string jobGroup, List<string> excludedGroupNames = null)
        {
            JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
            JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
            JobGroup = jobGroup ?? throw new ArgumentNullException(nameof(jobGroup));
            ExcludedGroupNames = excludedGroupNames;
        }
        ///<summary>
        ///Job名稱
        ///</summary>
        public string JobName { get; private set; }
        public string JobGroup { get; private set; }

        ///<summary>
        ///Job名稱
        ///</summary>
        public Type JobType { get; private set; }

        ///<summary>
        ///Job名稱
        ///</summary>
        public string CronExpression { get; private set; }
        public List<string> ExcludedGroupNames { get; private set; }

        ///<summary>
        ///Job名稱
        ///</summary>
        public JobStatus JobStatus { get; set; } = JobStatus.Init;
    }
}
