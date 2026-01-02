using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace RainbowMage.OverlayPlugin
{
    /// <summary>
    /// XmlSerializer でシリアライズ可能な IOverlayConfig のコレクション。
    /// </summary>
    [Serializable]
    public class OverlayConfigList<T> : Collection<T>, IXmlSerializable
    {
        public int MissingTypes { get; private set; }

        [NonSerialized]
        private ILogger _logger;

        public OverlayConfigList(ILogger logger)
        {
            _logger = logger;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            MissingTypes = 0;

            if (reader.IsEmptyElement)
            {
                return;
            }

            reader.ReadToDescendant("Overlay");
            do
            {
                string typeName = reader.GetAttribute("Type");

                reader.Read();

                var type = GetType(typeName);

                if (type != null)
                {
                    try
                    {
                        var serializer = new XmlSerializer(type);
                        var config = (T)serializer.Deserialize(reader);
                        this.Add(config);
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e);
                        _logger.Log(LogLevel.Error, e.ToString());
                    }
                }
                else
                {
                    reader.Skip();
                    MissingTypes++;
                }
            }
            while (reader.ReadToNextSibling("Overlay"));

            reader.ReadEndElement();
        }

        private Type GetType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var config in this)
            {
                writer.WriteStartElement("Overlay");
                writer.WriteAttributeString("Type", config.GetType().FullName);
                var serializer = new XmlSerializer(config.GetType());
                serializer.Serialize(writer, config);
                writer.WriteEndElement();
            }
        }
    }
}
