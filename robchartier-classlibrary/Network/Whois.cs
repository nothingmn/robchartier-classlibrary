using System;

namespace RobChartier.Network {
	/// <summary>
	/// Summary description for Whois.
	/// </summary>
	public class Whois {
		public Whois() {
		}
		public static string PerformWhois(string WhoisServerHost, int WhoisServerPort, string Host) {
			string result="";
			try {
				String strDomain = Host;
				char[] chSplit = {'.'};
				string[] arrDomain = strDomain.Split(chSplit);
				// There may only be exactly one domain name and one suffix
				if (arrDomain.Length != 2) {
					return "";
				}
 
				// The suffix may only be 2 or 3 characters long
				int nLength = arrDomain[1].Length;
				if (nLength != 2 && nLength != 3) {
					return "";
				}
 
				System.Collections.Hashtable table = new System.Collections.Hashtable();
				table.Add("de", "whois.denic.de");
				table.Add("be", "whois.dns.be");
				table.Add("gov", "whois.nic.gov");
				table.Add("mil", "whois.nic.mil");
 
				String strServer = WhoisServerHost;
				if (table.ContainsKey(arrDomain[1])) {
					strServer = table[arrDomain[1]].ToString();
				}
				else if (nLength == 2) {
					// 2-letter TLD's always default to RIPE in Europe
					strServer = "whois.ripe.net";
				}

				System.Net.Sockets.TcpClient tcpc = new System.Net.Sockets.TcpClient ();
				tcpc.Connect(strServer, WhoisServerPort);
				String strDomain1 = Host+"\r\n";
				Byte[] arrDomain1 = System.Text.Encoding.ASCII.GetBytes(strDomain1.ToCharArray());
				System.IO.Stream s = tcpc.GetStream();
				s.Write(arrDomain1, 0, strDomain1.Length);
				System.IO.StreamReader sr = new System.IO.StreamReader(tcpc.GetStream(), System.Text.Encoding.ASCII);
				System.Text.StringBuilder strBuilder = new System.Text.StringBuilder();
				string strLine = null;
				while (null != (strLine = sr.ReadLine())) {
					strBuilder.Append(strLine+"\r\n");
				}
				result = strBuilder.ToString();
				tcpc.Close();
			}catch(Exception exc) {
				result="Could not connect to WHOIS server!\r\n"+exc.ToString();
			}
			return result;
		}
	}
}