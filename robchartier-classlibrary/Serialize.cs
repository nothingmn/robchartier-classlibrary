
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;


namespace RobChartier
{
    public static class Serialize
    {

        public enum SerializationMethods { Binary, XML };
        public static bool SerializeToDisk(object request, SerializationMethods Method, string Filename)
        {
            if (Method == SerializationMethods.XML) return SerializeXMLToDisk(request, Filename);
            return SerializeBinaryToDisk(request, Filename);
        }
        public static bool SerializeBinaryToDisk(object request, string Filename)
        {
            System.IO.File.WriteAllBytes(Filename, SerializeBinaryAsBytes(request));
            return System.IO.File.Exists(Filename);
        }

        public static MemoryStream SerializeBinary(object request)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream1 = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream1, request);
                return memoryStream1;
            }
        }
        public static byte[] SerializeBinaryAsBytes(object request)
        {
            using (MemoryStream stm = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(stm, request);
                return ConvertStreamToBytes(stm);
            }
        }
        public static object DeSerializeBinary(MemoryStream memStream)
        {
            if (memStream.CanSeek && memStream.Position > 0) memStream.Seek(0, SeekOrigin.Begin);
            return (new BinaryFormatter()).Deserialize(memStream);
        }

        public static object DeSerializeXML(MemoryStream memStream, Type type)
        {

            if (memStream.CanSeek && memStream.Position > 0) memStream.Seek(0, SeekOrigin.Begin);
            return (new XmlSerializer(type)).Deserialize(memStream);
        }

        public static byte[] SerializeXMLAsBytes(object request)
        {
            using (MemoryStream stm = SerializeXML(request))
            {
                return ConvertStreamToBytes(stm);
            }
        }
        public static bool SerializeXMLToDisk(object request, string Filename)
        {
            if (System.IO.File.Exists(Filename)) System.IO.File.Delete(Filename);
            System.IO.File.WriteAllText(Filename, SerializeXMLAsString(request));
            return System.IO.File.Exists(Filename);
        }
        public static string SerializeXMLAsString(object request)
        {
            using (MemoryStream stm = SerializeXML(request))
            {
                return ConvertStreamToUTF8String(stm);
            }
        }

        public static MemoryStream SerializeXML(object request)
        {
            return SerializeXML(request, request.GetType());
        }

        public static MemoryStream SerializeXML(object request, Type type)
        {
            MemoryStream memoryStream1 = new MemoryStream();
            (new XmlSerializer(type)).Serialize(memoryStream1, request);
            return memoryStream1;

        }

        public static T DeserializeXMLFromDisk<T>(string Filename)
        {
            string contents = System.IO.File.ReadAllText(Filename);
            return DeSerializeXML<T>(contents);
        }

        public static T DeSerializeXML<T>(string envelope)
        {
                using (MemoryStream memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(envelope)))
                {
                    return (T)(new XmlSerializer(typeof(T))).Deserialize(memoryStream);                    
                }
        }

      
        public static T DeSerializeBinary<T>(byte[] envelope)
        {

            using (MemoryStream memoryStream = new MemoryStream(envelope))
            {

                return (T)DeSerializeBinary(memoryStream);
            }
        }


        public static string ConvertStreamToUTF8String(System.IO.Stream Stream)
        {
            if (Stream.CanSeek && Stream.Position > 0) Stream.Seek(0, SeekOrigin.Begin);
            byte[] data = new byte[Stream.Length];
            Stream.Read(data, 0, data.Length);
            return System.Text.Encoding.UTF8.GetString(data);

        }

        public static string ConvertStreamToASCIIString(System.IO.MemoryStream Stream)
        {
            if (Stream.CanSeek && Stream.Position > 0) Stream.Seek(0, SeekOrigin.Begin);
            byte[] data = new byte[Stream.Length];
            Stream.Read(data, 0, data.Length);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        public static byte[] ConvertStreamToBytes(System.IO.MemoryStream Stream)
        {
            if (Stream == null) return null;
            if (Stream.CanSeek && Stream.Position > 0) Stream.Seek(0, SeekOrigin.Begin);
            byte[] d = new byte[(int)Stream.Length];
            Stream.Read(d, 0, d.Length);
            return d;
        }
        public static System.IO.MemoryStream ConvertASCIIStringToStream(string Data)
        {
            return ConvertBytesToStream(System.Text.ASCIIEncoding.ASCII.GetBytes(Data));
        }
        public static System.IO.MemoryStream ConvertBytesToStream(byte[] Data)
        {
            return new System.IO.MemoryStream(Data);
        }


    }

}
