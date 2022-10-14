public byte[] Fuzz(JsonDocument doc) {
	byte[] fuzzResult = Array.Empty<byte>();
	using var stream = new MemoryStream();
	using (var writer = new Utf8JsonWriter(stream)) {
		writer.WriteStartObject();
		foreach (var elem in doc.RootElement.EnumerateObject()) {
			writer.WritePropertyName(elem.Name);
			byte[] fuzzed = Fuzz(elem.Value.ToString());
			writer.WriteStringValue(fuzzed.Length > 0
				? System.Text.Encoding.UTF8.GetString(fuzzed)
				: elem.Value.GetRawText().Trim('"'));
		}

		writer.WriteEndObject();
		writer.Flush();
		fuzzResult = stream.GetBuffer();
	}

	return fuzzResult;
}