using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace RainbowMage.OverlayPlugin;

/// <summary>
///     XmlSerializer でシリアライズ可能な IOverlayConfig のコレクション。
/// </summary>
[Serializable]
public class OverlayConfigList<T>(ILogger logger) : Collection<T>, IXmlSerializable where T : notnull {
	public int MissingTypes { get; private set; }

	[NonSerialized] private ILogger _logger = logger;

	public XmlSchema? GetSchema() => null;

	public void ReadXml(XmlReader reader) {
		MissingTypes = 0;
		if (reader.IsEmptyElement) return;
		reader.ReadToDescendant("Overlay");
		do {
			var typeName = reader.GetAttribute("Type");
			reader.Read();
			var type = GetType(typeName);
			if (type != null) {
				try {
					var serializer = new XmlSerializer(type);
					var config = (T?)serializer.Deserialize(reader);
					if (config == null) throw new Exception("config is null");
					Add(config);
				} catch (Exception e) {
					Trace.WriteLine(e);
					_logger.Log(LogLevel.Error, e.ToString());
				}
			} else {
				reader.Skip();
				MissingTypes++;
			}
		} while (reader.ReadToNextSibling("Overlay"));

		reader.ReadEndElement();
	}

	private static Type? GetType(string? fullName) => fullName == null ? null : AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetType(fullName, false)).OfType<Type>().FirstOrDefault();

	public void WriteXml(XmlWriter writer) {
		foreach (var config in this) {
			writer.WriteStartElement("Overlay");
			writer.WriteAttributeString("Type", config.GetType().FullName);
			var serializer = new XmlSerializer(config.GetType());
			serializer.Serialize(writer, config);
			writer.WriteEndElement();
		}
	}
}