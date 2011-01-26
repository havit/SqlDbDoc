using System;
using System.Data;
using System.Xml;

namespace Altairis.SqlDbDoc {
    internal static class ExtensionMethods {

        public static string ToXmlString(this DataRow row, DataColumn column) {
            if (row == null) throw new ArgumentNullException("row");
            if (column == null) throw new ArgumentNullException("column");

            var val = row[column];

            if (val == null || val == DBNull.Value) return null;
            if (column.DataType.Equals(typeof(string))) return (string)val;
            if (column.DataType.Equals(typeof(byte))) return XmlConvert.ToString((byte)val);
            if (column.DataType.Equals(typeof(short))) return XmlConvert.ToString((short)val);
            if (column.DataType.Equals(typeof(int))) return XmlConvert.ToString((int)val);
            if (column.DataType.Equals(typeof(long))) return XmlConvert.ToString((long)val);
            if (column.DataType.Equals(typeof(decimal))) return XmlConvert.ToString((decimal)val);
            if (column.DataType.Equals(typeof(bool))) return XmlConvert.ToString((bool)val);
            if (column.DataType.Equals(typeof(DateTime))) return XmlConvert.ToString((DateTime)val, XmlDateTimeSerializationMode.RoundtripKind);
            return val.ToString();
        }

    }
}
