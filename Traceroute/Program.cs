/*
 * Traceroute implementation by Matthew Dennis (msd7734)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace Traceroute
{
    class Program
    {
        static readonly string START_MSG = "Tracing route to {0} [{1}] over a maximum of {2} hops:";
        static readonly string START_MSG_NO_NAME = "Tracing route to {0} over a maximum of {1} hops:";
        static readonly string COULD_NOT_RESOLVE = "Unable to resolve target system name {0}.";
        static readonly int MAX_HOPS = 30;
        static readonly int TIMEOUT = 5000;
        // using 0.0.0.0 to indicate case of an unknown remote
        static readonly IPAddress NULL_REMOTE = new IPAddress(new byte[] { 0, 0, 0, 0 }); 

        static void Main(string[] args)
        {

            // resolve local host
            IPHostEntry localhost = Dns.GetHostEntry(Dns.GetHostName());
            var localIPs = localhost.AddressList.Where(x => !x.IsIPv6LinkLocal);

            var localIP4 = localIPs.Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToList();
            bool hasLocalIP4 = (localIP4.Count > 0);
            var localIP6 = localIPs.Where(x => x.AddressFamily == AddressFamily.InterNetworkV6).ToList();
            bool hasLocalIP6 = (localIP6.Count > 0);


            IPAddress[] remoteIPs;
            string remoteHostName;
            IPAddress[] IP4;
            IPAddress[] IP6;
            try
            {
                // resolve given remote host
                IPHostEntry destHost = Dns.GetHostEntry(args[0]);
                remoteIPs = destHost.AddressList;
                remoteHostName = destHost.HostName;
                //SocketException
            }

            catch (SocketException se)
            {
                // likely could not resolve IP to hostname
                try
                {
                    remoteIPs = Dns.GetHostAddresses(args[0]);
                    remoteHostName = String.Empty;
                }
                // couldn't resolve anything to anything (bad name/address)
                catch (SocketException innerse)
                {
                    Console.WriteLine(COULD_NOT_RESOLVE, args[0]);
                    return;
                }
            }

            IP4 = remoteIPs.Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToArray();
            bool hasRemoteIP4 = (IP4.Length > 0);
            IP6 = remoteIPs.Where(x => x.AddressFamily == AddressFamily.InterNetworkV6).ToArray();
            bool hasRemoteIP6 = (IP6.Length > 0);

            // if we have a local IPv4 and a remote IPv4, traceroute on IPv4
            // if we have a local IPv6 and a remote IPv6, traceroute on IPv6
            // if both, traceroute on both

            if (hasLocalIP4 && hasRemoteIP4)
            {
                if (String.IsNullOrEmpty(remoteHostName))
                {
                    Console.WriteLine(START_MSG_NO_NAME, IP4[0], MAX_HOPS);
                }
                else
                {
                    Console.WriteLine(START_MSG, remoteHostName, IP4[0], MAX_HOPS);
                }

                Traceroute(IP4[0]);
                Console.WriteLine("\nTrace complete.");
            }

            if (hasLocalIP6 && hasRemoteIP6)
            {
                if (String.IsNullOrEmpty(remoteHostName))
                {
                    Console.WriteLine(START_MSG_NO_NAME, IP6[0], MAX_HOPS);
                    
                }
                else
                {
                    Console.WriteLine(START_MSG, remoteHostName, IP6[0], MAX_HOPS);
                }

                Traceroute(IP6[0]);
                Console.WriteLine("\nTrace complete.");
            }
        }

        static void Traceroute(IPAddress dest)
        {
            Ping p = new Ping();
            byte[] buf = new byte[32];
            IPAddress hop = NULL_REMOTE;

            Stopwatch watch = new Stopwatch();

            for (int i = 1; i <= MAX_HOPS; ++i)
            {
                Console.Write(" {0}\t", i);

                for (int pass = 1; pass <= 3; ++pass)
                {
                    watch.Start();
                    PingReply reply = p.Send(dest, TIMEOUT, buf, new PingOptions(i, true));
                    watch.Stop();
                    long tripTime = watch.ElapsedMilliseconds;

                    if (reply.Status == IPStatus.TimedOut)
                    {
                        Console.Write("  *\t");
                    }
                    else
                    {
                        if (tripTime == 0)
                            Console.Write("<1 ms\t");
                        else
                            Console.Write("{0} ms\t", tripTime);

                        hop = reply.Address;
                    }

                    watch.Reset();
                }

                // All attempts timed out, so the hop is unknown
                if (hop.Equals(NULL_REMOTE))
                {
                    Console.WriteLine("Request timed out.");
                }
                else
                {
                    string hopName;
                    try
                    {
                        hopName = Dns.GetHostEntry(hop).HostName;
                    }
                    catch (SocketException se)
                    {
                        // couldn't find hop's hostname
                        hopName = String.Empty;
                    }

                    if (String.IsNullOrEmpty(hopName))
                        Console.WriteLine(hop);
                    else
                        Console.WriteLine("{0} [{1}]", hopName, hop);


                    if (hop.Equals(dest))
                        return;
                    else
                        hop = NULL_REMOTE;
                }
            }
        }
    }
}
