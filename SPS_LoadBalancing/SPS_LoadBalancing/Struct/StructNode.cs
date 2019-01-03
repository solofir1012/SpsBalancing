using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPS_LoadBalancing.Struct
{
    public class StructNode
    {
        /// <summary>
        /// 主备机权限
        /// </summary>
        public enum Priority
        {
            /// <summary>
            /// 主机
            /// </summary>
            main,
            /// <summary>
            /// 备机
            /// </summary>
            standby
        }
    }
}
