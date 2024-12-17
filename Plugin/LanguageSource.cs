using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    class LanguageSource : ILanguageSource
    {
        public LanguageSourceData SourceData { get => ContentLoader.lsd; set { } }
    }
}
