using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Net.Sockets;

namespace Gravity.Server.Utility
{
    /// <summary>
    /// Allows testing of an IP address to see if it is within a range.
    /// Pre-computes as much as possible to make the comparison efficient
    /// </summary>
    internal class IPAddressRange
    {
        private enum RangeType { Loopback, LinkLocal, SiteLocal, Mask }

        private static uint[] _ipv4NonRoutableAddresses;
        private static uint[] _ipv4NonRoutableMasks;

        private IPAddress _ipAddress;
        private RangeType _rangeType;

        private uint _ipv4Address;
        private uint _ipv4Mask;

        private ulong _ipv6NetworkAddress;
        private ulong _ipv6NodeAddress;
        private ulong _ipv6Mask;

        static IPAddressRange()
        {
            _ipv4NonRoutableAddresses = new[] 
            { 
                IpV4AddressValue(IPAddress.Parse("192.168.0.0")),
                IpV4AddressValue(IPAddress.Parse("172.16.0.0")),
                IpV4AddressValue(IPAddress.Parse("10.0.0.0"))
            };

            _ipv4NonRoutableMasks = new[] 
            {
                IpV4CidrMask(16),
                IpV4CidrMask(12),
                IpV4CidrMask(8)
            };
        }

        /// <summary>
        /// Calculates the numeric value of an IP v4 address
        /// </summary>
        public static uint IpV4AddressValue(IPAddress ipAddress)
        {
            return (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ipAddress.GetAddressBytes(), 0));
        }

        /// <summary>
        /// Calculates the mask to apply to an IP v4 CIDR block
        /// </summary>
        public static uint IpV4CidrMask(byte cidrBlock)
        {
            return unchecked(uint.MaxValue << 32 - cidrBlock);
        }

        /// <summary>
        /// Tests if an IPv4 address is routable beyond the local network
        /// </summary>
        public static bool IsIpV4NonRoutable(IPAddress ipAddress)
        {
            return IsIpV4NonRoutable(IpV4AddressValue(ipAddress));
        }

        /// <summary>
        /// Tests if an IPv4 address is routable beyond the local network
        /// </summary>
        public static bool IsIpV4NonRoutable(uint ipAddress)
        {
            for (var i = 0; i < _ipv4NonRoutableAddresses.Length; i++)
            {
                if ((ipAddress & _ipv4NonRoutableMasks[i]) == _ipv4NonRoutableAddresses[i]) return true;
            }
            return false;
        }

        /// <summary>
        /// Calculates the numeric value of the network address part of an IP v6 address
        /// </summary>
        public static ulong IpV6NetworkValue(IPAddress ipAddress)
        {
            return (ulong)IPAddress.NetworkToHostOrder(BitConverter.ToInt64(ipAddress.GetAddressBytes(), 0));
        }

        /// <summary>
        /// Calculates the numeric value of the node address part of an IP v6 address
        /// </summary>
        public static ulong IpV6NodeValue(IPAddress ipAddress)
        {
            return (ulong)IPAddress.NetworkToHostOrder(BitConverter.ToInt64(ipAddress.GetAddressBytes(), 8));
        }

        /// <summary>
        /// Calculates the mask to apply to an IP v6 CIDR block
        /// </summary>
        public static ulong IpV6CidrMask(byte cidrBlock)
        {
            return unchecked(ulong.MaxValue << 64 - cidrBlock);
        }

        public static bool IsNonRoutable(IPAddress ipAddress)
        {
            switch (ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return IsIpV4NonRoutable(ipAddress);
                case AddressFamily.InterNetworkV6:
                    return ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal;
            }
            return false;
        }


