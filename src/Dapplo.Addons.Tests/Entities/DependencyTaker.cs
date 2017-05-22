using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapplo.Addons.Tests.Entities
{
    public class DependencyTaker
    {
        [Import]
        public string Take { get; set; }
    }
}
