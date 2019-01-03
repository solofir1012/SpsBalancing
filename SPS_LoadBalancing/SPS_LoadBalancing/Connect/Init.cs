using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using SPS_LoadBalancing.Struct;
using System.Net;
using System.Net.Sockets;

namespace SPS_LoadBalancing.Connect
{
    public class Init
    {
        public static StructNode.Priority Priority(string priority)
        {
            if (priority == "main")
            {
                return StructNode.Priority.main;
            }
            else
            {
                return StructNode.Priority.standby;
            }
        }

        /// <summary>
        /// 查看网络是否正常
        /// </summary>
        /// <param name="host">网络IP地址</param>
        /// <returns></returns>
        public static bool PingAll(string host)
        {
            //Ping 实例对象;
            Ping pingSender = new Ping();
            PingReply reply = pingSender.Send(host);
            if (reply.Status == IPStatus.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 获取本机IP地址
        /// </summary>
        /// <returns>本机IP地址</returns>
        public static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
