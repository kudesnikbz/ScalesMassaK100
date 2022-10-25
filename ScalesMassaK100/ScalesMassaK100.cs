using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Net;
using System.Net.Sockets;

    public class ScalesMassaK100
    {
        [SqlProcedureAttribute]
        public static void GetWeightMassaK100(SqlString ip, SqlInt32 port, out SqlBoolean stableFlag, out SqlDecimal weight, out SqlString error)
        {
            var dt1 = DateTime.Now;
            weight = SqlDecimal.Null;
            stableFlag = false;
            error = SqlString.Null;

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var address = IPAddress.Parse(ip.ToString());
            var remoteEP = new IPEndPoint(address, (int)port);
            try
            {
                socket.Connect(remoteEP);
            }
            catch (Exception ex)
            {
                error = $"Ошибка : {ex}";
                return;
            }
            try
            {
                var msg = new byte[] { 0x23 };
                //var crc = CRC.Calc(msg);

                var packet = new byte[] { 0xF8, 0x55, 0xCE, 0x01, 0x00, 0x23, 0x23, 0x00 };
                socket.Send(packet, SocketFlags.None);
                var answer = new byte[0x100];
                socket.ReceiveTimeout = 5000;

                var count = socket.Receive(answer, 0, 3, SocketFlags.None);
                if (answer[0] != 0xF8 || answer[1] != 0x55 || answer[2] != 0xCE)
                    return;

                count = socket.Receive(answer, 0, sizeof(UInt16), SocketFlags.None);
                var length = BitConverter.ToUInt16(answer, 0);
                count = socket.Receive(answer, 0, length, SocketFlags.None);
                if (answer[0] == 0x28)
                {
                    error = $"Ошибка : {answer[1]}";
                    return;
                }
                var p = 1;
                var w = BitConverter.ToUInt32(answer, p);
                var d = answer[p + 4];

                var rate = 1D;
                if (d == 0) //100 мг
                {
                    rate = 0.000001D;
                }
                else if (d == 1) //1 г
                {
                    rate = 0.001D;
                }
                else if (d == 2) //10 г
                {
                    rate = 0.01D;
                }
                else if (d == 3) //100 г
                {
                    rate = 0.1D;
                }
                else if (d == 4) //1 кг    
                {
                    rate = 1D;
                }
                weight = (SqlDecimal)(Convert.ToDouble(w * rate));
                stableFlag = (SqlBoolean)(answer[p + 5] == 1);
                //Console.WriteLine($"Вec = {weight}, Стабильно = {stable},  время {(DateTime.Now - dt1).TotalMilliseconds} мс");
                //Console.ReadKey();
            }
            catch (Exception ex)
            {
                error = $"Ошибка : {ex.Message}";
            }
            finally
            {
                socket.Disconnect(false);
            }
        }
    }