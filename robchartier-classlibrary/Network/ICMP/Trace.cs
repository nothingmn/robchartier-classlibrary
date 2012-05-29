/******************************************************************************************************************
*	Class:				Trace
*	Description:		Traces path of an ip packet with its respond time
*	Author:				Sanjay Ahuja, Rob Chartier
*	Date:				5/15/2002
*	Copyright©:			© 2002, Sanjay Ahuja (lparam@hotmail.com). Use it as you want till you leave my name intact
*                       Rob Chartier:  Made  into a more useful and resusable class library instead of a console app.
                                       Also added Threaded Lookups.
/******************************************************************************************************************/

namespace RobChartier.Network {

    using System;
    using System.Net;
    using System.Net.Sockets;


    //ICMP constants
    public struct ICMPConstants {
        public const int ICMP_ECHOREPLY= 0;			// Echo reply query
        public const int ICMP_TIMEEXCEEDED= 11;		// TTL exceeded error
        public const int ICMP_ECHOREQ=	8;			// Echo request query
        public const int MAX_TTL= 256;				// Max TTL
    }

    //ICMP header, size is 8 bytes
    public struct ICMP {
        public byte	type;				// Type
        public byte	code;				// Code
        public ushort	checksum;		// Checksum
        public ushort	id;				// Identification
        public ushort	seq;			// Sequence
    }

    // ICMP Echo Request, size is 12+ 32 (PACKET_SIZE as defined in class Trace)= 44 bytes
    public struct REQUEST {
        public ICMP	m_icmp;
        public byte	[]m_data;
    }


    public class TraceResults {
        public System.Collections.ArrayList Results = new System.Collections.ArrayList();
        public string Error;
        public bool HasError=false;
        public string Host;
        public double Average;
        public long Total;
        public void AddResult(TraceResult r) {
            Results.Add(r);
            Total+=r.Time;
            Average=(double)Total/(double)Results.Count;
        }

        public delegate void HostNameUpdatedHandler(object sender, HostNameUpdatedEventArgs e);
        public event HostNameUpdatedHandler OnHostNameUpdated;


