using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Configuration;
using SPS_LoadBalancing.Connect;
using SPS_LoadBalancing.Struct;
using System.Net.Sockets;
using System.Net;

namespace SPS_LoadBalancing
{
    public partial class mainForm : Form
    {
        //mainForm初始化
        public static mainForm thisForm;

        #region 网络载入
        /// <summary>
        /// 主机IP
        /// </summary>
        //public string _tcpInitMainServerHost = ConfigurationManager.AppSettings["TCP_MainServerIP"].ToString();
        /// <summary>
        /// 备机IP
        /// </summary>
        public string tcpInitStandByServerHost = ConfigurationManager.AppSettings["TCP_StandByServerIP"].ToString();
        /// <summary>
        /// 链接的端口
        /// </summary>
        public string tcpInitServerPort = ConfigurationManager.AppSettings["TCP_ServerPort"].ToString();
        /// <summary>
        /// 主备机权限
        /// </summary>
        public string priorityInit = ConfigurationManager.AppSettings["Priority"].ToString();
        public StructNode.Priority priority = StructNode.Priority.standby;
        /// <summary>
        /// 上次切换主备机的时间
        /// </summary>
        public DateTime switchPriorityTime = DateTime.Now;
        /// <summary>
        /// 科室信息
        /// </summary>
        public string office = ConfigurationManager.AppSettings["Office"].ToString();

        #endregion
        
        #region 系统当前主备机的配置
        /// <summary>
        /// 本机IP
        /// </summary>
        public string localHost = "";
        /// <summary>
        /// 主备机消息处理锁
        /// </summary>
        public object lockSwitch = new object();
        /// <summary>
        /// 当前主机IP
        /// </summary>
        public string tcpNowMainServerHost = "";
        /// <summary>
        /// 当前备机的IP
        /// </summary>
        public string tcpNowStandByServerHost = "";
        /// <summary>
        /// 链接的端口
        /// </summary>
        //public string _tcpNowServerPort = "";
        /// <summary>
        /// 当前本机的主备机情况
        /// </summary>
        //public StructNode.Priority _nowLocalPriority = StructNode.Priority.standby;
        public int intervalTime = Convert.ToInt16(ConfigurationManager.AppSettings["intervalTime"].ToString());
        public int intervalDis = Convert.ToInt16(ConfigurationManager.AppSettings["intervalDis"].ToString());
        #endregion

        #region UDP配置
        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint endpoint_Send = new IPEndPoint(IPAddress.Parse(ConfigurationManager.AppSettings["SendAlarmIp"].ToString()),
           Convert.ToInt32(ConfigurationManager.AppSettings["SendAlarmPort"].ToString()));

        //public UdpClient _UDPClient = new UdpClient();
        /// <summary>
        /// 用于UDP发送的网络服务类
        /// </summary>
        public UdpClient udpcSend = new UdpClient();
        /// <summary>
        /// 用于UDP接收的网络服务类
        /// </summary>
        public UdpClient udpcRecv = new UdpClient(new IPEndPoint(IPAddress.Any, Convert.ToInt32(ConfigurationManager.AppSettings["SendAlarmPort"].ToString())));

        /// <summary>
        /// 参数刷新
        /// </summary>
        //public UdpClient _UDPClient = new UdpClient(new IPEndPoint(IPAddress.Any, Convert.ToInt32(ConfigurationManager.AppSettings["UdpPort"].ToString())));
        #endregion


        public mainForm()
        {
            InitializeComponent();
            thisForm = this;
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            //获取本机IP
            localHost = Init.GetLocalIP();
            //启动计算主备机进程
            Thread mainOrStandByThread = new Thread(new ThreadStart(mainOrStandBySwitchMethod));
            mainOrStandByThread.IsBackground = true;
            mainOrStandByThread.Start();

            //启动接收网络UDP消息线程
            Thread UdpThread = new Thread(new ThreadStart(UDP.UdpReceive));
            UdpThread.IsBackground = true;
            UdpThread.Start();
            //探查网络是否正常
            if (Init.PingAll(tcpInitStandByServerHost))
            {
                //等待8S钟,接收当前系统的主备机状态
                Thread.Sleep(intervalTime * intervalDis * 1000);
                //当前系统是否存在主机的情况
                priority = Init.Priority(priorityInit);
                //网络正常
                //启动发送心跳 
                heartOfTimer.Interval = intervalTime * 1000;
                heartOfTimer.Enabled = true;
                heartOfTimer.Start();

            }
            else
            {
                //发出告警
            }
        }
                    

