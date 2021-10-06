using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuartzTests.DTOs
{
    public class Cron
    {
        public string combined;
        public Cron(string cronexpression)
        {
            combined = cronexpression;
        }
    }
}