        /// <summary>
        /// Parses an IP address range in CIDR block format. The CIDR block is
        /// optional and defaults to /32 for IP v4 and /64 for IP v6
        /// </summary>
        public static  IPAddressRange Parse(string rangeText)
        {
            if (string.Equals("loopback", rangeText, StringComparison.OrdinalIgnoreCase))
            {
                return new IPAddressRange { _rangeType = RangeType.Loopback };
            }
            else if (string.Equals("link", rangeText, StringComparison.OrdinalIgnoreCase))
            {
                return new IPAddressRange { _rangeType = RangeType.LinkLocal };
            }
            else if (string.Equals("site", rangeText, StringComparison.OrdinalIgnoreCase))
            {
                return new IPAddressRange { _rangeType = RangeType.SiteLocal };
            }

            var cidrSeparator = rangeText.IndexOf("/");
            byte ipv4Block;
            byte ipv6Block;
            IPAddress ipAddress;

            if (cidrSeparator < 0)
            {
                if (!IPAddress.TryParse(rangeText, out ipAddress))
                    throw new Exception($"Invalid IP address in '{rangeText}'");
                ipv4Block = 32;
                ipv6Block = 64;
            }
            else
            {
                if (!IPAddress.TryParse(rangeText.Substring(0, cidrSeparator), out ipAddress))
                    throw new Exception($"Invalid IP address in '{rangeText}'");

                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (!byte.TryParse(rangeText.Substring(cidrSeparator + 1), out ipv4Block))
                        throw new Exception($"CIDR block specifier is not a number in '{rangeText}'");
                    ipv6Block = (byte)(ipv4Block << 1); // TODO: This is not strictly correct
                }
                else
                {
                    if (!byte.TryParse(rangeText.Substring(cidrSeparator + 1), out ipv6Block))
                        throw new Exception($"CIDR block specifier is not a number in '{rangeText}'");
                    ipv4Block = (byte)(ipv6Block >> 1); // TODO: This is not strictly correct
                }

                if (ipv4Block > 32 || ipv4Block < 0)
                    throw new Exception($"IPv4 CIDR block specifier is out of range in '{rangeText}'");

                if (ipv6Block > 64 || ipv6Block < 0)
                    throw new Exception($"IPv6 CIDR block specifier is out of range in '{rangeText}'");
            }

            return new IPAddressRange(ipAddress, ipv4Block, ipv6Block);
        }

        private IPAddressRange()
        {
        }

        /// <summary>
        /// Constrcuts a new IP address range
        /// </summary>
        /// <param name="ipAddress">The base address of the address range</param>
        /// <param name="ipv4Block">The number of significant address bits in IP v4</param>
        /// <param name="ipv6Block">The number of significant address bits in IP v6</param>
        public IPAddressRange(IPAddress ipAddress, byte ipv4Block, byte ipv6Block)
        {
            _ipAddress = ipAddress;
            _rangeType = RangeType.Mask;

            IPAddress ipV4Address = ipAddress.AddressFamily == AddressFamily.InterNetwork ? ipAddress : ipAddress.MapToIPv6();
            IPAddress ipV6Address = ipAddress.AddressFamily == AddressFamily.InterNetwork ? ipAddress.MapToIPv6() : ipAddress;

            _ipv4Address = IpV4AddressValue(ipV4Address);
            _ipv4Mask = IpV4CidrMask(ipv4Block);

            _ipv6NetworkAddress = IpV6NetworkValue(ipV6Address);
            _ipv6NodeAddress = IpV6NodeValue(ipV6Address);
            _ipv6Mask = IpV6CidrMask(ipv6Block);
        }

        /// <summary>
        /// Returns true if the specified IP address is within the range
        /// </summary>
        public bool Contains(IPAddress ipAddress)
        { 
            switch (_rangeType)
            {
                case RangeType.Loopback:
                    switch (ipAddress.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            return ipAddress.Equals(IPAddress.Loopback);
                        case AddressFamily.InterNetworkV6:
                            return ipAddress.Equals(IPAddress.IPv6Loopback);
                    }
                    break;
                case RangeType.LinkLocal:
                    switch (ipAddress.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            return false;
                        case AddressFamily.InterNetworkV6:
                            return ipAddress.IsIPv6LinkLocal;
                    }
                    break;
                case RangeType.SiteLocal:
                    switch (ipAddress.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            return IsIpV4NonRoutable(ipAddress);
                        case AddressFamily.InterNetworkV6:
                            return ipAddress.IsIPv6LinkLocal;
                    }
                    break;
                case RangeType.Mask:
                    switch (ipAddress.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            return (IpV4AddressValue(ipAddress) & _ipv4Mask) == _ipv4Address;
                        case AddressFamily.InterNetworkV6:
                            return (IpV6NetworkValue(ipAddress) & _ipv6Mask) == _ipv6NetworkAddress;
                    }
                    break;
            }
            return false;
        }
    }
}