        #region 计算主备机
        /// <summary>
        /// 上次接收到备机消息
        /// </summary>
        public DateTime _timeStandByLast = DateTime.Now;
        /// <summary>
        /// 计算主备机切换
        /// </summary>
        private void mainOrStandBySwitchMethod()
        {
            while (true)
            {
                try
                {
                    //显示当前设备主备情况
                    label1.Text = priority.ToString();
                    if (Init.PingAll(tcpInitStandByServerHost))
                    {
                        //显示正常状态
                    }
                    else
                    {
                        //显示异常状态
                        //告警，标红灯
                        listBox1.Items.Add(DateTime.Now.ToString() + "--告警--备机无法连通，请检查网络和备机");
                    }
                    //计算上次接收到主备机到现在的时间长度
                    double lastTimeStandby = (DateTime.Now - _timeStandByLast).TotalSeconds;
                    if (lastTimeStandby > intervalTime * intervalDis)
                    {
                        lock (lockSwitch)
                        {
                            //加锁
                            //说明丢失了周期超过阈值
                            if (priority == StructNode.Priority.main)
                            {
                                //说明本机是主机，对面是备机
                                //告警，备机掉机
                                tcpNowStandByServerHost = "";
                                listBox1.Items.Add(DateTime.Now.ToString() + "--告警--网络异常--无法检测到备机心跳，备机掉机");
                            }
                            else
                            {
                                //告警，主机掉机，并发布切换消息
                                listBox1.Items.Add(DateTime.Now.ToString() + "--告警--主机掉机--备机切换到主机模式");
                                priority = StructNode.Priority.main;
                                switchPriorityTime = DateTime.Now;
                                //设置主备机状态

                                //将自己设置成主机
                                //1是设置主机消息
                                string sendInfo = "0x02#Info_Switch_MainOrStandBy#" + localHost + "#" + StructNode.Priority.main.ToString() +
                                    "#" + DateTime.Now.ToString();
                                //发送
                                byte[] bytes = Encoding.Default.GetBytes(sendInfo);
                                mainForm.thisForm.udpcSend.Send(bytes, bytes.Length, mainForm.thisForm.endpoint_Send);
                                listBox1.Items.Add(DateTime.Now.ToString() + "--发送--切换主备--" + sendInfo);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                Thread.Sleep(1000);
            }
        }
        #endregion
        /// <summary>
        /// 发送心跳
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            //当前主备机状态
            //主备机的指令
            string sendInfo = "0x02#Info_Heart_MainOrStandBy#" + localHost + "#" + priority +
                "#" + switchPriorityTime.ToString() + "#" + DateTime.Now.ToString();
            //发送
            byte[] bytes = Encoding.Default.GetBytes(sendInfo);
            mainForm.thisForm.udpcSend.Send(bytes, bytes.Length, mainForm.thisForm.endpoint_Send);
            listBox1.Items.Add(DateTime.Now.ToString() + "--发送--心跳正常--"  + sendInfo);
        }

        /// <summary>
        /// 窗口推出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }

        /// <summary>
        /// 主备切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (priority == StructNode.Priority.standby)
            {
                mainForm.thisForm.priority = StructNode.Priority.main;
                mainForm.thisForm.switchPriorityTime = DateTime.Now;
                //将自己设置成主机
                //1是设置主机消息
                string sendInfo = "0x02#Info_Switch_MainOrStandBy#" + mainForm.thisForm.localHost + "#" + StructNode.Priority.main.ToString() +
                    "#" + DateTime.Now.ToString();
                //发送
                byte[] bytes = Encoding.Default.GetBytes(sendInfo);
                mainForm.thisForm.udpcSend.Send(bytes, bytes.Length, mainForm.thisForm.endpoint_Send);
            }
            else
            {
                //将自己设置成主机
                //1是设置主机消息
                string sendInfo = "0x02#Info_Switch_MainOrStandBy#" + mainForm.thisForm.tcpInitStandByServerHost + "#" + StructNode.Priority.main.ToString() +
                    "#" + DateTime.Now.ToString();
                //发送
                byte[] bytes = Encoding.Default.GetBytes(sendInfo);
                mainForm.thisForm.udpcSend.Send(bytes, bytes.Length, mainForm.thisForm.endpoint_Send);
            }
        }

    }
}
