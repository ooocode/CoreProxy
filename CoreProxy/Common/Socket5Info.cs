using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CoreProxy.Common
{
    public class Socket5Result
    {
        // ver 版本号 固定 0x05
        public byte Ver { get; set; }

        //SOCK的命令码，占用1个字节，表示建立连接的类型，其中，0x01表示CONNECT请求，0x02表示BIND请求，0x03表示UDP转发
        public byte Cmd { get; set; }

        //rsv 默认0x00  保留位 不使用
        public byte Rsv { get; set; }


        //DST.ADDR类型，
        //0x01表示IPv4地址，此时DST.ADDR部分4字节长度。
        //0x03则表示域名，此时DST.ADDR部分第一个字节为域名长度，DST.ADDR剩余的内容为域名，没有\0结尾。
        //0x04表示IPv6地址，此时DST.ADDR部分16个字节长度。
        public byte Atype { get; set; }

        /// <summary>
        /// 目标地址
        /// </summary>
        public byte[] Address { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        public override string ToString()
        {
            string strAddr = System.Text.Encoding.UTF8.GetString(Address);
            return string.Format("ver={0} cmd={1} rsv={2} atype={3} address={4} port={5}", Ver, Cmd, Rsv, Atype, strAddr, Port);
        }
    }


    /// <summary>
    /// socket5信息
    /// </summary>
    public static class Socket5Utility
    {
        static bool CanParse(byte[] data)
        {
            if (data.Length > 3 && data[0] == 0x05 && data[1] == 0x01 && data[2] == 0x00)
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// 解析socket5信息
        /// </summary>
        /// <param name="vs"></param>
        /// <returns></returns>
        static public bool TryParse(byte[] vs, out Socket5Result result)
        {
            if (CanParse(vs))
            {
                result = new Socket5Result();
                result.Ver = vs.Skip(0).Take(1).ToArray()[0];
                result.Cmd = vs.Skip(1).Take(1).ToArray()[0];
                result.Rsv = vs.Skip(2).Take(1).ToArray()[0];
                result.Atype = vs.Skip(3).Take(1).ToArray()[0];

                if (result.Atype == 0x01)    //ip v4
                {
                    result.Address = vs.Skip(4).Take(4).ToArray();
                    result.Port = BitConverter.ToInt16(vs, 8);
                }
                else if (result.Atype == 0x03)   //域名
                {
                    int domainNameLenth = vs.Skip(4).Take(1).ToArray()[0];
                    result.Address = vs.Skip(5).Take(domainNameLenth).ToArray();

                    byte[] port = vs.Skip(5 + domainNameLenth).Take(2).ToArray();

                    result.Port = Convert.ToInt16((port[0].ToString("X2") + port[1].ToString("X2")), 16);
                }
                else if (result.Atype == 0x04)  //ip v6
                {
                    result.Address = vs.Skip(4).Take(16).ToArray();
                    result.Port = BitConverter.ToInt16(vs, 20);
                }
                return true;
            }

            result = null;
            return false;
        }

        //public override string ToString()
        //{
        //    string strAddr = System.Text.Encoding.UTF8.GetString(Address);
        //    return string.Format("ver={0} cmd={1} rsv={2} atype={3} address={4} port={5}", Ver, Cmd, Rsv, Atype, strAddr, Port);
        //}

        //public async Task<Socket> ConnectThisSocketAsync()
        //{
        //    //foreach (var i in Dns.GetHostEntry(Encoding.UTF8.GetString(Address)).AddressList)
        //    {
        //        Socket remote = new Socket(SocketType.Stream, ProtocolType.Tcp);
        //        //try
        //        {
        //            await remote.ConnectAsync(Encoding.UTF8.GetString(Address), Port);
        //            //remote.Connect(i, Port);
        //            remote.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
        //            return remote;
        //        }
        //        //catch (Exception ex)
        //        {
        //           // remote.Close();
        //            //Console.WriteLine("连接失败：" + ex.Message + " 地址：" + Encoding.UTF8.GetString(Address));
        //        }
        //    }

        //    //throw new Exception("没有可用socket");
        //}
    }
}
