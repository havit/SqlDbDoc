using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using NConsoler;

namespace Altairis.SqlDbDoc {
    class Program {
        private static readonly string[] FORMATS = { "html", "wikiplex", "xml" };
        private static readonly string[] HTML_EXTENSIONS = { ".htm", ".html", ".xhtml" };
        private static readonly string[] WIKI_EXTENSIONS = { ".txt", ".wiki" };

        private static string connectionString;

        static void Main(string[] args) {
            Console.WriteLine("Altairis DB>doc version {0:4}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("Copyright (c) Altairis, 2011 | www.altairis.cz | SqlDbDoc.codeplex.com");
            Console.WriteLine();

            // Add console trace listener
            Trace.Listeners.Add(new ConsoleTraceListener());

            // Run actions
            Consolery.Run();
        }

        // Actions
        [Action("Generate documentation from given database")]
        public static void CreateDocumentation(
            [Required(Description = "connection string")] string connection,
            [Required(Description = "output file name")] string fileName,
            [Optional(false, "y", Description = "overwrite output file")] bool overwrite,
            [Optional(null, "f", Description = "output format: html, wikiplex, xml (autodetected when omitted)")] string format,
            [Optional(false, Description = "debug mode (show detailed error messages)")] bool debug
            ) {

            // Validate arguments
            if (connection == null) throw new ArgumentNullException("connection");
            if (string.IsNullOrWhiteSpace(connection)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "connection");
            if (fileName == null) throw new ArgumentNullException("fileName");
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "fileName");

            // Validate output file
            if (File.Exists(fileName) && !overwrite) {
                Console.WriteLine("ERROR: Target file already exists. Use /y to overwrite.");
                return;
            }

            // Get output format
            if (string.IsNullOrWhiteSpace(format)) {
                Console.WriteLine("Autodetecting output format...");
                if (Array.IndexOf(HTML_EXTENSIONS, Path.GetExtension(fileName)) > -1) {
                    format = "html";
                }
                else if (Array.IndexOf(WIKI_EXTENSIONS, Path.GetExtension(fileName)) > -1) {
                    format = "wikiplex";
                }
                else {
                    format = "xml";
                }
            }
            else {
                format = format.ToLower().Trim();
                if (Array.IndexOf(FORMATS, format) == -1) throw new ArgumentOutOfRangeException("format", "Unknown format string.");
            }
            Console.WriteLine("Output format: {0}", format);

            try {

                // Prepare XML document
                var doc = new XmlDocument();

                // Process database info
                connectionString = connection;
                doc.AppendChild(doc.CreateElement("database"));
                doc.DocumentElement.SetAttribute("dateGenerated", XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.RoundtripKind));
                RenderDatabase(doc.DocumentElement);

                // Process schemas
                RenderSchemas(doc.DocumentElement);

                // Process top-level objects
                RenderChildObjects(0, doc.DocumentElement);

                if (format.Equals("xml")) {
                    // Save raw XML
                    Console.Write("Saving raw XML...");
                    doc.Save(fileName);
                    Console.WriteLine("OK");
                    return;
                }

                // Read XSL template code
                string xslt;
                if (format.Equals("html")) {
                    xslt = Resources.Templates.Html;
                }
                else {
                    xslt = Resources.Templates.WikiPlex;
                }

                // Prepare XSL transformation
                Console.Write("Preparing XSL transformation...");
                using (var sr = new StringReader(xslt))
                using (var xr = XmlReader.Create(sr)) {
                    var tran = new XslCompiledTransform();
                    tran.Load(xr);
                    Console.WriteLine("OK");

                    Console.Write("Performing XSL transformation...");
                    using (var fw = File.CreateText(fileName)) {
                        tran.Transform(doc, null, fw);
                    }
                    Console.WriteLine("OK");
                }
            }
            catch (Exception ex) {
                Console.WriteLine("Failed!");
                Console.WriteLine(ex.Message);
                if (debug) Console.WriteLine(ex.ToString());
            }
        }

        // Helper methods

        static void RenderSchemas(XmlElement parentElement) {
            // Get list of schemas
            var dt = new DataTable();
            using (var da = new SqlDataAdapter(Resources.Commands.GetSchemas, connectionString)) {
                da.Fill(dt);
            }

            // Populate schemas
            for (int i = 0; i < dt.Rows.Count; i++) {
                var e = parentElement.AppendChild(parentElement.OwnerDocument.CreateElement("schema")) as XmlElement;
                e.SetAttribute("name", (string)dt.Rows[i][0]);
            }
        }

        static void RenderDatabase(XmlElement parentElement) {
            // Get current database info
            var dt = new DataTable();
            using (var da = new SqlDataAdapter(Resources.Commands.GetDatabase, connectionString)) {
                da.Fill(dt);
            }

            // Display database info
            foreach (DataColumn col in dt.Columns) {
                var value = dt.Rows[0].ToXmlString(col);
                if (!string.IsNullOrWhiteSpace(value)) parentElement.SetAttribute(col.ColumnName, value);
            }
        }

        static void RenderChildObjects(int parentObjectId, XmlElement parentElement) {
            // Get all database objects with given parent
            var dt = new DataTable();
            using (var da = new SqlDataAdapter(Resources.Commands.GetObjects, connectionString)) {
                da.SelectCommand.Parameters.Add("@parent_object_id", SqlDbType.Int).Value = parentObjectId;
                da.Fill(dt);
            }

            // Process all objects
            foreach (DataRow row in dt.Rows) {
                var objectId = (int)row["id"];
                Trace.WriteLine(string.Format("{0}.{1}", row["schema"], row["name"]));

                // Create object element
                var e = parentElement.AppendChild(parentElement.OwnerDocument.CreateElement("object")) as XmlElement;
                foreach (DataColumn col in dt.Columns) {
                    var value = row.ToXmlString(col);
                    if (!string.IsNullOrWhiteSpace(value)) e.SetAttribute(col.ColumnName, value);
                }

                Trace.Indent();
                // Process columns
                RenderColumns(objectId, e);

                // Process child objects
                RenderChildObjects(objectId, e);
                Trace.Unindent();
            }
        }

        static void RenderColumns(int objectId, XmlElement parentElement) {
            // Get all columns object with given parent
            var dt = new DataTable();
            using (var da = new SqlDataAdapter(Resources.Commands.GetColumns, connectionString)) {
                da.SelectCommand.Parameters.Add("@object_id", SqlDbType.Int).Value = objectId;
                da.Fill(dt);
            }

            // Process all columns
            foreach (DataRow row in dt.Rows) {
                Trace.WriteLine(string.Format("{0} {1}", row["name"], row["type"]));

                // Create object element
                var e = parentElement.AppendChild(parentElement.OwnerDocument.CreateElement("column")) as XmlElement;
                foreach (DataColumn col in dt.Columns) {
                    var value = row.ToXmlString(col);
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    if (col.ColumnName.IndexOf(':') == -1) {
                        // Plain attribute
                        e.SetAttribute(col.ColumnName, value);
                    }
                    else {
                        // Nested element/attribute
                        var names = col.ColumnName.Split(':');
                        var se = (e.SelectSingleNode(names[0]) ?? e.AppendChild(e.OwnerDocument.CreateElement(names[0]))) as XmlElement;
                        se.SetAttribute(names[1], value);
                    }
                }
            }
        }

    }
}
