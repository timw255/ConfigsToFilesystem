using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.Data.Configuration;
using Telerik.Sitefinity.Web.Configuration;

namespace SitefinityWebApp.Utilities
{
    public partial class ConfigsToFilesystem : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string response = String.Empty;
            string configurationPath = HttpContext.Current.Server.MapPath("~/App_Data/Sitefinity/Configuration/");
            string backupPath = configurationPath + "_backup-48A004E0-DD62-48BE-ABCB-CC44F79A6126/";
            string mergedPath = configurationPath + "_merged-48A004E0-DD62-48BE-ABCB-CC44F79A6126/";

            Directory.CreateDirectory(backupPath);
            Directory.CreateDirectory(mergedPath);

            string[] fileList = System.IO.Directory.GetFiles(configurationPath);

            foreach (string file in fileList)
            {
                string fileName = Path.GetFileName(file);
                string copyTo = backupPath + fileName;

                File.Copy(file, copyTo);
            }

            ConfigManager manager = Config.GetManager();
            DataConfig section = manager.GetSection<DataConfig>();

            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = section.ConnectionStrings["Sitefinity"].ConnectionString;
                conn.Open();

                using (var cmd = new SqlCommand("SELECT * FROM sf_xml_config_items", conn))
                {
                    XmlWriterSettings settings = new XmlWriterSettings()
                    {
                        OmitXmlDeclaration = false,
                        ConformanceLevel = ConformanceLevel.Document,
                        Encoding = UTF8Encoding.UTF8,
                        Indent = true
                    };

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();

                    da.Fill(dt);

                    foreach (DataRow dr in dt.Rows)
                    {
                        string path = configurationPath + dr["path"].ToString();

                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }

                        XmlDocument document = new XmlDocument();

                        document.LoadXml(dr["dta"].ToString());

                        using (XmlWriter writer = XmlWriter.Create(path, settings))
                        {
                            document.Save(writer);
                        }
                    }
                }
            }

            string[] oldFileList = System.IO.Directory.GetFiles(backupPath);

            foreach (string file in oldFileList)
            {
                string fileName = Path.GetFileName(file);
                string newFile = configurationPath + fileName;
                string oldFile = file;
                string mergedFile = mergedPath + fileName;

                MergeConfigFiles(newFile, oldFile, mergedFile);
            }

            Response.Write(response);
            Response.End();
        }

        public static void MergeConfigFiles(string sourceConfigPath, string destConfigPath, string outputConfigPath)
        {
            try
            {
                XDocument sourceDoc = XDocument.Load(sourceConfigPath);
                XDocument destDoc = XDocument.Load(destConfigPath);

                MergeConfigFiles(sourceDoc, destDoc);

                destDoc.Save(outputConfigPath);
            }
            catch (Exception e)
            {
                HttpContext.Current.Response.Write("<strong>" + Path.GetFileName(sourceConfigPath) + " was not merged. Here's why:</strong><br/><p>" + e + "</p><br>");
            }
        }

        public static void MergeConfigFiles(XDocument sourceDoc, XDocument destDoc)
        {
            XElement sourceRoot = sourceDoc.Root;
            XElement destRoot = destDoc.Root;

            CopyAllXElementAttributes(sourceRoot, destRoot);
            MergeXElements(sourceRoot, destRoot);
        }

        private static void MergeXElements(XElement sourceElement, XElement destElement)
        {
            foreach (var element in sourceElement.Elements())
            {
                string xPath = element.GetAbsoluteXPath();
                var existingElement = destElement.XPathSelectElement(xPath);

                if (existingElement == null)
                {
                    existingElement = new XElement(element);

                    destElement.Add(existingElement);
                }
                else
                {
                    CopyAllXElementAttributes(element, existingElement);
                }

                MergeXElements(element, existingElement);
            }
        }

        private static void CopyAllXElementAttributes(XElement sourceElement, XElement destElement)
        {
            foreach (var sourceAttribute in sourceElement.Attributes())
            {
                var existingAttribute = destElement.GetAttributeByAttributeName(sourceAttribute.Name.LocalName);

                if (existingAttribute == null)
                {
                    XAttribute newAttribute = new XAttribute(sourceAttribute);

                    destElement.Add(newAttribute);
                }
                else
                {
                    existingAttribute.SetValue(sourceAttribute.Value);
                }
            }
        }
    }

    public static class XExtensions
    {
        /// <summary>
        /// Get the absolute XPath to a given XElement
        /// (e.g. "/people/person[6]/name[1]/last[1]").
        /// </summary>
        public static string GetAbsoluteXPath(this XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            Func<XElement, string> relativeXPath = e =>
            {

                int index = e.IndexPosition();
                string name = e.Name.LocalName;

                XAttribute nameAttribute = e.GetNameAttribute();
                XAttribute idAttribute = e.GetIdAttribute();

                if (nameAttribute != null)
                {
                    return string.Format("/{0}[@{1}='{2}']", name, nameAttribute.Name.LocalName, nameAttribute.Value);
                }
                else if (idAttribute != null)
                {
                    return string.Format("/{0}[@{1}='{2}']", name, idAttribute.Name.LocalName, idAttribute.Value);
                }
                else if (index == -1)
                {
                    return "/" + name;
                }
                else
                {
                    return string.Format("/{0}[{1}]", name, index.ToString());
                }
            };

            var ancestors = from e in element.Ancestors()
                            select relativeXPath(e);

            return string.Concat(ancestors.Reverse().ToArray()) + relativeXPath(element);
        }

        /// <summary>
        /// Get the index of the given XElement relative to its
        /// siblings with identical names. If the given element is
        /// the root, -1 is returned.
        /// </summary>
        /// <param name="element">
        /// The element to get the index of.
        /// </param>
        public static int IndexPosition(this XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            if (element.Parent == null)
            {
                return -1;
            }

            int i = 1; // Indexes for nodes start at 1, not 0

            foreach (var sibling in element.Parent.Elements(element.Name))
            {
                if (sibling == element)
                {
                    return i;
                }

                i++;
            }

            throw new InvalidOperationException("element has been removed from its parent.");
        }

        public static XAttribute GetNameAttribute(this XElement element)
        {
            foreach (XAttribute attribute in element.Attributes())
            {
                if (attribute.Name.LocalName.ToLower().StartsWith("name") || attribute.Name.LocalName.ToLower().EndsWith("name"))
                {
                    return attribute;
                }
            }

            return null;
        }

        public static XAttribute GetIdAttribute(this XElement element)
        {
            return GetAttributeByAttributeName(element, "id");
        }

        public static XAttribute GetAttributeByAttributeName(this XElement element, string attributeName)
        {
            foreach (XAttribute attribute in element.Attributes())
            {
                if (attribute.Name.LocalName.ToLower() == attributeName.ToLower())
                {
                    return attribute;
                }
            }

            return null;
        }
    }
}