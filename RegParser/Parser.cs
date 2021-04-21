using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegParser
{
    /// <summary>
    /// This Class helps in creating a registry backup based on provided registry key
    /// as well as retore them using only the built in Registry class of c#,
    /// with that said, this only supports specific Registry data types supported by the said class.
    /// 
    /// Supported types { REG_SZ, REG_DWORD, REG_QWORD, REG_BINARY, REG_EXPAND_SZ, REG_MULTI_SZ REG_NONE }
    /// 
    /// Unsupported types will likely be treated as string. Any improvement to this class are welcome! (^_^)
    /// </summary>
    public static class Parser
    {
    }
}
