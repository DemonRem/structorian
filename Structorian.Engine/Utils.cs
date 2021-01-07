using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Structorian
{
    public class Utils
    {
        public static byte[] ToBytes(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                    return BitConverter.GetBytes((byte)o);
                case TypeCode.SByte:
                    return BitConverter.GetBytes((sbyte)o);
                case TypeCode.UInt16:
                    return BitConverter.GetBytes((UInt16)o);
                case TypeCode.UInt32:
                    return BitConverter.GetBytes((UInt32)o);
                case TypeCode.UInt64:
                    return BitConverter.GetBytes((UInt64)o);
                case TypeCode.Int16:
                    return BitConverter.GetBytes((Int16)o);
                case TypeCode.Int32:
                    return BitConverter.GetBytes((Int32)o);
                case TypeCode.Int64:
                    return BitConverter.GetBytes((Int64)o);
                case TypeCode.Decimal:
                    return Utils.GetBytes(Decimal.Parse(o.ToString()));
                case TypeCode.Double:
                    return BitConverter.GetBytes((Double)o);
                case TypeCode.Single:
                    return BitConverter.GetBytes((Single)o);
                case TypeCode.String:
                    return Encoding.Unicode.GetBytes(o.ToString());
                case TypeCode.Char:
                    return Encoding.ASCII.GetBytes(o.ToString());
                default:
                    throw new Exception("Object type not supported");
            }
        }
        public static bool IsNumeric(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static byte[] GetBytes(decimal dec)
        {
            //Load four 32 bit integers from the Decimal.GetBits function
            Int32[] bits = decimal.GetBits(dec);
            //Create a temporary list to hold the bytes
            List<byte> bytes = new List<byte>();
            //iterate each 32 bit integer
            foreach (Int32 i in bits)
            {
                //add the bytes of the current 32bit integer
                //to the bytes list
                bytes.AddRange(BitConverter.GetBytes(i));
            }
            //return the bytes list as an array
            return bytes.ToArray();
        }
        public static decimal ToDecimal(byte[] bytes)
        {
            //check that it is even possible to convert the array
            if (bytes.Length != 16)
                throw new Exception("A decimal must be created from exactly 16 bytes");
            //make an array to convert back to int32's
            Int32[] bits = new Int32[4];
            for (int i = 0; i <= 15; i += 4)
            {
                //convert every 4 bytes into an int32
                bits[i / 4] = BitConverter.ToInt32(bytes, i);
            }
            //Use the decimal's new constructor to
            //create an instance of decimal
            return new decimal(bits);
        }
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