        public void UpdateHostNames() {
            foreach(TraceResult r in Results) {
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(UpdateHostName),r);
            }

        }
        public void UpdateHostName(object TR) {
            HostNameUpdatedEventArgs args = new HostNameUpdatedEventArgs();
            TraceResult r = (TraceResult)TR;
            args.Result=r;
            try {                
                System.Net.IPHostEntry entry = System.Net.Dns.GetHostByAddress(r.IP);
                r.HostName=entry.HostName;
                args.Success=true;
            }catch(Exception e) {
                r.HostName="";
                args.Success=false;
                args.FailureReason=e.ToString();
            }
            OnHostNameUpdated(this, args);
        }
        public override string ToString() {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach(TraceResult r in Results) {
                sb.Append(r.ToString());
            }
            sb.Append(System.String.Format("Total:{0}ms, Average:{1}ms\r\n",Total, Average));
            return sb.ToString();
        }

    }
    public class HostNameUpdatedEventArgs : EventArgs{
        public TraceResult Result;
        public bool Success=false;
        public string FailureReason;
    }
    public class TraceResult {
        public int TTL;
        public string IP;
        public string HostName;
        public int Time;
        public override string ToString() {
            string r = String.Format("{0},{1},{2},{3}\r\n",TTL, IP, HostName, Time);
            if(HostName==null || HostName.Trim()=="") r = r.Replace(",","");
            return r;
        }
        public string ToFormattedString() {
            string r = String.Format("TTL:{0,-5} IP:{1,-15},{2,10} Time:{3}ms\r\n",TTL, IP, HostName, Time);
            if(HostName==null || HostName.Trim()=="") r = r.Replace(",","");
            return r;
        }
    }

    public class TraceRoute {
        const int PACKET_SIZE= 32;

        public static TraceResults TraceHost(string host) {
            TraceResults results = new TraceResults();
            results.Host=host;
            try {
                //Create Raw ICMP Socket 
                Socket s= new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
                //destination
                IPEndPoint ipdest= new IPEndPoint(Dns.Resolve(host).AddressList[0],80);
                //Source
                IPEndPoint ipsrc= new IPEndPoint(Dns.GetHostByName(Dns.GetHostName()).AddressList[0],80);
                EndPoint epsrc= (EndPoint)ipsrc;
				
                ICMP ip= new ICMP();
                ip.type = ICMPConstants.ICMP_ECHOREQ; 
                ip.code = 0;
                ip.checksum = 0;
                ip.id = (ushort)DateTime.Now.Millisecond;	//any number you feel is kinda unique :)
                ip.seq  = 0;
			
                REQUEST req= new REQUEST();
                req.m_icmp= ip;
                req.m_data = new Byte[PACKET_SIZE];
				
                //Initialize data
                for (int i = 0; i < req.m_data.Length; i++) {
                    req.m_data[i] = (byte)'S';
                }

                //this function would gets byte array from the REQUEST structure
                Byte[] ByteSend= CreatePacket(req);

                //send requests with increasing number of TTL
                for(int ittl=1; ittl<= ICMPConstants.MAX_TTL; ittl++) {
                    TraceResult tr = new TraceResult();
                    tr.TTL=ittl;

                    Byte[] ByteRecv = new Byte[256];
                    //Socket options to set TTL and Timeouts 
                    s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, ittl);
                    s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout,10000); 
                    s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout,10000); 

                    //Get current time
                    DateTime dt= DateTime.Now;
                    //Send Request
                    int iRet= s.SendTo(ByteSend, ByteSend.Length, SocketFlags.None, ipdest);
                    //check for Win32 SOCKET_ERROR
                    if(iRet== -1) {
                        tr.HostName="error sending data";
                    }

                    //Receive
                    iRet= s.ReceiveFrom(ByteRecv, ByteRecv.Length, SocketFlags.None, ref epsrc);

                    //Calculate time required
                    TimeSpan ts= DateTime.Now- dt;;

                    //check if response is OK
                    if(iRet== -1) {
                        tr.HostName="error getting data";
                    }
					
                    IPAddress addy = ((IPEndPoint)epsrc).Address;
                    tr.IP=addy.ToString();
                    tr.Time=ts.Milliseconds;
                    results.AddResult(tr);

                    //reply size should be sizeof REQUEST + 20 (i.e sizeof IP header),it should be an echo reply
                    //and id should be same
                    if((iRet == PACKET_SIZE+ 8 +20)&& (BitConverter.ToInt16(ByteRecv,24) == BitConverter.ToInt16(ByteSend,4))&& (ByteRecv[20] == ICMPConstants.ICMP_ECHOREPLY)) 
                        break;
                    //time out
                    if(ByteRecv[20] != ICMPConstants.ICMP_TIMEEXCEEDED) {
                        tr.HostName="unexpected reply, quitting...";
                        break;
                    }

                }
            }
            catch(SocketException e) {
                results.Error=e.ToString();
                results.HasError=true;
            }
            catch(Exception e) {
                results.Error=e.ToString();
                results.HasError=true;
            }
            return results;

        }
        public static byte[] CreatePacket( REQUEST req ) {
            Byte[] ByteSend= new Byte[PACKET_SIZE+ 8];
            //Create Byte array from REQUEST structure
            ByteSend[0]= req.m_icmp.type;
            ByteSend[1]= req.m_icmp.code;
            Array.Copy(BitConverter.GetBytes(req.m_icmp.checksum), 0, ByteSend, 2, 2);
            Array.Copy(BitConverter.GetBytes(req.m_icmp.id), 0, ByteSend, 4, 2);
            Array.Copy(BitConverter.GetBytes(req.m_icmp.seq), 0, ByteSend, 6, 2);
            for(int i=0; i< req.m_data.Length; i++)
                ByteSend[i+8]= req.m_data[i];

            //calculate checksum
            int iCheckSum = 0;
            for (int i= 0; i < ByteSend.Length; i+= 2) 
                iCheckSum += Convert.ToInt32( BitConverter.ToUInt16(ByteSend,i));

            iCheckSum = (iCheckSum >> 16) + (iCheckSum & 0xffff);
            iCheckSum += (iCheckSum >> 16);

            //update byte array to reflect checksum 
            Array.Copy(BitConverter.GetBytes((ushort)~iCheckSum), 0, ByteSend, 2, 2);
            return ByteSend;
        }

    }
}