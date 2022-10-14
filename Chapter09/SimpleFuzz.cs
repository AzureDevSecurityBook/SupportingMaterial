using System.Text.Json;

var data = "98722, 2022-03-16T15:50-06:00, PharmaA, 4, These are the last batches of PharmaA";
var jsonData = @"{
    ""centerId"" : ""98722"",
    ""date_time"" : ""2022-03-16T15:50-06:00"",
 	""vaccination_type"" : ""PharmaA"",
 	""open_spots"" : ""4"",
 	""comment"" : ""These are the last batches of PharmaA""
}";

var doc = JsonDocument.Parse(jsonData);

var fuzz = new SimpleFuzz(threshold: 7);

for (int i = 0; i < 5; i++)
{
    byte[] fuzzedJsonResult = fuzz.Fuzz(doc);
    if (fuzzedJsonResult.Length > 0)
    {
        string res = System.Text.Encoding.UTF8.GetString(fuzzedJsonResult);
        Console.WriteLine(res);
    } else
    {
        Console.WriteLine("Not Fuzzed");
    }
}
/*
byte[] fuzzedResult = fuzz.Fuzz(data);
if (fuzzedResult.Length > 0) {
    string res = System.Text.Encoding.UTF8.GetString(fuzzedResult);
    Console.WriteLine(res);
}
*/

public class SimpleFuzz
{
    public SimpleFuzz(int threshold=5, int? seed = null) {
        _rnd = seed is null ? new Random() : new Random((int)seed);
        _threshold = threshold;
    }

    private readonly Random _rnd;
    private readonly int?   _threshold;

    public byte[] Fuzz(JsonDocument doc)
    {
        byte[] fuzzResult = new byte[]{ };
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) {
            writer.WriteStartObject();
            foreach (var elem in doc.RootElement.EnumerateObject())
            {
                writer.WritePropertyName(elem.Name);
                string f = elem.Value.ToString();
                byte[] fuzzed = Fuzz(f);
                if (fuzzed.Length > 0)
                {
                    writer.WriteStringValue(System.Text.Encoding.UTF8.GetString(fuzzed));
                } else
                {
                    writer.WriteStringValue(elem.Value.GetRawText().ToString().Trim('"'));
                }
            }
            writer.WriteEndObject();
            writer.Flush();

            fuzzResult = stream.GetBuffer();
        }

        return fuzzResult;
    }

    public byte[] Fuzz(string input) {
        return Fuzz(System.Text.Encoding.UTF8.GetBytes(input));
    }
        
    public byte[] Fuzz(byte[] input) {

        if (input.Length == 0) return new byte[] { };
        if (_rnd.Next(0, 100) > _threshold) return new byte[] { };

        var data = new Span<byte>(input);

        var mutationCount = _rnd.Next(1, 5);
        for (int i = 0; i < mutationCount; i++) {

            if (input.Length == 0) break;

            var whichMutation = _rnd.Next(0, 7);

            int lo = _rnd.Next(0, input.Length);
            int range = _rnd.Next(1, 1+ input.Length / 10);
            if (lo + range >= input.Length) range = input.Length - lo;

            switch (whichMutation)
            {
                case 0: // set all upper bits to 1
                    for (int j = lo; j < lo + range; j++)
                        input[j] |= 0x80;
                    break;

                case 1: // set all upper bits to 0
                    for (int j = lo; j < lo + range; j++)
                        input[j] &= 0x7F;
                    break;

                case 2: // set one char to a random value
                    input[lo] = (byte)_rnd.Next(0, 256);
                    break;

                case 3: // insert interesting numbers
                    byte[] interesting = new byte[] { 0, 1, 8, 7, 9, 16, 15, 17, 63, 64, 127, 128, 255 };
                    input[lo] = interesting[_rnd.Next(0,interesting.Length)];
                    break;

                case 4: // swap bytes
                    for (int j = lo; j < lo + range; j++) {
                        if (j + 1 < input.Length)
                            (input[j + 1], input[j]) = (input[j], input[j + 1]);
                    }
                    break;

                case 5: // remove sections of the data
                    input = _rnd.Next(100) > 50 ? input[..lo] : input[(lo + range)..];
                    break;

                case 6: // add interesting pathname/filename characters
                    var fname = new string[] { "\\", "/", ":", ".."};

                    int which = _rnd.Next(fname.Length);
                    for (int j = 0; j < fname[which].Length; j++)
                        if (lo+j < input.Length)
                            input[lo+j] = (byte)fname[which][j];
           
                    break;

                default:
                    break;
            }
        }

        return input.ToArray();
    }
}
