﻿using FreeSql;

namespace Todd.ApplicationKernel.Persistence.FreeSql
{
    public class FreeSqlCloud : FreeSqlCloud<string>
    {
        public FreeSqlCloud() : base(null) { }
        public FreeSqlCloud(string distributeKey) : base(distributeKey) { }
    }
}
