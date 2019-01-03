using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SPS_LoadBalancing.Struct;

namespace SPS_LoadBalancing.Connect
{
    public class UDP
    {
        //模拟数据源
        private static IPEndPoint endpoint_Rev = new IPEndPoint(IPAddress.Any, 0);
        
        /// <summary>
        /// 接收UDP消息处理
        /// </summary>
        public static void UdpReceive()
        {
            while (true)
            {
                try
                {
                    #region
                    //接收一条 数据
                    byte[] buf = mainForm.thisForm.udpcRecv.Receive(ref endpoint_Rev);
                    string str = Encoding.UTF8.GetString(buf);
                    //输出当前收到的信息
                    List<string> listRecMsg = str.Split('#').ToList();
                    if (listRecMsg.Count >= 3)
                    {
                        if (listRecMsg[0] == "0x02")
                        {
                            //说明是主备机切换消息
                            if (listRecMsg[1] == "Info_Heart_MainOrStandBy")
                            {
                                if (listRecMsg[2] != mainForm.thisForm._localHost)
                                {
                                    mainForm.thisForm.listBox1.Items.Add(DateTime.Now.ToString() + "--接收--心跳正常--" + str);
                                    lock (mainForm.thisForm._lockSwitch)
                                    {
                                        if (mainForm.thisForm._tcpInitStandByServerHost == listRecMsg[2])
                                        {
                                            //设置上次接收的时间
                                            mainForm.thisForm._timeStandByLast = DateTime.Now;
                                            //说明是备机发送的消息
                                            //判断里面是主机设置还是备机设置
                                            if (listRecMsg[3] == StructNode.Priority.main.ToString())
                                            {
                                                //说明收到主机消息
                                                //查看本机的状态
                                                if (mainForm.thisForm._priority == StructNode.Priority.main)
                                                {
                                                    //本机也是主机
                                                    //判断谁是主机的时间早，谁早谁成为主机
                                                    DateTime _switchTimeStandBy = Convert.ToDateTime(listRecMsg[4]);
                                                    if (_switchTimeStandBy < mainForm.thisForm._switchPriorityTime)
                                                    {
                                                        //说明另外一个机器成为主机的时间早
                                                        //保持原有的设置
                                                        //不做切换
                                                        mainForm.thisForm._priority = StructNode.Priority.standby;
                                                        mainForm.thisForm._switchPriorityTime = DateTime.Now;
                                                        //发送切换的广播消息
                                                        //待定发送
                                                        //1是设置主机消息
                                                        string sendInfo = "0x02#Info_Switch_MainOrStandBy#1#" + mainForm.thisForm._tcpInitStandByServerHost + "#" + StructNode.Priority.main.ToString() +
                                                            "#" + DateTime.Now.ToString();
                                                        //发送
                                                        byte[] bytes = Encoding.Default.GetBytes(sendInfo);
                                                        mainForm.thisForm.udpcSend.Send(bytes, bytes.Length, mainForm.thisForm.endpoint_Send);
                                                    }
                                                    else
                                                    {
                                                        //将自己是主机的设置重新发布一次
                                                        //1是设置主机消息
                                                        string sendInfo = "0x02#Info_Switch_MainOrStandBy#1#" + mainForm.thisForm._localHost + "#" + StructNode.Priority.main.ToString() +
                                                            "#" + DateTime.Now.ToString();
                                                        //发送
                                                        byte[] bytes = Encoding.Default.GetBytes(sendInfo);
                                                        mainForm.thisForm.udpcSend.Send(bytes, bytes.Length, mainForm.thisForm.endpoint_Send);
                                                    }
                                                }
                                                else
                                                {
                                                    //说明对方是主机，自己是备机，不用做操作
                                                }
                                            }
                                            else
                                            {
                                                //说明收到备机消息
                                                //查看本机的状态
                                                if (mainForm.thisForm._priority == StructNode.Priority.main)
                                                {
                                                    //本机是主机，不需要操作
                                                }
                                                else
                                                {
                                                    //说明是双备机
                                                    DateTime _switchTimeStandBy = Convert.ToDateTime(listRecMsg[4]);
                                                    if (_switchTimeStandBy < mainForm.thisForm._switchPriorityTime)
                                                    {
                                                        mainForm.thisForm._priority = StructNode.Priority.main;
                                                        mainForm.thisForm._switchPriorityTime = DateTime.Now;
                                                        //将自己设置成主机
                                                        //1是设置主机消息
                                                        string sendInfo = "0x02#Info_Switch_MainOrStandBy#1#" + mainForm.thisForm._localHost + "#" + StructNode.Priority.main.ToString() +
                                                            "#" + DateTime.Now.ToString();
                                                        //发送
                                                        byte[] bytes = Encoding.Default.GetBytes(sendInfo);
                                                        mainForm.thisForm.udpcSend.Send(bytes, bytes.Length, mainForm.thisForm.endpoint_Send);
                                                    }
                                                    else
                                                    {
                                                        //将自己设置成主机
                                                        //1是设置主机消息
                                                        string sendInfo = "0x02#Info_Switch_MainOrStandBy#1#" + mainForm.thisForm._tcpInitStandByServerHost + "#" + StructNode.Priority.main.ToString() +
                                                            "#" + DateTime.Now.ToString();
                                                        //发送
                                                        byte[] bytes = Encoding.Default.GetBytes(sendInfo);
                                                        mainForm.thisForm.udpcSend.Send(bytes, bytes.Length, mainForm.thisForm.endpoint_Send);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                   //报错
                }
            }
        }

       
    }
}
