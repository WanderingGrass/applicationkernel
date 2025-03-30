using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrainInterfaces
{
    public interface IHello: IGrainWithIntegerKey
    {
        ValueTask<string> SayHello(string greeting);
    }
}
